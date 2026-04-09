# 03 — Camada de Aplicação (Services, DTOs e Ports)

A camada de aplicação (`CompanyManager.Application`) orquestra os casos de uso do sistema. Ela não contém regras de negócio (isso é responsabilidade do domínio) nem detalhes de infraestrutura (isso é responsabilidade do Infrastructure). Ela coordena o fluxo: busca entidades, aplica operações de domínio e persiste o resultado.

---

## O que são Ports?

**Ports** são interfaces — contratos que a camada de aplicação define mas não implementa. Existem dois tipos:

- **Input Ports** (`Ports/Input/`) — interfaces que os controllers chamam. Define o que a aplicação expõe.
- **Output Ports** (`Ports/Output/`) — interfaces que os services precisam. Define o que a aplicação precisa da infraestrutura.

Isso é o coração da arquitetura hexagonal: a Application manda e a Infrastructure obedece.

```
Controller ──► IAuthService (Input Port)
                    └──► AuthService (implementação)
                              └──► IUserRepository (Output Port)
                                        └──► UserRepository (implementação na Infrastructure)
```

---

## Input Ports (Interfaces de entrada)

### `IAuthService` — `Ports/Input/IAuthService.cs`

```csharp
public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
}
```

Única operação: realizar login. Implementado por `AuthService`.

> Os demais services (`UserService`, `DepartmentService` etc.) não possuem input port explícito — são usados diretamente pelos controllers via injeção de dependência. Apenas o `AuthService` tem interface, pois é o mais crítico e facilita testes.

---

## Output Ports (Interfaces de saída)

### `IUserRepository`

```csharp
Task<List<User>> GetAllAsync();
Task<User?> GetByIdAsync(Guid id);
Task<User?> FindByEmailAsync(string email);
Task AddAsync(User user);
Task UpdateAsync(User user);
Task DeleteAsync(User user);
```

- `FindByEmailAsync` — busca por e-mail (normaliza para minúsculas antes da consulta).
- Os métodos Add/Update/Delete chamam `SaveChangesAsync()` internamente.

### `IDepartmentRepository`

```csharp
Task<List<Department>> GetAllAsync();
Task<Department?> GetByIdAsync(Guid id);
Task AddAsync(Department department);
Task UpdateAsync(Department department);
Task DeleteAsync(Department department);
```

Operações CRUD padrão. `GetAllAsync()` retorna ordenado por nome.

### `IItemRepository`

```csharp
Task<List<Item>> GetAllAsync();
Task<List<Item>> GetByDepartmentAsync(Guid departmentId);
Task<Item?> GetByIdAsync(Guid id);
Task AddAsync(Item item);
Task UpdateAsync(Item item);
Task DeleteAsync(Item item);
```

- `GetAllAsync()` — retorna todos os itens (usado pelo `TechnicianService` para contar itens por técnico).
- `GetByDepartmentAsync(id)` — retorna apenas os itens de um departamento, ordenados por nome.

### `ITechnicianRepository` (arquivo `IPersonRepository.cs`)

```csharp
Task<List<Technician>> GetAllAsync();
Task<Technician?> GetByIdAsync(Guid id);
Task AddAsync(Technician technician);
Task UpdateAsync(Technician technician);
Task DeleteAsync(Technician technician);
```

`GetAllAsync()` retorna todos os técnicos incluindo o "A Definir" — o filtro para excluí-lo da listagem normal é feito no `TechnicianService`.

### `ITechnicianScheduleRepository`

```csharp
Task<List<TechnicianSchedule>> GetByTechnicianAsync(Guid technicianId);
Task<TechnicianSchedule?> GetByIdAsync(Guid id);
Task AddAsync(TechnicianSchedule schedule);
Task UpdateAsync(TechnicianSchedule schedule);
Task DeleteAsync(TechnicianSchedule schedule);
```

- `GetByTechnicianAsync(id)` — retorna a agenda de um técnico específico, ordenada por data.

### `IPermissionRepository`

