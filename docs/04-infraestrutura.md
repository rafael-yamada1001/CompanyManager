# 04 — Camada de Infraestrutura

A camada de infraestrutura (`CompanyManager.Infrastructure`) contém todas as implementações concretas dos contratos (ports) definidos pela Application. É aqui que vivem o banco de dados, a segurança e as migrações.

---

## Registro de Dependências (`DependencyInjection.cs`)

O método de extensão `AddInfrastructure()` é chamado no `Program.cs` e registra tudo no contêiner de DI do ASP.NET Core:

```csharp
// Configuração JWT
services.Configure<JwtSettings>(configuration.GetSection("Jwt").Bind);

// Banco de dados
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

// Repositórios (Scoped = uma instância por request HTTP)
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IDepartmentRepository, DepartmentRepository>();
services.AddScoped<ITechnicianRepository, TechnicianRepository>();
services.AddScoped<ITechnicianScheduleRepository, TechnicianScheduleRepository>();
services.AddScoped<IItemRepository, ItemRepository>();
services.AddScoped<IPermissionRepository, PermissionRepository>();

// Segurança
services.AddScoped<ITokenService, JwtTokenService>();
services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

// Services de aplicação
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<DepartmentService>();
services.AddScoped<ItemService>();
services.AddScoped<UserService>();
services.AddScoped<TechnicianService>();

services.AddScoped<DatabaseSeeder>();
```

**Por que `Scoped`?** No ASP.NET Core, Scoped significa que a instância vive durante um request HTTP e é descartada ao final. Isso é ideal para o `AppDbContext` do EF Core, que mantém um cache (change tracker) durante o request.

---

## `AppDbContext`

Classe central do EF Core. Herda `DbContext` e define:
- Os `DbSet<T>` — representam as tabelas
- As configurações de mapeamento em `OnModelCreating`

### DbSets (tabelas)

```csharp
public DbSet<User>                     Users
public DbSet<Department>               Departments
public DbSet<Technician>               Technicians
public DbSet<TechnicianSchedule>       TechnicianSchedules
public DbSet<Item>                     Items
public DbSet<UserDepartmentPermission> UserDepartmentPermissions
```

### Configurações por entidade (`OnModelCreating`)

#### Tabela `Users`

| Configuração | Significado |
|--------------|-------------|
| `HasKey(u => u.Id)` | Chave primária |
| `HasMaxLength(256)` no Email | Limita tamanho no banco |
| `HasIndex(u => u.Email).IsUnique()` | Index único — garante que não haja dois usuários com o mesmo e-mail |
| `HasDefaultValue("user")` no Role | Valor padrão no banco se não informado |
| `HasDefaultValue(false)` em IsBlocked, HasTechnicianAccess | Valores padrão `false` |
| `HasDefaultValue(0)` em FailedLoginAttempts | Padrão zero |
| `IsRequired(false)` em LastLoginAt | Coluna nullable (permite `null`) |

#### Tabela `Technicians`

| Configuração | Significado |
|--------------|-------------|
| `HasMaxLength(150)` no Name | Limite de tamanho |
| `HasMaxLength(30)` no Phone | Suficiente para `(11) 99999-0000` |
| `HasMaxLength(100)` no Region | Limite para nome de região |

#### Tabela `TechnicianSchedules`

| Configuração | Significado |
|--------------|-------------|
| `HasMaxLength(200)` em Title e Client | Limite de tamanho |
| `HasMaxLength(30)` em Status | Suficiente para `"em_andamento"` |
| `HasDefaultValue("confirmado")` em Status | Status padrão |
| `HasIndex(s => s.TechnicianId)` | Index para acelerar consultas por técnico |

#### Tabela `Items`

| Configuração | Significado |
|--------------|-------------|
| `HasConversion<string>()` em Location | Armazena o enum como string no SQLite (`"Estoque"`, `"Campo"`, `"Manutencao"`) |
| `HasDefaultValue(ItemLocation.Estoque)` | Padrão: estoque |
| `HasIndex(i => i.DepartmentId)` | Index para acelerar consultas por departamento |

#### Tabela `UserDepartmentPermissions`

| Configuração | Significado |
|--------------|-------------|
| `HasKey(p => new { p.UserId, p.DepartmentId })` | Chave primária composta |
| `HasConversion<string>()` em Level | Armazena `"Visualizar"`, `"Editar"` ou `"Gerenciar"` como string |

---

## `DatabaseSeeder`

Executado na inicialização da aplicação (em `Program.cs`, antes do pipeline HTTP). Garante que o banco esteja no estado correto.

