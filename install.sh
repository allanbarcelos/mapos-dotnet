#!/usr/bin/env bash
# ==============================================================================
#  install.sh — Instalador de produção do netpos
#  ASP.NET Core + PostgreSQL  ·  Docker Swarm
#
#  Uso: sudo bash install.sh
#
#  COMPATÍVEL com servidores que já possuam outros stacks Docker Swarm
#  (ex: dotnet-angular-pgsql) — nunca remove regras UFW/fail2ban existentes.
#
#  O QUE ESTE SCRIPT FAZ:
#
#  FASE 1  — Configuração interativa (apenas o necessário)
#    - Modo de instalação: VPS+Cloudflare | VPS+Let's Encrypt | Local (intranet)
#    - Domínio (obrigatório no modo LE, opcional nos demais)
#    - E-mail do administrador
#    - SMTP (opcional)
#    - MercadoPago (opcional)
#    - Resumo e confirmação antes de tocar no sistema
#
#  FASE 2  — Pacotes do sistema (apenas o que estiver faltando)
#    curl, wget, ca-certificates, gnupg2, lsb-release, openssl, jq,
#    netcat-openbsd, ufw, fail2ban, apt-transport-https,
#    docker (via get.docker.com), docker-compose-plugin,
#    certbot (modo LE), dnsmasq (modo local com DNS)
#
#  FASE 3  — Docker Swarm (idempotente — ignorado se já ativo)
#
#  FASE 4  — Geração de credenciais (senhas aleatórias, nunca reutilizadas)
#    - Senha do PostgreSQL (32 chars)
#    - Senha do admin da aplicação
#
#  FASE 5  — Diretório /opt/netpos
#    data/postgres, logs/{app,nginx}, scripts, etc/nginx
#
#  FASE 6  — Docker Swarm secrets (encriptados em repouso no Raft)
#    Prefixo netpos_ em todos os secrets para coexistir com outros stacks.
#    Montados nos containers via source/target aliasing — app vê os nomes
#    originais em /run/secrets/ sem nenhuma alteração no código.
#
#  FASE 7  — Arquivos de configuração (nginx.conf + certificado LE)
#
#  FASE 8  — Stack file /opt/netpos/docker-compose.prod.yml
#    Modo Cloudflare : db + app (porta APP_PORT exposta ao Cloudflare)
#    Modo LE         : db + app (interno) + nginx (80 + 443)
#    Modo local      : db + app (interno) + nginx (80)
#
#  FASE 9  — Deploy do stack (docker stack deploy --prune)
#
#  FASE 10 — UFW firewall (ADIÇÃO de regras — nunca remove existentes)
#    SSH sempre permitido.
#    Modo Cloudflare: live fetch de IPs + fallback + DOCKER-USER iptables
#      (impede bypass do Docker via DNAT) + systemd unit de persistência.
#    Modo LE   : abre 80 + 443.
#    Modo local : abre 80 (+ 53 se dnsmasq).
#
#  FASE 11 — Fail2ban
#    Jails: sshd (24h ban), nginx-http-auth, nginx-limit-req,
#           nginx-botsearch, recidive (1 semana).
#
#  FASE 12 — Scripts de manutenção em /opt/netpos/scripts/
#    update-images.sh         — atualiza imagem + rolling update
#    update-cloudflare-ips.sh — atualiza UFW + DOCKER-USER (modo CF)
#    renew-certs.sh           — renova certificado LE
#
#  FASE 13 — Cron jobs em /etc/cron.d/netpos
#
#  FASE 14 — Aguardar serviços ficarem saudáveis
#
#  FASE 15 — Resumo completo com TODAS as credenciais e secrets gerados
# ==============================================================================
set -euo pipefail
IFS=$'\n\t'

export PATH="/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:${PATH:-}"

# ── Identidade do projeto (fixo) ───────────────────────────────────────────────
readonly APP_NAME="netpos"
readonly APP_DIR="/opt/netpos"
readonly STACK_NAME="netpos"
readonly APP_IMAGE="ghcr.io/allanbarcelos/netpos:latest"

# ── Cores ──────────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GRN='\033[0;32m'; YLW='\033[1;33m'
CYN='\033[0;36m'; BLD='\033[1m'; DIM='\033[2m'; NC='\033[0m'

# ── Helpers ────────────────────────────────────────────────────────────────────
info()  { echo -e "${CYN}[INFO]${NC}  $*"; }
ok()    { echo -e "${GRN}[OK]${NC}    $*"; }
warn()  { echo -e "${YLW}[AVISO]${NC} $*"; }
die()   { echo -e "${RED}[ERRO]${NC}  $*" >&2; exit 1; }
phase() { echo -e "\n${BLD}${CYN}━━━  $*  ━━━${NC}"; }
sep()   { echo -e "${DIM}──────────────────────────────────────────────────────${NC}"; }

ask() {
  # ask "Prompt" "default" VAR  — default vazio = obrigatório
  local prompt="$1" default="${2:-}" var_name="$3" value
  if [[ -n "$default" ]]; then
    read -rp "$(echo -e "  ${BLD}${prompt}${NC} ${DIM}[${default}]${NC}: ")" value </dev/tty
    value="${value:-$default}"
  else
    while true; do
      read -rp "$(echo -e "  ${BLD}${prompt}${NC}: ")" value </dev/tty
      [[ -n "$value" ]] && break
      echo -e "  ${RED}Obrigatório — informe um valor.${NC}"
    done
  fi
  printf -v "$var_name" '%s' "$value"
}

ask_optional() {
  local prompt="$1" var_name="$2" value
  read -rp "$(echo -e "  ${BLD}${prompt}${NC} ${DIM}(opcional — Enter para pular)${NC}: ")" value </dev/tty
  printf -v "$var_name" '%s' "${value:-}"
}

ask_yn() {
  local prompt="$1" var_name="$2" default="${3:-n}" value hint
  [[ "$default" == "s" ]] && hint="S/n" || hint="s/N"
  read -rp "$(echo -e "  ${BLD}${prompt}${NC} ${DIM}[${hint}]${NC}: ")" value </dev/tty
  value="${value:-$default}"
  [[ "$value" =~ ^[SsYy]$ ]] && printf -v "$var_name" 'y' || printf -v "$var_name" 'n'
}

gen_pass() {
  local len="${1:-32}"
  openssl rand -base64 48 | tr -d '/+=' | head -c "$len"
}

require_root() { [[ $EUID -eq 0 ]] || die "Execute como root: sudo bash install.sh"; }

detect_os() {
  [[ -f /etc/os-release ]] || die "Sistema operacional não identificado."
  # shellcheck source=/dev/null
  . /etc/os-release
  case "${ID:-}" in
    ubuntu|debian) ;;
    *) warn "Sistema '${ID:-desconhecido}' não testado — continuando mesmo assim." ;;
  esac
}

swarm_secret_exists() { docker secret inspect "$1" &>/dev/null; }

create_swarm_secret() {
  local name="$1" value="$2"
  if swarm_secret_exists "$name"; then
    warn "Secret '${name}' já existe — ignorando"
  else
    printf '%s' "$value" | docker secret create "$name" - >/dev/null
    ok "Secret criado: ${name}"
  fi
}

# ── Guards ─────────────────────────────────────────────────────────────────────
require_root
detect_os

# ── Banner ─────────────────────────────────────────────────────────────────────
clear
echo -e "${RED}${BLD}"
cat <<'WARN'
 ╔══════════════════════════════════════════════════════════════════════════════╗
 ║  ATENÇÃO — LEIA ANTES DE EXECUTAR                                            ║
 ║                                                                              ║
 ║  Este script instala o netpos em produção via Docker Swarm.                  ║
 ║  Pode ser executado em um servidor que já possua outros stacks Swarm         ║
 ║  sem interferir neles (UFW e fail2ban são apenas complementados).            ║
 ║                                                                              ║
 ║  Para remover a instalação use:  sudo bash uninstall.sh                      ║
 ╚══════════════════════════════════════════════════════════════════════════════╝
