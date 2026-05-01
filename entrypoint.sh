#!/bin/sh
# Carrega Docker secrets como variáveis de ambiente do ASP.NET Core.

load_secret() {
    local file="/run/secrets/$1"
    local env_var="$2"
    if [ -f "$file" ]; then
        export "$env_var"="$(cat "$file")"
    fi
}

# ConnectionStrings:DefaultConnection (notação __ = hierarquia de config)
load_secret postgres_connection "ConnectionStrings__DefaultConnection"

# Senha do admin para o seed inicial
load_secret admin_password "MAPOS_ADMIN_PASSWORD"

# Configurações SMTP para envio da fila de e-mails
load_secret smtp_host     "Smtp__Host"
load_secret smtp_port     "Smtp__Port"
load_secret smtp_ssl      "Smtp__EnableSsl"
load_secret smtp_username "Smtp__Username"
load_secret smtp_password "Smtp__Password"
load_secret smtp_from     "Smtp__From"

# Credenciais MercadoPago (opcionais — gateway desabilitado se ausente)
load_secret mp_access_token      "MercadoPago__AccessToken"
load_secret mp_boleto_expiration "MercadoPago__BoletoExpiration"

exec dotnet mapos-dotnet.dll "$@"
