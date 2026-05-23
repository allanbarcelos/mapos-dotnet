#!/usr/bin/env bash
# ==============================================================================
#  uninstall.sh — Remove SOMENTE a instalação do netpos
#
#  Uso: sudo bash uninstall.sh
#
#  O QUE ESTE SCRIPT REMOVE:
#    ✓ Docker stack "netpos" (todos os serviços)
#    ✓ Docker Swarm secrets com prefixo netpos_
#    ✓ Regras UFW específicas do netpos (Cloudflare-netpos, Web-netpos, etc.)
#    ✓ Regras DOCKER-USER iptables do netpos
#    ✓ Systemd unit docker-user-rules-netpos.service
#    ✓ Jail fail2ban /etc/fail2ban/jail.d/netpos.conf
#    ✓ Cron /etc/cron.d/netpos
#    ✓ Diretório /opt/netpos (configurações, scripts, logs)
#    ✓ Configuração dnsmasq /etc/dnsmasq.d/netpos.conf (se existir)
#
#  O QUE NÃO É REMOVIDO:
#    ✗ Docker, Docker Swarm (podem ter outros stacks)
#    ✗ UFW (regras de outros projetos preservadas)
#    ✗ Fail2ban (serviço permanece ativo para outros jails)
#    ✗ Certificado Let's Encrypt em /etc/letsencrypt (pode ser reutilizado)
#    ✗ Dados do PostgreSQL (requer confirmação explícita para remover)
# ==============================================================================
set -euo pipefail
IFS=$'\n\t'

export PATH="/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:${PATH:-}"

# ── Identidade do projeto (fixo — deve bater com install.sh) ──────────────────
readonly APP_NAME="netpos"
readonly APP_DIR="/opt/netpos"
readonly STACK_NAME="netpos"

# ── Cores ──────────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GRN='\033[0;32m'; YLW='\033[1;33m'
CYN='\033[0;36m'; BLD='\033[1m'; DIM='\033[2m'; NC='\033[0m'

info()  { echo -e "${CYN}[INFO]${NC}  $*"; }
ok()    { echo -e "${GRN}[OK]${NC}    $*"; }
warn()  { echo -e "${YLW}[AVISO]${NC} $*"; }
skip()  { echo -e "${DIM}[SKIP]${NC}  $*"; }
step()  { echo -e "\n${BLD}${CYN}━━━  $*  ━━━${NC}"; }
sep()   { echo -e "${DIM}──────────────────────────────────────────────────────${NC}"; }

require_root() { [[ $EUID -eq 0 ]] || { echo -e "${RED}[ERRO]${NC}  Execute como root: sudo bash uninstall.sh" >&2; exit 1; }; }

require_root

# ── Banner + aviso ─────────────────────────────────────────────────────────────
clear
echo -e "${RED}${BLD}"
cat <<'WARN'
 ╔══════════════════════════════════════════════════════════════════════════════╗
 ║  ATENÇÃO — DESINSTALAÇÃO DO NETPOS                                           ║
 ║                                                                              ║
 ║  Este script remove o stack netpos, seus secrets e configurações.            ║
 ║  Docker, UFW e fail2ban permanecem instalados (preserva outros projetos).    ║
 ║                                                                              ║
 ║  Os DADOS do banco de dados serão removidos somente se você confirmar        ║
 ║  explicitamente na etapa correspondente.                                     ║
 ╚══════════════════════════════════════════════════════════════════════════════╝
WARN
echo -e "${NC}"

read -rp "$(echo -e "  ${BLD}Confirmar desinstalação do netpos?${NC} ${DIM}[s/N]${NC}: ")" _CONFIRM </dev/tty
[[ "${_CONFIRM:-n}" =~ ^[SsYy]$ ]] || { echo "Cancelado."; exit 0; }
echo ""

# Pergunta sobre os dados agora (antes de qualquer ação) para não interromper o fluxo
REMOVE_DATA="n"
if [[ -d "${APP_DIR}/data/postgres" ]]; then
  echo ""
  echo -e "  ${YLW}${BLD}DADOS DO BANCO DE DADOS${NC}"
  echo -e "  Os dados PostgreSQL estão em ${BLD}${APP_DIR}/data/postgres${NC}"
  echo -e "  ${RED}Esta ação é IRREVERSÍVEL — faça backup antes de confirmar.${NC}"
  echo ""
  read -rp "$(echo -e "  ${BLD}Remover dados do banco de dados?${NC} ${DIM}[s/N]${NC}: ")" _REMOVE_DATA </dev/tty
  [[ "${_REMOVE_DATA:-n}" =~ ^[SsYy]$ ]] && REMOVE_DATA="y"
  echo ""
