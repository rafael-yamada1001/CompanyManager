# 07 — Deploy e Operações

---

## Dockerfile — Build Multi-Stage

O Dockerfile usa dois stages para produzir uma imagem final pequena e segura:

```dockerfile
# Stage 1: BUILD
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia apenas os .csproj primeiro (aproveita cache do Docker)
COPY src/CompanyManager.Domain/CompanyManager.Domain.csproj           src/CompanyManager.Domain/
COPY src/CompanyManager.Application/CompanyManager.Application.csproj src/CompanyManager.Application/
COPY src/CompanyManager.Infrastructure/CompanyManager.Infrastructure.csproj src/CompanyManager.Infrastructure/
COPY src/CompanyManager.API/CompanyManager.API.csproj                 src/CompanyManager.API/
RUN dotnet restore src/CompanyManager.API/CompanyManager.API.csproj

# Copia o restante e publica
COPY . .
RUN dotnet publish src/CompanyManager.API/CompanyManager.API.csproj \
    -c Release -o /app/publish --no-restore

# Stage 2: RUNTIME
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN mkdir -p /var/data && chmod 777 /var/data  # diretório do SQLite
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CompanyManager.API.dll"]
```

**Por que dois stages?**
- Stage 1 usa a imagem `sdk` (~700MB) que tem compilador, dotnet CLI etc.
- Stage 2 usa a imagem `aspnet` (~220MB) que só tem o runtime
- A imagem final tem apenas o binário compilado — sem código-fonte, sem compilador, sem pacotes NuGet
- Resultado: imagem menor e superfície de ataque reduzida

**Cache de camadas:**
Copiar os `.csproj` antes do código-fonte é uma otimização: o `dotnet restore` só roda novamente se os arquivos de projeto mudarem. Se só o código mudou, o cache do restore é reaproveitado.

**`/var/data`:** Diretório criado para armazenar o arquivo SQLite. Precisa ser mapeado como volume para persistir entre reinicializações.

---

## `docker-compose.yml`

```yaml
services:
  companymanager:
    build: .
    container_name: companymanager
    restart: unless-stopped
    ports:
      - "8080:8080"
    volumes:
      - db_data:/var/data      # banco SQLite persistente
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Jwt__Secret=${JWT_SECRET}  # lido do arquivo .env
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 20s

volumes:
  db_data:   # volume Docker gerenciado — sobrevive a `docker compose down`
```

### Variáveis de ambiente

| Variável | Descrição |
|----------|-----------|
| `ASPNETCORE_ENVIRONMENT` | Define o ambiente (`Production` desativa Swagger e logs detalhados) |
| `Jwt__Secret` | Chave secreta do JWT. A notação `__` (duplo underscore) é como o ASP.NET Core lê seções aninhadas (`Jwt:Secret`) a partir de variáveis de ambiente |

**`${JWT_SECRET}`** é lido de um arquivo `.env` na mesma pasta do `docker-compose.yml`. Nunca deve ser commitado no repositório.

### Restart policy

`restart: unless-stopped` — o container reinicia automaticamente se cair (crash, reinicialização do sistema etc.), exceto se o usuário parar manualmente com `docker compose stop`.

### Health check

O Docker verifica a saúde do container a cada 30 segundos chamando `/health`. Após 3 falhas consecutivas, o container é marcado como "unhealthy". O `start_period: 20s` dá tempo para o container subir e o seeder rodar antes das primeiras verificações.

---

## `nginx.conf` — Proxy Reverso

```nginx
server {
    listen 80;
    server_name SEU_DOMINIO.com www.SEU_DOMINIO.com;
    return 301 https://$host$request_uri;  # redireciona HTTP → HTTPS
}

server {
    listen 443 ssl;
    server_name SEU_DOMINIO.com www.SEU_DOMINIO.com;

    ssl_certificate     /etc/letsencrypt/live/SEU_DOMINIO.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/SEU_DOMINIO.com/privkey.pem;
    ssl_protocols       TLSv1.2 TLSv1.3;
    ssl_ciphers         HIGH:!aNULL:!MD5;

    location / {
        proxy_pass         http://localhost:8080;  # encaminha para o container
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```

**Função do Nginx:**
- Termina o TLS (HTTPS) — o container recebe apenas HTTP na porta 8080
- Redireciona todo HTTP para HTTPS (301 permanente)
- Encaminha requests para o container via `proxy_pass`
- Passa headers importantes (`X-Real-IP`, `X-Forwarded-For`) para que o ASP.NET Core saiba o IP real do cliente (importante para o rate limiter)

**Por que o container não faz HTTPS diretamente?** Gerenciar certificados SSL dentro do container é complexo. O Nginx fica fora do container, gerencia os certificados com Certbot/Let's Encrypt e o container só precisa lidar com HTTP simples.

**`proxy_set_header X-Forwarded-Proto $scheme`** — informa ao ASP.NET Core que a requisição original foi HTTPS. Necessário para que `app.UseHsts()` funcione corretamente.

