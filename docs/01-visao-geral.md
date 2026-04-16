# 01 — Visão Geral do Projeto

## O que é o CompanyManager?

O **CompanyManager** é um sistema web de gestão empresarial que centraliza dois fluxos principais:

1. **Gestão de ativos/ferramentas por departamento** — cada departamento da empresa possui itens (equipamentos, ferramentas, instrumentos de medição etc.) que podem estar no estoque, em campo (com um técnico) ou em manutenção. O sistema rastreia onde cada item está e quem é o responsável.

2. **Agenda de técnicos** — técnicos são globais (não pertencem a um departamento específico) e possuem uma agenda de compromissos com data, serviço, cliente e status. Um calendário visual mostra todos os técnicos agendados por dia.

---

## Stack Tecnológica

| Camada | Tecnologia |
|--------|-----------|
| Backend | .NET 8, ASP.NET Core Web API |
| ORM | Entity Framework Core 8 |
| Banco de dados | SQLite (arquivo em `/var/data/`) |
| Autenticação | JWT (JSON Web Token) via `System.IdentityModel.Tokens.Jwt` |
| Hash de senha | BCrypt.Net |
| Frontend | HTML + CSS + JavaScript vanilla (arquivo único `dashboard.html`) |
| Containerização | Docker (multi-stage build) |
| Proxy reverso | Nginx + Let's Encrypt (HTTPS) |
| Deploy | AWS Lightsail |

---

## Arquitetura Hexagonal (Ports & Adapters)

O projeto segue a **Arquitetura Hexagonal** (também chamada de Ports & Adapters ou Clean Architecture). O objetivo é proteger as regras de negócio de detalhes de infraestrutura — o domínio não sabe que existe um banco de dados, uma API REST ou um frontend.

```
┌────────────────────────────────────────────────────────┐
│                        API Layer                       │
│   Controllers, Middleware, Program.cs, wwwroot/        │
├────────────────────────────────────────────────────────┤
│                   Application Layer                    │
│   Services, DTOs, Ports (interfaces)                   │
├────────────────────────────────────────────────────────┤
│                     Domain Layer                       │
│   Entities, Enums, Exceptions (zero dependências)      │
├────────────────────────────────────────────────────────┤
│                 Infrastructure Layer                   │
│   EF Core, Repositories, JWT, BCrypt, Migrations       │
└────────────────────────────────────────────────────────┘
```

### Por que cada camada existe?

**Domain** (`CompanyManager.Domain`)
- Contém as entidades (`User`, `Item`, `Technician` etc.) e as exceções de negócio.
- Não depende de nenhum outro projeto. É o núcleo.
- As entidades encapsulam regras: `User.RecordFailedLogin()` incrementa tentativas e bloqueia quando atinge 5; `Item.Move()` impede que `PersonId` seja definido fora do modo "campo".

**Application** (`CompanyManager.Application`)
- Contém os **Services** (casos de uso: `AuthService`, `UserService` etc.) e os **DTOs** (objetos de transferência de dados).
- Define **Ports**: interfaces que descrevem o que a camada Application precisa (ex.: `IUserRepository`, `ITokenService`). Ela não sabe *como* essas interfaces são implementadas.
- Depende apenas do Domain.

**Infrastructure** (`CompanyManager.Infrastructure`)
- Implementa as interfaces (ports) definidas pela Application.
- Contém o `AppDbContext` (EF Core), os repositórios, o `JwtTokenService`, o `BCryptPasswordHasher`, as migrations e o `DatabaseSeeder`.
- Depende do Domain e da Application.

**API** (`CompanyManager.API`)
- Ponto de entrada HTTP. Configura o pipeline ASP.NET Core, registra os serviços na injeção de dependências e serve o frontend estático.
- Depende de todos os outros projetos.

---

## Estrutura de Pastas

```
CompanyManager/
├── Dockerfile
├── docker-compose.yml
├── nginx.conf
├── CompanyManager.sln
└── src/
    ├── CompanyManager.Domain/
    │   ├── Entities/          # User, Department, Item, Technician, TechnicianSchedule, UserDepartmentPermission
    │   ├── Enums/             # ItemLocation, PermissionLevel
    │   └── Exceptions/        # DomainException, BusinessException, InvalidCredentialsException...
    │
    ├── CompanyManager.Application/
    │   ├── DTOs/              # Objetos de entrada/saída das APIs
    │   ├── Ports/
    │   │   ├── Input/         # IAuthService (interfaces que os Controllers chamam)
    │   │   └── Output/        # IUserRepository, ITokenService... (interfaces que os Services precisam)
    │   └── Services/          # AuthService, UserService, DepartmentService, ItemService, TechnicianService
    │
    ├── CompanyManager.Infrastructure/
    │   ├── Adapters/
    │   │   ├── Persistence/
    │   │   │   ├── AppDbContext.cs
    │   │   │   ├── Repositories/   # Implementações concretas dos repositórios
    │   │   │   └── Seeding/        # DatabaseSeeder
    │   │   └── Security/           # JwtTokenService, BCryptPasswordHasher
    │   ├── Configuration/          # JwtSettings
    │   ├── Migrations/             # Histórico de migrações EF Core
    │   └── DependencyInjection.cs  # Registra todos os serviços no DI
    │
    └── CompanyManager.API/
        ├── Controllers/            # AuthController, UsersController, DepartmentsController, ItemsController, TechniciansController
        ├── Middleware/             # ExceptionMiddleware
        ├── Program.cs              # Configuração e pipeline
        └── wwwroot/                # dashboard.html (SPA vanilla JS)
```

---

## Fluxo de uma Requisição HTTP

Exemplo: `POST /departments/{id}/items` (criar item)

```
1. HTTP Request chega na porta 8080
   └─► Nginx (terminação TLS) encaminha para o container

2. ASP.NET Core Pipeline
   ├─► ExceptionMiddleware (captura exceções globalmente)
   ├─► UseStaticFiles (serve wwwroot — não se aplica aqui)
   ├─► UseRateLimiter (verifica limites — só ativo na rota /auth/login)
   ├─► UseAuthentication (valida o JWT no header Authorization)
   ├─► UseAuthorization (verifica roles e claims)
   └─► MapControllers → ItemsController.Create()

3. ItemsController.Create(departmentId, dto)
   ├─► RequireAccess() → verifica se o usuário tem permissão Editar no departamento
   └─► _service.CreateAsync(departmentId, dto)

4. ItemService.CreateAsync()
   ├─► Cria entidade: new Item(Guid.NewGuid(), departmentId, ...)
   ├─► _items.AddAsync(item)          ← chama a interface (port de saída)
   └─► Retorna ItemResponseDto

5. ItemRepository.AddAsync(item)      ← implementação concreta
   ├─► _ctx.Items.Add(item)
   └─► _ctx.SaveChangesAsync()        ← EF Core gera e executa o SQL no SQLite

6. Resposta volta ao controller → Ok(dto) → HTTP 200 JSON
```

**Ponto-chave:** O `ItemService` não sabe que o banco é SQLite. Ele conhece apenas a interface `IItemRepository`. Se amanhã migrarmos para PostgreSQL, só a implementação do repositório muda — o serviço permanece intacto.
