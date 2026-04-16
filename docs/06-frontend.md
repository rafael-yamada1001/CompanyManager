# 06 — Frontend (dashboard.html)

O frontend é uma **Single-Page Application (SPA)** implementada em um único arquivo HTML: `src/CompanyManager.API/wwwroot/dashboard.html`. Não usa nenhum framework (React, Vue, Angular etc.) — é JavaScript vanilla puro com CSS inline.

O arquivo é servido pelo ASP.NET Core via `UseStaticFiles()` + `UseDefaultFiles()`. A página de login é o `index.html` (arquivo separado não documentado aqui). Após o login bem-sucedido, o usuário é redirecionado para `dashboard.html`.

---

## Estrutura Visual

```
┌────────────────────────────────────────────────────────┐
│  SIDEBAR (fixa)   │  TOPBAR (sticky)                   │
│  ─────────────    │  ─────────────────────────────────  │
│  Logo             │  Título da seção + botões de ação   │
│  Nav:             ├────────────────────────────────────  │
│  • Departamentos  │                                     │
│  • Técnicos       │       CONTEÚDO DA SEÇÃO ATIVA       │
│  • Usuários       │       (.section.active)             │
│  ─────────────    │                                     │
│  User info        │                                     │
│  Botão logout     │                                     │
└────────────────────────────────────────────────────────┘
```

**Seções disponíveis** (apenas uma ativa por vez):
- `section-departamentos` — tabela de departamentos
- `section-dep-items` — itens de um departamento (subnível)
- `section-tecnicos` — calendário + painel lateral
- `section-tec-schedule` — agenda individual de um técnico (subnível)
- `section-usuarios` — tabela de usuários (apenas admin)

---

## Fluxo de Autenticação

### 1. Verificação inicial (antes de qualquer código)

```javascript
const token = localStorage.getItem('cm_token');
if (!token || Date.now() > Number(localStorage.getItem('cm_expires'))) {
  localStorage.clear(); window.location.href = '/';
}
```

Se não houver token ou ele estiver expirado, redireciona para a página de login imediatamente.

### 2. Decodificação do JWT

```javascript
function decodeJwt(t) {
  try {
    return JSON.parse(atob(t.split('.')[1].replace(/-/g,'+').replace(/_/g,'/')));
  } catch { return {}; }
}
const payload = decodeJwt(token);
```

O JWT é dividido em 3 partes por `.`. A segunda parte (índice 1) é o payload em Base64url. A função `atob` decodifica Base64, e `JSON.parse` converte para objeto. Os caracteres `-` e `_` são substituídos por `+` e `/` para compatibilidade com `atob`.

### 3. Claims extraídas do payload

```javascript
const myEmail       = payload.email || '';
const myRole        = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || payload.role || 'user';
const myId          = payload.sub || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || '';
const isAdmin       = myRole === 'admin';
const hasTechAccess = payload.tech_access === 'true' || isAdmin;
```

O ASP.NET Core serializa alguns claims com URLs longas no JWT (padrão WS-Federation). O código tenta ambos os formatos para compatibilidade.

### 4. Visibilidade da navegação

```javascript
if (!isAdmin) document.getElementById('navUsuarios').style.display = 'none';
if (hasTechAccess) document.getElementById('navTecnicos').style.display = 'flex';
```

- Aba "Usuários": visível apenas para admin
- Aba "Técnicos": visível apenas se `tech_access === 'true'` no JWT

---

## Função `api()` — Helper Central

```javascript
function api(path, opts = {}) {
  return fetch(path, {
    ...opts,
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`,
      ...(opts.headers || {})
    }
  }).then(async r => {
    if (r.status === 204) return null;  // sem body
    const data = await r.json().catch(() => ({}));
    if (!r.ok) throw new Error(data.message || data.error || 'Erro desconhecido');
    return data;
  });
}
```

**O que faz:**
1. Faz um `fetch` para o endpoint
2. Injeta automaticamente o header `Authorization: Bearer <token>`
3. Para HTTP 204 (No Content): retorna `null`
4. Para erros (status >= 400): extrai a mensagem do JSON e lança um `Error`
5. Para sucesso: retorna o objeto JSON

**Uso padrão:**
```javascript
// GET
const items = await api('/departments').catch(() => []);

// POST
const result = await api('/technicians', {
  method: 'POST',
  body: JSON.stringify({ name: 'Rafael', phone: '(11) 99999-0000' })
});