WARN
echo -e "${NC}"

read -rp "$(echo -e "  ${BLD}Confirmo que li o aviso acima${NC} ${DIM}[S/n]${NC}: ")" _WARN_OK </dev/tty
[[ "${_WARN_OK:-s}" =~ ^[SsYy]$ ]] || { echo "Instalação cancelada."; exit 0; }

echo -e "${CYN}${BLD}"
cat <<'LOGO'

  ███╗   ██╗███████╗████████╗██████╗  ██████╗ ███████╗
  ████╗  ██║██╔════╝╚══██╔══╝██╔══██╗██╔═══██╗██╔════╝
  ██╔██╗ ██║█████╗     ██║   ██████╔╝██║   ██║███████╗
  ██║╚██╗██║██╔══╝     ██║   ██╔═══╝ ██║   ██║╚════██║
  ██║ ╚████║███████╗   ██║   ██║     ╚██████╔╝███████║
  ╚═╝  ╚═══╝╚══════╝   ╚═╝   ╚═╝      ╚═════╝ ╚══════╝

              Instalador de Produção  ·  Docker Swarm
LOGO
echo -e "${NC}"
sep

# ==============================================================================
# FASE 1 — CONFIGURAÇÃO
# ==============================================================================
phase "FASE 1 — Configuração"

echo ""

# ── Modo de instalação ────────────────────────────────────────────────────────
echo -e "  ${BLD}Modo de instalação${NC}"
echo ""
echo -e "  ${BLD}1)${NC} ${CYN}VPS + Cloudflare${NC}  ${DIM}(recomendado)${NC}"
echo -e "     Cloudflare termina o HTTPS; servidor responde em HTTP na porta configurada"
echo -e "     UFW bloqueia acesso direto — apenas IPs do Cloudflare são permitidos"
echo ""
echo -e "  ${BLD}2)${NC} ${CYN}VPS + Let's Encrypt${NC}"
echo -e "     TLS direto no servidor via certbot (portas 80 + 443)"
echo -e "     O domínio deve apontar para este servidor antes de continuar"
echo ""
echo -e "  ${BLD}3)${NC} ${CYN}Local (intranet)${NC}"
echo -e "     Nginx na porta 80, sem TLS — rede interna"
echo ""

while true; do
  read -rp "$(echo -e "  ${BLD}Modo${NC} ${DIM}[1/2/3]${NC}: ")" _MODE_CHOICE </dev/tty
  _MODE_CHOICE="${_MODE_CHOICE:-1}"
  [[ "$_MODE_CHOICE" =~ ^[123]$ ]] && break
  echo -e "  ${RED}Digite 1, 2 ou 3.${NC}"
done

APP_PORT="8080"
DOMAIN=""
CERTBOT_EMAIL=""
USE_LETSENCRYPT="n"
INSTALL_LOCAL="n"
SRV_HOSTNAME=""
SETUP_DNS=false

case "$_MODE_CHOICE" in
  1)
    INSTALL_MODE="vps_cloudflare"
    echo ""
    ask "Porta da aplicação (Cloudflare → este servidor)" "8080" APP_PORT
    [[ "$APP_PORT" =~ ^[0-9]+$ && "$APP_PORT" -ge 1 && "$APP_PORT" -le 65535 ]] \
      || die "Porta inválida: ${APP_PORT}"
    echo ""
    ask_optional "Domínio Cloudflare (usado para exibição — ex: netpos.empresa.com.br)" DOMAIN
    echo ""
    echo -e "  ${YLW}Lembre-se de criar uma Origin Rule no Cloudflare:${NC}"
    echo -e "  ${YLW}  Hostname = seu domínio  →  Destination port = ${APP_PORT}${NC}"
    ;;
  2)
    INSTALL_MODE="vps_letsencrypt"
    USE_LETSENCRYPT="y"
    echo ""
    ask "Domínio (ex: netpos.empresa.com.br)" "" DOMAIN
    echo ""
    warn "O DNS de '${DOMAIN}' deve apontar para este servidor e a porta 80 deve estar acessível."
    echo ""
    ask "E-mail para notificações Let's Encrypt" "" CERTBOT_EMAIL
    ;;
  3)
    INSTALL_MODE="local"
    INSTALL_LOCAL="y"
    echo ""
    info "Modo: servidor local (intranet)"

    sep
    echo -e "  ${BLD}Hostname do servidor${NC}"
    sep
    _current_hostname=$(hostname)
    echo -e "  Hostname atual: ${BLD}${CYN}${_current_hostname}${NC}"
    echo ""
    ask_yn "Deseja alterar o hostname?" _CHANGE_HOST "n"
    if [[ "$_CHANGE_HOST" == "y" ]]; then
      ask "Novo hostname (ex: netpos-server)" "" SRV_HOSTNAME
      hostnamectl set-hostname "$SRV_HOSTNAME"
      if grep -q "127.0.1.1" /etc/hosts; then
        sed -i "s/^127\.0\.1\.1.*/127.0.1.1\t${SRV_HOSTNAME}/" /etc/hosts
      else
        printf '127.0.1.1\t%s\n' "$SRV_HOSTNAME" >> /etc/hosts
      fi
      ok "Hostname alterado para: ${SRV_HOSTNAME}"
    else
      SRV_HOSTNAME="$_current_hostname"
    fi

    echo ""
    sep
    echo -e "  ${BLD}DNS na rede local${NC}"
    sep
    echo ""
    ask_yn "Já existe um servidor DNS na rede (roteador, Pi-hole, etc.)?" _HAS_DNS "n"
    if [[ "$_HAS_DNS" == "y" ]]; then
      info "Adicione manualmente uma entrada A: '${SRV_HOSTNAME}' → IP deste servidor."
      SETUP_DNS=false
    else
      echo ""
      echo -e "  Instalando ${BLD}dnsmasq${NC} neste servidor, basta apontar o roteador para ele"
      echo -e "  e toda a rede resolverá ${BLD}http://${SRV_HOSTNAME}${NC} automaticamente."
      echo ""
      ask_yn "Instalar dnsmasq e tornar este servidor um DNS local?" _INSTALL_DNS "n"
      if [[ "$_INSTALL_DNS" == "y" ]]; then
        SETUP_DNS=true
      else
        SETUP_DNS=false
        _lip=$(hostname -I | awk '{print $1}')
        info "Acesso pelo IP: http://${_lip}"
      fi
    fi
    ;;
esac

# ── E-mail do administrador ───────────────────────────────────────────────────
echo ""
sep
ask "E-mail do administrador (conta criada no primeiro acesso)" "" ADMIN_EMAIL

# ── SMTP (opcional) ───────────────────────────────────────────────────────────
echo ""
sep
echo -e "  ${BLD}Configuração de e-mail (SMTP) — opcional${NC}"
sep
echo ""
SMTP_HOST=""; SMTP_PORT="587"; SMTP_SSL="true"
SMTP_USERNAME=""; SMTP_PASSWORD=""; SMTP_FROM=""

ask_yn "Configurar envio de e-mails?" _SETUP_SMTP "n"
if [[ "$_SETUP_SMTP" == "y" ]]; then
  ask "Servidor SMTP" "smtp.gmail.com" SMTP_HOST
  ask "Porta SMTP" "587" SMTP_PORT
  ask "Usar SSL/TLS? (true/false)" "true" SMTP_SSL
  ask "Usuário SMTP (e-mail)" "" SMTP_USERNAME
  ask "Senha / App Password SMTP" "" SMTP_PASSWORD
  ask "E-mail remetente (from)" "$SMTP_USERNAME" SMTP_FROM
  ok "SMTP configurado."
