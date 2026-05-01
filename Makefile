.DEFAULT_GOAL := help
SHELL         := /usr/bin/env bash

# ── Cores ─────────────────────────────────────────────────────────────────────
BOLD  := $(shell tput bold 2>/dev/null)
RESET := $(shell tput sgr0 2>/dev/null)
GREEN := $(shell tput setaf 2 2>/dev/null)
CYAN  := $(shell tput setaf 6 2>/dev/null)

# ── Variáveis (.env → Make) ───────────────────────────────────────────────────
get_env = $(shell grep -E "^$(1)=" .env 2>/dev/null | head -1 | sed "s/^$(1)=//")

POSTGRES_DB       ?= $(or $(call get_env,POSTGRES_DB),mapos)
POSTGRES_USER     ?= $(or $(call get_env,POSTGRES_USER),mapos_user)
POSTGRES_PASSWORD ?= $(or $(call get_env,POSTGRES_PASSWORD),troque_esta_senha)
ADMIN_PASSWORD    ?= $(or $(call get_env,ADMIN_PASSWORD),troque_esta_senha)
SMTP_HOST         ?= $(or $(call get_env,SMTP_HOST),smtp.gmail.com)
SMTP_PORT         ?= $(or $(call get_env,SMTP_PORT),587)
SMTP_SSL          ?= $(or $(call get_env,SMTP_SSL),true)
SMTP_USERNAME        ?= $(call get_env,SMTP_USERNAME)
SMTP_PASSWORD        ?= $(call get_env,SMTP_PASSWORD)
SMTP_FROM            ?= $(call get_env,SMTP_FROM)
MP_ACCESS_TOKEN      ?= $(call get_env,MP_ACCESS_TOKEN)
MP_BOLETO_EXPIRATION ?= $(or $(call get_env,MP_BOLETO_EXPIRATION),P3D)

# ── Ajuda ─────────────────────────────────────────────────────────────────────
.PHONY: help
help: ## Exibe esta ajuda
	@awk 'BEGIN {FS = ":.*##"} /^[a-zA-Z_-]+:.*##/ \
	  { printf "  $(CYAN)%-18s$(RESET) %s\n", $$1, $$2 }' $(MAKEFILE_LIST)

# ── Setup ─────────────────────────────────────────────────────────────────────
.PHONY: setup
setup: .env ## Gera secrets/ a partir do .env (execute uma vez antes do `make up`)
	@set -euo pipefail; \
	mkdir -p secrets; \
	chmod 700 secrets; \
	printf 'Host=db;Port=5432;Database=%s;Username=%s;Password=%s' \
	  "$(POSTGRES_DB)" "$(POSTGRES_USER)" "$(POSTGRES_PASSWORD)" > secrets/postgres_connection; \
	printf '%s' "$(POSTGRES_PASSWORD)" > secrets/postgres_password; \
	printf '%s' "$(ADMIN_PASSWORD)"    > secrets/admin_password; \
	printf '%s' "$(SMTP_HOST)"         > secrets/smtp_host; \
	printf '%s' "$(SMTP_PORT)"         > secrets/smtp_port; \
	printf '%s' "$(SMTP_SSL)"          > secrets/smtp_ssl; \
	printf '%s' "$(SMTP_USERNAME)"     > secrets/smtp_username; \
	printf '%s' "$(SMTP_PASSWORD)"     > secrets/smtp_password; \
	printf '%s' "$(SMTP_FROM)"            > secrets/smtp_from; \
	printf '%s' "$(MP_ACCESS_TOKEN)"      > secrets/mp_access_token; \
	printf '%s' "$(MP_BOLETO_EXPIRATION)" > secrets/mp_boleto_expiration; \
	chmod 600 secrets/*; \
	echo "$(GREEN)secrets/ criados$(RESET)"; \
	for f in secrets/*; do echo "  $$f"; done

.env:
	@echo "$(BOLD)ERRO:$(RESET) .env não encontrado. Execute:  cp .env.example .env"; exit 1

# ── Docker ────────────────────────────────────────────────────────────────────
.PHONY: up
up: secrets/postgres_connection secrets/postgres_password secrets/admin_password secrets/smtp_password secrets/smtp_host secrets/smtp_username secrets/smtp_from secrets/mp_access_token ## Sobe a stack completa (build se necessário)
	docker compose up --build

.PHONY: up-d
up-d: secrets/postgres_connection secrets/postgres_password secrets/admin_password secrets/smtp_password secrets/smtp_host secrets/smtp_username secrets/smtp_from secrets/mp_access_token ## Sobe a stack em background
	docker compose up --build -d

.PHONY: down
down: ## Para e remove containers (mantém volumes)
	docker compose down

.PHONY: down-v
down-v: ## Para containers E remove volumes (apaga dados do banco)
	docker compose down -v

.PHONY: logs
logs: ## Acompanha logs de todos os serviços
	docker compose logs -f

.PHONY: ps
ps: ## Lista containers em execução
	docker compose ps

# ── Banco de dados ────────────────────────────────────────────────────────────
.PHONY: migration
migration: ## Cria uma nova migration  (uso: make migration NAME=AddMinhaTabela)
	@test -n "$(NAME)" || (echo "Uso: make migration NAME=<NomeDaMigration>"; exit 1)
	dotnet ef migrations add $(NAME)

.PHONY: db-update
db-update: ## Aplica migrations pendentes (local, requer Postgres rodando)
	dotnet ef database update

.PHONY: psql
psql: ## Abre shell psql dentro do container db
	docker compose exec db psql -U $(POSTGRES_USER) $(POSTGRES_DB)

# ── App local ─────────────────────────────────────────────────────────────────
.PHONY: build
build: ## dotnet build (local, sem Docker)
	dotnet build mapos-dotnet.csproj

.PHONY: run
run: ## dotnet run (local, sem Docker)
	dotnet run --project mapos-dotnet.csproj

.PHONY: css
css: ## Compila Tailwind CSS
	npm run build:css

.PHONY: css-watch
css-watch: ## Tailwind em modo watch
	npm run watch:css

# ── Guard de secrets ──────────────────────────────────────────────────────────
secrets/postgres_connection secrets/postgres_password secrets/admin_password secrets/smtp_host secrets/smtp_port secrets/smtp_ssl secrets/smtp_username secrets/smtp_password secrets/smtp_from secrets/mp_access_token secrets/mp_boleto_expiration:
	@echo "$(BOLD)secrets/ não encontrado.$(RESET) Execute:  make setup"; exit 1
