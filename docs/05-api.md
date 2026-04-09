# 05 — Camada de API (Controllers e Program.cs)

---

## `Program.cs` — Configuração e Pipeline

### Serviços registrados

```csharp
builder.Services.AddControllers()  // controllers MVC
builder.Services.AddRateLimiter()  // rate limiting
builder.Services.AddEndpointsApiExplorer() + AddSwaggerGen()  // documentação Swagger
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)  // JWT
builder.Services.AddAuthorization()
builder.Services.AddHealthChecks()
builder.Services.AddInfrastructure(configuration)  // todos os repos, services e segurança
```

### Configuração do Rate Limiter

```csharp
options.AddFixedWindowLimiter("login", o =>
{
    o.Window      = TimeSpan.FromMinutes(1);
    o.PermitLimit = 10;   // máx 10 requisições por minuto por IP
    o.QueueLimit  = 0;    // sem fila — rejeita imediatamente se exceder
});
```

Aplicado apenas à rota `POST /auth/login` via atributo `[EnableRateLimiting("login")]`. Responde com HTTP 429 e JSON `{"error":"too_many_requests","message":"..."}`.

### Configuração do JWT

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer           = true,  // verifica o campo "iss" do token
    ValidateAudience         = true,  // verifica o campo "aud" do token
    ValidateLifetime         = true,  // rejeita tokens expirados
    ValidateIssuerSigningKey = true,  // verifica a assinatura HMAC
    ValidIssuer   = jwtSettings.Issuer,
    ValidAudience = jwtSettings.Audience,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
};
```

### Formato de erro de validação

O ASP.NET Core por padrão retorna erros de model binding em formato `ProblemDetails`. O projeto substitui isso por um formato mais simples:

```json
{
  "error": "validation_error",
  "message": "Um ou mais campos são inválidos.",
  "fields": [
    { "field": "Email", "message": "The Email field is required." }
  ]
}
```

### Pipeline de middlewares (ordem importa)

```csharp
app.UseMiddleware<ExceptionMiddleware>()   // 1. captura exceções de todo o pipeline
app.UseSwagger() + app.UseSwaggerUI()      // 2. apenas em desenvolvimento
app.UseHsts()                              // 3. header HSTS em produção
app.UseDefaultFiles()                      // 4. serve index.html como padrão
app.UseStaticFiles()                       // 5. serve wwwroot/ (dashboard.html, etc.)
app.UseRateLimiter()                       // 6. aplica limites de taxa
app.UseAuthentication()                    // 7. processa o header Authorization
app.UseAuthorization()                     // 8. verifica permissões
app.MapControllers()                       // 9. roteia para controllers
app.MapHealthChecks("/health")             // 10. endpoint de health check
```

**Por que a ordem importa:**
- `ExceptionMiddleware` vem primeiro para capturar exceções de qualquer middleware abaixo
- `UseAuthentication` deve vir antes de `UseAuthorization`
- `UseStaticFiles` antes dos controllers para servir o dashboard sem passar pelo pipeline de auth
- O `ExceptionMiddleware` vem antes de `UseStaticFiles` também — garante que qualquer erro seja formatado corretamente

### Health Check

`GET /health` retorna `"Healthy"` com HTTP 200 se a aplicação está respondendo. Usado pelo Docker Compose (`healthcheck.test`) para verificar se o container está saudável.

---

## `ExceptionMiddleware`

Captura exceções de todo o pipeline e as converte em respostas JSON padronizadas.

| Exceção | HTTP Status | Exemplo de resposta |
|---------|-------------|---------------------|
| `DomainException` com code "not found" | 404 | `{"error":"user_not_found","message":"Usuário não encontrado."}` |
| `DomainException` outros | 400 | `{"error":"email_in_use","message":"E-mail já cadastrado."}` |
| `UnauthorizedAccessException` | 403 | `{"error":"forbidden","message":"Sem permissão..."}` |
| `Exception` genérica | 500 | Em dev: mensagem real; em prod: mensagem genérica |

Códigos mapeados para 404: `"user_not_found"`, `"department_not_found"`, `"item_not_found"`, `"technician_not_found"`, `"schedule_not_found"`.

---

## Controllers

Todos os controllers usam:
- `[ApiController]` — habilita binding automático e validação do model state
- `[Route("...")]` — define o prefixo da rota
- `[Authorize]` — exige JWT válido em todos os endpoints (a menos que sobrescrito)

### `AuthController`

**Rota base:** `/auth`

| Método | Rota | Atributos | Descrição |
|--------|------|-----------|-----------|
| `POST` | `/auth/login` | `[EnableRateLimiting("login")]` | Login com e-mail e senha, retorna JWT |

**Sem `[Authorize]`** — é a única rota pública do sistema.

**Rate Limiting:** máx 10 tentativas por minuto por IP.

**Request:** `{ "email": "...", "password": "..." }`

**Resposta 200:** `{ "token": "eyJ...", "expiresIn": 7200 }`

**Respostas de erro:**
- 401: credenciais inválidas, usuário não encontrado ou bloqueado
- 429: muitas tentativas

---

### `UsersController`

**Rota base:** `/users`
**Autenticação:** `[Authorize]` (todos os endpoints exigem JWT)

| Método | Rota | Autorização | Descrição |
|--------|------|-------------|-----------|
| `GET` | `/users` | admin | Lista todos os usuários com permissões |
| `GET` | `/users/{id}` | admin | Busca usuário por ID |
| `GET` | `/users/me/permissions` | qualquer usuário logado | Permissões do próprio usuário |
| `POST` | `/users` | admin | Cria novo usuário |
| `PUT` | `/users/{id}` | admin | Atualiza role e/ou senha |
| `PATCH` | `/users/{id}/unblock` | admin | Desbloqueia usuário |
| `DELETE` | `/users/{id}` | admin | Exclui usuário |
| `POST` | `/users/{id}/permissions` | admin | Define/atualiza permissão em um departamento |
| `DELETE` | `/users/{id}/permissions/{departmentId}` | admin | Remove permissão |
| `PATCH` | `/users/{id}/technician-access` | admin | Alterna acesso à aba de técnicos |

**`GET /users/me/permissions`** — o ID do usuário é extraído do JWT:
```csharp
var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
```
`ClaimTypes.NameIdentifier` corresponde ao claim `sub` (Subject) do JWT.

**`PATCH /users/{id}/technician-access`** — body: `{ "hasAccess": true }`
Define no banco o campo `HasTechnicianAccess`. Na próxima vez que o usuário fizer login, o token JWT incluirá `tech_access: "true"`.

---

### `DepartmentsController`

**Rota base:** `/departments`
**Autenticação:** `[Authorize]`

| Método | Rota | Autorização | Descrição |
|--------|------|-------------|-----------|
| `GET` | `/departments` | qualquer logado | Lista departamentos (admin vê todos; outros veem apenas os que têm permissão) |
| `GET` | `/departments/{id}` | Visualizar no depto | Detalhe do departamento com contagens |
| `POST` | `/departments` | admin | Cria departamento |
| `PUT` | `/departments/{id}` | Gerenciar no depto | Atualiza nome/descrição |
| `DELETE` | `/departments/{id}` | admin | Exclui departamento |

**Lógica de acesso em `GET /departments`:**

```csharp
if (IsAdmin()) return Ok(all);  // admin vê tudo