```csharp
Task<List<UserDepartmentPermission>> GetByUserAsync(Guid userId);
Task<UserDepartmentPermission?> GetAsync(Guid userId, Guid departmentId);
Task UpsertAsync(UserDepartmentPermission permission);
Task DeleteAsync(UserDepartmentPermission permission);
Task<bool> HasAccessAsync(Guid userId, Guid departmentId, PermissionLevel minLevel);
```

- `UpsertAsync` — insere se não existir, atualiza se já existir (Upsert = Update + Insert).
- `HasAccessAsync` — retorna `true` se a permissão existe E o nível é >= ao mínimo exigido.

### `ITokenService`

```csharp
string GenerateToken(User user);
int ExpiresIn { get; }
```

Gera um JWT para o usuário. `ExpiresIn` é o tempo em segundos (configurado como 7200 — 2 horas).

### `IPasswordHasher`

```csharp
bool Verify(string password, string hash);
string Hash(string password);
```

Abstração sobre o BCrypt. `Verify` compara a senha em texto puro com o hash armazenado.

---

## DTOs (Data Transfer Objects)

DTOs são `record` (imutáveis por padrão em C#). Carregam dados entre as camadas sem expor as entidades de domínio.

### DTOs de Login

**`LoginRequestDto`** — entrada do `POST /auth/login`:
```csharp
record LoginRequestDto(string Email, string Password)
```

**`LoginResponseDto`** — resposta após login bem-sucedido:
```csharp
record LoginResponseDto(string Token, int ExpiresIn)
// ExpiresIn: tempo de expiração em segundos (7200 = 2h)
```

### DTOs de Usuário

**`CreateUserDto`** — `POST /users`:
```csharp
record CreateUserDto(string Email, string Password, string Role = "user", bool HasTechnicianAccess = false)
```

**`UpdateUserDto`** — `PUT /users/{id}`:
```csharp
record UpdateUserDto(string? Password, string? Role)
// Parâmetros null = não alterar. E-mail não pode ser alterado.
```

**`UserResponseDto`** — resposta da API de usuários:
```csharp
record UserResponseDto(
    Guid Id, string Email, string Role, bool IsBlocked,
    int FailedLoginAttempts, bool HasTechnicianAccess,
    DateTime? LastLoginAt, List<UserPermissionDto> Permissions
)
```

**`UserPermissionDto`** — permissão de um usuário em um departamento (dentro do `UserResponseDto`):
```csharp
record UserPermissionDto(Guid DepartmentId, string DepartmentName, string Level)
// Level: "Visualizar" | "Editar" | "Gerenciar"
```

**`SetPermissionDto`** — `POST /users/{id}/permissions`:
```csharp
record SetPermissionDto(Guid DepartmentId, string Level)
```

### DTOs de Departamento

**`CreateDepartmentDto`** / **`UpdateDepartmentDto`**:
```csharp
record CreateDepartmentDto(string Name, string? Description)
record UpdateDepartmentDto(string Name, string? Description)
```

**`DepartmentResponseDto`** — inclui contagens de itens por localização:
```csharp
record DepartmentResponseDto(
    Guid Id, string Name, string? Description,
    int ItemCount, int EstoqueCount, int CampoCount, int ManutencaoCount,
    DateTime CreatedAt
)
```

### DTOs de Item

**`CreateItemDto`** / **`UpdateItemDto`**:
```csharp
record CreateItemDto(string Name, string? Serial, string Category, string? Observations)
```

**`MoveItemDto`** — `POST /departments/{id}/items/{itemId}/move`:
```csharp
record MoveItemDto(string Location, Guid? PersonId, string? Observations)
// Location: "estoque" | "campo" | "manutencao" (minúsculo)
// PersonId: obrigatório quando Location == "campo"
```

**`ItemResponseDto`**:
```csharp
record ItemResponseDto(
    Guid Id, Guid DepartmentId, string Name, string? Serial,
    string Category, string Location, Guid? PersonId, string? PersonName,
    string? Observations, DateTime CreatedAt
)
// Location é string em minúsculo: "estoque", "campo", "manutencao"
// PersonName: nome do técnico (resolvido pela Service)
```

### DTOs de Técnico

**`CreateTechnicianDto`** / **`UpdateTechnicianDto`**:
```csharp
record CreateTechnicianDto(string Name, string? Phone, string? Region)
```

**`TechnicianResponseDto`**:
```csharp
record TechnicianResponseDto(
    Guid Id, string Name, string? Phone, string? Region,
    int ItemsWithTechnician, DateTime CreatedAt
)
// ItemsWithTechnician: quantos itens estão "em campo" com este técnico
```

**`CreateTechnicianScheduleDto`** / **`UpdateTechnicianScheduleDto`**:
```csharp
record CreateTechnicianScheduleDto(
    DateTime Date, string Title, string? Client, string? Notes,
    string Status = "confirmado"
)
```

**`TechnicianScheduleResponseDto`**:
```csharp
record TechnicianScheduleResponseDto(
    Guid Id, Guid TechnicianId, string TechnicianName, DateTime Date,
    string Title, string? Client, string? Notes, string Status, DateTime CreatedAt
)
```

---

## Services (Implementações dos casos de uso)

### `AuthService`

Implementa `IAuthService`. Responsável pelo login.

**`LoginAsync(request)`:**
1. Busca o usuário pelo e-mail → se não encontrar: `throw new UserNotFoundException()`
2. Verifica se está bloqueado → se estiver: `throw new UserBlockedException()`
3. Verifica a senha com `IPasswordHasher.Verify()` → se incorreta:
   - Chama `user.RecordFailedLogin()` (pode bloquear o usuário)
   - Salva no banco
   - `throw new InvalidCredentialsException()`
4. Se senha correta:
   - `user.ResetFailedLogins()` — zera contador
   - `user.RecordLogin()` — registra `LastLoginAt = UtcNow`
   - Salva no banco
   - Gera JWT com `ITokenService.GenerateToken(user)`
   - Retorna `LoginResponseDto(token, expiresIn)`

---

### `UserService`

Gerencia usuários e permissões.

**`GetAllAsync()`** — carrega todos os usuários e para cada um busca suas permissões (`BuildDtoAsync`).

**`GetByIdAsync(id)`** — busca por ID, lança `BusinessException("user_not_found")` se não encontrar.

**`GetMyPermissionsAsync(userId)`** — usado pela rota `GET /users/me/permissions`. Retorna as permissões do próprio usuário logado (qualquer role pode chamar).

**`CreateAsync(dto)`:**
1. Verifica se o e-mail já está em uso → `BusinessException("email_in_use")`
2. Cria a entidade `User` com hash BCrypt da senha
3. Se `HasTechnicianAccess`, chama `user.SetTechnicianAccess(true)`
4. Persiste e retorna o DTO

**`UpdateAsync(id, dto)`** — atualiza role e/ou senha (só o que foi fornecido).

**`UnblockAsync(id)`** — chama `user.Unblock()` que zera contadores e `IsBlocked`.

**`SetPermissionAsync(userId, dto)`** — faz Upsert da permissão (cria ou atualiza):
1. Valida que usuário e departamento existem
2. Converte string `"Visualizar"/"Editar"/"Gerenciar"` para `PermissionLevel` via `Enum.TryParse`
3. Chama `IPermissionRepository.UpsertAsync()`

**`RemovePermissionAsync(userId, departmentId)`** — remove a permissão existente.

**`SetTechnicianAccessAsync(userId, hasAccess)`** — alterna o acesso à aba de técnicos.

**`BuildDtoAsync(user)` (privado)** — helper que monta o `UserResponseDto` completo, buscando as permissões de cada usuário. Chamado por vários métodos.

---

### `DepartmentService`

Gerencia departamentos com suas contagens de itens.

**`GetAllAsync()`** — para cada departamento, busca seus itens e conta por localização.

**`CreateAsync(dto)`** — cria departamento (com `.Trim()` nos campos de texto) e retorna com contagens zeradas.

**`UpdateAsync(id, dto)`** — atualiza nome e descrição, retorna com contagens atualizadas.

**`ToDto(d, items)` (helper estático):**
```csharp
private static DepartmentResponseDto ToDto(Department d, List<Item> items) =>
    new(d.Id, d.Name, d.Description,
        items.Count,
        items.Count(i => i.Location == ItemLocation.Estoque),
        items.Count(i => i.Location == ItemLocation.Campo),
        items.Count(i => i.Location == ItemLocation.Manutencao),
        d.CreatedAt);
```
Conta os itens por localização usando LINQ.

---

### `ItemService`

Gerencia itens dentro de um departamento.

**`GetByDepartmentAsync(departmentId)`** — carrega itens + todos os técnicos para resolver os nomes.

**`CreateAsync(departmentId, dto)`** — cria item sempre com `Location = Estoque` (padrão do construtor).

**`MoveAsync(departmentId, itemId, dto)`:**
1. Verifica que o item pertence ao departamento (`GetItemOfDept`)
2. Converte a string de localização para o enum:
   ```csharp
   var location = dto.Location.ToLowerInvariant() switch
   {
       "estoque"    => ItemLocation.Estoque,
       "campo"      => ItemLocation.Campo,
       "manutencao" => ItemLocation.Manutencao,
       _            => throw new BusinessException("Localização inválida.", "invalid_location")
   };
   ```
3. Se for para `Campo` sem `PersonId`: `BusinessException("technician_required")`
4. Chama `item.Move(location, personId, observations)` — a entidade aplica as regras

**`GetItemOfDept(departmentId, itemId)` (helper privado):**
- Busca o item por ID
- Verifica se o `DepartmentId` do item bate com o `departmentId` da rota
- Lança `BusinessException("item_not_found")` se não existir ou não pertencer ao departamento (mesmo código de erro, intencionalmente — não revela que o item existe em outro departamento)

**`ToDto(item, techs)` (helper estático):** resolve o nome do técnico procurando na lista pelo `PersonId`.

---

### `TechnicianService`

Gerencia técnicos globais e suas agendas.

#### `ADefinirId` — constante especial

```csharp
public static readonly Guid ADefinirId = new("00000000-0000-0000-0000-000000000001");
```

Este ID fixo identifica o técnico de sistema "A Definir". É declarado como `static readonly` para ser acessível sem instanciar o service. O controller `TechniciansController` expõe este ID via `GET /technicians/a-definir-id` para que o frontend possa usá-lo.

**Por que existe o "A Definir"?** Às vezes é necessário criar um agendamento em uma data mas o técnico ainda não foi definido. Em vez de deixar o campo técnico nulo (o que quebraria a FK), usa-se este técnico especial. No calendário, ele aparece com um círculo tracejado cinza, diferente dos círculos coloridos sólidos dos técnicos reais.

**`GetAllAsync()`** — retorna todos os técnicos EXCETO o "A Definir":
```csharp
return techs
    .Where(t => t.Id != ADefinirId)
    .Select(t => new TechnicianResponseDto(
        t.Id, t.Name, t.Phone, t.Region,
        allItems.Count(i => i.PersonId == t.Id),  // conta itens em campo
        t.CreatedAt
    )).ToList();
```

**`GetScheduleAsync(technicianId)`** — retorna a agenda de um técnico, ordenada por data.

**`AddScheduleAsync(technicianId, dto)`** — cria nova entrada na agenda.

**`UpdateScheduleAsync(scheduleId, dto)`** — busca a entrada pelo ID (não pelo técnico), atualiza e salva.

**`DeleteScheduleAsync(scheduleId)`** — busca e deleta a entrada pelo ID.

> **Nota sobre troca de técnico:** A API de update de agenda (`PUT /technicians/schedule/{id}`) não suporta troca de técnico — a entrada fica no técnico original. A troca de técnico é implementada no frontend: ele deleta a entrada antiga e cria uma nova no novo técnico.
