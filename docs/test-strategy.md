# Estrategia de Testes - Mapa Tributario

## Piramide de Testes

A estrategia segue a piramide classica de testes, priorizando testes unitarios na base e testes E2E no topo.

```
        /  E2E  \           <-- Poucos, lentos, alto valor
       / Integra \          <-- Moderados, endpoints + DB
      /  cao      \
     / Unitarios   \        <-- Muitos, rapidos, isolados
    /________________\
```

### Distribuicao Esperada

| Camada | Quantidade | Velocidade | Foco |
|--------|-----------|------------|------|
| Unitarios | ~70% dos testes | Rapidos (<1s cada) | Regras, servicos, componentes |
| Integracao | ~20% dos testes | Moderados (1-5s cada) | Endpoints, persistencia, fluxos |
| E2E | ~10% dos testes | Lentos (5-30s cada) | Fluxos criticos de usuario |

---

## Backend - Testes Unitarios

### Framework e Ferramentas

- **Framework:** xUnit
- **Mocking:** NSubstitute
- **Assertions:** FluentAssertions
- **Dados de teste:** Bogus (quando necessario gerar massa)

### O que testar

| Componente | O que cobrir | Prioridade |
|------------|-------------|------------|
| `AuthService` | Registro, login, validacao de credenciais, geracao de token, refresh token | Alta |
| `ConsultaService` | Busca por estado, busca por municipio, busca por servico, filtros combinados | Alta |
| `CrawlerService` | Parsing de resposta da API externa, normalizacao de dados, tratamento de erro | Alta |
| Normalizacao de codigo municipal | Codigos IBGE validos, codigos invalidos, formatos inesperados | Alta |
| Validadores de dominio | DTOs de entrada, regras de negocio, limites | Media |
| Mapeamento de entidades | Entity para DTO, DTO para Response, Response para modelo | Media |

### Estrategia de Mocking

```
Camada testada        O que mockar
-----------------     ---------------------------
Application Services  Repositories, External APIs
Domain Services       Nenhum (logica pura)
Controllers           Application Services
Validators            Nenhum (logica pura)
```

**Regras de mocking:**
- Mockar sempre dependencias externas (banco, APIs, file system)
- Nunca mockar a classe sendo testada
- Preferir fakes simples a mocks complexos quando possivel
- Mockar repositorios, nunca o MongoDB diretamente nos testes unitarios

### Exemplo de Organizacao

```
backend/
  tests/
    Unit/
      Services/
        AuthServiceTests.cs
        ConsultaServiceTests.cs
        CrawlerServiceTests.cs
      Domain/
        CodigoMunicipalTests.cs
        AliquotaTests.cs
      Validators/
        RegisterRequestValidatorTests.cs
        ConsultaFilterValidatorTests.cs
```

---

## Backend - Testes de Integracao

### Framework e Ferramentas

- **Framework:** xUnit com `WebApplicationFactory<Program>`
- **Banco:** MongoDB em container Docker (testcontainers ou docker compose)
- **HTTP Client:** `HttpClient` do TestServer
- **Fixtures:** Classes de setup compartilhadas

### O que testar

| Cenario | Descricao |
|---------|-----------|
| Auth endpoints | POST /auth/register, POST /auth/login, POST /auth/refresh |
| Consulta endpoints | GET /estados, GET /estados/{uf}/municipios, GET /municipios/{codigo}/servicos |
| Filtros | Query params de filtro funcionando com MongoDB |
| Autorizacao | Endpoints protegidos retornam 401 sem token, 403 sem permissao |
| Erros | Payload invalido retorna 400, recurso inexistente retorna 404 |
| Persistencia | Dados gravados no MongoDB sao recuperados corretamente |

### MongoDB em Docker para Testes

```yaml
# docker-compose.test.yml
services:
  mongodb-test:
    image: mongo:7
    ports:
      - "27018:27017"
    environment:
      MONGO_INITDB_DATABASE: mapatributario_test
    tmpfs:
      - /data/db
```

Usar `tmpfs` para que os dados sejam descartados ao final dos testes, garantindo isolamento.

### Fixtures de Teste

Cada suite de integracao deve:

1. Criar dados de seed antes de executar
2. Limpar a colecao apos cada teste ou usar banco isolado por classe
3. Nao depender de ordem de execucao dos testes

