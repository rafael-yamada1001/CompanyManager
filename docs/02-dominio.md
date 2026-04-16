# 02 — Camada de Domínio (Entities & Exceptions)

A camada de domínio (`CompanyManager.Domain`) é o núcleo do sistema. Não depende de nenhum framework externo — sem EF Core, sem ASP.NET, sem nada. Contém apenas as regras de negócio puras.

**Regra de ouro:** os setters das propriedades são `private set`. Isso significa que nenhum código externo pode alterar o estado de uma entidade diretamente — ele precisa chamar um método da própria entidade. Isso garante que as regras de negócio sejam sempre respeitadas.

---

## Enums

### `ItemLocation`
```csharp
public enum ItemLocation
{
    Estoque,
    Campo,
    Manutencao
}
```
Representa onde um item está fisicamente. Usado como string no banco (`HasConversion<string>()`).

### `PermissionLevel`
```csharp
public enum PermissionLevel
{
    Visualizar = 1,
    Editar     = 2,
    Gerenciar  = 3
}
```
Hierarquia de acesso de um usuário a um departamento. O valor numérico permite comparações (`perm.Level >= minLevel`). Também armazenado como string no banco.

---

## Entidades

### `User`

Representa um usuário do sistema. Arquivo: `Entities/User.cs`

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `Id` | `Guid` | Identificador único |
| `Email` | `string` | E-mail em minúsculas (normalizado no construtor) |
| `PasswordHash` | `string` | Hash BCrypt da senha |
| `Role` | `string` | `"admin"` ou `"user"` |
| `IsBlocked` | `bool` | Se o usuário está bloqueado |
| `FailedLoginAttempts` | `int` | Contador de tentativas de login falhas |
| `HasTechnicianAccess` | `bool` | Se o usuário pode ver a aba de técnicos |
| `LastLoginAt` | `DateTime?` | Data/hora do último login bem-sucedido (UTC) |

**Constante de negócio:**
```csharp
private const int MaxFailedAttempts = 5;
```
Após 5 tentativas incorretas, o usuário é bloqueado automaticamente.

**Métodos de negócio:**

- `RecordFailedLogin()` — incrementa `FailedLoginAttempts`; se atingir 5, seta `IsBlocked = true`.
- `ResetFailedLogins()` — zera o contador após login bem-sucedido.
- `Unblock()` — desbloqueia o usuário e zera o contador (somente admin pode chamar).
- `UpdateProfile(role, passwordHash)` — atualiza role e/ou hash de senha (parâmetros `null` são ignorados).
- `SetTechnicianAccess(bool)` — habilita ou revoga o acesso à aba de técnicos.
- `RecordLogin()` — registra `LastLoginAt = DateTime.UtcNow` (chamado no login bem-sucedido).

**Construtor:** `new User(id, email, passwordHash, role = "user")` — normaliza o e-mail para minúsculas.

**Construtor privado sem parâmetros** — obrigatório pelo EF Core para reconstruir entidades do banco.

---

### `Department`

Representa um departamento da empresa. Arquivo: `Entities/Department.cs`

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `Id` | `Guid` | Identificador único |
| `Name` | `string` | Nome do departamento (max 150 chars) |
| `Description` | `string?` | Descrição opcional (max 500 chars) |
| `CreatedAt` | `DateTime` | Data de criação (UTC, setada no construtor) |

**Métodos:**
- `Update(name, description)` — atualiza nome e descrição.

Departamentos não têm regras de negócio complexas. São simples agrupadores de itens.

---

### `Item`

Representa um ativo/ferramenta pertencente a um departamento. Arquivo: `Entities/Item.cs`

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `Id` | `Guid` | Identificador único |
| `DepartmentId` | `Guid` | FK para o departamento dono do item |
| `Name` | `string` | Nome do item (max 200 chars) |
| `Serial` | `string?` | Número de série (opcional, max 100 chars) |
| `Category` | `string` | Categoria: Medição, Informática, Ferramenta, Rede, Elétrico, Hidráulico, Outros |
| `Location` | `ItemLocation` | Onde o item está: Estoque, Campo ou Manutencao |
| `PersonId` | `Guid?` | ID do técnico responsável (somente quando `Location == Campo`) |
| `Observations` | `string?` | Observações (max 500 chars) |
| `CreatedAt` | `DateTime` | Data de criação (UTC) |

**Regra de negócio central — `Move(location, personId, observations)`:**

```csharp
public void Move(ItemLocation location, Guid? personId, string? observations)
{
    Location = location;
    PersonId = location == ItemLocation.Campo ? personId : null;
    if (observations is not null)
        Observations = observations;
}
```

- Quando o item vai para `Campo`, o `PersonId` é definido com o técnico informado.
- Quando vai para `Estoque` ou `Manutencao`, o `PersonId` é automaticamente zerado — assim nunca fica um técnico "fantasma" associado a um item que não está com ele.

**Construtor:** cria o item sempre com `Location = Estoque` (valor padrão do negócio).

---

### `Technician` (arquivo `DepartmentPerson.cs`)

