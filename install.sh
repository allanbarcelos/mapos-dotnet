#!/usr/bin/env bash
#
# ╔══════════════════════════════════════════════════════════════════════════════╗
# ║  install.sh — Instalador automático do mapos-dotnet                         ║
# ╠══════════════════════════════════════════════════════════════════════════════╣
# ║                                                                              ║
# ║  ATENÇÃO — LEIA ANTES DE EXECUTAR                                            ║
# ║                                                                              ║
# ║  Este é um script básico que considera o cenário ideal:                      ║
# ║    • Ubuntu 22.04 LTS ou Debian 12 recém-instalado                           ║
# ║    • Servidor sem Docker, nginx, PostgreSQL ou qualquer outro serviço        ║
# ║      previamente instalado ou configurado                                    ║
# ║                                                                              ║
# ║  NUNCA execute este script em um servidor que já possua dados                ║
# ║  ou sistemas em execução. Isso pode causar perda irreversível de dados       ║
# ║  e sobrescrever configurações existentes.                                    ║
# ║                                                                              ║
# ╠══════════════════════════════════════════════════════════════════════════════╣
# ║  Portas abertas pelo instalador (via UFW)                                    ║
# ║                                                                              ║
# ║  Modo VPS + Cloudflare                                                       ║
# ║    22/tcp   — SSH (acesso remoto ao servidor)                                ║
# ║    8080/tcp — Aplicação (apenas IPs do Cloudflare; acesso direto bloqueado)  ║
# ║                                                                              ║
# ║  Modo VPS + Let's Encrypt                                                    ║
# ║    22/tcp   — SSH                                                            ║
# ║    80/tcp   — HTTP (redireciona para HTTPS via nginx)                        ║
# ║    443/tcp  — HTTPS (certificado Let's Encrypt, proxy para a aplicação)      ║
# ║                                                                              ║
# ║  Modo local (intranet)                                                       ║
# ║    22/tcp   — SSH                                                            ║
# ║    80/tcp   — HTTP (proxy nginx para a aplicação)                            ║
# ║    53/udp+tcp — DNS (apenas se dnsmasq for instalado neste servidor)         ║
# ║                                                                              ║
# ║  Em todos os modos o PostgreSQL (5432) permanece interno ao Docker           ║
# ║  e nunca é exposto ao sistema operacional ou à rede.                         ║
# ║                                                                              ║
# ╚══════════════════════════════════════════════════════════════════════════════╝

# ── Cores ──────────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GRN='\033[0;32m'; YLW='\033[1;33m'
CYN='\033[0;36m'; BLD='\033[1m';    NC='\033[0m'

# ── Variáveis globais ──────────────────────────────────────────────────────────
INSTALL_DIR="$HOME/mapos"
IMAGE="ghcr.io/allanbarcelos/mapos-dotnet:latest"
INSTALL_MODE=""       # vps_cloudflare | vps_letsencrypt | local
DOMAIN=""
EMAIL=""
SRV_HOSTNAME=""
SETUP_DNS=false
SMTP_HOST=""
SMTP_PORT="587"
SMTP_SSL="true"
SMTP_USERNAME=""
SMTP_PASSWORD=""
SMTP_FROM=""
MP_ACCESS_TOKEN=""
MP_BOLETO_EXPIRATION="P3D"
DB_PASS=""
ADMIN_PASS=""
POSTGRES_USER="mapos_user"
POSTGRES_DB="mapos"

# IPs do Cloudflare (fonte: https://www.cloudflare.com/ips/)
CF_IPS_V4=(
  "173.245.48.0/20" "103.21.244.0/22"  "103.22.200.0/22" "103.31.4.0/22"
  "141.101.64.0/18" "108.162.192.0/18" "190.93.240.0/20" "188.114.96.0/20"
  "197.234.240.0/22" "198.41.128.0/17" "162.158.0.0/15"  "104.16.0.0/13"
  "104.24.0.0/14"   "172.64.0.0/13"   "131.0.72.0/22"
)
CF_IPS_V6=(
  "2400:cb00::/32" "2606:4700::/32" "2803:f800::/32" "2405:b500::/32"
  "2405:8100::/32" "2a06:98c0::/29" "2c0f:f248::/32"
)