### `SeedAsync()` — ponto de entrada

```csharp
public async Task SeedAsync()
{
    await EnsureMigrationsReadyAsync();    // 1. garante schema correto
    await EnsureADefinirTechnicianAsync(); // 2. garante técnico especial
    if (await _context.Users.AnyAsync())   // 3. seed de usuários apenas se banco vazio
        return;
    // cria 3 usuários padrão...
}
```

### `EnsureMigrationsReadyAsync()` — lógica de segurança do schema

Esta função resolve um problema clássico de deploy: o banco pode existir mas ter sido criado de formas diferentes.

```
┌─ Banco não existe?
│     └─► cria via MigrateAsync() (migrations EF Core)
│
├─ Banco existe SEM tabela __EFMigrationsHistory?
│     └─► foi criado por EnsureCreated() (sem migrações)
│         └─► apaga e recria via MigrateAsync()
│             (PERDA DE DADOS — só ocorre na transição inicial)
│
└─ Banco existe COM __EFMigrationsHistory?
      └─► aplica apenas migrações pendentes (sem perda de dados)
```

A verificação da tabela de histórico:
```csharp
cmd.CommandText =
    "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
```
Consulta direta ao `sqlite_master` para verificar se a tabela existe.

### `EnsureADefinirTechnicianAsync()`

```csharp
private async Task EnsureADefinirTechnicianAsync()
{
    var exists = await _context.Technicians.AnyAsync(t => t.Id == ADefinirTechnicianId);
    if (!exists)
    {
        var aDefinir = new Technician(ADefinirTechnicianId, "A Definir", null, null);
        _context.Technicians.Add(aDefinir);
        await _context.SaveChangesAsync();
    }
}
```

O ID `00000000-0000-0000-0000-000000000001` é fixo e hardcoded tanto aqui quanto no `TechnicianService.ADefinirId` e no `A_DEFINIR_ID` do JavaScript. Esta constante precisa ser consistente em todos os três lugares.

### Usuários padrão

Criados apenas se o banco estiver vazio (`AnyAsync()` retorna false):

| E-mail | Senha | Role |
|--------|-------|------|
| `rafaelyamada@company.com` | `Rafa@123` | admin |
| `admin@company.com` | `Admin@123` | admin |
| `user@company.com` | `User@123` | user |

---

## Repositórios

Todos os repositórios seguem o mesmo padrão:
- Recebem `AppDbContext` via construtor
- Implementam a interface de output port correspondente
- Chamam `SaveChangesAsync()` nas operações de escrita

### `UserRepository`

```csharp
public Task<List<User>> GetAllAsync() =>
    _ctx.Users.OrderBy(u => u.Email).ToListAsync();

public Task<User?> FindByEmailAsync(string email) =>
    _ctx.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
```

- `GetAllAsync` — ordenado por e-mail
- `FindByEmailAsync` — normaliza para minúsculas antes de buscar (garante comparação case-insensitive)

### `DepartmentRepository`

```csharp
public Task<List<Department>> GetAllAsync() =>
    _ctx.Departments.OrderBy(d => d.Name).ToListAsync();
```

Ordenado por nome alfabeticamente.

### `ItemRepository`

```csharp
public Task<List<Item>> GetByDepartmentAsync(Guid departmentId) =>
    _ctx.Items
        .Where(i => i.DepartmentId == departmentId)
        .OrderBy(i => i.Name)
        .ToListAsync();
```

Filtrado por departamento, ordenado por nome.

### `TechnicianRepository` (arquivo `PersonRepository.cs`)

```csharp
public Task<List<Technician>> GetAllAsync() =>
    _ctx.Technicians.OrderBy(t => t.Name).ToListAsync();
```

Retorna TODOS os técnicos incluindo o "A Definir" (ID 00000001). O filtro é feito no Service.

### `TechnicianScheduleRepository`

```csharp
public Task<List<TechnicianSchedule>> GetByTechnicianAsync(Guid technicianId) =>
    _ctx.TechnicianSchedules
        .Where(s => s.TechnicianId == technicianId)
        .OrderBy(s => s.Date)
        .ToListAsync();
```

Filtrado por técnico, ordenado por data. Não há query "buscar por período" — o frontend carrega toda a agenda e filtra localmente no calendário.

### `PermissionRepository`

```csharp
public async Task UpsertAsync(UserDepartmentPermission permission)
{
    var existing = await GetAsync(permission.UserId, permission.DepartmentId);
    if (existing is null)
        _ctx.UserDepartmentPermissions.Add(permission);
    else
        _ctx.UserDepartmentPermissions.Update(permission);
    await _ctx.SaveChangesAsync();
}

public async Task<bool> HasAccessAsync(Guid userId, Guid departmentId, PermissionLevel minLevel)
{
    var perm = await GetAsync(userId, departmentId);
    return perm is not null && perm.Level >= minLevel;
}
```