Representa um técnico global — não está vinculado a nenhum departamento específico e pode retirar itens de qualquer departamento.

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `Id` | `Guid` | Identificador único |
| `Name` | `string` | Nome do técnico (max 150 chars) |
| `Phone` | `string?` | Telefone com máscara (max 30 chars) |
| `Region` | `string?` | Região de atuação (max 100 chars) |
| `CreatedAt` | `DateTime` | Data de criação (UTC) |

**Observação importante:** O arquivo se chama `DepartmentPerson.cs` mas a classe é `Technician`. Isso é um resquício histórico — no início o técnico era vinculado a um departamento. A migration `RemoveTechnicianDepartment` removeu essa coluna.

**Técnico especial "A Definir":** Existe um técnico com ID fixo `00000000-0000-0000-0000-000000000001` chamado "A Definir". Ele é criado pelo `DatabaseSeeder` e nunca aparece na listagem normal de técnicos. É usado quando um agendamento precisa ser feito mas o técnico ainda não foi escolhido.

---

### `TechnicianSchedule`

Representa uma entrada na agenda de um técnico. Arquivo: `Entities/TechnicianSchedule.cs`

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `Id` | `Guid` | Identificador único |
| `TechnicianId` | `Guid` | FK para o técnico |
| `Date` | `DateTime` | Data do compromisso (apenas a parte date — sem hora) |
| `Title` | `string` | Descrição do serviço / onde estará (max 200 chars) |
| `Client` | `string?` | Nome do cliente (ex: "USINA BOM SUCESSO") (max 200 chars) |
| `Notes` | `string?` | Observações extras (max 500 chars) |
| `Status` | `string` | Um de: `confirmado`, `em_andamento`, `pendente`, `a_definir` |
| `CreatedAt` | `DateTime` | Data de criação (UTC) |

**Detalhe importante — `Date`:** O construtor faz `Date = date.Date`, o que descarta a parte de hora/minuto/segundo. Isso garante que sempre seja meia-noite UTC — a data pura, sem hora.

**`Status`** define a situação do agendamento:
- `confirmado` — o serviço está confirmado
- `em_andamento` — o técnico está executando agora
- `pendente` — aguardando confirmação
- `a_definir` — técnico ainda não definido (usado com o técnico "A Definir")

---

### `UserDepartmentPermission`

Tabela de permissões: associa um usuário a um departamento com um nível de acesso. Arquivo: `Entities/UserDepartmentPermission.cs`

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `UserId` | `Guid` | FK para o usuário |
| `DepartmentId` | `Guid` | FK para o departamento |
| `Level` | `PermissionLevel` | Nível: Visualizar (1), Editar (2) ou Gerenciar (3) |

**Chave primária composta:** `(UserId, DepartmentId)` — um usuário tem no máximo uma permissão por departamento.

**Método:** `UpdateLevel(level)` — atualiza o nível de acesso (usado no Upsert).

**Usuários com Role = "admin"** ignoram essa tabela — têm acesso total a tudo.

---

## Exceções de Domínio

Hierarquia de herança:

```
Exception
└── DomainException (abstract)
    ├── BusinessException
    ├── InvalidCredentialsException
    ├── UserBlockedException
    └── UserNotFoundException
```

### `DomainException` (abstract)
Classe base de todas as exceções de domínio. Possui a propriedade `Code` (string de erro em snake_case) além da `Message`. O `ExceptionMiddleware` usa o `Code` para decidir o status HTTP da resposta.

```csharp
public abstract class DomainException : Exception
{
    public string Code { get; }
    protected DomainException(string message, string code) : base(message) { Code = code; }
}
```

### `BusinessException`
Exceção genérica para regras de negócio violadas. Usada pelos Services quando uma validação falha.

```csharp
// Exemplo de uso:
throw new BusinessException("E-mail já cadastrado.", "email_in_use");
throw new BusinessException("Usuário não encontrado.", "user_not_found");
throw new BusinessException("Nível de permissão inválido.", "invalid_permission_level");
```

O `ExceptionMiddleware` verifica se o `Code` é um dos códigos "not found" (`user_not_found`, `department_not_found`, `item_not_found`, `technician_not_found`, `schedule_not_found`) e retorna HTTP 404. Para os demais, retorna HTTP 400.

### `InvalidCredentialsException`
Lançada pelo `AuthService` quando a senha está incorreta. Código: `"invalid_credentials"`. Mensagem: `"Credenciais inválidas."`.

### `UserBlockedException`
Lançada pelo `AuthService` quando o usuário está bloqueado. Código: `"user_blocked"`. Mensagem: `"Usuário bloqueado. Entre em contato com o administrador."`.

### `UserNotFoundException`
Lançada pelo `AuthService` quando o e-mail não existe no banco. Código: `"user_not_found"`. Mensagem: `"Usuário ou e-mail não encontrado."`.

---

## Como o ExceptionMiddleware usa as exceções

```
DomainException com Code "user_not_found"  →  HTTP 404
DomainException com Code "email_in_use"    →  HTTP 400
UnauthorizedAccessException                →  HTTP 403
Exception genérica                         →  HTTP 500 (em prod: mensagem genérica)
```

O frontend consome o campo `message` da resposta JSON para exibir mensagens de erro ao usuário.