# ── Utilitários ────────────────────────────────────────────────────────────────
info()    { echo -e "${CYN}[INFO]${NC}  $*"; }
success() { echo -e "${GRN}[OK]${NC}    $*"; }
warn()    { echo -e "${YLW}[AVISO]${NC} $*"; }
die()     { echo -e "${RED}[ERRO]${NC}  $*" >&2; exit 1; }
hr()      { echo -e "${BLD}────────────────────────────────────────────────────${NC}"; }

ask() {
  # ask "Pergunta" ["padrão"]  → imprime a resposta
  local prompt="$1" default="${2:-}" answer
  if [[ -n "$default" ]]; then
    read -rp "$(echo -e "${BLD}${prompt}${NC} [${CYN}${default}${NC}]: ")" answer
    printf '%s' "${answer:-$default}"
  else
    read -rp "$(echo -e "${BLD}${prompt}${NC}: ")" answer
    printf '%s' "$answer"
  fi
}

confirm() {
  # confirm "Pergunta?"  → 0=sim 1=não
  local answer
  read -rp "$(echo -e "${BLD}$1${NC} ${CYN}[s/N]${NC}: ")" answer
  [[ "${answer,,}" =~ ^(s|sim|y|yes)$ ]]
}

gen_pass() {
  # Gera senha aleatória alfanumérica de tamanho $1 (padrão 24)
  local len="${1:-24}"
  openssl rand -base64 48 | tr -d '+/=' | cut -c1-"$len"
}

# ── Verificações ───────────────────────────────────────────────────────────────
check_root() {
  [[ $EUID -eq 0 ]] || die "Execute com sudo:  sudo bash install.sh"
}

check_os() {
  [[ -f /etc/os-release ]] || die "Sistema operacional não identificado."
  # shellcheck source=/dev/null
  source /etc/os-release
  case "${ID:-}" in
    ubuntu|debian) ;;
    *)
      warn "Sistema detectado: ${PRETTY_NAME:-$ID}."
      warn "Este script foi testado apenas em Ubuntu 22.04 e Debian 12."
      confirm "Deseja continuar mesmo assim?" || exit 0
      ;;
  esac
}

# ── Instalação de dependências e Docker ────────────────────────────────────────
install_deps() {
  info "Atualizando pacotes e instalando dependências..."
  apt-get update -qq
  apt-get install -y -qq curl openssl ufw ca-certificates
  success "Dependências instaladas."
}

install_docker() {
  if command -v docker &>/dev/null; then
    success "Docker já instalado: $(docker --version | cut -d' ' -f3 | tr -d ',')"
    return
  fi
  info "Instalando Docker..."
  curl -fsSL https://get.docker.com | sh
  systemctl enable docker
  systemctl start docker
  if [[ -n "${SUDO_USER:-}" ]]; then
    usermod -aG docker "$SUDO_USER"
    info "Usuário '${SUDO_USER}' adicionado ao grupo docker."
  fi
  success "Docker instalado."
}

# ── Firewall (UFW) ─────────────────────────────────────────────────────────────
ufw_base() {
  info "Configurando firewall (UFW)..."
  ufw --force reset        > /dev/null
  ufw default deny incoming > /dev/null
  ufw default allow outgoing > /dev/null
  ufw allow 22/tcp comment 'SSH' > /dev/null
}

configure_ufw_cloudflare() {
  ufw_base
  info "Liberando porta 8080 apenas para os IPs do Cloudflare..."
  for ip in "${CF_IPS_V4[@]}"; do
    ufw allow from "$ip" to any port 8080 proto tcp comment 'Cloudflare' > /dev/null
  done
  for ip in "${CF_IPS_V6[@]}"; do
    ufw allow from "$ip" to any port 8080 proto tcp comment 'Cloudflare v6' > /dev/null 2>&1 || true
  done
  ufw --force enable > /dev/null
  success "UFW: porta 8080 aberta somente para o Cloudflare. Acesso direto ao IP bloqueado."
}

configure_ufw_public() {
  ufw_base
  ufw allow 80/tcp  comment 'HTTP'  > /dev/null
  ufw allow 443/tcp comment 'HTTPS' > /dev/null
  ufw --force enable > /dev/null
  success "UFW: portas 22, 80 e 443 abertas."
}

configure_ufw_local() {
  ufw_base
  ufw allow 80/tcp comment 'HTTP intranet' > /dev/null
  if $SETUP_DNS; then
    ufw allow 53 comment 'DNS' > /dev/null 2>&1 || true
  fi
  ufw --force enable > /dev/null
  success "UFW: portas 22 e 80 abertas$(${SETUP_DNS} && echo ' + 53 DNS' || true)."
}