fi

sep
echo ""

# ==============================================================================
# PASSO 1 — Docker stack
# ==============================================================================
step "1/9 — Removendo stack Docker"

if docker stack ls 2>/dev/null | grep -q "^${STACK_NAME}"; then
  info "Removendo stack '${STACK_NAME}'..."
  docker stack rm "$STACK_NAME"

  # Aguarda containers pararem completamente antes de remover secrets
  info "Aguardando containers pararem..."
  _wait=0
  until ! docker ps --filter "name=${STACK_NAME}_" --quiet 2>/dev/null | grep -q .; do
    sleep 2
    _wait=$((_wait + 2))
    [[ $_wait -ge 30 ]] && break
  done
  sleep 3   # margem extra para o Swarm liberar os secrets
  ok "Stack '${STACK_NAME}' removido"
else
  skip "Stack '${STACK_NAME}' não encontrado"
fi

# ==============================================================================
# PASSO 2 — Docker Swarm secrets
# ==============================================================================
step "2/9 — Removendo Swarm secrets"

NETPOS_SECRETS=(
  netpos_db_password
  netpos_db_conn
  netpos_admin_email
  netpos_admin_password
  netpos_smtp_host
  netpos_smtp_port
  netpos_smtp_ssl
  netpos_smtp_username
  netpos_smtp_password
  netpos_smtp_from
  netpos_mp_access_token
  netpos_mp_boleto_expiration
)

_removed_secrets=0
for secret in "${NETPOS_SECRETS[@]}"; do
  if docker secret inspect "$secret" &>/dev/null 2>&1; then
    if docker secret rm "$secret" >/dev/null 2>&1; then
      ok "Secret removido: ${secret}"
      _removed_secrets=$((_removed_secrets + 1))
    else
      warn "Não foi possível remover '${secret}' — pode ainda estar em uso"
    fi
  else
    skip "Secret não existe: ${secret}"
  fi
done

[[ $_removed_secrets -eq 0 ]] && skip "Nenhum secret netpos_ encontrado"

# ==============================================================================
# PASSO 3 — Regras UFW
# ==============================================================================
step "3/9 — Removendo regras UFW"

if command -v ufw &>/dev/null; then
  # Remove todas as regras com comentário contendo "netpos"
  _removed_rules=0
  while ufw status numbered 2>/dev/null | grep -qiE "netpos|Cloudflare-netpos|Web-netpos|HTTP-netpos|Block-direct-netpos|DNS-netpos"; do
    _num=$(ufw status numbered 2>/dev/null \
      | grep -iE "netpos|Cloudflare-netpos|Web-netpos|HTTP-netpos|Block-direct-netpos|DNS-netpos" \
      | head -1 \
      | awk -F'[][]' '{print $2}')
    if [[ -n "$_num" ]]; then
      ufw --force delete "$_num" >/dev/null 2>&1 \
        && { ok "Regra UFW #${_num} removida"; _removed_rules=$((_removed_rules + 1)); } \
        || break
    else
      break
    fi
  done
  [[ $_removed_rules -gt 0 ]] && ufw reload >/dev/null && ok "UFW recarregado" || skip "Nenhuma regra UFW do netpos encontrada"
else
  skip "UFW não instalado"
fi

# ==============================================================================
# PASSO 4 — DOCKER-USER iptables
# ==============================================================================
step "4/9 — Removendo regras DOCKER-USER iptables"

_removed_ipt=0
for table_cmd in iptables ip6tables; do
  for comment in "CF-netpos" "BLOCK-netpos" "ESTAB-netpos"; do
    while $table_cmd -D DOCKER-USER -m comment --comment "$comment" 2>/dev/null; do
      _removed_ipt=$((_removed_ipt + 1))
    done
  done
done

[[ $_removed_ipt -gt 0 ]] \
  && ok "Removidas ${_removed_ipt} regra(s) iptables DOCKER-USER do netpos" \
  || skip "Nenhuma regra DOCKER-USER do netpos encontrada"

# ==============================================================================
# PASSO 5 — Systemd unit
# ==============================================================================
step "5/9 — Removendo systemd unit"