fi

# ── MercadoPago (opcional) ────────────────────────────────────────────────────
echo ""
sep
echo -e "  ${BLD}MercadoPago — opcional${NC}"
sep
echo ""
MP_ACCESS_TOKEN=""; MP_BOLETO_EXPIRATION="P3D"

ask_yn "Configurar integração MercadoPago?" _SETUP_MP "n"
if [[ "$_SETUP_MP" == "y" ]]; then
  ask "Access Token do MercadoPago" "" MP_ACCESS_TOKEN
  ask "Validade do boleto (ISO 8601)" "P3D" MP_BOLETO_EXPIRATION
  ok "MercadoPago configurado."
fi

# ── Resumo + confirmação ──────────────────────────────────────────────────────
echo ""
sep
echo -e "  ${BLD}Resumo — o que será instalado${NC}"
sep
echo -e "  Projeto    : ${CYN}${APP_NAME}${NC}  →  ${CYN}${APP_DIR}${NC}"
echo -e "  Stack      : ${CYN}${STACK_NAME}${NC}"
echo -e "  Imagem     : ${CYN}${APP_IMAGE}${NC}"
case "$INSTALL_MODE" in
  vps_cloudflare)
    echo -e "  Modo       : ${CYN}VPS + Cloudflare${NC}  (porta ${APP_PORT})"
    [[ -n "$DOMAIN" ]] && echo -e "  Domínio    : ${CYN}${DOMAIN}${NC}"
    ;;
  vps_letsencrypt)
    echo -e "  Modo       : ${CYN}VPS + Let's Encrypt${NC}"
    echo -e "  Domínio    : ${CYN}${DOMAIN}${NC}"
    ;;
  local)
    echo -e "  Modo       : ${CYN}Local / intranet${NC}  (${SRV_HOSTNAME})"
    $SETUP_DNS && echo -e "  DNS local  : ${CYN}dnsmasq${NC}"
    ;;
esac
echo -e "  Admin      : ${CYN}${ADMIN_EMAIL}${NC}"
[[ -n "$SMTP_HOST" ]] && echo -e "  SMTP       : ${CYN}${SMTP_HOST}:${SMTP_PORT}${NC}"
[[ -n "$MP_ACCESS_TOKEN" ]] && echo -e "  MercadoPago: ${CYN}configurado${NC}"
sep
echo ""
read -rp "$(echo -e "  ${BLD}Prosseguir com a instalação?${NC} ${DIM}[S/n]${NC}: ")" _CONFIRM </dev/tty
[[ "${_CONFIRM:-s}" =~ ^[SsYy]$ ]] || { echo "Abortado."; exit 0; }

# ==============================================================================
# FASE 2 — PACOTES DO SISTEMA
# ==============================================================================
phase "FASE 2 — Pacotes do sistema"

export DEBIAN_FRONTEND=noninteractive
apt-get update -qq

install_pkg() {
  local pkg="$1"
  if dpkg -s "$pkg" &>/dev/null 2>&1; then
    info "${pkg} já instalado"
  else
    info "Instalando ${pkg}..."
    apt-get install -y -qq "$pkg" >/dev/null
    ok "${pkg} instalado"
  fi
}

for pkg in \
  curl wget ca-certificates gnupg2 lsb-release \
  openssl jq netcat-openbsd \
  ufw fail2ban \
  apt-transport-https; do
  install_pkg "$pkg"
done

if command -v docker &>/dev/null; then
  info "Docker já instalado: $(docker --version)"
else
  info "Instalando Docker (script oficial)..."
  curl -fsSL https://get.docker.com | sh >/dev/null 2>&1
  systemctl enable --now docker >/dev/null 2>&1
  ok "Docker instalado: $(docker --version)"
fi

if ! docker compose version &>/dev/null 2>&1; then
  info "Instalando docker-compose-plugin..."
  apt-get install -y -qq docker-compose-plugin >/dev/null
fi

[[ "$USE_LETSENCRYPT" == "y" ]] && install_pkg "certbot"
$SETUP_DNS && install_pkg "dnsmasq"

ok "Todos os pacotes prontos"

# ==============================================================================
# FASE 3 — DOCKER SWARM
# ==============================================================================
phase "FASE 3 — Docker Swarm"

if docker info --format '{{.Swarm.LocalNodeState}}' 2>/dev/null | grep -q "^active$"; then
  info "Swarm já ativo (node: $(docker info --format '{{.Swarm.NodeID}}'))"
else
  _HOST_IP=$(hostname -I | awk '{print $1}')
  info "Inicializando Docker Swarm em ${_HOST_IP}..."
  docker swarm init --advertise-addr "$_HOST_IP" >/dev/null
  ok "Swarm inicializado"
fi

# ==============================================================================
# FASE 4 — GERAR CREDENCIAIS
# ==============================================================================
phase "FASE 4 — Gerando credenciais"

POSTGRES_DB="netpos"
POSTGRES_USER="netpos_user"
POSTGRES_PASSWORD="$(gen_pass 32)"
ADMIN_PASSWORD="$(gen_pass 12)Aa1!"

ok "Credenciais geradas"

# ==============================================================================
# FASE 5 — DIRETÓRIO DA APLICAÇÃO
# ==============================================================================
phase "FASE 5 — Diretório da aplicação"

[[ -d "$APP_DIR" ]] && info "Diretório ${APP_DIR} já existe — reutilizando"

mkdir -p "${APP_DIR}"/{data/postgres,logs/{app,nginx},scripts,etc/nginx}
chmod 750 "${APP_DIR}"
chmod 700 "${APP_DIR}/data" "${APP_DIR}/etc"

ok "Diretório: ${APP_DIR}"

# ==============================================================================
# FASE 6 — DOCKER SWARM SECRETS
# ==============================================================================
phase "FASE 6 — Docker Swarm secrets"

# Todos prefixados com netpos_ para não colidir com outros stacks.
# Montados nos containers via source/target aliasing — o app vê
# os nomes originais em /run/secrets/ sem alteração.

DB_CONN_STR="Host=db;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"

create_swarm_secret "netpos_db_password"          "$POSTGRES_PASSWORD"
create_swarm_secret "netpos_db_conn"              "$DB_CONN_STR"
create_swarm_secret "netpos_admin_email"          "$ADMIN_EMAIL"
create_swarm_secret "netpos_admin_password"       "$ADMIN_PASSWORD"
create_swarm_secret "netpos_smtp_host"            "$SMTP_HOST"
create_swarm_secret "netpos_smtp_port"            "$SMTP_PORT"
create_swarm_secret "netpos_smtp_ssl"             "$SMTP_SSL"
create_swarm_secret "netpos_smtp_username"        "$SMTP_USERNAME"
create_swarm_secret "netpos_smtp_password"        "$SMTP_PASSWORD"
create_swarm_secret "netpos_smtp_from"            "$SMTP_FROM"
create_swarm_secret "netpos_mp_access_token"      "$MP_ACCESS_TOKEN"
create_swarm_secret "netpos_mp_boleto_expiration" "$MP_BOLETO_EXPIRATION"

# ==============================================================================
# FASE 7 — CONFIGURAÇÕES (nginx + certbot)
# ==============================================================================
phase "FASE 7 — Configurações"

if [[ "$USE_LETSENCRYPT" == "y" ]]; then
  cat > "${APP_DIR}/etc/nginx/nginx.conf" <<NGINXLE