# ── Let's Encrypt ──────────────────────────────────────────────────────────────
setup_certbot() {
  info "Instalando Certbot..."
  apt-get install -y -qq certbot
  info "Obtendo certificado TLS para ${DOMAIN} ..."
  certbot certonly --standalone \
    -d "$DOMAIN" \
    --non-interactive --agree-tos \
    -m "$EMAIL" --quiet \
    || die "Falha ao obter certificado. Verifique se o DNS de ${DOMAIN} aponta para este servidor e se a porta 80 está acessível."
  success "Certificado obtido em /etc/letsencrypt/live/${DOMAIN}/"
  # Cron de renovação automática
  ( crontab -l 2>/dev/null
    echo "0 3 * * * certbot renew --quiet && docker compose -f ${INSTALL_DIR}/docker-compose.yml restart nginx"
  ) | crontab -
  success "Renovação automática agendada (cron 03:00)."
}

# ── dnsmasq ────────────────────────────────────────────────────────────────────
setup_dnsmasq() {
  local server_ip
  server_ip=$(hostname -I | awk '{print $1}')
  info "Instalando dnsmasq..."
  apt-get install -y -qq dnsmasq
  cat > /etc/dnsmasq.d/mapos.conf <<EOF
# Resolve ${SRV_HOSTNAME} e ${SRV_HOSTNAME}.local para este servidor
address=/${SRV_HOSTNAME}/${server_ip}
address=/${SRV_HOSTNAME}.local/${server_ip}

# Não encaminhar nomes locais para DNS externo
domain-needed
bogus-priv
EOF
  systemctl restart dnsmasq
  systemctl enable dnsmasq
  success "dnsmasq configurado: ${SRV_HOSTNAME} → ${server_ip}"
  echo ""
  warn "Próximo passo manual:"
  warn "Configure o roteador da rede para usar ${BLD}${server_ip}${NC} como servidor DNS primário."
  warn "Após isso, todos os dispositivos da rede acessarão via http://${SRV_HOSTNAME}"
  echo ""
}

# ── Perguntas SMTP ─────────────────────────────────────────────────────────────
ask_smtp() {
  echo ""
  hr
  echo -e "${BLD}  Configuração de e-mail (SMTP)${NC}"
  hr
  if ! confirm "Deseja configurar envio de e-mails?"; then
    SMTP_HOST=""
    return
  fi
  SMTP_HOST=$(ask     "Servidor SMTP"               "smtp.gmail.com")
  SMTP_PORT=$(ask     "Porta SMTP"                  "587")
  SMTP_SSL=$(ask      "Usar SSL/TLS? (true/false)"  "true")
  SMTP_USERNAME=$(ask "Usuário SMTP (e-mail)")
  SMTP_PASSWORD=$(ask "Senha / App Password SMTP")
  SMTP_FROM=$(ask     "E-mail remetente (from)"     "$SMTP_USERNAME")
  success "SMTP configurado."
}

# ── Perguntas MercadoPago ──────────────────────────────────────────────────────
ask_mercadopago() {
  echo ""
  hr
  echo -e "${BLD}  Integração MercadoPago (opcional)${NC}"
  hr
  if ! confirm "Deseja configurar o gateway MercadoPago?"; then
    MP_ACCESS_TOKEN=""
    return
  fi
  MP_ACCESS_TOKEN=$(ask    "Access Token do MercadoPago")
  MP_BOLETO_EXPIRATION=$(ask "Validade do boleto (ISO 8601)" "P3D")
  success "MercadoPago configurado."
}