// DELETE
await api(`/technicians/${id}`, { method: 'DELETE' });
```

---

## Estado Global

```javascript
let departments       = [];      // lista de departamentos carregados
let currentDepId      = null;    // ID do departamento atual (na tela de itens)
let currentTab        = 'all';   // aba ativa na seção de itens
let depItems          = [];      // itens do departamento atual
let allUsers          = [];      // todos os usuários (admin)
let myPerms           = [];      // permissões do usuário logado (não-admin)
let allTechnicians    = [];      // técnicos (sem "A Definir")
let allScheduleEntries = [];     // todos os agendamentos de todos os técnicos
let calendarDate      = new Date();  // mês/ano exibido no calendário
let selectedDay       = null;    // dia selecionado no calendário
let currentTechId     = null;    // técnico cuja agenda está sendo visualizada
let currentTech       = null;    // objeto do técnico atual
let _scheduleCache    = [];      // cache das entradas da agenda individual
let newPermissions    = [];      // lista temporária de permissões no modal de usuário
```

---

## Seção de Departamentos

### `loadDepartments()`

```javascript
async function loadDepartments() {
  departments = await api('/departments').catch(() => []);
  if (!isAdmin) {
    myPerms = await api('/users/me/permissions').catch(() => []);
  }
  renderDepTable(departments);
}
```

Carrega departamentos + permissões do usuário (para não-admin). O servidor já filtra os departamentos visíveis para cada usuário, mas as permissões locais (`myPerms`) são usadas para decidir quais botões de ação mostrar.

### `filterDep()`

Filtra a tabela de departamentos localmente (sem nova requisição) pelo valor do campo de busca.

### CRUD de Departamentos

- `openNewDep()` / `saveDep(e)` — abre modal e faz `POST /departments`
- `openEditDep(id, name, desc)` / `updateDep(e, id)` — abre modal preenchido e faz `PUT /departments/{id}`
- `deleteDep(id, name)` / `confirmDelDep(id)` — modal de confirmação e `DELETE /departments/{id}`

---

## Seção de Itens

### Navegação para itens

```javascript
async function openDepItems(id, name) {
  currentDepId = id;
  currentTab = 'all';
  showSection('dep-items');
  await Promise.all([loadItems(), loadPeople()]);
}
```

`loadPeople()` carrega os técnicos globais para popular o select "Pessoa responsável" nos modais de item.

### Tabs de filtro

```javascript
function setTab(el) {
  currentTab = el.dataset.tab;  // 'all' | 'estoque' | 'campo' | 'pessoa'
  renderItems();
}
```

As tabs filtram os dados já carregados em `depItems` — sem nova requisição à API.

### Tab "Por técnico" (pessoa)

Quando `currentTab === 'pessoa'`, o `renderItems()` agrupa os itens (em campo) por `personName`:

```javascript
if (currentTab === 'pessoa') {
  const byPerson = {};
  data.forEach(i => {
    const k = i.personName || 'Sem técnico';
    if (!byPerson[k]) byPerson[k] = [];
    byPerson[k].push(i);
  });
  // renderiza grupos com cabeçalho por técnico
}
```

### Mover item (`openMoveItem`, `saveMove`)

O modal de mover exibe a localização atual e permite selecionar a nova. O campo "Pessoa responsável" só aparece quando a localização selecionada é "campo" — controlado por `togglePersonSelect()`.

```javascript
function togglePersonSelect(wrapId, selId) {
  const val = document.getElementById(selId)?.value;
  const wrap = document.getElementById(wrapId);
  if (wrap) wrap.style.display = val === 'campo' ? 'block' : 'none';
}
```

---

## Seção de Técnicos — Calendário

### `loadTechnicians()`

```javascript
async function loadTechnicians() {
  allTechnicians = await api('/technicians').catch(() => []);

  // Carrega agendas de todos os técnicos em paralelo
  allScheduleEntries = [];
  await Promise.all(allTechnicians.map(async (tech, idx) => {
    const entries = await api(`/technicians/${tech.id}/schedule`).catch(() => []);
    const color   = TECH_COLORS[idx % TECH_COLORS.length];
    for (const e of entries) {
      allScheduleEntries.push({ ...e, technicianName: tech.name, color });
    }
    tech._color = color;
  }));

  // Carrega entradas "A Definir" separadamente (não está na lista normal)
  const aDefEntries = await api(`/technicians/00000000-0000-0000-0000-000000000001/schedule`).catch(() => []);
  for (const e of aDefEntries) {
    allScheduleEntries.push({ ...e, technicianName: 'A Definir', color: '#9ca3af' });
  }
  // ...
}
```

**Por que "A Definir" é carregado separadamente?** O `GET /technicians` filtra o técnico "A Definir" da listagem normal (`TechnicianService.GetAllAsync()` exclui o ID fixo). Mas o frontend precisa das entradas "A Definir" para exibir no calendário. Por isso faz uma requisição extra para `/technicians/00000000.../schedule`.

### Cores dos técnicos

```javascript
const TECH_COLORS = [
  '#4f46e5','#0ea5e9','#10b981','#f59e0b','#ef4444',
  '#8b5cf6','#ec4899','#14b8a6','#f97316','#6366f1',
  '#84cc16','#06b6d4',
];
```

Cada técnico recebe uma cor da lista ciclicamente (`idx % TECH_COLORS.length`). O técnico "A Definir" sempre usa cinza (`#9ca3af`).