```
backend/
  tests/
    Integration/
      Fixtures/
        TestWebApplicationFactory.cs
        MongoDbFixture.cs
        SeedData.cs
      Auth/
        AuthEndpointsTests.cs
      Consulta/
        ConsultaEndpointsTests.cs
        FiltrosEndpointsTests.cs
```

---

## Frontend - Testes Unitarios

### Framework e Ferramentas

- **Framework:** Vitest
- **Test utilities:** Angular Testing Library ou TestBed
- **Mocking:** vi.mock, vi.fn
- **DOM:** jsdom (via Vitest)

### O que testar

| Componente | O que cobrir | Prioridade |
|------------|-------------|------------|
| Componentes base (`LoadingSpinner`, `EmptyState`, `ErrorState`) | Renderizacao condicional, inputs, outputs | Alta |
| `PageHeader` | Titulo, breadcrumb, acoes | Media |
| `FilterBar` | Emissao de eventos, estado dos filtros | Alta |
| `FormField` | Exibicao de erros, label, estado disabled | Alta |
| Auth guard | Redirecionar se nao autenticado, permitir se autenticado | Alta |
| Auth interceptor | Adicionar token, tratar 401, refresh token | Alta |
| `AuthService` | Login, logout, estado de autenticacao | Alta |
| `ConsultaService` | Chamadas HTTP, mapeamento de resposta | Media |
| Form helpers | Validacao de campos, mapeamento de mensagens de erro | Media |
| Pipes e diretivas | Formatacao, comportamento | Baixa |

### Estrategia de Teste para Componentes

Testar comportamento, nao implementacao interna:

- **Sim:** "Quando o usuario clica no botao, o evento de filtro e emitido"
- **Nao:** "O metodo privado `_buildFilter` retorna o objeto correto"

```
frontend/
  src/
    app/
      shared/
        components/
          loading-spinner/
            loading-spinner.component.ts
            loading-spinner.component.spec.ts
          empty-state/
            empty-state.component.ts
            empty-state.component.spec.ts
          error-state/
            error-state.component.ts
            error-state.component.spec.ts
      core/
        guards/
          auth.guard.spec.ts
        interceptors/
          auth.interceptor.spec.ts
        services/
          auth.service.spec.ts
          consulta.service.spec.ts
```

---

## E2E - Cypress

### Estrutura do Projeto

```
e2e/
  cypress/
    e2e/
      auth.cy.ts
      navigation.cy.ts
      map.cy.ts
      consultation.cy.ts
      errors.cy.ts
    fixtures/
      users.json
      estados.json
      municipios.json
      servicos.json
    support/
      commands.ts
      e2e.ts
    plugins/
      index.ts
  cypress.config.ts
  tsconfig.json
```

### Ambiente de Teste

O ambiente E2E roda inteiramente via Docker Compose com perfil de teste.

```yaml
# docker-compose.e2e.yml
services:
  api:
    build: ./backend
    environment:
      - ASPNETCORE_ENVIRONMENT=Test
      - MongoDB__ConnectionString=mongodb://mongodb-e2e:27017
      - MongoDB__DatabaseName=mapatributario_e2e
    depends_on:
      - mongodb-e2e

  mongodb-e2e:
    image: mongo:7
    ports:
      - "27019:27017"

  frontend:
    build: ./frontend
    ports:
      - "4200:80"
    depends_on:
      - api

  seed:
    build: ./backend
    command: ["dotnet", "run", "--project", "Seed", "--", "--environment", "e2e"]
    depends_on:
      - mongodb-e2e
```

### Estrategia de Seed

A massa de dados para E2E deve ser:

1. **Deterministica:** Mesmos dados toda vez, sem aleatoriedade
2. **Minima:** Apenas o necessario para cobrir os cenarios
3. **Isolada:** Cada execucao comeca com dados limpos

**Dados de seed:**
- 2 usuarios (um valido, um para teste de permissao)
- 3 estados com dados completos (SP, RJ, MG)
- 5 municipios por estado de teste
- 10 servicos com aliquotas variadas por municipio de teste
- 1 municipio sem servicos (para testar estado vazio)

### Custom Commands