UNIT_FILE="/etc/systemd/system/docker-user-rules-netpos.service"

if [[ -f "$UNIT_FILE" ]]; then
  systemctl stop  docker-user-rules-netpos.service 2>/dev/null || true
  systemctl disable docker-user-rules-netpos.service 2>/dev/null || true
  rm -f "$UNIT_FILE"
  systemctl daemon-reload
  ok "Systemd unit removida: docker-user-rules-netpos.service"
else
  skip "Systemd unit não encontrada"
fi

# ==============================================================================
# PASSO 6 — Fail2ban
# ==============================================================================
step "6/9 — Removendo jail fail2ban"

F2B_JAIL="/etc/fail2ban/jail.d/netpos.conf"

if [[ -f "$F2B_JAIL" ]]; then
  rm -f "$F2B_JAIL"
  ok "Jail removida: ${F2B_JAIL}"
  if systemctl is-active --quiet fail2ban 2>/dev/null; then
    systemctl restart fail2ban
    ok "Fail2ban reiniciado"
  fi
else
  skip "Jail não encontrada: ${F2B_JAIL}"
fi

# ==============================================================================
# PASSO 7 — Cron
# ==============================================================================
step "7/9 — Removendo cron"

CRON_FILE="/etc/cron.d/netpos"

if [[ -f "$CRON_FILE" ]]; then
  rm -f "$CRON_FILE"
  ok "Cron removido: ${CRON_FILE}"
else
  skip "Cron não encontrado: ${CRON_FILE}"
fi

# ==============================================================================
# PASSO 8 — Dnsmasq
# ==============================================================================
step "8/9 — Removendo configuração dnsmasq"

DNS_CONF="/etc/dnsmasq.d/netpos.conf"

if [[ -f "$DNS_CONF" ]]; then
  rm -f "$DNS_CONF"
  ok "dnsmasq config removida: ${DNS_CONF}"
  if systemctl is-active --quiet dnsmasq 2>/dev/null; then
    systemctl restart dnsmasq
    ok "dnsmasq reiniciado"
  fi
else
  skip "dnsmasq config não encontrada"
fi

# ==============================================================================
# PASSO 9 — Diretório da aplicação
# ==============================================================================
step "9/9 — Removendo diretório da aplicação"

if [[ -d "$APP_DIR" ]]; then
  if [[ "$REMOVE_DATA" == "y" ]]; then
    info "Removendo ${APP_DIR} (incluindo dados do banco)..."
    rm -rf "$APP_DIR"
    ok "Diretório removido: ${APP_DIR}"
  else
    # Remove tudo exceto data/
    info "Removendo configurações e scripts (dados preservados)..."
    rm -rf \
      "${APP_DIR}/docker-compose.prod.yml" \
      "${APP_DIR}/scripts" \
      "${APP_DIR}/etc" \
      "${APP_DIR}/logs" \
      2>/dev/null || true
    ok "Configurações removidas. Dados preservados em: ${APP_DIR}/data/postgres"
    warn "Para remover os dados manualmente: rm -rf ${APP_DIR}/data"
  fi
else
  skip "Diretório não encontrado: ${APP_DIR}"
fi

# ==============================================================================
# RESUMO
# ==============================================================================
echo ""
sep
echo -e "${BLD}${GRN}  Desinstalação do netpos concluída!${NC}"
sep
echo ""
echo -e "  ${GRN}✓${NC}  Stack Docker removido"
echo -e "  ${GRN}✓${NC}  Swarm secrets removidos"
echo -e "  ${GRN}✓${NC}  Regras UFW do netpos removidas"
echo -e "  ${GRN}✓${NC}  Regras DOCKER-USER iptables removidas"
echo -e "  ${GRN}✓${NC}  Systemd unit removida"
echo -e "  ${GRN}✓${NC}  Jail fail2ban removida"
echo -e "  ${GRN}✓${NC}  Cron removido"
if [[ "$REMOVE_DATA" == "y" ]]; then
  echo -e "  ${GRN}✓${NC}  Dados do banco removidos"
else
  echo -e "  ${YLW}⚠${NC}  Dados do banco preservados em: ${APP_DIR}/data/postgres"
  echo -e "      Para remover:  ${BLD}rm -rf ${APP_DIR}/data${NC}"
fi
echo ""
echo -e "  ${DIM}Docker, Docker Swarm, UFW, fail2ban e outros stacks não foram afetados.${NC}"
echo ""
sep
echo ""