# ── Geração de secrets ─────────────────────────────────────────────────────────
generate_secrets() {
  info "Gerando credenciais com openssl..."
  DB_PASS=$(gen_pass 32)
  ADMIN_PASS=$(gen_pass 16)

  mkdir -p "${INSTALL_DIR}/secrets"
  chmod 700 "${INSTALL_DIR}/secrets"

  printf '%s' "Host=db;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${DB_PASS}" \
    > "${INSTALL_DIR}/secrets/postgres_connection"
  printf '%s' "$DB_PASS"              > "${INSTALL_DIR}/secrets/postgres_password"
  printf '%s' "$ADMIN_PASS"           > "${INSTALL_DIR}/secrets/admin_password"
  printf '%s' "${SMTP_HOST}"          > "${INSTALL_DIR}/secrets/smtp_host"
  printf '%s' "${SMTP_PORT}"          > "${INSTALL_DIR}/secrets/smtp_port"
  printf '%s' "${SMTP_SSL}"           > "${INSTALL_DIR}/secrets/smtp_ssl"
  printf '%s' "${SMTP_USERNAME}"      > "${INSTALL_DIR}/secrets/smtp_username"
  printf '%s' "${SMTP_PASSWORD}"      > "${INSTALL_DIR}/secrets/smtp_password"
  printf '%s' "${SMTP_FROM}"          > "${INSTALL_DIR}/secrets/smtp_from"
  printf '%s' "${MP_ACCESS_TOKEN}"      > "${INSTALL_DIR}/secrets/mp_access_token"
  printf '%s' "${MP_BOLETO_EXPIRATION}" > "${INSTALL_DIR}/secrets/mp_boleto_expiration"

  chmod 600 "${INSTALL_DIR}/secrets/"*
  success "Secrets criados em ${INSTALL_DIR}/secrets/"
}

# ── docker-compose.yml ─────────────────────────────────────────────────────────

# Bloco de secrets comum (reutilizado nos heredocs abaixo)
# Função auxiliar: gera o docker-compose sem nginx (modo Cloudflare)
create_compose_cloudflare() {
  cat > "${INSTALL_DIR}/docker-compose.yml" <<'YAML'
secrets:
  postgres_connection:
    file: ./secrets/postgres_connection
  postgres_password:
    file: ./secrets/postgres_password
  admin_password:
    file: ./secrets/admin_password
  smtp_host:
    file: ./secrets/smtp_host
  smtp_port:
    file: ./secrets/smtp_port
  smtp_ssl:
    file: ./secrets/smtp_ssl
  smtp_username:
    file: ./secrets/smtp_username
  smtp_password:
    file: ./secrets/smtp_password
  smtp_from:
    file: ./secrets/smtp_from
  mp_access_token:
    file: ./secrets/mp_access_token
  mp_boleto_expiration:
    file: ./secrets/mp_boleto_expiration

services:

  # Aplicação exposta diretamente na porta 8080.
  # O Cloudflare atua como proxy HTTPS — não é necessário nginx nem certificado local.
  # Configure uma Origin Rule no painel do Cloudflare: Destination port → 8080
  app:
    image: ghcr.io/allanbarcelos/mapos-dotnet:latest
    restart: unless-stopped
    depends_on:
      db:
        condition: service_healthy
        restart: true
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
    secrets:
      - postgres_connection
      - admin_password
      - smtp_host
      - smtp_port
      - smtp_ssl
      - smtp_username
      - smtp_password
      - smtp_from
      - mp_access_token
      - mp_boleto_expiration
    networks:
      - internal_network
    healthcheck:
      test: ["CMD-SHELL", "curl -sf http://localhost:8080/health || exit 1"]
      interval: 15s
      timeout: 5s
      retries: 5
      start_period: 30s

  db:
    image: postgres:16-alpine
    restart: unless-stopped
    environment:
      POSTGRES_DB: mapos
      POSTGRES_USER: mapos_user
      POSTGRES_PASSWORD_FILE: /run/secrets/postgres_password
    secrets:
      - postgres_password
    volumes:
      - pgdata:/var/lib/postgresql/data
    networks:
      - internal_network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U mapos_user -d mapos"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 20s

volumes:
  pgdata:

networks:
  internal_network:
    driver: bridge
YAML
  success "docker-compose.yml gerado (modo Cloudflare, sem nginx)."
}