### `renderCalendar()`

1. Monta um `dayMap`: dicionário de `"YYYY-MM-DD"` → lista de entradas
2. Calcula o primeiro dia do mês e quantos dias tem
3. Renderiza uma grade CSS de 7 colunas (Dom-Sáb)
4. Para cada dia, exibe:
   - Número do dia
   - Bolinhas coloridas (máx 4) — uma por técnico agendado
   - Bolinhas tracejadas para entradas "A Definir"
   - Contador se houver mais de 4 entradas
5. Estilo do dia: selecionado (azul sólido) > hoje (fundo claro) > tem entradas (fundo levemente roxo) > normal

```javascript
const dots = entries.slice(0, 4).map(e => {
  const isADef = e.technicianId === A_DEFINIR_ID;
  return isADef
    ? `<span style="border:1.5px dashed #9ca3af; ..."></span>`
    : `<span style="background:${e.color}; ..."></span>`;
}).join('');
```

### Clique simples vs duplo clique

```javascript
let _calClickTimer = null;

function onCalDayClick(event, date, dateStr) {
  if (_calClickTimer) {
    clearTimeout(_calClickTimer);
    _calClickTimer = null;
    openScheduleOnDay(dateStr);   // duplo clique → abre modal de agendamento
  } else {
    _calClickTimer = setTimeout(() => {
      _calClickTimer = null;
      selectDay(date);            // clique simples → seleciona o dia
    }, 220);
  }
}
```

Clique único seleciona o dia (atualiza o painel lateral). Duplo clique em 220ms abre o modal de criação de agendamento.

### `A_DEFINIR_ID` — constante JS

```javascript
const A_DEFINIR_ID = '00000000-0000-0000-0000-000000000001';
```

Deve sempre coincidir com `TechnicianService.ADefinirId` no backend e `DatabaseSeeder.ADefinirTechnicianId` na Infrastructure.

### `toggleADefinirTech()`

Chamado quando o status muda no modal de agendamento:

```javascript
function toggleADefinirTech() {
  const isADef = document.getElementById('sdStatus')?.value === 'a_definir';
  document.getElementById('sdTechWrap').style.display = isADef ? 'none' : 'block';
  document.getElementById('sdADefinirInfo').style.display = isADef ? 'block' : 'none';
}
```

Quando status é `"a_definir"`, esconde o select de técnico e mostra um aviso amarelo. Na submissão, usa `A_DEFINIR_ID` como `techId`.

---

## Editar/Excluir do Painel Lateral

### `openEditCalEntry(id)`

Abre o modal de edição de um agendamento existente. Preenche todos os campos com os dados da entrada. Suporta troca de técnico.

```javascript
async function saveEditCalEntry(e, id, oldTechId, dateStr) {
  const newTechId = status === 'a_definir' ? A_DEFINIR_ID : document.getElementById('sdTech').value;

  if (newTechId !== oldTechId) {
    // Técnico mudou: DELETE na entrada antiga + POST no novo técnico
    await api(`/technicians/schedule/${id}`, { method:'DELETE' });
    await api(`/technicians/${newTechId}/schedule`, { method:'POST', body: JSON.stringify(body) });
  } else {
    // Mesmo técnico: apenas PUT
    await api(`/technicians/schedule/${id}`, { method:'PUT', body: JSON.stringify(body) });
  }
}
```

**Troca de técnico:** Como a API não tem endpoint para trocar o técnico de uma entrada, o frontend simula isso fazendo DELETE + POST. O resultado é o mesmo, mas a entrada terá um novo ID.

### `deleteCalEntry(id)`

Usa `confirm()` nativo do browser (sem modal customizado) para confirmar antes de excluir.