```typescript
// support/commands.ts

// Autenticacao
Cypress.Commands.add('login', (email?: string, password?: string) => { /* ... */ });
Cypress.Commands.add('logout', () => { /* ... */ });
Cypress.Commands.add('register', (user: RegisterPayload) => { /* ... */ });

// Navegacao
Cypress.Commands.add('navigateTo', (route: string) => { /* ... */ });

// API
Cypress.Commands.add('seedDatabase', () => { /* ... */ });
Cypress.Commands.add('resetDatabase', () => { /* ... */ });

// Seletores
Cypress.Commands.add('getByCy', (selector: string) => {
  return cy.get(`[data-cy="${selector}"]`);
});
```

### Convencao de Seletores

Usar atributo `data-cy` para todos os elementos que serao acessados nos testes E2E.

```html
<!-- Sim -->
<button data-cy="login-submit">Entrar</button>
<input data-cy="email-input" />
<div data-cy="state-SP" />

<!-- Nao -->
<button class="btn-primary">Entrar</button>
<button id="loginBtn">Entrar</button>
```

**Nomenclatura de seletores:**
- Formularios: `{form}-{field}` (ex: `login-email`, `login-password`, `login-submit`)
- Navegacao: `nav-{item}` (ex: `nav-home`, `nav-consulta`)
- Mapa: `map-{element}` (ex: `map-container`, `state-SP`, `state-RJ`)
- Listagem: `list-{contexto}` (ex: `list-municipios`, `list-servicos`)
- Acoes: `action-{verbo}` (ex: `action-filter`, `action-clear`)

### Suites de Teste

#### auth.cy.ts - Autenticacao

| Teste | Descricao |
|-------|-----------|
| Registro com dados validos | Preencher formulario, submeter, verificar redirecionamento |
| Registro com email duplicado | Submeter com email existente, verificar mensagem de erro |
| Login com credenciais validas | Preencher login, submeter, verificar acesso ao app |
| Login com credenciais invalidas | Submeter com senha errada, verificar mensagem de erro |
| Login com campos vazios | Submeter sem preencher, verificar validacao |
| Logout | Clicar em sair, verificar redirecionamento para login |
| Acesso a rota protegida sem auth | Navegar para /consulta sem login, verificar redirect |

#### navigation.cy.ts - Navegacao

| Teste | Descricao |
|-------|-----------|
| Menu lateral renderiza | Verificar que o menu lateral esta visivel apos login |
| Navegacao por menu | Clicar em items do menu, verificar rota ativa |
| Breadcrumb atualiza | Navegar entre paginas, verificar breadcrumb correto |
| Toggle do menu | Clicar no toggle, verificar que o menu colapsa/expande |
| Navegacao por URL direta | Acessar URL direta, verificar pagina correta |

#### map.cy.ts - Mapa

| Teste | Descricao |
|-------|-----------|
| Mapa renderiza | Verificar que o componente de mapa esta visivel |
| Hover em estado | Passar mouse sobre estado, verificar destaque visual |
| Click em estado | Clicar em estado, verificar navegacao para municipios |
| Todos os estados clicaveis | Verificar que todos os 27 estados (26 + DF) sao interativos |
| Voltar do estado para mapa | Navegar de volta, verificar retorno ao mapa |

#### consultation.cy.ts - Consulta

| Teste | Descricao |
|-------|-----------|
| Lista de municipios por estado | Selecionar estado, verificar lista de municipios |
| Buscar municipio por nome | Digitar nome no filtro, verificar resultados filtrados |
| Listagem de servicos por municipio | Selecionar municipio, verificar servicos e aliquotas |
| Filtrar por codigo de servico | Aplicar filtro, verificar resultados |
| Filtrar por faixa de aliquota | Aplicar filtro de aliquota, verificar resultados |
| Limpar filtros | Aplicar filtros, limpar, verificar todos os resultados |
| Estado vazio (municipio sem servicos) | Selecionar municipio sem dados, verificar mensagem vazia |
| Paginacao | Navegar entre paginas quando houver muitos resultados |

#### errors.cy.ts - Erros

