# Mapa Tributario — Crawler de Aliquotas ISS

[![CI](https://github.com/john182/poc-open-spec/actions/workflows/ci.yml/badge.svg)](https://github.com/john182/poc-open-spec/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/john182/poc-open-spec/graph/badge.svg)](https://codecov.io/gh/john182/poc-open-spec)

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![Angular 21](https://img.shields.io/badge/Angular-21-DD0031?logo=angular&logoColor=white)
![MongoDB](https://img.shields.io/badge/MongoDB-7-47A248?logo=mongodb&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)

[![SonarQube Cloud](https://sonarcloud.io/images/project_badges/sonarcloud-light.svg)](https://sonarcloud.io/summary/new_code?id=john182_poc-open-spec)

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
| [Benchmark do Crawler](docs/crawler-benchmark-otimizacao.md) | Resultados de benchmark e otimizacoes de performance do crawler |

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
# API disponivel em http://localhost:5000
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
docker run -d -p 27018:27017 --name mongo mongo:7
```

> **Nota:** A porta padrão do host para o MongoDB neste projeto e `27018` (mapeada para `27017` dentro do container), conforme definido no `docker-compose.yml`.

---

## Testes

```bash
# Backend — unit + integration
cd backend/MapaTributario
dotnet test

# Frontend — unit
cd frontend/MapaTributario-ui
npx ng test --watch=false

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

### Consulta (requerem JWT)

| Metodo | Endpoint | Descricao |
|--------|----------|-----------|
| GET | /api/v1/estados | Lista 27 estados |
| GET | /api/v1/estados/:uf/municipios | Municipios por UF |
| GET | /api/v1/municipios/:ibge/aliquotas | Aliquotas paginadas com filtros |
| GET | /api/v1/municipios/:ibge/aliquotas/:servico | Detalhe de aliquota |

### Crawler (requerem JWT + role Admin)

| Metodo | Endpoint | Descricao |
|--------|----------|-----------|
| POST | /api/v1/crawler/executar | Trigger manual (filtro UF, capitais primeiro) |
| GET | /api/v1/crawler/status | Status da ultima execucao |
| GET | /api/v1/crawler/execucoes | Historico de execucoes |
| POST | /api/v1/crawler/certificado | Upload de certificado PFX |
| GET | /api/v1/crawler/certificado | Status do certificado |
| DELETE | /api/v1/crawler/certificado | Remover certificado |
| GET | /api/v1/crawler/configuracao | Obter configuracao atual do crawler |
| PUT | /api/v1/crawler/configuracao | Atualizar configuracao completa |
| PATCH | /api/v1/crawler/configuracao | Atualizar campos especificos da configuracao |

### Infra

| Metodo | Endpoint | Descricao |
|--------|----------|-----------|
| GET | /health | Health check (inclui MongoDB) |

---

## Status do Projeto

### Round 1 — Concluido

- [x] **Infra Docker** — Docker Compose, Dockerfiles, Nginx, .env
- [x] **Backend Auth** — JWT auth (register/login/refresh), DDD, FluentResults, FluentValidation
- [x] **Frontend Foundation** — Layout PrimeNG, design tokens, componentes base, rotas, Vitest + Cypress base

### Round 2 — Concluido

- [x] **Backend Consulta** — Seed IBGE + endpoints de consulta com filtros e paginacao
- [x] **Worker Crawler** — Crawler com protecao do certificado, 3 fases, early-stop, filtro por UF
- [x] **Frontend Auth** — AuthService, guards, interceptor JWT, paginas login/signup reais
- [x] **Frontend Role Control** — RoleService, AdminGuard, menu condicional por role
- [x] **Frontend Consulta** — Mapa do Brasil, selecao estado/municipio, listagem aliquotas com filtros
- [x] **Frontend Crawler Admin** — Status, execucao manual, certificado, historico de execucoes

### Round 3 — Concluido

- [x] **Collection configuracoesCrawler** — Entidade, repositorio, seed, compound index, constante UfsBrasil
- [x] **API CRUD Configuracao** — GET/PUT/PATCH com validacao compartilhada FluentValidation
- [x] **Tela Angular Admin** — Configuracao do crawler, tooltips, labels sem detalhes de implementacao
- [x] **Tracking por UF** — Progresso por UF com polling 5s na tela de status
- [x] **Capitais Primeiro** — Botao que processa capitais estaduais primeiro, depois demais municipios

### Integracao e Qualidade — Concluido

- [x] **Integracao Ponta a Ponta (Issue #9)** — Fluxo completo admin: upload certificado, configurar crawler, executar, consultar aliquotas coletadas
- [x] **E2E Cypress (Issue #10)** — 48 testes E2E cobrindo auth, layout, consulta, crawler admin e erros
- [x] **Persistencia de Certificado PFX** — Certificado armazenado em MongoDB (colecao `certificados_digitais`) com cache em memoria
- [x] **Documentacao Final (Issue #11)** — Atualizacao de README, contratos, diagramas e estrategia de testes

### Metricas de Teste

| Camada | Testes | Framework |
|--------|--------|-----------|
| Backend unitario | 457 | xUnit + Moq + Shouldly |
| Backend integracao | 38 | xUnit + Testcontainers + MongoDB |
| Frontend unitario | 281 | Vitest + Angular Testing Library |
| E2E | 48 (5 specs) | Cypress 14.4.1 |

---

## Referencia da API Externa

**API NFS-e:** `adn.nfse.gov.br` (requer certificado PFX/mTLS)

```
GET /parametrizacao/{municipio}/{servico}/{competencia}/aliquota
GET /parametrizacao/{municipio}/convenio
```

Consulte `docs/external-api-analysis.md` para detalhes completos.
