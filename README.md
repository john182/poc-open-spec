# Mapa Tributario — Crawler de Aliquotas ISS

Um projeto full-stack que coleta periodicamente aliquotas de ISS da API NFS-e (adn.nfse.gov.br) e apresenta os dados atraves de um frontend interativo com navegacao por mapa, estado, municipio e codigo de servico.

---

## Quick Start

```bash
# 1. Clone
git clone https://github.com/john182/poc-open-spec.git
cd poc-open-spec

# 2. Configure
cp .env.example .env

# 3. Suba tudo
docker compose up --build
```

**URLs locais:**

| Servico | URL |
|---------|-----|
| Frontend | http://localhost:4200 |
| Backend API | http://localhost:5000/api/v1 |
| Health Check | http://localhost:5000/health |

---

## Arquitetura

```
┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│   Crawler    │────────> │   MongoDB    │ <───────│  Backend API │
│   Worker     │         │              │         │  (.NET 10)   │
│(BackgroundSvc)│         └──────────────┘         └───────┬──────┘
└──────────────┘                                           │ HTTP
                                                           │
                                                    ┌──────v──────┐
                                                    │  Frontend   │
                                                    │ (Angular 21)│
                                                    └─────────────┘
```

**Stack:**
- **Backend:** .NET 10, ASP.NET Core, MongoDB Driver, JWT Bearer, FluentValidation, FluentResults
- **Worker:** BackgroundService .NET com CRON, mTLS/PFX para API NFS-e
- **Frontend:** Angular 21, PrimeNG, Tailwind CSS
- **Banco:** MongoDB
- **Infra:** Docker Compose, Nginx reverse proxy
- **Testes:** xUnit + Moq + Testcontainers (backend), Vitest + Testing Library (frontend), Cypress (E2E)

---

## Estrutura do Projeto

```
├── backend/MapaTributario/
│   ├── src/MapaTributario.API/
│   │   ├── Application/          # Use cases e DTOs
│   │   ├── Controllers/          # Endpoints REST
│   │   ├── Domain/               # Entidades e interfaces
│   │   ├── Extensions/           # DI service extensions
│   │   ├── Infrastructure/       # Repositorios MongoDB, auth, external clients
│   │   ├── Middleware/           # Error handling
│   │   ├── Validators/          # FluentValidation
│   │   └── Worker/              # BackgroundService (crawler)
│   └── tests/
│       ├── MapaTributario.Tests.Unit/
│       └── MapaTributario.Tests.Integration/
├── frontend/MapaTributario-ui/
│   └── src/app/
│       ├── core/                # Auth, guards, interceptors
│       ├── features/            # Auth, consulta, admin, errors
│       ├── layout/              # Layout components (topbar, sidebar, menu, footer)
│       └── shared/              # Componentes reutilizaveis
├── cypress/                     # Testes E2E
├── context/                     # Dados de referencia (municipios.json)
├── openspec/                    # Specs e docs do projeto
├── docker-compose.yml
└── .env.example
```

---

## Documentacao

| Documento | Descricao |
|-----------|-----------|
| [Produto](docs/product-doc.md) | Visao geral do produto, personas, motivacao para autenticacao, escopo MVP |
| [Arquitetura Tecnica](docs/technical-doc.md) | Stack, decisoes arquiteturais (ADRs), estrutura de camadas, seguranca |
| [Contratos da API](docs/api-contracts.md) | Endpoints, payloads, codigos de resposta, exemplos de requisicao |
| [Estrategia de Testes](docs/test-strategy.md) | Piramide de testes, cobertura, convencoes de nomenclatura |
| [Estrategia do Worker](docs/worker-strategy.md) | Crawler: fases de execucao, early-stop, protecao do certificado |
| [Fundacao Frontend](docs/frontend-foundation.md) | Estrutura Angular, componentes de layout, design tokens, rotas |
| [Design System](docs/design-system.md) | Componentes visuais, paleta de cores, tipografia |
| [Design Tokens](docs/design-tokens.md) | Tokens CSS, variaveis de tema, personalizacao PrimeNG |
| [Estrategia de PRs](docs/pr-strategy.md) | Fluxo de branches, convencao de commits, processo de review |
| [API Externa NFS-e](docs/external-api-analysis.md) | Analise da API adn.nfse.gov.br, endpoints, autenticacao mTLS |

