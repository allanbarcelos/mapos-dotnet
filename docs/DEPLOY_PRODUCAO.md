# Manual de Publicação em Produção

**Imagem oficial:** `ghcr.io/allanbarcelos/mapos-dotnet:latest`

---

## Sobre este manual

O mapos-dotnet é uma evolução do [mapos](https://github.com/mapos/mapos), sistema open-source de gerenciamento de ordens de serviço amplamente utilizado por oficinas e assistências técnicas. Além de modernizar a base tecnológica — migrando para ASP.NET Core 9, PostgreSQL e uma interface responsiva — o mapos-dotnet expande o escopo original: além do gerenciamento completo de ordens de serviço, clientes, produtos e financeiro, o sistema agora conta com um módulo de **Ponto de Venda (PDV)**, tornando-o adequado também para mercados, lojas e estabelecimentos que combinam venda direta com prestação de serviços.

Por sua natureza, sistemas desse tipo são frequentemente implantados em redes internas — um servidor local acessado apenas pelos computadores e dispositivos do próprio estabelecimento. Esse cenário é simples, econômico e suficiente para a grande maioria dos casos de uso.

No entanto, o mapos-dotnet foi construído para operar também em redes públicas com segurança: autenticação robusta, comunicação criptografada, variáveis sensíveis isoladas em Docker secrets e suporte a proxy reverso com cabeçalhos de encaminhamento corretos.

Este manual cobre com detalhes os dois cenários:

- **Rede pública via VPS** — o sistema fica acessível pela internet, com domínio próprio, HTTPS e, opcionalmente, proteção pelo Cloudflare. Indicado para empresas com múltiplas filiais, técnicos externos ou acesso remoto.
- **Servidor local (intranet)** — o sistema roda em um computador ou servidor na rede do estabelecimento, sem exposição à internet. Indicado para o uso no balcão, em redes Wi-Fi internas ou ambientes sem necessidade de acesso externo.

Independentemente do cenário, o processo de instalação é o mesmo: Docker, um arquivo `docker-compose.yml` e os arquivos de configuração descritos nas seções a seguir.

---

## Índice

1. [Requisitos do servidor](#1-requisitos-do-servidor)
2. [Instalar Docker](#2-instalar-docker)
3. [Criar estrutura de diretórios](#3-criar-estrutura-de-diretórios)
4. [Criar o docker-compose.yml de produção](#4-criar-o-docker-composeyml-de-produção)
5. [Configurar nginx.conf](#5-configurar-nginxconf)
6. [Configurar secrets](#6-configurar-secrets)
7. [HTTPS com Let's Encrypt (Certbot)](#7-https-com-lets-encrypt-certbot)
8. [Cloudflare — cache, proteção e HTTPS gerenciado](#8-cloudflare--cache-proteção-e-https-gerenciado)
9. [Uso em intranet (sem domínio público)](#9-uso-em-intranet-sem-domínio-público)
10. [Subir a stack](#10-subir-a-stack)
11. [Verificar saúde dos serviços](#11-verificar-saúde-dos-serviços)
12. [Atualizar para nova versão](#12-atualizar-para-nova-versão)
13. [Comandos de manutenção](#13-comandos-de-manutenção)
14. [Backup do banco de dados](#14-backup-do-banco-de-dados)

---

## 1. Requisitos do servidor

| Recurso | VPS (rede pública) | Servidor local (intranet) |
|---|---|---|
| CPU | 1 vCPU | Qualquer x86-64 com 1+ núcleo |
| RAM | 1 GB | 1 GB |
| Disco | 20 GB SSD | 20 GB (HDD aceito) |
| SO | Ubuntu 22.04 LTS ou Debian 12 | Ubuntu 22.04 LTS, Debian 12 ou qualquer Linux com suporte a Docker |
| Rede | IP público fixo, portas 80 e 443 abertas no firewall | IP fixo na rede local (reserva DHCP ou estático) |
| Acesso SSH | Porta 22 liberada | Porta 22 liberada (ou acesso físico) |

> Máquinas recicladas, mini-PCs (Intel NUC, Raspberry Pi 4 com 4 GB+) e máquinas virtuais também funcionam como servidor local, desde que o SO seja Linux e o Docker esteja instalado.

---

## 2. Instalar Docker

```bash
# Atualizar pacotes
sudo apt update && sudo apt upgrade -y

# Instalar Docker
curl -fsSL https://get.docker.com | sudo sh

# Adicionar seu usuário ao grupo docker (dispensa sudo)
sudo usermod -aG docker $USER

# Reconectar SSH para o grupo ter efeito
exit
# (reconecte via SSH)

# Verificar instalação
docker --version
docker compose version
```

---

## 3. Criar estrutura de diretórios

```bash
mkdir -p ~/mapos/secrets
chmod 700 ~/mapos/secrets
cd ~/mapos
```

---

## 4. Criar o docker-compose.yml de produção

> **Diferença em relação ao repositório:** usa a imagem pré-compilada do GitHub Container Registry em vez de fazer build local — não é necessário ter o código-fonte no VPS.

Crie o arquivo `~/mapos/docker-compose.yml`:

```yaml
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
```

---

## 5. Configurar nginx.conf

Crie `~/mapos/nginx.conf` com HTTP simples primeiro (necessário para o Certbot validar o domínio):

```nginx
upstream app {
    server app:8080;
}

server {
    listen 80;
    server_name seudominio.com.br www.seudominio.com.br;

    client_max_body_size 20M;

    proxy_set_header Host              $host;
    proxy_set_header X-Real-IP         $remote_addr;
    proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;

    location / {
        proxy_pass http://app;
    }
}
```

> Substitua `seudominio.com.br` pelo domínio real apontando para o IP do VPS.

---

## 6. Configurar secrets

Crie cada arquivo de secret com as senhas reais (sem quebra de linha no final — o `printf` garante isso):

```bash
cd ~/mapos

# Defina suas senhas aqui
DB_PASS="senha_forte_do_banco"
ADMIN_PASS="senha_forte_do_admin"

# PostgreSQL
printf '%s' "Host=db;Port=5432;Database=mapos;Username=mapos_user;Password=${DB_PASS}" > secrets/postgres_connection
printf '%s' "${DB_PASS}"    > secrets/postgres_password
printf '%s' "${ADMIN_PASS}" > secrets/admin_password

# SMTP (exemplo Gmail com App Password)
printf '%s' "smtp.gmail.com"      > secrets/smtp_host
printf '%s' "587"                 > secrets/smtp_port
printf '%s' "true"                > secrets/smtp_ssl
printf '%s' "seuemail@gmail.com"  > secrets/smtp_username
printf '%s' "sua_senha_de_app"    > secrets/smtp_password
printf '%s' "seuemail@gmail.com"  > secrets/smtp_from

# MercadoPago (deixe vazio para desabilitar)
printf '%s' ""   > secrets/mp_access_token
printf '%s' "P3D" > secrets/mp_boleto_expiration

# Restringir permissões
chmod 600 secrets/*

# Confirmar
ls -la secrets/
```

---

## 7. HTTPS com Let's Encrypt (Certbot)

### 7.1 — Subir a stack com HTTP para o Certbot validar

```bash
cd ~/mapos
docker compose up -d
```

### 7.2 — Instalar Certbot no host

```bash
sudo apt install -y certbot
```

### 7.3 — Obter certificado (modo standalone temporário)

```bash
# Parar apenas o nginx para liberar a porta 80
docker compose stop nginx

# Obter certificado
sudo certbot certonly --standalone \
  -d seudominio.com.br \
  -d www.seudominio.com.br \
  --non-interactive \
  --agree-tos \
  -m seuemail@gmail.com

# Os certificados ficam em /etc/letsencrypt/live/seudominio.com.br/
```

### 7.4 — Atualizar nginx.conf com HTTPS

Substitua o conteúdo de `~/mapos/nginx.conf`:

```nginx
upstream app {
    server app:8080;
}

# Redirecionar HTTP → HTTPS
server {
    listen 80;
    server_name seudominio.com.br www.seudominio.com.br;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl;
    server_name seudominio.com.br www.seudominio.com.br;

    ssl_certificate     /etc/letsencrypt/live/seudominio.com.br/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/seudominio.com.br/privkey.pem;

    ssl_protocols       TLSv1.2 TLSv1.3;
    ssl_ciphers         HIGH:!aNULL:!MD5;

    client_max_body_size 20M;

    proxy_set_header Host              $host;
    proxy_set_header X-Real-IP         $remote_addr;
    proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;

    location / {
        proxy_pass http://app;
    }
}
```

### 7.5 — Reiniciar nginx

```bash
docker compose start nginx
```

### 7.6 — Renovação automática do certificado

Adicione um cron job para renovar antes do vencimento (90 dias):

```bash
sudo crontab -e
```

Adicione a linha:

```
0 3 * * * certbot renew --quiet && docker compose -f /home/SEU_USUARIO/mapos/docker-compose.yml restart nginx
```

> Substitua `SEU_USUARIO` pelo seu usuário no VPS.

---

## 8. Cloudflare — cache, proteção e HTTPS gerenciado

O Cloudflare atua como proxy reverso na nuvem: todo o tráfego passa pelos servidores dele antes de chegar ao VPS. Isso traz quatro benefícios imediatos sem custo no plano gratuito:

- **Oculta o IP real do VPS** — ataques volumétricos não chegam diretamente ao servidor.
- **WAF básico e proteção contra DDoS** — regras gerenciadas bloqueiam bots e exploits comuns.
- **CDN/cache de assets estáticos** — CSS, JS e imagens são servidos da borda mais próxima do visitante.
- **HTTPS automático** — o Cloudflare emite e renova o certificado TLS voltado ao visitante sem precisar de Certbot.

---

### 8.1 — Adicionar domínio ao Cloudflare

1. Acesse [dash.cloudflare.com](https://dash.cloudflare.com) e clique em **Add a Site**.
2. Informe o domínio (`seudominio.com.br`) e selecione o plano **Free**.
3. O Cloudflare listará os registros DNS existentes para importação — revise e confirme.
4. Troque os **nameservers** do registrador (Registro.br, GoDaddy, etc.) pelos fornecidos pelo Cloudflare. Ex.:
   ```
   ns1.cloudflare.com
   ns2.cloudflare.com
   ```
5. Aguarde a propagação (geralmente < 5 minutos; pode levar até 24 h).

---

### 8.2 — Registro DNS e modo proxy (nuvem laranja)

No painel **DNS → Records** crie um registro A apontando para o IP do VPS:

| Type | Name | Content | Proxy status |
|---|---|---|---|
| A | `@` | `203.0.113.10` | **Proxied** (nuvem laranja) |
| A | `www` | `203.0.113.10` | **Proxied** (nuvem laranja) |

> A nuvem laranja (Proxied) é o que ativa CDN, WAF e ocultação de IP. Nuvem cinza (DNS only) é roteamento direto, sem proteção.

---

### 8.3 — Modo SSL/TLS e HTTPS sem Certbot

Com o Cloudflare no modo proxy o certificado TLS para o visitante é emitido e gerenciado pelo próprio Cloudflare — **não é necessário instalar o Certbot nem configurar Let's Encrypt no VPS**.

A comunicação funciona em dois segmentos:

```
Visitante ──[HTTPS]──▶ Cloudflare ──[HTTP]──▶ nginx no VPS
```

#### Configurar o modo SSL

Em **SSL/TLS → Overview** selecione o modo **Flexible**:

| Modo | Trecho visitante → CF | Trecho CF → VPS | Quando usar |
|---|---|---|---|
| **Flexible** | HTTPS | HTTP | VPS sem certificado (mais simples) |
| Full | HTTPS | HTTPS (autoassinado aceito) | VPS com cert autoassinado |
| Full (strict) | HTTPS | HTTPS (CA válida) | VPS com cert Let's Encrypt |

> Para intranet com domínio público, o modo **Flexible** é suficiente: o VPS só precisa ouvir na porta 80, sem nenhum certificado local.

#### nginx.conf simplificado (sem TLS local)

```nginx
upstream app {
    server app:8080;
}

server {
    listen 80;
    server_name seudominio.com.br www.seudominio.com.br;

    client_max_body_size 20M;

    # Cloudflare envia o IP real do visitante neste header
    real_ip_header     CF-Connecting-IP;

    proxy_set_header Host              $host;
    proxy_set_header X-Real-IP         $remote_addr;
    proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;

    location / {
        proxy_pass http://app;
    }
}
```

#### Forçar HTTPS via Cloudflare (sem redirecionar no nginx)

Em **SSL/TLS → Edge Certificates** ative:

- **Always Use HTTPS** → ON
- **Automatic HTTPS Rewrites** → ON
- **Minimum TLS Version** → TLS 1.2

Isso garante que visitantes que acessem `http://` sejam redirecionados para `https://` pela borda do Cloudflare, antes mesmo de o tráfego chegar ao VPS.

---

### 8.4 — Rules: redirecionar porta 80 para a porta do container

Por padrão o Cloudflare roteia a porta 443 (HTTPS) do visitante para a **porta 80 do servidor de origem**. Se o nginx não estiver na porta 80 — por exemplo, se o container nginx estiver exposto na porta `8080` do host — crie uma **Origin Rule** para reescrever a porta de destino.

#### Criar a Origin Rule

1. No painel vá em **Rules → Origin Rules**.
2. Clique em **Create Rule**.
3. Configure:

   | Campo | Valor |
   |---|---|
   | **Rule name** | `Porta container mapos` |
   | **When incoming requests match** | `Hostname equals seudominio.com.br` |
   | **Destination Port** | `8080` (ou a porta exposta no `docker-compose.yml`) |

4. Salve e aguarde o deploy (< 30 s).

> Origin Rules estão disponíveis no plano **Free** com limite de 10 regras.

#### Exemplo visual da regra (formato Expression Editor)

```
(http.host eq "seudominio.com.br")
```

Ação: **Rewrite → Destination port → 8080**

#### Porta exposta no docker-compose.yml

Certifique-se de que o nginx no compose expõe a porta correspondente:

```yaml
  nginx:
    ports:
      - "8080:80"   # porta do host : porta interna do nginx
```

Com essa configuração:

```
Visitante :443 ──▶ Cloudflare ──▶ VPS :8080 ──▶ nginx ──▶ app :8080
```

---

### 8.5 — Cache de assets estáticos

Em **Caching → Configuration**:

| Configuração | Valor recomendado |
|---|---|
| **Caching Level** | Standard |
| **Browser Cache TTL** | 4 hours |
| **Always Online** | ON |

Para cachear assets com TTL longo crie uma **Cache Rule** em **Rules → Cache Rules**:

| Campo | Valor |
|---|---|
| **When** | `URI Path matches regex` `\.(css\|js\|woff2?\|png\|jpe?g\|svg\|ico\|webp)$` |
| **Cache status** | Override → **Cache** |
| **Edge TTL** | 1 month |
| **Browser TTL** | 1 day |

Para **limpar o cache** após um deploy:

```
Caching → Cache Purge → Purge Everything
```

Ou via API (útil em CI/CD):

```bash
curl -X POST "https://api.cloudflare.com/client/v4/zones/SEU_ZONE_ID/purge_cache" \
  -H "Authorization: Bearer SEU_API_TOKEN" \
  -H "Content-Type: application/json" \
  --data '{"purge_everything":true}'
```

---

### 8.6 — Proteção adicional (WAF e Security)

Em **Security → Settings**:

| Configuração | Valor recomendado |
|---|---|
| **Security Level** | Medium |
| **Bot Fight Mode** | ON |
| **Browser Integrity Check** | ON |

Em **Security → WAF → Managed Rules** ative o ruleset **Cloudflare Free Managed Ruleset** — bloqueia automaticamente tentativas de SQLi, XSS e path traversal.

Para bloquear acesso direto ao IP do VPS (bypassar o Cloudflare), adicione ao nginx a validação dos IPs de origem do Cloudflare:

```nginx
# Permite apenas tráfego vindo do Cloudflare
# Lista atualizada em: https://www.cloudflare.com/ips/
allow 173.245.48.0/20;
allow 103.21.244.0/22;
allow 103.22.200.0/22;
allow 103.31.4.0/22;
allow 141.101.64.0/18;
allow 108.162.192.0/18;
allow 190.93.240.0/20;
allow 188.114.96.0/20;
allow 197.234.240.0/22;
allow 198.41.128.0/17;
allow 162.158.0.0/15;
allow 104.16.0.0/13;
allow 104.24.0.0/14;
allow 172.64.0.0/13;
allow 131.0.72.0/22;
# IPv6
allow 2400:cb00::/32;
allow 2606:4700::/32;
allow 2803:f800::/32;
allow 2405:b500::/32;
allow 2405:8100::/32;
allow 2a06:98c0::/29;
allow 2c0f:f248::/32;
deny  all;
```

---

## 9. Uso em intranet (sem domínio público)

A aplicação pode ser executada inteiramente dentro de uma rede local (escritório, oficina, etc.) sem precisar de domínio público, servidor de e-mail externo ou acesso à internet. Nesse cenário o servidor pode ser um computador dedicado, um mini-PC ou uma máquina virtual na rede.

### 8.1 — Acesso direto por IP

A forma mais simples: qualquer dispositivo da rede acessa pelo IP fixo do servidor.

```nginx
# nginx.conf — intranet sem domínio
upstream app {
    server app:8080;
}

server {
    listen 80;
    server_name _;          # aceita qualquer hostname / IP

    client_max_body_size 20M;

    proxy_set_header Host              $host;
    proxy_set_header X-Real-IP         $remote_addr;
    proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;

    location / {
        proxy_pass http://app;
    }
}
```

Acesso: `http://192.168.1.100` (substituindo pelo IP real do servidor).

> **Dica:** configure um IP fixo (estático) no servidor ou reserve o IP no roteador via DHCP reservation para que o endereço nunca mude.

---

### 8.2 — Servidor DNS local para acesso por nome

Para acessar via `http://mapos.local` em vez de memorizar o IP, instale um servidor DNS leve na rede. A opção mais simples é o **Pi-hole** ou o **dnsmasq** direto no servidor da aplicação.

#### Opção A — dnsmasq (leve, só DNS/DHCP)

```bash
sudo apt install -y dnsmasq
```

Crie `/etc/dnsmasq.d/mapos.conf`:

```
# Resolve mapos.local para o IP do servidor
address=/mapos.local/192.168.1.100

# Opcional: redirecionar toda a rede para este DNS
# (configure o roteador para apontar para este servidor como DNS primário)
```

Reinicie o serviço:

```bash
sudo systemctl restart dnsmasq
sudo systemctl enable dnsmasq
```

Configure o **roteador** (ou gateway) para distribuir `192.168.1.100` como servidor DNS via DHCP — assim todos os dispositivos da rede resolvem `mapos.local` automaticamente, sem configuração individual.

#### Opção B — Pi-hole (DNS + bloqueio de anúncios)

O Pi-hole é uma boa escolha se a rede já usa ou quer usar bloqueio de rastreadores.

```bash
curl -sSL https://install.pi-hole.net | bash
```

Após a instalação, adicione o registro local em **Local DNS Records** na interface web do Pi-hole (`http://192.168.1.100/admin`):

| Domain | IP |
|---|---|
| `mapos.local` | `192.168.1.100` |

#### Verificar resolução DNS

Em qualquer máquina da rede (após configurar o DNS):

```bash
nslookup mapos.local
# ou
ping mapos.local
```

---

### 8.3 — HTTPS na intranet com certificado autoassinado (opcional)

Sem um domínio público o Let's Encrypt não funciona. Para ter HTTPS na intranet gere um certificado autoassinado:

```bash
sudo openssl req -x509 -nodes -days 3650 -newkey rsa:2048 \
  -keyout /etc/ssl/private/mapos.key \
  -out    /etc/ssl/certs/mapos.crt \
  -subj "/CN=mapos.local/O=Intranet/C=BR" \
  -addext "subjectAltName=DNS:mapos.local,IP:192.168.1.100"
```

Atualize o `nginx.conf`:

```nginx
upstream app {
    server app:8080;
}

server {
    listen 80;
    server_name mapos.local;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl;
    server_name mapos.local;

    ssl_certificate     /etc/ssl/certs/mapos.crt;
    ssl_certificate_key /etc/ssl/private/mapos.key;

    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers   HIGH:!aNULL:!MD5;

    client_max_body_size 20M;

    proxy_set_header Host              $host;
    proxy_set_header X-Real-IP         $remote_addr;
    proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;

    location / {
        proxy_pass http://app;
    }
}
```

Monte os certificados no container nginx — adicione ao `docker-compose.yml`:

```yaml
  nginx:
    volumes:
      - ./nginx.conf:/etc/nginx/conf.d/default.conf:ro
      - /etc/ssl/certs/mapos.crt:/etc/ssl/certs/mapos.crt:ro
      - /etc/ssl/private/mapos.key:/etc/ssl/private/mapos.key:ro
```

> O navegador exibirá aviso de "certificado não confiável" porque não é emitido por uma CA pública. Para eliminar o aviso, importe o arquivo `mapos.crt` como **CA raiz confiável** nos dispositivos da rede (Windows: `certmgr.msc` → Autoridades de Certificação Raiz Confiáveis; macOS: Keychain Access).

---

## 10. Subir a stack

```bash
cd ~/mapos

# Baixar a imagem mais recente
docker pull ghcr.io/allanbarcelos/mapos-dotnet:latest

# Subir em background
docker compose up -d

# Acompanhar logs na inicialização
docker compose logs -f
```

A aplicação estará disponível em `https://seudominio.com.br`.

---

## 11. Verificar saúde dos serviços

```bash
# Status de todos os containers
docker compose ps

# Health check da aplicação
curl -sf http://localhost:8080/health || echo "app indisponível"

# Logs de um serviço específico
docker compose logs app
docker compose logs db
docker compose logs nginx
```

---

## 12. Atualizar para nova versão

```bash
cd ~/mapos

# Baixar nova imagem
docker pull ghcr.io/allanbarcelos/mapos-dotnet:latest

# Recriar apenas o container da aplicação (zero downtime do banco)
docker compose up -d --no-deps app

# Verificar se subiu corretamente
docker compose ps
docker compose logs app --tail=50
```

---

## 13. Comandos de manutenção

```bash
# Parar a stack (mantém dados)
docker compose down

# Reiniciar um serviço
docker compose restart app

# Abrir shell psql no banco
docker compose exec db psql -U mapos_user mapos

# Ver uso de recursos
docker stats

# Limpar imagens antigas não utilizadas
docker image prune -f
```

---

## 14. Backup do banco de dados

### Backup manual

```bash
docker compose exec db pg_dump -U mapos_user mapos \
  | gzip > ~/backups/mapos_$(date +%Y%m%d_%H%M%S).sql.gz
```

### Backup automático via cron

```bash
mkdir -p ~/backups
crontab -e
```

Adicione:

```
0 2 * * * docker compose -f /home/SEU_USUARIO/mapos/docker-compose.yml exec -T db pg_dump -U mapos_user mapos | gzip > /home/SEU_USUARIO/backups/mapos_$(date +\%Y\%m\%d).sql.gz
```

### Restaurar backup

```bash
gunzip -c ~/backups/mapos_YYYYMMDD_HHMMSS.sql.gz \
  | docker compose exec -T db psql -U mapos_user mapos
```