# docker-compose com nginx + Let's Encrypt (portas 80 e 443)
create_compose_letsencrypt() {
  cat > "${INSTALL_DIR}/docker-compose.yml" <<'YAML'
secrets:
  postgres_connection:
    file: ./secrets/postgres_connection
  postgres_password:
    file: ./secrets/postgres_password
  admin_password:
    file: ./secrets/admin_password
  smtp_host:
    file: ./secrets/smtp_host
  smtp_port:
    file: ./secrets/smtp_port
  smtp_ssl:
    file: ./secrets/smtp_ssl
  smtp_username:
    file: ./secrets/smtp_username
  smtp_password:
    file: ./secrets/smtp_password
  smtp_from:
    file: ./secrets/smtp_from
  mp_access_token:
    file: ./secrets/mp_access_token
  mp_boleto_expiration:
    file: ./secrets/mp_boleto_expiration

services:

  app:
    image: ghcr.io/allanbarcelos/mapos-dotnet:latest
    restart: unless-stopped
    depends_on:
      db:
        condition: service_healthy
        restart: true
    environment:
      ASPNETCORE_ENVIRONMENT: Production
    secrets:
      - postgres_connection
      - admin_password
      - smtp_host
      - smtp_port
      - smtp_ssl
      - smtp_username
      - smtp_password
      - smtp_from
      - mp_access_token
      - mp_boleto_expiration
    networks:
      - internal_network
    healthcheck:
      test: ["CMD-SHELL", "curl -sf http://localhost:8080/health || exit 1"]
      interval: 15s
      timeout: 5s
      retries: 5
      start_period: 30s

  db:
    image: postgres:16-alpine
    restart: unless-stopped
    environment:
      POSTGRES_DB: mapos
      POSTGRES_USER: mapos_user
      POSTGRES_PASSWORD_FILE: /run/secrets/postgres_password
    secrets:
      - postgres_password
    volumes:
      - pgdata:/var/lib/postgresql/data
    networks:
      - internal_network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U mapos_user -d mapos"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 20s

  nginx:
    image: nginx:1.27-alpine
    restart: unless-stopped
    depends_on:
      app:
        condition: service_healthy
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/conf.d/default.conf:ro
      - /etc/letsencrypt:/etc/letsencrypt:ro
    networks:
      - internal_network

volumes:
  pgdata:

networks:
  internal_network:
    driver: bridge
YAML
  success "docker-compose.yml gerado (modo Let's Encrypt, nginx + HTTPS)."
}

# docker-compose com nginx local (apenas porta 80, intranet)
create_compose_local() {
  cat > "${INSTALL_DIR}/docker-compose.yml" <<'YAML'
secrets:
  postgres_connection:
    file: ./secrets/postgres_connection
  postgres_password:
    file: ./secrets/postgres_password
  admin_password:
    file: ./secrets/admin_password
  smtp_host:
    file: ./secrets/smtp_host
  smtp_port:
    file: ./secrets/smtp_port
  smtp_ssl:
    file: ./secrets/smtp_ssl
  smtp_username:
    file: ./secrets/smtp_username
  smtp_password:
    file: ./secrets/smtp_password
  smtp_from:
    file: ./secrets/smtp_from
  mp_access_token:
    file: ./secrets/mp_access_token
  mp_boleto_expiration:
    file: ./secrets/mp_boleto_expiration

services:

  app:
    image: ghcr.io/allanbarcelos/mapos-dotnet:latest
    restart: unless-stopped
    depends_on:
      db:
        condition: service_healthy
        restart: true
    environment:
      ASPNETCORE_ENVIRONMENT: Production
    secrets:
      - postgres_connection
      - admin_password
      - smtp_host
      - smtp_port
      - smtp_ssl
      - smtp_username
      - smtp_password
      - smtp_from
      - mp_access_token
      - mp_boleto_expiration
    networks:
      - internal_network
    healthcheck:
      test: ["CMD-SHELL", "curl -sf http://localhost:8080/health || exit 1"]
      interval: 15s
      timeout: 5s
      retries: 5
      start_period: 30s

  db:
    image: postgres:16-alpine
    restart: unless-stopped
    environment:
      POSTGRES_DB: mapos
      POSTGRES_USER: mapos_user
      POSTGRES_PASSWORD_FILE: /run/secrets/postgres_password
    secrets:
      - postgres_password
    volumes:
      - pgdata:/var/lib/postgresql/data
    networks:
      - internal_network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U mapos_user -d mapos"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 20s

  nginx:
    image: nginx:1.27-alpine
    restart: unless-stopped
    depends_on:
      app:
        condition: service_healthy
    ports:
      - "80:80"
    volumes:
      - ./nginx.conf:/etc/nginx/conf.d/default.conf:ro
    networks:
      - internal_network

volumes:
  pgdata:

networks:
  internal_network:
    driver: bridge
YAML
  success "docker-compose.yml gerado (modo local, nginx porta 80)."
}

# ── nginx.conf ─────────────────────────────────────────────────────────────────
create_nginx_letsencrypt() {
  cat > "${INSTALL_DIR}/nginx.conf" <<NGINX
upstream app {
    server app:8080;
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

    client_max_body_size 20M;

    proxy_set_header Host              \$host;
    proxy_set_header X-Real-IP         \$remote_addr;
    proxy_set_header X-Forwarded-For   \$proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto \$scheme;

    location / {
        proxy_pass http://app;
    }
}
NGINX
  success "nginx.conf gerado (Let's Encrypt, HTTPS)."
}