---

## AWS Lightsail — Setup Inicial

Passos resumidos para subir o sistema do zero:

1. **Criar instância Lightsail** — Ubuntu 22.04 LTS (plano mínimo $3.50/mês para teste)

2. **Instalar dependências**
   ```bash
   sudo apt update && sudo apt upgrade -y
   sudo apt install -y docker.io docker-compose-plugin nginx certbot python3-certbot-nginx git
   sudo systemctl enable docker && sudo usermod -aG docker $USER
   ```

3. **Clonar o repositório**
   ```bash
   git clone https://github.com/SEU_USUARIO/CompanyManager.git /opt/companymanager
   cd /opt/companymanager
   ```

4. **Criar o arquivo `.env`**
   ```bash
   echo "JWT_SECRET=$(openssl rand -base64 32)" > .env
   # Guarde este valor em local seguro!
   ```

5. **Configurar o Nginx**
   ```bash
   sudo cp nginx.conf /etc/nginx/sites-available/companymanager
   # Editar e substituir SEU_DOMINIO.com pelo domínio real
   sudo nano /etc/nginx/sites-available/companymanager
   sudo ln -s /etc/nginx/sites-available/companymanager /etc/nginx/sites-enabled/
   sudo nginx -t && sudo systemctl reload nginx
   ```

6. **Emitir certificado SSL** (necessita que o domínio já aponte para o servidor)
   ```bash
   sudo certbot --nginx -d seudominio.com -d www.seudominio.com
   ```

7. **Subir o container**
   ```bash
   docker compose up -d --build
   ```

8. **Verificar**
   ```bash
   docker compose logs -f          # ver logs em tempo real
   docker compose ps               # verificar status
   curl http://localhost:8080/health  # testar health check
   ```

---

## Atualizar o Servidor

Quando houver mudanças no código:

```bash
cd /opt/companymanager
git pull origin main
docker compose up -d --build
```

O `--build` reconstrói a imagem com o novo código. O SQLite não é afetado — o volume `db_data` persiste. As novas migrações são aplicadas automaticamente pelo `DatabaseSeeder.EnsureMigrationsReadyAsync()` na inicialização.

**Downtime:** Haverá alguns segundos de indisponibilidade enquanto o container antigo para e o novo sobe. Para zero downtime seria necessário uma estratégia mais elaborada (não implementada).

---

## Backup do Banco de Dados

O SQLite está em um volume Docker em `/var/data/companymanager.db`. Para fazer backup:

```bash
# Localizar o volume no sistema de arquivos do host
docker inspect companymanager | grep -i source

# Copiar o arquivo (enquanto o container está rodando — SQLite suporta hot backup)
docker cp companymanager:/var/data/companymanager.db ~/backup_$(date +%Y%m%d).db

# Ou via volume diretamente:
sudo cp /var/lib/docker/volumes/companymanager_db_data/_data/companymanager.db ~/backup_$(date +%Y%m%d).db
```

**Para restaurar:**
```bash
docker compose stop
sudo cp ~/backup_20260101.db /var/lib/docker/volumes/companymanager_db_data/_data/companymanager.db
docker compose start
```

**Automação com cron** (exemplo — backup diário às 3h):
```bash
0 3 * * * docker cp companymanager:/var/data/companymanager.db /backups/db_$(date +\%Y\%m\%d).db
```

---

## A variável `JWT_SECRET`

### O que é

A chave secreta usada para assinar e verificar tokens JWT com HMAC-SHA256. Se um atacante obtiver essa chave, poderá gerar tokens válidos para qualquer usuário.

### Onde é usada

1. **`.env`** no servidor: `JWT_SECRET=...`
2. **`docker-compose.yml`**: `Jwt__Secret=${JWT_SECRET}` — passa para o container como variável de ambiente
3. **`appsettings.json`** (config base): define a estrutura `Jwt:Secret` mas sem o valor real
4. **`Program.cs`** e **`JwtTokenService`**: leem via `builder.Configuration.GetSection("Jwt").Get<JwtSettings>()`

### Boas práticas

- Nunca commitar o `.env` no git (está no `.gitignore`)
- Usar pelo menos 32 bytes aleatórios: `openssl rand -base64 32`
- Rotacionar periodicamente — ao trocar, todos os tokens existentes se tornam inválidos e os usuários precisam fazer login novamente
- Em produção, considerar usar AWS Secrets Manager ou variáveis de ambiente do Lightsail em vez de arquivo `.env`

---

## Resumo de Comandos Úteis

```bash
# Ver logs do container
docker compose logs -f

# Reiniciar sem rebuild
docker compose restart

# Parar e remover containers (dados no volume são preservados)
docker compose down

# Parar e remover containers E volumes (APAGA O BANCO)
docker compose down -v

# Entrar no container
docker exec -it companymanager bash

# Ver status do health check
docker inspect companymanager | grep -A5 '"Health"'

# Verificar uso de disco do volume
docker system df -v | grep db_data
```
