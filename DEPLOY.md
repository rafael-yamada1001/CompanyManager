# Deploy — CompanyManager

Guia de publicação para o ambiente de produção (AWS Lightsail · Ubuntu · Docker).

---

## Infraestrutura

| Item | Valor |
|---|---|
| Provedor | AWS Lightsail |
| SO | Ubuntu |
| Projeto no servidor | `/root/CompanyManager` |
| Porta exposta | `8080` |
| Banco de dados | SQLite — volume Docker `db_data` (persistente) |

---

## Pré-requisitos (já configurado)

- Docker instalado (`docker.service` ativo)
- Docker Compose v2+ integrado (`docker compose`)
- Arquivo `.env` em `/root/CompanyManager/.env` com o `JWT_SECRET`

---

## Publicar uma atualização

### 1. Na máquina local — commitar e enviar as mudanças

```bash
git add <arquivos-alterados>
git commit -m "descrição da mudança"
git push origin main
```

### 2. No servidor — conectar via SSH

```bash
ssh ubuntu@<ip-do-lightsail>
```

### 3. No servidor — entrar como root e atualizar

```bash
sudo -i
cd /root/CompanyManager
git pull origin main
docker compose up --build -d
```

O `--build` recompila a imagem com o código novo.  
O `-d` mantém rodando em background.  
O volume `db_data` **não é afetado** — nenhum dado do banco é perdido.

### 4. Verificar se subiu corretamente

```bash
docker compose logs --tail=50
curl http://localhost:8080/health
```

Resposta esperada do health check: `Healthy`

---

## Primeiro deploy (servidor zerado)

```bash
# 1. Clonar o repositório
sudo -i
cd /root
git clone https://github.com/rafael-yamada1001/CompanyManager.git
cd CompanyManager

# 2. Criar o arquivo de variáveis de ambiente
cp .env.example .env
nano .env
# Preencher: JWT_SECRET=<chave-forte-min-32-chars>
# Gerar chave: openssl rand -base64 48

# 3. Subir
docker compose up --build -d
```

---

## Comandos úteis no servidor

```bash
# Ver containers rodando
docker ps

# Ver logs em tempo real
docker compose logs -f

# Reiniciar sem rebuild (só restart)
docker compose restart

# Parar tudo
docker compose down

# Ver uso de recursos
docker stats companymanager
```

---

## Observações importantes

- **Nunca commitar o `.env`** — ele contém o `JWT_SECRET` e não está no `.gitignore` por padrão no servidor.
- Alterações apenas no `wwwroot/` (HTML/JS/CSS) entram em vigor com o rebuild — não é necessário reiniciar manualmente.
- Alterações em arquivos `.cs` exigem rebuild (`docker compose up --build -d`) para compilar.
- O container aparece como `unhealthy` se o endpoint `/health` demorar mais de 10s para responder nos primeiros 20s — isso é normal na inicialização fria.