create_nginx_local() {
  cat > "${INSTALL_DIR}/nginx.conf" <<NGINX
upstream app {
    server app:8080;
}

server {
    listen 80;
    server_name ${SRV_HOSTNAME} ${SRV_HOSTNAME}.local _;

    client_max_body_size 20M;

    proxy_set_header Host              \$host;
    proxy_set_header X-Real-IP         \$remote_addr;
    proxy_set_header X-Forwarded-For   \$proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto \$scheme;

    location / {
        proxy_pass http://app;
    }
}
NGINX
  success "nginx.conf gerado (intranet, HTTP)."
}

# ── Pull + start ───────────────────────────────────────────────────────────────
start_stack() {
  info "Baixando imagem ${IMAGE} ..."
  docker pull "$IMAGE"
  info "Iniciando serviços..."
  cd "${INSTALL_DIR}"
  docker compose up -d

  info "Aguardando serviços ficarem saudáveis (até 90 s)..."
  local elapsed=0
  until docker compose ps | grep -q "healthy" || [[ $elapsed -ge 90 ]]; do
    sleep 3
    (( elapsed += 3 ))
  done
  success "Stack iniciada."
}

# ── Resumo final ───────────────────────────────────────────────────────────────
show_summary() {
  local server_ip access_url
  server_ip=$(hostname -I | awk '{print $1}')

  case "$INSTALL_MODE" in
    vps_cloudflare)
      access_url="https://SEU_DOMINIO  (via Cloudflare → ${server_ip}:8080)"
      ;;
    vps_letsencrypt)
      access_url="https://${DOMAIN}"
      ;;
    local)
      if $SETUP_DNS; then
        access_url="http://${SRV_HOSTNAME}  (após configurar o roteador)  ou  http://${server_ip}"
      else
        access_url="http://${server_ip}"
      fi
      ;;
  esac

  echo ""
  echo -e "${GRN}${BLD}"
  cat <<'BANNER'
 ╔══════════════════════════════════════════════════════════════════════════════╗
 ║              INSTALAÇÃO CONCLUÍDA COM SUCESSO                               ║
 ╚══════════════════════════════════════════════════════════════════════════════╝
BANNER
  echo -e "${NC}"
  echo -e "  ${BLD}Modo:${NC}              ${INSTALL_MODE}"
  echo -e "  ${BLD}Diretório:${NC}         ${INSTALL_DIR}"
  echo -e "  ${BLD}URL de acesso:${NC}     ${CYN}${access_url}${NC}"
  echo ""
  hr
  echo -e "  ${BLD}Credenciais de acesso${NC}"
  hr
  echo -e "  ${BLD}Usuário admin:${NC}     admin"
  echo -e "  ${BLD}Senha admin:${NC}       ${YLW}${BLD}${ADMIN_PASS}${NC}"
  echo ""
  hr
  echo -e "  ${BLD}Banco de dados${NC}"
  hr
  echo -e "  ${BLD}Usuário:${NC}           ${POSTGRES_USER}"
  echo -e "  ${BLD}Banco:${NC}             ${POSTGRES_DB}"
  echo -e "  ${BLD}Senha:${NC}             ${YLW}${DB_PASS}${NC}"
  echo ""
  if [[ -n "$SMTP_HOST" ]]; then
    hr
    echo -e "  ${BLD}SMTP${NC}"
    hr
    echo -e "  ${BLD}Servidor:${NC}          ${SMTP_HOST}:${SMTP_PORT}"
    echo -e "  ${BLD}Usuário:${NC}           ${SMTP_USERNAME}"
    echo ""
  fi
  if [[ -n "$MP_ACCESS_TOKEN" ]]; then
    hr
    echo -e "  ${BLD}MercadoPago${NC}"
    hr
    echo -e "  ${BLD}Validade boleto:${NC}   ${MP_BOLETO_EXPIRATION}"
    echo ""
  fi
  hr
  echo -e "  ${BLD}Comandos úteis${NC}"
  hr
  echo -e "  cd ${INSTALL_DIR}"
  echo -e "  docker compose logs -f        ${CYN}# acompanhar logs${NC}"
  echo -e "  docker compose ps             ${CYN}# status dos containers${NC}"
  echo -e "  docker compose restart app    ${CYN}# reiniciar aplicação${NC}"
  echo -e "  docker compose down           ${CYN}# parar tudo${NC}"
  echo ""
  echo -e "  ${RED}${BLD}Guarde a senha admin em local seguro — ela não será exibida novamente.${NC}"
  echo ""
}