- `UpsertAsync` — insere ou atualiza conforme existência
- `HasAccessAsync` — a comparação `perm.Level >= minLevel` funciona por causa dos valores numéricos do enum (Visualizar=1, Editar=2, Gerenciar=3)

---

## Segurança

### `JwtTokenService`

Implementa `ITokenService`. Gera tokens JWT com as claims do usuário.

**`JwtSettings`** — configuração lida do `appsettings.json` / variáveis de ambiente:

| Propriedade | Descrição | Padrão |
|-------------|-----------|--------|
| `Secret` | Chave secreta HMAC-SHA256 | (obrigatório, via `JWT_SECRET` no .env) |
| `Issuer` | Emissor do token | (configurado no appsettings) |
| `Audience` | Audiência do token | (configurado no appsettings) |
| `ExpiresIn` | Duração em segundos | `7200` (2 horas) |

**Claims incluídas no token:**

| Claim | Valor |
|-------|-------|
| `sub` (Subject) | `user.Id` (GUID) |
| `email` | `user.Email` |
| `http://...claims/role` | `user.Role` (`"admin"` ou `"user"`) |
| `tech_access` | `"true"` ou `"false"` (true se admin OU `HasTechnicianAccess`) |
| `jti` (JWT ID) | GUID aleatório por token |

```csharp
new Claim("tech_access", (user.HasTechnicianAccess || user.Role == "admin").ToString().ToLower()),
```

Admins sempre têm `tech_access = true`, independentemente do campo `HasTechnicianAccess`.

O algoritmo de assinatura é `HmacSha256` com a chave secreta configurada.

### `BCryptPasswordHasher`

Implementação simples sobre `BCrypt.Net.BCrypt`:

```csharp
public bool Verify(string password, string hash) =>
    BCrypt.Net.BCrypt.Verify(password, hash);

public string Hash(string password) =>
    BCrypt.Net.BCrypt.HashPassword(password);
```

**Como o BCrypt funciona (resumo):**
- Ao fazer hash, gera um salt aleatório e embute no resultado
- O hash resultante tem ~60 caracteres e contém o salt, o custo e o hash em si
- Na verificação, extrai o salt do hash armazenado e reaplica para comparar
- Nenhuma informação extra precisa ser armazenada além do hash em si
- O custo computacional (work factor) torna ataques de força bruta lentos

---

## Migrações EF Core

Localizadas em `Migrations/`. Cada migration representa uma mudança no schema do banco.

### 1. `InitialCreate` (02/04/2026)

Cria todas as tabelas iniciais:
- `Users` — sem `LastLoginAt`
- `Departments`
- `Technicians` — COM coluna `DepartmentId` (depois removida)
- `TechnicianSchedules` — sem `Client` e `Status`
- `Items`
- `UserDepartmentPermissions`

Também cria os índices: `IX_Items_DepartmentId`, `IX_TechnicianSchedules_TechnicianId`, `IX_Users_Email` (unique), `IX_Technicians_DepartmentId`.

### 2. `RemoveTechnicianDepartment` (02/04/2026)

Remove a coluna `DepartmentId` da tabela `Technicians` e o índice correspondente.

**Motivo:** Técnicos passaram a ser globais — não pertencem a um departamento específico.

### 3. `AddScheduleClientStatus` (02/04/2026)

Adiciona à tabela `TechnicianSchedules`:
- Coluna `Client` (TEXT, nullable, max 200)
- Coluna `Status` (TEXT, não-null, padrão `"confirmado"`, max 30)

**Motivo:** Necessidade de registrar o cliente do agendamento e o status (confirmado/pendente etc.).

### 4. `AddUserLastLoginAt` (06/04/2026)

Adiciona à tabela `Users`:
- Coluna `LastLoginAt` (TEXT, nullable)

**Motivo:** Necessidade de exibir a data do último login de cada usuário na tela de gerenciamento de usuários. A data é armazenada em UTC e convertida para horário de Brasília (America/Sao_Paulo) pelo frontend.

---

## Configuração do banco (`appsettings.json`)

O SQLite armazena tudo em um arquivo. Em produção (Docker), esse arquivo fica em `/var/data/companymanager.db` — mapeado como volume Docker para persistência entre reinicializações do container.

A connection string típica:
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=/var/data/companymanager.db"
}
```