| Teste | Descricao |
|-------|-----------|
| Pagina 404 | Acessar URL inexistente, verificar pagina 404 |
| Acesso negado | Acessar rota sem permissao, verificar pagina 403 |
| Erro de API | Simular falha na API, verificar mensagem de erro e retry |
| Timeout de API | Simular timeout, verificar tratamento |
| Erro de rede | Simular offline, verificar mensagem |

---

## Massa de Dados

### Fixtures

```json
// fixtures/users.json
{
  "validUser": {
    "name": "Usuario Teste",
    "email": "teste@mapatributario.com",
    "password": "Teste@123"
  },
  "invalidUser": {
    "email": "invalido@teste.com",
    "password": "senhaerrada"
  }
}
```

```json
// fixtures/estados.json
{
  "estados": [
    { "uf": "SP", "nome": "Sao Paulo", "totalMunicipios": 5 },
    { "uf": "RJ", "nome": "Rio de Janeiro", "totalMunicipios": 5 },
    { "uf": "MG", "nome": "Minas Gerais", "totalMunicipios": 5 }
  ]
}
```

```json
// fixtures/municipios.json
{
  "SP": [
    { "codigo": "3550308", "nome": "Sao Paulo", "totalServicos": 10 },
    { "codigo": "3509502", "nome": "Campinas", "totalServicos": 8 },
    { "codigo": "3547809", "nome": "Santos", "totalServicos": 0 }
  ]
}
```

### Estrategia de Reset

Antes de cada suite:
1. Chamar endpoint de reset do banco de teste: `POST /api/test/reset`
2. Executar seed: `POST /api/test/seed`
3. Verificar que seed foi aplicado: `GET /api/test/health`

Esses endpoints so existem quando `ASPNETCORE_ENVIRONMENT=Test`.

---

## Ambiente de Teste

### Docker Compose - Perfil de Teste

```bash
# Subir ambiente de testes
docker compose -f docker-compose.yml -f docker-compose.e2e.yml up -d

# Rodar testes E2E
cd e2e && npx cypress run

# Derrubar ambiente
docker compose -f docker-compose.yml -f docker-compose.e2e.yml down -v
```

### CI Pipeline

```
1. Build de imagens Docker
2. Subir ambiente com docker compose
3. Rodar testes unitarios (backend + frontend em paralelo)
4. Rodar testes de integracao (backend)
5. Rodar seed do banco E2E
6. Rodar testes E2E
7. Coletar relatorios de cobertura
8. Derrubar ambiente
```

---

## Metas de Cobertura

| Camada | Meta Minima | Foco |
|--------|-------------|------|
| Backend unitario | 80%+ | Services, domain, validators |
| Backend integracao | Endpoints criticos 100% | Auth, consulta, erros |
| Frontend unitario | 70%+ | Componentes, guards, interceptors, services |
| E2E | Fluxos criticos 100% | Auth, navegacao, consulta, erros |

### Detalhamento das Metas

**Backend 80%+:**
- `AuthService`: 90%+
- `ConsultaService`: 90%+
- `CrawlerService`: 85%+
- Domain models: 80%+
- Controllers: Cobertos via testes de integracao

**Frontend 70%+:**
- Componentes base: 90%+
- Guards e interceptors: 90%+
- Services: 80%+
- Pages: 60%+ (complementado por E2E)

**E2E 100% dos fluxos criticos:**
- Login/logout: Obrigatorio
- Registro: Obrigatorio
- Navegacao autenticada: Obrigatorio
- Selecao no mapa: Obrigatorio
- Consulta de aliquotas: Obrigatorio
- Filtros: Obrigatorio
- Erros (404, 403, API): Obrigatorio

---

## O que NAO Testar

### Backend
- Getters e setters triviais sem logica
- Construtores que apenas atribuem valores
- Configuracao de injecao de dependencia (coberta por testes de integracao)
- Detalhes internos do MongoDB driver

### Frontend
- Detalhes de implementacao interna de componentes
- Internals do PrimeNG (confiar na lib)
- CSS e estilos visuais (usar testes visuais separados se necessario)
- Chamadas do Angular lifecycle que nao contem logica
- Templates HTML sem logica condicional

### E2E
- Cenarios ja cobertos adequadamente por testes unitarios
- Combinatorias exaustivas de filtros (cobrir representativos)
- Performance (usar ferramentas especificas)
- Testes de layout pixel-perfect