# ── Fluxo VPS ──────────────────────────────────────────────────────────────────
flow_vps() {
  echo ""
  hr
  echo -e "  ${BLD}VPS — Escolha o modo de HTTPS${NC}"
  hr
  echo ""
  echo -e "  ${BLD}1)${NC} Cloudflare   — HTTPS gerenciado pelo Cloudflare (sem nginx local)"
  echo -e "  ${BLD}2)${NC} Let's Encrypt — certificado TLS no próprio servidor"
  echo ""
  local choice
  read -rp "$(echo -e "${BLD}Opção [1/2]:${NC} ")" choice

  case "$choice" in
    1)
      INSTALL_MODE="vps_cloudflare"
      echo ""
      info "Modo: VPS + Cloudflare"
      echo ""
      echo -e "  O que será configurado:"
      echo -e "   • Aplicação exposta na ${BLD}porta 8080${NC} do servidor"
      echo -e "   • Nginx ${BLD}não${NC} será instalado — o Cloudflare faz o proxy HTTPS"
      echo -e "   • UFW bloqueará todo acesso direto; apenas IPs do Cloudflare serão aceitos"
      echo ""
      echo -e "  ${YLW}Lembre-se de criar uma Origin Rule no painel do Cloudflare:${NC}"
      echo -e "  ${YLW}  Hostname = seu domínio  →  Destination port = 8080${NC}"
      echo ""
      ask_smtp
      ask_mercadopago
      generate_secrets
      create_compose_cloudflare
      configure_ufw_cloudflare
      ;;
    2)
      INSTALL_MODE="vps_letsencrypt"
      echo ""
      info "Modo: VPS + Let's Encrypt"
      echo ""
      DOMAIN=$(ask "Nome do domínio (ex: minha-oficina.com.br)")
      [[ -z "$DOMAIN" ]] && die "Domínio não informado."
      EMAIL=$(ask "E-mail do responsável (notificações Let's Encrypt)")
      [[ -z "$EMAIL" ]] && die "E-mail não informado."
      echo ""
      warn "Certifique-se de que o DNS de ${DOMAIN} já aponta para o IP deste servidor"
      warn "e que a porta 80 está acessível antes de continuar."
      echo ""
      confirm "O DNS está configurado e a porta 80 acessível?" || die "Configure o DNS e tente novamente."
      ask_smtp
      ask_mercadopago
      generate_secrets
      # Certbot precisa da porta 80 livre — Docker ainda não está rodando
      setup_certbot
      create_compose_letsencrypt
      create_nginx_letsencrypt
      configure_ufw_public
      ;;
    *)
      die "Opção inválida. Execute o script novamente."
      ;;
  esac
}