upstream netpos_app {
    server netpos_app:8080;
}

server {
    listen 80;
    server_name ${DOMAIN} www.${DOMAIN};
    return 301 https://\$host\$request_uri;
}

server {
    listen 443 ssl;
    server_name ${DOMAIN} www.${DOMAIN};

    ssl_certificate     /etc/letsencrypt/live/${DOMAIN}/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/${DOMAIN}/privkey.pem;
    ssl_protocols       TLSv1.2 TLSv1.3;
    ssl_ciphers         HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers off;

    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

    client_max_body_size 20M;
    resolver 127.0.0.11 valid=10s ipv6=off;

    location / {
        proxy_pass http://netpos_app;
        proxy_set_header Host              \$host;
        proxy_set_header X-Real-IP         \$remote_addr;
        proxy_set_header X-Forwarded-For   \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_read_timeout 120s;
    }
}
NGINXLE
  chmod 644 "${APP_DIR}/etc/nginx/nginx.conf"
  ok "nginx.conf gerado (Let's Encrypt)"

  info "Obtendo certificado TLS para ${DOMAIN}..."
  certbot certonly --standalone \
    -d "$DOMAIN" \
    --non-interactive --agree-tos \
    -m "$CERTBOT_EMAIL" --quiet \
    || die "Falha ao obter certificado. Verifique se '${DOMAIN}' aponta para este servidor."
  ok "Certificado obtido em /etc/letsencrypt/live/${DOMAIN}/"

elif [[ "$INSTALL_LOCAL" == "y" ]]; then
  cat > "${APP_DIR}/etc/nginx/nginx.conf" <<NGINXLOCAL
upstream netpos_app {
    server netpos_app:8080;
}

server {
    listen 80;
    server_name ${SRV_HOSTNAME} ${SRV_HOSTNAME}.local _;

    client_max_body_size 20M;
    resolver 127.0.0.11 valid=10s ipv6=off;

    location / {
        proxy_pass http://netpos_app;
        proxy_set_header Host              \$host;
        proxy_set_header X-Real-IP         \$remote_addr;
        proxy_set_header X-Forwarded-For   \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_read_timeout 120s;
    }
}
NGINXLOCAL
  chmod 644 "${APP_DIR}/etc/nginx/nginx.conf"
  ok "nginx.conf gerado (intranet)"
fi

# ==============================================================================
# FASE 8 — STACK FILE
# ==============================================================================
phase "FASE 8 — Arquivo de stack"

STACK_FILE="${APP_DIR}/docker-compose.prod.yml"

case "$INSTALL_MODE" in
  vps_cloudflare)
    APP_PORTS_YAML="    ports:
      - \"${APP_PORT}:8080\""
    NGINX_SVC_YAML=""
    ;;
  vps_letsencrypt)
    APP_PORTS_YAML=""
    NGINX_SVC_YAML="
  nginx:
    image: nginx:1.27-alpine
    ports:
      - \"80:80\"
      - \"443:443\"
    volumes:
      - /etc/letsencrypt:/etc/letsencrypt:ro
      - ${APP_DIR}/etc/nginx/nginx.conf:/etc/nginx/conf.d/default.conf:ro
      - ${APP_DIR}/logs/nginx:/var/log/nginx
    networks:
      - internal
    deploy:
      restart_policy:
        condition: on-failure
        delay: 5s
        max_attempts: 5"
    ;;
  local)
    APP_PORTS_YAML=""
    NGINX_SVC_YAML="
  nginx:
    image: nginx:1.27-alpine
    ports:
      - \"80:80\"
    volumes:
      - ${APP_DIR}/etc/nginx/nginx.conf:/etc/nginx/conf.d/default.conf:ro
      - ${APP_DIR}/logs/nginx:/var/log/nginx
    networks:
      - internal
    deploy:
      restart_policy:
        condition: on-failure
        delay: 5s
        max_attempts: 5"
    ;;
esac

cat > "$STACK_FILE" <<STACK
# =============================================================================
# Docker Swarm Stack — netpos
# Gerado: $(date -u '+%Y-%m-%d %H:%M:%S UTC')
#
# Secrets prefixados com netpos_ para coexistir com outros stacks.
# Montados via source/target aliasing — app vê nomes originais sem prefixo.
# =============================================================================
version: "3.8"

services:

  # ── PostgreSQL ──────────────────────────────────────────────────────────────
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: "${POSTGRES_DB}"
      POSTGRES_USER: "${POSTGRES_USER}"
      POSTGRES_PASSWORD_FILE: /run/secrets/postgres_password
    secrets:
      - source: netpos_db_password
        target: postgres_password
    volumes:
      - ${APP_DIR}/data/postgres:/var/lib/postgresql/data
    networks:
      - internal
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s
    deploy:
      placement:
        constraints:
          - node.role == manager
      restart_policy:
        condition: on-failure
        delay: 5s
        max_attempts: 5

  # ── Aplicação netpos ────────────────────────────────────────────────────────
  app:
    image: ${APP_IMAGE}
${APP_PORTS_YAML}
    environment:
      ASPNETCORE_ENVIRONMENT: Production
    secrets:
      - source: netpos_db_conn
        target: postgres_connection
      - source: netpos_admin_email
        target: admin_email
      - source: netpos_admin_password
        target: admin_password
      - source: netpos_smtp_host
        target: smtp_host
      - source: netpos_smtp_port
        target: smtp_port
      - source: netpos_smtp_ssl
        target: smtp_ssl
      - source: netpos_smtp_username
        target: smtp_username
      - source: netpos_smtp_password
        target: smtp_password
      - source: netpos_smtp_from
        target: smtp_from
      - source: netpos_mp_access_token
        target: mp_access_token
      - source: netpos_mp_boleto_expiration
        target: mp_boleto_expiration
    volumes:
      - ${APP_DIR}/logs/app:/app/logs
    networks:
      - internal
    healthcheck:
      test: ["CMD-SHELL", "curl -sf http://localhost:8080/health || exit 1"]
      interval: 15s
      timeout: 5s
      retries: 5
      start_period: 60s
    deploy:
      restart_policy:
        condition: on-failure
        delay: 10s
        max_attempts: 10
${NGINX_SVC_YAML}

networks:
  internal:
    driver: overlay
    attachable: false

secrets:
  netpos_db_password:
    external: true
  netpos_db_conn:
    external: true
  netpos_admin_email:
    external: true
  netpos_admin_password:
    external: true
  netpos_smtp_host:
    external: true
  netpos_smtp_port:
    external: true
  netpos_smtp_ssl:
    external: true
  netpos_smtp_username:
    external: true
  netpos_smtp_password:
    external: true
  netpos_smtp_from:
    external: true
  netpos_mp_access_token:
    external: true
  netpos_mp_boleto_expiration:
    external: true
STACK

chmod 600 "$STACK_FILE"
ok "Stack file: ${STACK_FILE}"

# ==============================================================================
# FASE 9 — DEPLOY DO STACK
# ==============================================================================
phase "FASE 9 — Deploy do stack"

info "Baixando imagem ${APP_IMAGE} ..."
docker pull "$APP_IMAGE"

info "Fazendo deploy do stack '${STACK_NAME}'..."
docker stack deploy \
  --compose-file "$STACK_FILE" \
  --resolve-image always \
  --prune \
  "$STACK_NAME"

ok "Stack deployado"

# ==============================================================================
# FASE 10 — UFW FIREWALL (apenas adição — nunca remove regras existentes)
# ==============================================================================
phase "FASE 10 — Firewall (UFW)"

UFW_WAS_ACTIVE="n"
ufw status | grep -q "Status: active" && UFW_WAS_ACTIVE="y"

