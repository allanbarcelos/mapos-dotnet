# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install Node.js for Tailwind CSS build
RUN apt-get update && apt-get install -y --no-install-recommends nodejs npm \
    && rm -rf /var/lib/apt/lists/*

# Restore .NET dependencies (layer cache)
COPY mapos-dotnet.csproj .
RUN dotnet restore mapos-dotnet.csproj

# Copy source and build Tailwind CSS
COPY . .
RUN npm ci && npm run build:css

# Publish — skip MSBuild Tailwind step (already built above)
RUN SKIP_TAILWIND=true dotnet publish mapos-dotnet.csproj -c Release \
    /p:DebugType=none \
    /p:DebugSymbols=false \
    -o /app/publish

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# curl for health check
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["/entrypoint.sh"]