# ── Fluxo Local ────────────────────────────────────────────────────────────────
flow_local() {
  INSTALL_MODE="local"
  echo ""
  info "Modo: servidor local (intranet)"

  # ── Hostname ──
  echo ""
  hr
  echo -e "  ${BLD}Hostname do servidor${NC}"
  hr
  local current_hostname
  current_hostname=$(hostname)
  echo -e "  Hostname atual: ${BLD}${CYN}${current_hostname}${NC}"
  echo ""

  if confirm "Deseja alterar o hostname?"; then
    SRV_HOSTNAME=$(ask "Novo hostname (sem espaços, ex: mapos-server)")
    [[ -z "$SRV_HOSTNAME" ]] && die "Hostname não informado."
    hostnamectl set-hostname "$SRV_HOSTNAME"
    if grep -q "127.0.1.1" /etc/hosts; then
      sed -i "s/^127\.0\.1\.1.*/127.0.1.1\t${SRV_HOSTNAME}/" /etc/hosts
    else
      echo -e "127.0.1.1\t${SRV_HOSTNAME}" >> /etc/hosts
    fi
    success "Hostname alterado para: ${SRV_HOSTNAME}"
  else
    SRV_HOSTNAME="$current_hostname"
  fi

  # ── DNS ──
  echo ""
  hr
  echo -e "  ${BLD}Configuração de DNS na rede local${NC}"
  hr
  echo ""

  if confirm "Já existe um servidor DNS na rede (roteador com DNS, Pi-hole, etc.)?"; then
    echo ""
    info "Este servidor responderá pelo nome: ${BLD}http://${SRV_HOSTNAME}${NC}"
    info "Adicione manualmente no seu DNS uma entrada A apontando"
    info "${BLD}${SRV_HOSTNAME}${NC} para o IP deste servidor."
    SETUP_DNS=false
  else
    echo ""
    echo -e "  Sem um servidor DNS, os dispositivos da rede precisarão acessar"
    echo -e "  pelo IP do servidor. Instalando o ${BLD}dnsmasq${NC} neste servidor,"
    echo -e "  basta configurar o roteador para usá-lo como DNS e toda a rede"
    echo -e "  passará a resolver ${BLD}http://${SRV_HOSTNAME}${NC} automaticamente."
    echo ""
    if confirm "Deseja instalar o dnsmasq e tornar este servidor um DNS local?"; then
      SETUP_DNS=true
    else
      SETUP_DNS=false
      server_ip_local=$(hostname -I | awk '{print $1}')
      info "Sem DNS local. Acesse pelo IP: http://${server_ip_local}"
    fi
  fi

  # ── SMTP e MercadoPago ──
  ask_smtp
  ask_mercadopago

  # ── Gerar arquivos ──
  generate_secrets
  create_compose_local
  create_nginx_local

  if $SETUP_DNS; then
    setup_dnsmasq
  fi

  configure_ufw_local
}

# ── Menu principal ─────────────────────────────────────────────────────────────
main_menu() {
  echo ""
  hr
  echo -e "  ${BLD}Selecione o modo de instalação${NC}"
  hr
  echo ""
  echo -e "  ${BLD}1)${NC} Instalar em VPS        — rede pública, domínio próprio"
  echo -e "  ${BLD}2)${NC} Instalar em servidor local — intranet, rede interna"
  echo -e "  ${BLD}3)${NC} Sair"
  echo ""
  local choice
  read -rp "$(echo -e "${BLD}Opção [1/2/3]:${NC} ")" choice
  case "$choice" in
    1) flow_vps   ;;
    2) flow_local ;;
    3) echo "Saindo."; exit 0 ;;
    *) die "Opção inválida. Execute o script novamente." ;;
  esac
}

# ── Ponto de entrada ───────────────────────────────────────────────────────────
main() {
  clear
  echo -e "${RED}${BLD}"
  cat <<'WARN'
 ╔══════════════════════════════════════════════════════════════════════════════╗
 ║  ATENÇÃO — LEIA ANTES DE EXECUTAR                                            ║
 ║                                                                              ║
 ║  Este é um script básico que considera o cenário ideal:                      ║
 ║    • Ubuntu 22.04 LTS ou Debian 12 recém-instalado                           ║
 ║    • Servidor sem Docker, nginx, PostgreSQL ou qualquer outro serviço        ║
 ║      previamente instalado ou configurado                                    ║
 ║                                                                              ║
 ║  NUNCA execute este script em um servidor que já possua dados                ║
 ║  ou sistemas em execução. Isso pode causar perda irreversível de dados       ║
 ║  e sobrescrever configurações existentes.                                    ║
 ╚══════════════════════════════════════════════════════════════════════════════╝
WARN
  echo -e "${NC}"

  confirm "Confirmo que li o aviso acima e estou em um servidor sem dados ou serviços" \
    || { echo "Instalação cancelada."; exit 0; }

  echo ""
  echo -e "${CYN}${BLD}"
  cat <<'LOGO'
  ███╗   ███╗ █████╗ ██████╗  ██████╗ ███████╗
  ████╗ ████║██╔══██╗██╔══██╗██╔═══██╗██╔════╝
  ██╔████╔██║███████║██████╔╝██║   ██║███████╗
  ██║╚██╔╝██║██╔══██║██╔═══╝ ██║   ██║╚════██║
  ██║ ╚═╝ ██║██║  ██║██║     ╚██████╔╝███████║
  ╚═╝     ╚═╝╚═╝  ╚═╝╚═╝      ╚═════╝ ╚══════╝
           dotnet  •  Instalador de Produção
LOGO
  echo -e "${NC}"

  check_root
  check_os

  mkdir -p "$INSTALL_DIR"

  install_deps
  install_docker

  main_menu

  start_stack
  show_summary
}

main "$@"