---

## Agenda Individual do Técnico (`openTechSchedule`)

Navega para `section-tec-schedule` mostrando a agenda de um técnico específico.

- `loadSchedule()` — carrega via `GET /technicians/{currentTechId}/schedule`
- `renderSchedule(data)` — renderiza tabela com entradas, colorindo passadas com `opacity:.6`
- `openEditSchedule(id)` / `updateSchedule(e, id)` — edita entrada via `PUT /technicians/schedule/{id}`
- `deleteSchedule(id)` / `confirmDelSchedule(id)` — exclui via `DELETE /technicians/schedule/{id}`

A agenda individual não suporta troca de técnico (apenas edição de campos). Para trocar, é necessário usar o painel do dia no calendário.

---

## Seção de Usuários

### `renderUsrTable(data)`

Tabela com colunas: Usuário, E-mail, Perfil, Status, Último Login, Técnicos, Permissões, Ações.

**Último Login:**
```javascript
new Intl.DateTimeFormat('pt-BR', {
  day:'2-digit', month:'2-digit', year:'numeric',
  hour:'2-digit', minute:'2-digit',
  timeZone:'America/Sao_Paulo'  // converte de UTC para horário de Brasília
}).format(new Date(u.lastLoginAt))
```

O banco armazena em UTC. O `Intl.DateTimeFormat` com `timeZone:'America/Sao_Paulo'` converte automaticamente para o fuso de Brasília (UTC-3 ou UTC-2 no horário de verão).

**Coluna Técnicos:**
- Admin: exibe "Sempre" (admin sempre tem acesso)
- Usuário com acesso: botão verde "✓ Liberado"
- Usuário sem acesso: botão "Liberar"

### `toggleTechAccess(userId, current)`

```javascript
async function toggleTechAccess(userId, current) {
  await api(`/users/${userId}/technician-access`, {
    method: 'PATCH',
    body: JSON.stringify({ hasAccess: !current })
  });
  await loadUsers();
}
```

Alterna o acesso: se `current = true`, envia `false` (revoga). Se `current = false`, envia `true` (libera).

### Permissões no modal de usuário

O sistema de permissões usa "chips" visuais:

```javascript
function addPermChip() {
  const existing = newPermissions.findIndex(p => p.departmentId === dep.value);
  if (existing >= 0) newPermissions[existing].level = level;  // atualiza existente
  else newPermissions.push({ departmentId, departmentName, level });  // adiciona novo
  renderPermChips();
}
```

Cada chip exibe `"NomeDepartamento · NívelAcesso"` com um botão X para remover. Ao salvar:
1. Cria o usuário (POST /users)
2. Para cada permissão em `newPermissions`: POST /users/{id}/permissions

Para edição, compara permissões antigas com novas e faz DELETE das removidas + POST das novas/atualizadas.

---

## Máscara de Telefone (`maskPhone`)

```javascript
function maskPhone(el) {
  let v = el.value.replace(/\D/g, '').slice(0, 11);  // apenas dígitos, max 11
  if (v.length <= 10)
    v = v.replace(/(\d{2})(\d{4})(\d{0,4})/, '($1) $2-$3');  // fixo: (11) 1234-5678
  else
    v = v.replace(/(\d{2})(\d{5})(\d{0,4})/, '($1) $2-$3');  // celular: (11) 91234-5678
  el.value = v.replace(/-$/, '');  // remove hífen final incompleto
}
```

Chamada no evento `oninput` do campo de telefone. Suporta tanto fixo (8 dígitos) quanto celular (9 dígitos).

---

## Inicialização

```javascript
// No final do script:
setTopbar('departamentos');
loadDepartments();
```

Ao carregar o dashboard, configura o topbar e carrega os departamentos imediatamente.

---

## Função de Escape HTML (`esc`)

```javascript
function esc(s) {
  return String(s ?? '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}
```

Essencial para segurança: todos os dados vindos da API são escapados antes de serem inseridos como HTML via `innerHTML`. Isso previne XSS (Cross-Site Scripting).

---

## Sistema de Toast (notificações)

```javascript
function toast(msg, ms = 2800) {
  const el = document.getElementById('toast');
  el.textContent = msg;
  el.classList.add('show');
  setTimeout(() => el.classList.remove('show'), ms);
}
```

Exibe uma notificação no canto inferior direito por 2,8 segundos. Usado após toda operação de sucesso ("Item adicionado!", "Técnico atualizado!" etc.) e também para erros ("Erro: E-mail já cadastrado.").