if [[ "$UFW_WAS_ACTIVE" == "n" ]]; then
  ufw default deny incoming >/dev/null
  ufw default allow outgoing >/dev/null
fi

if ! ufw status | grep -qE "^22/tcp|^OpenSSH"; then
  ufw allow 22/tcp comment "SSH" >/dev/null
  ok "UFW: SSH permitido"
else
  info "UFW: regra SSH já presente"
fi

case "$INSTALL_MODE" in
  vps_letsencrypt)
    for port in 80 443; do
      if ! ufw status | grep -q "^${port}/tcp"; then
        ufw allow "${port}/tcp" comment "Web-netpos" >/dev/null
        ok "UFW: porta ${port} permitida"
      else
        info "UFW: porta ${port} já aberta"
      fi
    done
    ;;

  local)
    if ! ufw status | grep -q "^80/tcp"; then
      ufw allow 80/tcp comment "HTTP-netpos" >/dev/null
      ok "UFW: porta 80 permitida"
    else
      info "UFW: porta 80 já aberta"
    fi
    if $SETUP_DNS && ! ufw status | grep -q "^53"; then
      ufw allow 53 comment "DNS-netpos" >/dev/null
      ok "UFW: porta 53 (DNS) permitida"
    fi
    ;;

  vps_cloudflare)
    info "Buscando IPs atuais do Cloudflare..."
    CF_IPV4=""
    CF_IPV6=""
    CF_IPV4=$(curl -s --max-time 15 https://www.cloudflare.com/ips-v4 2>/dev/null || true)
    CF_IPV6=$(curl -s --max-time 15 https://www.cloudflare.com/ips-v6 2>/dev/null || true)

    if [[ -z "$CF_IPV4" ]]; then
      warn "Não foi possível buscar IPs do Cloudflare — usando lista embutida"
      CF_IPV4="173.245.48.0/20
103.21.244.0/22
103.22.200.0/22
103.31.4.0/22
141.101.64.0/18
108.162.192.0/18
190.93.240.0/20
188.114.96.0/20
197.234.240.0/22
198.41.128.0/17
162.158.0.0/15
104.16.0.0/13
104.24.0.0/14
172.64.0.0/13
131.0.72.0/22"
      CF_IPV6="2400:cb00::/32
2606:4700::/32
2803:f800::/32
2405:b500::/32
2405:8100::/32
2a06:98c0::/29
2c0f:f248::/32"
    fi

    printf '%s\n' "$CF_IPV4" > "${APP_DIR}/etc/cloudflare-ips-v4.txt"
    printf '%s\n' "$CF_IPV6" > "${APP_DIR}/etc/cloudflare-ips-v6.txt"

    # Remover regras antigas desta app (idempotente)
    while ufw status numbered 2>/dev/null | grep -q "Cloudflare-netpos"; do
      _num=$(ufw status numbered | grep "Cloudflare-netpos" | head -1 | awk -F'[][]' '{print $2}')
      [[ -n "$_num" ]] && ufw --force delete "$_num" >/dev/null 2>&1 || break
    done

    info "Adicionando regras UFW para IPs Cloudflare (porta ${APP_PORT})..."
    while IFS= read -r ip; do
      [[ -z "$ip" ]] && continue
      ufw allow from "$ip" to any port "$APP_PORT" proto tcp \
        comment "Cloudflare-netpos" >/dev/null 2>&1 || true
    done <<< "$CF_IPV4"
    while IFS= read -r ip; do
      [[ -z "$ip" ]] && continue
      ufw allow from "$ip" to any port "$APP_PORT" proto tcp \
        comment "Cloudflare-netpos" >/dev/null 2>&1 || true
    done <<< "$CF_IPV6"

    if ! ufw status | grep -qE "DENY.*${APP_PORT}"; then
      ufw deny "${APP_PORT}/tcp" comment "Block-direct-netpos" >/dev/null
    fi
    ok "UFW: porta ${APP_PORT} restrita apenas a IPs do Cloudflare"

    # ── DOCKER-USER iptables ──────────────────────────────────────────────────
    # Docker publica portas via DNAT (nat/PREROUTING), bypassando o UFW.
    # A chain DOCKER-USER (avaliada em FORWARD, após DNAT) permite filtrar
    # pela porta original via conntrack, bloqueando acesso direto ao container.
    info "Configurando DOCKER-USER iptables..."

    DOCKER_RULES_SCRIPT="${APP_DIR}/scripts/docker-user-rules.sh"
    cat > "$DOCKER_RULES_SCRIPT" <<'DOCKERRULES_HEREDOC'
#!/usr/bin/env bash
# Restaurar regras DOCKER-USER para netpos — executado pelo systemd após docker.service
set -euo pipefail
export PATH="/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"
DOCKERRULES_HEREDOC

    # Variáveis injetadas separadamente para evitar expansão no heredoc acima
    cat >> "$DOCKER_RULES_SCRIPT" <<DOCKERRULES_VARS
APP_PORT="${APP_PORT}"
CF_V4="${APP_DIR}/etc/cloudflare-ips-v4.txt"
CF_V6="${APP_DIR}/etc/cloudflare-ips-v6.txt"
DOCKERRULES_VARS

    cat >> "$DOCKER_RULES_SCRIPT" <<'DOCKERRULES_BODY'

# Limpar regras antigas do netpos
while iptables  -D DOCKER-USER -m comment --comment "CF-netpos"    2>/dev/null; do :; done
while iptables  -D DOCKER-USER -m comment --comment "BLOCK-netpos" 2>/dev/null; do :; done
while iptables  -D DOCKER-USER -m comment --comment "ESTAB-netpos" 2>/dev/null; do :; done
while ip6tables -D DOCKER-USER -m comment --comment "CF-netpos"    2>/dev/null; do :; done
while ip6tables -D DOCKER-USER -m comment --comment "BLOCK-netpos" 2>/dev/null; do :; done
while ip6tables -D DOCKER-USER -m comment --comment "ESTAB-netpos" 2>/dev/null; do :; done

# Permitir pacotes de conexões já estabelecidas (tráfego de retorno)
iptables  -I DOCKER-USER 1 -m conntrack --ctstate RELATED,ESTABLISHED \
  -m comment --comment "ESTAB-netpos" -j RETURN
ip6tables -I DOCKER-USER 1 -m conntrack --ctstate RELATED,ESTABLISHED \
  -m comment --comment "ESTAB-netpos" -j RETURN

# Permitir IPs Cloudflare IPv4
if [[ -f "$CF_V4" ]]; then
  while IFS= read -r ip; do
    [[ -z "$ip" ]] && continue
    iptables -I DOCKER-USER 2 \
      -p tcp -m conntrack --ctorigdstport "${APP_PORT}" \
      -s "$ip" -m comment --comment "CF-netpos" -j RETURN
  done < "$CF_V4"
fi

# Permitir IPs Cloudflare IPv6
if [[ -f "$CF_V6" ]]; then
  while IFS= read -r ip; do
    [[ -z "$ip" ]] && continue
    ip6tables -I DOCKER-USER 2 \
      -p tcp -m conntrack --ctorigdstport "${APP_PORT}" \
      -s "$ip" -m comment --comment "CF-netpos" -j RETURN
  done < "$CF_V6"
fi

# Bloquear qualquer acesso direto que não veio do Cloudflare
iptables  -A DOCKER-USER -p tcp -m conntrack --ctorigdstport "${APP_PORT}" \
  -m comment --comment "BLOCK-netpos" -j DROP
ip6tables -A DOCKER-USER -p tcp -m conntrack --ctorigdstport "${APP_PORT}" \
  -m comment --comment "BLOCK-netpos" -j DROP

echo "[netpos] DOCKER-USER rules applied — port ${APP_PORT} restricted to Cloudflare"
DOCKERRULES_BODY

    chmod 750 "$DOCKER_RULES_SCRIPT"
    ok "Script: ${DOCKER_RULES_SCRIPT}"

    cat > "/etc/systemd/system/docker-user-rules-netpos.service" <<UNIT
[Unit]
Description=DOCKER-USER iptables rules for netpos
After=docker.service
BindsTo=docker.service

[Service]
Type=oneshot
ExecStart=${APP_DIR}/scripts/docker-user-rules.sh
RemainAfterExit=yes

[Install]
WantedBy=multi-user.target
UNIT
    systemctl daemon-reload
    systemctl enable --now docker-user-rules-netpos.service >/dev/null 2>&1 \
      && ok "Systemd: docker-user-rules-netpos habilitado e iniciado" \
      || warn "Execute manualmente: systemctl start docker-user-rules-netpos.service"
    ;;
esac

if [[ "$UFW_WAS_ACTIVE" == "n" ]]; then
  ufw --force enable >/dev/null
  ok "UFW habilitado"
fi
ufw reload >/dev/null
ok "UFW configurado"

# ==============================================================================
# FASE 11 — FAIL2BAN
# ==============================================================================
phase "FASE 11 — Fail2ban"

cat > "/etc/fail2ban/jail.d/netpos.conf" <<F2B
# fail2ban jails — netpos
# Gerado: $(date -u '+%Y-%m-%d %H:%M:%S UTC')

[DEFAULT]
bantime  = 1h
findtime = 10m
maxretry = 5
banaction = ufw

[sshd]
enabled  = true
port     = ssh
filter   = sshd
logpath  = /var/log/auth.log
maxretry = 3
bantime  = 24h

[nginx-http-auth]
enabled  = true
filter   = nginx-http-auth
port     = http,https,${APP_PORT}
logpath  = %(syslog_daemon)s
           ${APP_DIR}/logs/nginx/access.log
maxretry = 5

[nginx-limit-req]
enabled  = true
filter   = nginx-limit-req
port     = http,https,${APP_PORT}
logpath  = %(syslog_daemon)s
           ${APP_DIR}/logs/nginx/error.log
maxretry = 10

[nginx-botsearch]
enabled  = true
filter   = nginx-botsearch
port     = http,https,${APP_PORT}
logpath  = ${APP_DIR}/logs/nginx/access.log
maxretry = 2

[recidive]
enabled  = true
filter   = recidive
logpath  = /var/log/fail2ban.log
action   = ufw
bantime  = 1w
findtime = 1d
maxretry = 5
F2B

systemctl enable fail2ban >/dev/null 2>&1
systemctl restart fail2ban
ok "Fail2ban configurado: /etc/fail2ban/jail.d/netpos.conf"

# ==============================================================================
# FASE 12 — SCRIPTS DE MANUTENÇÃO
# ==============================================================================
phase "FASE 12 — Scripts de manutenção"

SCRIPTS_DIR="${APP_DIR}/scripts"

# ── update-images.sh ──────────────────────────────────────────────────────────
cat > "${SCRIPTS_DIR}/update-images.sh" <<UPDSCRIPT
#!/usr/bin/env bash
# Atualização de imagem do netpos — executa diariamente via cron
set -euo pipefail

LOG="${APP_DIR}/logs/update-images.log"
ts()  { date -u '+[%Y-%m-%d %H:%M:%S UTC]'; }
log() { echo "\$(ts) \$*" | tee -a "\$LOG"; }
exec 1>>"\$LOG" 2>&1

log "=== Atualização iniciada ==="
docker pull ${APP_IMAGE} && log "Pulled ${APP_IMAGE}" || log "AVISO: pull falhou"
docker service update \\
  --image ${APP_IMAGE} \\
  --update-order start-first \\
  netpos_app \\
  && log "netpos_app atualizado" \\
  || log "AVISO: atualização falhou"
docker image prune -f --filter "dangling=true" >/dev/null
log "=== Concluído ==="
UPDSCRIPT
chmod 750 "${SCRIPTS_DIR}/update-images.sh"
ok "Script: update-images.sh"

# ── update-cloudflare-ips.sh (modo Cloudflare) ────────────────────────────────
if [[ "$INSTALL_MODE" == "vps_cloudflare" ]]; then
  cat > "${SCRIPTS_DIR}/update-cloudflare-ips.sh" <<CFSCRIPT
#!/usr/bin/env bash
# Atualiza IPs do Cloudflare no UFW + DOCKER-USER — netpos
set -euo pipefail

APP_PORT="${APP_PORT}"
APP_DIR="${APP_DIR}"
LOG="\${APP_DIR}/logs/cf-ip-update.log"
ts()  { date -u '+[%Y-%m-%d %H:%M:%S UTC]'; }
log() { echo "\$(ts) \$*" | tee -a "\$LOG"; }
exec 1>>"\$LOG" 2>&1
log "=== Atualização IPs Cloudflare ==="

CF_IPV4=\$(curl -s --max-time 15 https://www.cloudflare.com/ips-v4 || true)
CF_IPV6=\$(curl -s --max-time 15 https://www.cloudflare.com/ips-v6 || true)
[[ -z "\$CF_IPV4" ]] && { log "ERRO: fetch falhou"; exit 1; }

while ufw status numbered 2>/dev/null | grep -q "Cloudflare-netpos"; do
  num=\$(ufw status numbered | grep "Cloudflare-netpos" | head -1 | awk -F'[][]' '{print \$2}')
  [[ -n "\$num" ]] && ufw --force delete "\$num" >/dev/null 2>&1 || break
done

while IFS= read -r ip; do [[ -z "\$ip" ]] && continue
  ufw allow from "\$ip" to any port "\$APP_PORT" proto tcp comment "Cloudflare-netpos" >/dev/null 2>&1 || true
done <<< "\$CF_IPV4"
while IFS= read -r ip; do [[ -z "\$ip" ]] && continue
  ufw allow from "\$ip" to any port "\$APP_PORT" proto tcp comment "Cloudflare-netpos" >/dev/null 2>&1 || true
done <<< "\$CF_IPV6"

printf '%s\n' "\$CF_IPV4" > "\${APP_DIR}/etc/cloudflare-ips-v4.txt"
printf '%s\n' "\$CF_IPV6" > "\${APP_DIR}/etc/cloudflare-ips-v6.txt"

[[ -x "\${APP_DIR}/scripts/docker-user-rules.sh" ]] \\
  && "\${APP_DIR}/scripts/docker-user-rules.sh" && log "DOCKER-USER atualizado" \\
  || log "AVISO: docker-user-rules.sh falhou"

ufw reload >/dev/null
log "=== Concluído ==="
CFSCRIPT
  chmod 750 "${SCRIPTS_DIR}/update-cloudflare-ips.sh"
  ok "Script: update-cloudflare-ips.sh"
fi

# ── renew-certs.sh (modo Let's Encrypt) ───────────────────────────────────────
if [[ "$USE_LETSENCRYPT" == "y" ]]; then
  cat > "${SCRIPTS_DIR}/renew-certs.sh" <<CERTSCRIPT
#!/usr/bin/env bash
# Renova certificado Let's Encrypt — netpos
# Para o nginx brevemente (libera porta 80), renova, reinicia.
set -euo pipefail

LOG="${APP_DIR}/logs/cert-renewal.log"
ts()  { date -u '+[%Y-%m-%d %H:%M:%S UTC]'; }
log() { echo "\$(ts) \$*" | tee -a "\$LOG"; }
exec 1>>"\$LOG" 2>&1

log "=== Verificação de renovação ==="
docker service scale netpos_nginx=0 && sleep 5
certbot renew --quiet --non-interactive 2>&1 | while IFS= read -r l; do log "\$l"; done
docker service scale netpos_nginx=1
log "=== Concluído ==="
CERTSCRIPT
  chmod 750 "${SCRIPTS_DIR}/renew-certs.sh"
  ok "Script: renew-certs.sh"
fi

# ── dnsmasq (modo local com DNS) ──────────────────────────────────────────────
if $SETUP_DNS; then
  _srv_ip=$(hostname -I | awk '{print $1}')
  cat > "/etc/dnsmasq.d/netpos.conf" <<DNSCONF
address=/${SRV_HOSTNAME}/${_srv_ip}
address=/${SRV_HOSTNAME}.local/${_srv_ip}
domain-needed
bogus-priv
DNSCONF
  systemctl restart dnsmasq
  systemctl enable dnsmasq >/dev/null 2>&1
  ok "dnsmasq: ${SRV_HOSTNAME} → ${_srv_ip}"
  warn "Configure o roteador da rede para usar ${_srv_ip} como DNS primário."
fi

# ==============================================================================
# FASE 13 — CRON JOBS
# ==============================================================================
phase "FASE 13 — Cron jobs"

{
  echo "# Cron jobs — netpos"
  echo "# Gerado: $(date -u '+%Y-%m-%d %H:%M:%S UTC')"
  echo "SHELL=/bin/bash"
  echo "PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"
  echo "CRON_TZ=America/Sao_Paulo"
  echo ""
  echo "# Atualização de imagem — diariamente às 04:00"
  echo "0 4 * * * root ${SCRIPTS_DIR}/update-images.sh"
  echo ""
  if [[ "$USE_LETSENCRYPT" == "y" ]]; then
    echo "CRON_TZ=UTC"
    echo "# Renovação Let's Encrypt — a cada 12 horas"
    echo "0 */12 * * * root ${SCRIPTS_DIR}/renew-certs.sh"
    echo ""
  fi
  if [[ "$INSTALL_MODE" == "vps_cloudflare" ]]; then
    echo "# Atualizar IPs Cloudflare — todo dia 1 às 03:00"
    echo "0 3 1 * * root ${SCRIPTS_DIR}/update-cloudflare-ips.sh"
    echo ""
  fi
} > "/etc/cron.d/netpos"

chmod 644 "/etc/cron.d/netpos"
ok "Cron: /etc/cron.d/netpos"

# ==============================================================================
# FASE 14 — AGUARDAR SERVIÇOS
# ==============================================================================
phase "FASE 14 — Aguardando serviços"

_svc_tasks() {
  local svc="$1"
  echo -e "  ${DIM}Tasks:${NC}"
  docker service ps "$svc" --no-trunc \
    --format "    {{printf \"%-42s\" .Name}}  {{printf \"%-22s\" .CurrentState}}  {{.Error}}" \
    2>/dev/null | head -8 || echo "    (sem tasks ainda)"
}

_svc_logs() {
  local svc="$1" n="${2:-10}"
  local out
  out=$(docker service logs --tail "$n" --no-task-ids --timestamps "$svc" 2>/dev/null || true)
  if [[ -n "$out" ]]; then
    echo -e "  ${DIM}Logs (últimas ${n} linhas):${NC}"
    echo "$out" | sed 's/^/    /'
  else
    echo -e "  ${DIM}Logs: (nenhum ainda)${NC}"
  fi
}

wait_for_svc() {
  local svc="$1" max="${2:-120}" tick=8 elapsed=0 running

  echo ""
  echo -e "${BLD}${CYN}▶  ${svc}${NC}  ${DIM}(timeout: ${max}s)${NC}"
  sep

  while [[ $elapsed -lt $max ]]; do
    running=$(docker service ps \
      --filter desired-state=running \
      --format "{{.CurrentState}}" \
      "$svc" 2>/dev/null | grep -c "^Running" || true)

    echo -e "  ${DIM}[${elapsed}s]${NC}"
    _svc_tasks "$svc"
    echo ""
    _svc_logs "$svc"

    if [[ "$running" -ge 1 ]]; then
      echo ""
      ok "${svc}  ✓  em execução"
      sep
      return 0
    fi

    echo ""
    echo -e "  ${YLW}Aguardando... próxima verificação em ${tick}s${NC}"
    echo ""
    sleep "$tick"
    elapsed=$((elapsed + tick))
  done

  echo ""
  echo -e "  ${RED}[TIMEOUT após ${max}s]${NC}"
  echo ""
  docker service ps "$svc" --no-trunc \
    --format "    {{printf \"%-42s\" .Name}}  {{printf \"%-22s\" .CurrentState}}  {{.Error}}" \
    2>/dev/null || true
  echo ""
  _svc_logs "$svc" 30
  echo ""
  warn "${svc} expirou — pode ainda estar convergindo. Verifique com:"
  echo "  docker service logs -f ${svc}"
  sep
  return 1
}

info "Estado imediatamente após o deploy:"
echo ""
docker stack ps "$STACK_NAME" \
  --format "  {{printf \"%-42s\" .Name}}  {{printf \"%-22s\" .CurrentState}}  {{.Error}}" \
  2>/dev/null || true
echo ""

_WAIT_FAILED=0
wait_for_svc "netpos_db"  120 || _WAIT_FAILED=1
wait_for_svc "netpos_app" 180 || _WAIT_FAILED=1
if [[ "$USE_LETSENCRYPT" == "y" || "$INSTALL_LOCAL" == "y" ]]; then
  wait_for_svc "netpos_nginx" 60 || _WAIT_FAILED=1
fi

echo ""
info "Estado final do stack:"
echo ""
docker stack services "$STACK_NAME" 2>/dev/null || true
echo ""

if [[ $_WAIT_FAILED -eq 0 ]]; then
  ok "Todos os serviços em execução"
else
  warn "Um ou mais serviços não atingiram Running. O stack está deployado e"
  warn "pode ainda estar convergindo. Verifique com:"
  echo "  docker stack services netpos"
  echo "  docker stack ps netpos --no-trunc"
fi

# ==============================================================================
# FASE 15 — RESUMO COMPLETO COM TODAS AS CREDENCIAIS
# ==============================================================================
phase "FASE 15 — Resumo e credenciais"

clear
echo ""
echo -e "${BLD}${GRN}"
cat <<'DONE'
 ╔══════════════════════════════════════════════════════════════════════════════╗
 ║              INSTALAÇÃO DO NETPOS CONCLUÍDA                                  ║
 ╚══════════════════════════════════════════════════════════════════════════════╝
DONE
echo -e "${NC}"

# ── URL de acesso ─────────────────────────────────────────────────────────────
sep
echo -e "  ${BLD}${CYN}APLICAÇÃO${NC}"
sep
echo -e "  Stack      : ${BLD}netpos${NC}"
echo -e "  Diretório  : ${BLD}${APP_DIR}${NC}"
echo -e "  Imagem     : ${BLD}${APP_IMAGE}${NC}"
echo -e "  Stack file : ${BLD}${STACK_FILE}${NC}"
echo ""
case "$INSTALL_MODE" in
  vps_cloudflare)
    echo -e "  Modo       : ${BLD}VPS + Cloudflare${NC}"
    echo -e "  Porta exp. : ${BLD}${APP_PORT}${NC}"
    if [[ -n "$DOMAIN" ]]; then
      echo -e "  URL        : ${BLD}${CYN}https://${DOMAIN}${NC}  ${DIM}(via Cloudflare)${NC}"
    else
      _srv_ip_final=$(hostname -I | awk '{print $1}')
      echo -e "  URL        : ${BLD}${CYN}http://${_srv_ip_final}:${APP_PORT}${NC}  ${DIM}(via Cloudflare → seu domínio)${NC}"
    fi
    ;;
  vps_letsencrypt)
    echo -e "  Modo       : ${BLD}VPS + Let's Encrypt${NC}"
    echo -e "  URL        : ${BLD}${CYN}https://${DOMAIN}${NC}"
    ;;
  local)
    _lip_final=$(hostname -I | awk '{print $1}')
    echo -e "  Modo       : ${BLD}Local / intranet${NC}"
    if $SETUP_DNS; then
      echo -e "  URL        : ${BLD}${CYN}http://${SRV_HOSTNAME}${NC}  ${DIM}(após configurar roteador)${NC}"
    fi
    echo -e "  URL (IP)   : ${BLD}${CYN}http://${_lip_final}${NC}"
    ;;
esac

# ── Credenciais de acesso ─────────────────────────────────────────────────────
echo ""
sep
echo -e "  ${BLD}${CYN}CONTA ADMIN${NC}  ${DIM}(criada no primeiro acesso)${NC}"
sep
echo -e "  E-mail     : ${BLD}${ADMIN_EMAIL}${NC}"
echo -e "  Senha      : ${BLD}${RED}${ADMIN_PASSWORD}${NC}"

# ── Banco de dados ────────────────────────────────────────────────────────────
echo ""
sep
echo -e "  ${BLD}${CYN}BANCO DE DADOS${NC}"
sep
echo -e "  Database   : ${BLD}${POSTGRES_DB}${NC}"
echo -e "  Usuário    : ${BLD}${POSTGRES_USER}${NC}"
echo -e "  Senha      : ${BLD}${RED}${POSTGRES_PASSWORD}${NC}"
echo -e "  Conn. str. : ${DIM}Host=db;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}${NC}"

# ── SMTP ──────────────────────────────────────────────────────────────────────
if [[ -n "$SMTP_HOST" ]]; then
  echo ""
  sep
  echo -e "  ${BLD}${CYN}SMTP${NC}"
  sep
  echo -e "  Servidor   : ${BLD}${SMTP_HOST}:${SMTP_PORT}${NC}"
  echo -e "  SSL/TLS    : ${BLD}${SMTP_SSL}${NC}"
  echo -e "  Usuário    : ${BLD}${SMTP_USERNAME}${NC}"
  echo -e "  Senha      : ${BLD}${RED}${SMTP_PASSWORD}${NC}"
  echo -e "  From       : ${BLD}${SMTP_FROM}${NC}"
fi

# ── MercadoPago ───────────────────────────────────────────────────────────────
if [[ -n "$MP_ACCESS_TOKEN" ]]; then
  echo ""
  sep
  echo -e "  ${BLD}${CYN}MERCADOPAGO${NC}"
  sep
  echo -e "  Access Token      : ${BLD}${RED}${MP_ACCESS_TOKEN}${NC}"
  echo -e "  Validade boleto   : ${BLD}${MP_BOLETO_EXPIRATION}${NC}"
fi

# ── Swarm secrets ─────────────────────────────────────────────────────────────
echo ""
sep
echo -e "  ${BLD}${CYN}DOCKER SWARM SECRETS${NC}  ${DIM}(prefixo netpos_ — encriptados no Raft)${NC}"
sep
echo -e "  ${DIM}netpos_db_password          →  /run/secrets/postgres_password${NC}"
echo -e "  ${DIM}netpos_db_conn              →  /run/secrets/postgres_connection${NC}"
echo -e "  ${DIM}netpos_admin_email          →  /run/secrets/admin_email${NC}"
echo -e "  ${DIM}netpos_admin_password       →  /run/secrets/admin_password${NC}"
echo -e "  ${DIM}netpos_smtp_host            →  /run/secrets/smtp_host${NC}"
echo -e "  ${DIM}netpos_smtp_port            →  /run/secrets/smtp_port${NC}"
echo -e "  ${DIM}netpos_smtp_ssl             →  /run/secrets/smtp_ssl${NC}"
echo -e "  ${DIM}netpos_smtp_username        →  /run/secrets/smtp_username${NC}"
echo -e "  ${DIM}netpos_smtp_password        →  /run/secrets/smtp_password${NC}"
echo -e "  ${DIM}netpos_smtp_from            →  /run/secrets/smtp_from${NC}"
echo -e "  ${DIM}netpos_mp_access_token      →  /run/secrets/mp_access_token${NC}"
echo -e "  ${DIM}netpos_mp_boleto_expiration →  /run/secrets/mp_boleto_expiration${NC}"

# ── Jobs agendados ────────────────────────────────────────────────────────────
echo ""
sep
echo -e "  ${BLD}${CYN}JOBS AGENDADOS${NC}  ${DIM}(/etc/cron.d/netpos)${NC}"
sep
echo -e "  Atualização imagem : ${BLD}Diariamente às 04:00 (Horário de Brasília)${NC}"
[[ "$USE_LETSENCRYPT" == "y" ]] && \
  echo -e "  Cert. LE renewal   : ${BLD}A cada 12 horas (UTC)${NC}"
[[ "$INSTALL_MODE" == "vps_cloudflare" ]] && \
  echo -e "  IPs Cloudflare     : ${BLD}Todo dia 1 do mês às 03:00${NC}"

# ── Comandos úteis ────────────────────────────────────────────────────────────
echo ""
sep
echo -e "  ${BLD}${CYN}COMANDOS ÚTEIS${NC}"
sep
echo -e "  Status dos serviços : ${BLD}docker stack services netpos${NC}"
echo -e "  Logs da aplicação   : ${BLD}docker service logs -f netpos_app${NC}"
echo -e "  Logs do banco       : ${BLD}docker service logs -f netpos_db${NC}"
echo -e "  Reiniciar app       : ${BLD}docker service update --force netpos_app${NC}"
echo -e "  Atualizar agora     : ${BLD}${SCRIPTS_DIR}/update-images.sh${NC}"
echo -e "  Desinstalar         : ${BLD}sudo bash uninstall.sh${NC}"
echo ""

sep
echo -e "${YLW}${BLD}  ╔══════════════════════════════════════════════════════════╗${NC}"
echo -e "${YLW}${BLD}  ║  IMPORTANTE: Anote as credenciais acima em local seguro.  ║${NC}"
echo -e "${YLW}${BLD}  ║  Elas NÃO estão salvas em nenhum arquivo do servidor.     ║${NC}"
echo -e "${YLW}${BLD}  ║  Se perdidas, será necessário reinstalar o stack.          ║${NC}"
echo -e "${YLW}${BLD}  ╚══════════════════════════════════════════════════════════╝${NC}"
echo ""

if [[ "$INSTALL_MODE" == "vps_cloudflare" ]]; then
  echo -e "${YLW}  CLOUDFLARE: Crie uma Origin Rule apontando seu domínio para"
  echo -e "  a porta ${APP_PORT}. Acesso direto (sem Cloudflare) está bloqueado"
  echo -e "  por UFW + regras DOCKER-USER iptables.${NC}"
  echo ""
fi

if [[ "$USE_LETSENCRYPT" == "y" ]]; then
  echo -e "${YLW}  LET'S ENCRYPT: Se o certbot falhou, verifique se '${DOMAIN}'"
  echo -e "  aponta para este servidor com porta 80 acessível, então execute:"
  echo -e "  ${BLD}certbot certonly --standalone -d ${DOMAIN} -m ${CERTBOT_EMAIL} --agree-tos${NC}"
  echo -e "${YLW}  e reinicie: ${BLD}docker service update --force netpos_nginx${NC}"
  echo ""
fi

sep
echo ""