var perms = await _permissions.GetByUserAsync(userId);
var allowed = perms.Select(p => p.DepartmentId).ToHashSet();
return Ok(all.Where(d => allowed.Contains(d.Id)));  // usuário comum vê apenas os seus
```

**`RequireAccess(departmentId, minLevel)` (helper privado):**
- Se admin, passa imediatamente
- Caso contrário, chama `HasAccessAsync` — se retornar false, lança `UnauthorizedAccessException` (capturado pelo middleware como HTTP 403)

---

### `ItemsController`

**Rota base:** `/departments/{departmentId}/items` (rota aninhada)
**Autenticação:** `[Authorize]`

Os itens são sempre acessados no contexto de um departamento — o `departmentId` faz parte da URL.

| Método | Rota | Permissão mínima | Descrição |
|--------|------|-----------------|-----------|
| `GET` | `/departments/{depId}/items` | Visualizar | Lista itens do departamento |
| `POST` | `/departments/{depId}/items` | Editar | Cria item no departamento |
| `PUT` | `/departments/{depId}/items/{itemId}` | Editar | Atualiza dados do item |
| `POST` | `/departments/{depId}/items/{itemId}/move` | Editar | Move item (estoque/campo/manutenção) |
| `DELETE` | `/departments/{depId}/items/{itemId}` | Gerenciar | Exclui item |

**Níveis de permissão por operação:**
- Visualizar — leitura (GET)
- Editar — criação e movimentação (POST, PUT)
- Gerenciar — exclusão (DELETE) — o nível mais alto

---

### `TechniciansController`

**Rota base:** `/technicians`
**Autenticação:** `[Authorize]`

Este controller usa uma verificação customizada `RequireTechnicianAccess()` em vez de `[Authorize(Roles)]`:

```csharp
private void RequireTechnicianAccess()
{
    var hasClaim = User.FindFirstValue("tech_access");
    if (hasClaim != "true")
        throw new UnauthorizedAccessException("Acesso à gestão de técnicos não autorizado.");
}
```

Verifica o claim `tech_access` do JWT (que é `"true"` se o usuário é admin ou tem `HasTechnicianAccess = true`).

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET` | `/technicians` | Lista técnicos (exclui "A Definir") |
| `GET` | `/technicians/a-definir-id` | Retorna o ID fixo do técnico "A Definir" |
| `POST` | `/technicians` | Cria técnico |
| `PUT` | `/technicians/{id}` | Atualiza técnico |
| `DELETE` | `/technicians/{id}` | Exclui técnico |
| `GET` | `/technicians/{id}/schedule` | Agenda completa de um técnico |
| `POST` | `/technicians/{id}/schedule` | Adiciona entrada na agenda |
| `PUT` | `/technicians/schedule/{scheduleId}` | Atualiza entrada (sem trocar técnico) |
| `DELETE` | `/technicians/schedule/{scheduleId}` | Remove entrada |

**Observação sobre `GET /technicians/a-definir-id`:** Esta rota deve vir antes de `GET /technicians/{id:guid}` no roteamento. Como a rota não tem parâmetro guid, o ASP.NET Core a resolve corretamente como endpoint literal.

**Troca de técnico em agendamento:** A API não tem um endpoint direto para "trocar o técnico de um agendamento". O `PUT /technicians/schedule/{scheduleId}` atualiza campos mas mantém o `TechnicianId` original. A troca é implementada no frontend: DELETE na entrada antiga + POST no novo técnico.

---

## Resumo dos Status HTTP usados

| Status | Significado |
|--------|-------------|
| 200 OK | Sucesso com body |
| 201 Created | Recurso criado (com Location header) |
| 204 No Content | Sucesso sem body (PATCH, DELETE) |
| 400 Bad Request | Erro de validação ou regra de negócio |
| 401 Unauthorized | Token inválido, expirado ou ausente |
| 403 Forbidden | Token válido mas sem permissão para o recurso |
| 404 Not Found | Recurso não encontrado |
| 429 Too Many Requests | Rate limit atingido |
| 500 Internal Server Error | Erro inesperado |