---

## Desenvolvimento Local

### Pre-requisitos

- .NET 10 SDK
- Node.js 22+
- MongoDB (local ou via Docker)
- Angular CLI (`npm install -g @angular/cli`)

### Backend

```bash
cd backend/MapaTributario
dotnet restore
dotnet run --project src/MapaTributario.API
# API disponivel em https://localhost:5001
```

### Frontend

```bash
cd frontend/MapaTributario-ui
npm install
ng serve
# App disponivel em http://localhost:4200
```

### MongoDB (standalone)

```bash
docker run -d -p 27017:27017 --name mongo mongo:8
```

---

## Testes

```bash
# Backend — unit + integration
cd backend/MapaTributario
dotnet test

# Frontend — unit
cd frontend/MapaTributario-ui
npx vitest run

# E2E — Cypress
cd cypress
npx cypress run
```

---

## API Endpoints

### Autenticacao (publicos)

| Metodo | Endpoint | Descricao |
|--------|----------|-----------|
| POST | /api/v1/auth/register | Cadastro de usuario |
| POST | /api/v1/auth/login | Login (retorna JWT) |
| POST | /api/v1/auth/refresh | Refresh do access token |

### Consulta (publicos)

| Metodo | Endpoint | Descricao |
|--------|----------|-----------|
| GET | /api/v1/estados | Lista 27 estados |
| GET | /api/v1/estados/:uf/municipios | Municipios por UF |
| GET | /api/v1/municipios/:ibge/aliquotas | Aliquotas paginadas com filtros |
| GET | /api/v1/municipios/:ibge/aliquotas/:servico | Detalhe de aliquota |

### Crawler (requerem JWT + role Admin)

| Metodo | Endpoint | Descricao |
|--------|----------|-----------|
| POST | /api/v1/crawler/executar | Trigger manual do crawler (com filtro por UF) |
| GET | /api/v1/crawler/status | Status da ultima execucao |
| GET | /api/v1/crawler/execucoes | Historico de execucoes |
| POST | /api/v1/crawler/certificado | Upload de certificado PFX |
| GET | /api/v1/crawler/certificado | Status do certificado |
| DELETE | /api/v1/crawler/certificado | Remover certificado |

### Infra

| Metodo | Endpoint | Descricao |
|--------|----------|-----------|
| GET | /health | Health check (inclui MongoDB) |

---

## Status do Projeto

### Round 1 — Concluido

- [x] **Infra Docker** — Docker Compose, Dockerfiles, Nginx, .env
- [x] **Backend Auth** — JWT auth (register/login/refresh), DDD, FluentResults, FluentValidation, 97% coverage
- [x] **Frontend Foundation** — Layout PrimeNG, design tokens, componentes base, rotas, Vitest + Cypress base

### Round 2 — Concluido

- [x] **Backend Consulta** — Seed IBGE + endpoints de consulta com filtros e paginacao
- [x] **Worker Crawler** — Crawler com protecao do certificado, 3 fases, early-stop, filtro por UF
- [x] **Frontend Auth** — AuthService, guards, interceptor JWT, paginas login/signup reais
- [x] **Frontend Role Control** — RoleService, AdminGuard, menu condicional por role
- [x] **Frontend Consulta** — Mapa do Brasil, selecao estado/municipio, listagem aliquotas com filtros
- [x] **Frontend Crawler Admin** — Status, execucao manual, certificado, historico de execucoes
- [x] **E2E Cypress** — Testes de auth, layout, navegacao, consulta e crawler admin
- [x] **Docs** — Contratos, especificacoes, estrategias documentadas

---

## Referencia da API Externa

**API NFS-e:** `adn.nfse.gov.br` (requer certificado PFX/mTLS)

```
GET /parametrizacao/{municipio}/{servico}/{competencia}/aliquota
GET /parametrizacao/{municipio}/convenio
```

Consulte `openspec/changes/full-stack-aliquotas-municipais/docs/external-api-analysis.md` para detalhes completos.
