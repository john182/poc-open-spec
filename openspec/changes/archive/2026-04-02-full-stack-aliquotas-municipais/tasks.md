## 1. Análise e Documentação Inicial

- [x] 1.1 Analisar arquivos Bruno em `context/` e gerar `docs/external-api-analysis.md` documentando: endpoints, headers, parâmetros, formatos de resposta, divergências com README, premissas adotadas
- [x] 1.2 Analisar template PrimeNG Sakai em `TEMPLATE_FRONT_PATH` e gerar `docs/frontend-foundation.md` com mapeamento de reuso/adaptação/descarte por componente
- [x] 1.3 Gerar `docs/product-doc.md` com visão de produto, personas, fluxos de usuário e escopo funcional
- [x] 1.4 Gerar `docs/technical-doc.md` com arquitetura, stack, modelo de dados, fluxos técnicos e diagramas Mermaid
- [x] 1.5 Gerar `docs/api-contracts.md` com contratos detalhados de todos os endpoints da API REST interna
- [x] 1.6 Gerar `docs/worker-strategy.md` com estratégia completa do crawler: fila, concorrência, retry, circuit breaker, descoberta de serviços
- [x] 1.7 Gerar `docs/pr-strategy.md` com estratégia de branches, release PRs, micro PRs, naming conventions e checkpoints de integração
- [x] 1.8 Gerar `docs/test-strategy.md` com estratégia de testes unitários, integração e E2E, massa de dados e ambiente
- [x] 1.9 Gerar `docs/design-system.md` com paleta de cores, tipografia, espaçamento, componentes e padrões visuais
- [x] 1.10 Gerar `docs/design-tokens.md` com tokens CSS custom properties para cores, superfícies, bordas, sombras e breakpoints

## 2. Infraestrutura Docker e Projeto Base

- [x] 2.1 Criar `docker-compose.yml` na raiz com serviços: frontend, backend, mongodb, com health checks e volumes
- [x] 2.2 Criar `.env.example` com variáveis documentadas (MongoDB URI, JWT secret, PFX path, CRON schedule)
- [x] 2.3 Criar `backend/MapaTributário/Dockerfile` multi-stage (.NET SDK build + runtime)
- [x] 2.4 Criar `frontend/MapaTributario-ui/Dockerfile` multi-stage (Node build + nginx serve)
- [x] 2.5 Criar `frontend/MapaTributario-ui/nginx.conf` com proxy reverso para `/api/*`
- [x] 2.6 Atualizar `.gitignore` para incluir `.env`, `*.pfx`, volumes docker, e arquivos de build
- [x] 2.7 Validar `docker compose up` sobe todos os serviços corretamente

## 3. Backend — Estrutura Base e Autenticação

- [x] 3.1 Corrigir typo na pasta `infrastructure/Rempository` → `infrastructure/Repository`
- [x] 3.2 Configurar MongoDB Driver no `Program.cs` com connection string do `.env`/appsettings
- [x] 3.3 Criar modelo de domínio `User` (email, passwordHash, nome, dataCriacao, ativo)
- [x] 3.4 Criar repositório MongoDB para `User`
- [x] 3.5 Criar `AuthService` com register (bcrypt hash) e login (validação + geração JWT)
- [x] 3.6 Configurar JWT authentication/authorization no pipeline ASP.NET Core
- [x] 3.7 Criar `AuthController` com endpoints: POST register, POST login, POST refresh
- [x] 3.8 Criar DTOs de request/response para auth (RegisterRequest, LoginRequest, AuthResponse)
- [x] 3.9 Criar middleware de tratamento de erros com formato padronizado `{ erro, detalhes, codigo }`
- [x] 3.10 Criar health check endpoint `GET /health` com verificação de MongoDB
- [x] 3.11 Configurar Swagger/OpenAPI com documentação de todos os endpoints de auth
- [x] 3.12 Escrever testes unitários para `AuthService` (register, login, refresh, casos de erro)
- [x] 3.13 Escrever testes de integração para `AuthController` (fluxo completo de registro e login)

## 4. Backend — Seed de Dados e Endpoints de Consulta

- [x] 4.1 Criar modelos de domínio: `Estado`, `Municipio`, `Servico`, `Aliquota`
- [x] 4.2 Criar repositórios MongoDB para cada modelo com índices compostos
- [x] 4.3 Criar seed de estados (27 UFs) e municípios a partir de `context/municipios.json` (5.570 registros com Id, Codigo, Nome, Uf) executável no startup
- [x] 4.4 Criar seed de códigos de tributação nacional (~391 códigos ii.ss.dd do portal gov.br/nfse — derivados dos 40 itens e ~197 subitens da LC 116/2003)
- [x] 4.5 Criar `ConsultaService` com lógica de listagem paginada e filtros
- [x] 4.6 Criar `ConsultaController` com endpoints: GET estados, GET municipios por UF, GET aliquotas por município, GET detalhe aliquota
- [x] 4.7 Criar DTOs de response para consulta (EstadoResponse, MunicipioResponse, AliquotaResponse, AliquotaDetalheResponse, PaginatedResponse)
- [x] 4.8 Implementar normalização de código de serviço (aceitar com/sem pontos, retornar formatado)
- [x] 4.9 Documentar todos os endpoints de consulta no Swagger com exemplos
- [x] 4.10 Escrever testes unitários para `ConsultaService` (filtros, paginação, normalização)
- [x] 4.11 Escrever testes de integração para `ConsultaController` (endpoints com dados seeded)

## 5. Worker / Crawler

- [x] 5.1 Criar modelo `ExecucaoCrawler` e repositório MongoDB
- [x] 5.2 Criar modelo `FilaProcessamento` e repositório MongoDB com índices por status
- [x] 5.3 Criar `NfseApiClient` com HttpClient configurado para mTLS (PFX), timeout 30s e headers
- [x] 5.4 Implementar `RateLimiter` configurável (default 5 req/s — conservador para proteger certificado)
- [x] 5.5 Implementar `CircuitBreaker` (threshold 50%, janela 1min, pausa 5min)
- [x] 5.6 Criar `CrawlerService` com lógica de: geração de fila, processamento de item, upsert de alíquota
- [x] 5.7 Implementar retry com exponential backoff (30s, 2min, 8min, max 3 tentativas)
- [x] 5.8 Implementar processamento incremental (skip de combinações já coletadas na competência atual)
- [x] 5.9 Implementar descoberta de municípios via endpoint convênio (Fase 1: filtro — skip municípios sem convênio)
- [x] 5.9.1 Implementar probe de município (Fase 2: testar 5 serviços representativos de grupos diferentes — se todos falharem, marcar "sem_dados_adn" e skip inteiro)
- [x] 5.10 Criar `CrawlerBackgroundService` (IHostedService) com CRON configurável
- [x] 5.11 Criar `CrawlerController` com endpoints: POST executar, GET status, GET execuções + endpoints de certificado (POST/GET/DELETE)
- [x] 5.12 Implementar concurrency control com SemaphoreSlim (default 2 — conservador para proteger certificado)
- [x] 5.13 Registrar execução completa com métricas (total, processados, erros)
- [x] 5.14 Implementar early-stop na iteração de subdivisões de código de serviço (parar após 9 misses consecutivos no grupo XX.XX.XX)
- [x] 5.15 Implementar estratégia de proteção do certificado: pausa entre batches (30s a cada 50 itens), budget diário (50K req), throttling adaptativo (detectar 429/403/latência alta e reduzir velocidade), detecção de bloqueio (3× 403 consecutivos = halt + alerta)
- [x] 5.16 Escrever testes unitários para `CrawlerService` (geração de fila, processamento, retry, circuit breaker, early-stop, proteção do certificado)
- [x] 5.17 Escrever testes de integração para endpoints do crawler
- [x] 5.18 Implementar filtro por UF na execução do crawler (parâmetro opcional `ufs` no ExecutarCrawlerRequest)
- [x] 5.19 Implementar autorização Admin nos endpoints do crawler (`[Authorize(Roles = "Admin")]`)

## 6. Frontend — Fundação Visual (Fase Obrigatória)

- [x] 6.1 Adaptar layout do Sakai: criar `layout/` com AppLayout, AppTopbar, AppSidebar, AppMenu, AppFooter
- [x] 6.2 Adaptar `LayoutService` com signals para estado do menu (remover configurador de preset)
- [x] 6.3 Configurar design tokens como CSS custom properties em `styles.scss` (cores, superfícies, tipografia, espaçamento)
- [x] 6.4 Configurar suporte a dark mode via classe `app-dark`
- [x] 6.5 Criar componente `LoadingSpinner` em `shared/components/`
- [x] 6.6 Criar componente `EmptyState` em `shared/components/`
- [x] 6.7 Criar componente `ErrorState` com botão de retry em `shared/components/`
- [x] 6.8 Criar componente `PageHeader` com título e breadcrumb em `shared/components/`
- [x] 6.9 Criar componente `FilterBar` genérico em `shared/components/`
- [x] 6.10 Criar form helpers: field wrapper com label/erro, validation message mapping
- [x] 6.11 Configurar rotas base: layout autenticado, auth (fora do layout), wildcard 404
- [x] 6.12 Escrever testes unitários para componentes base (LoadingSpinner, EmptyState, ErrorState, FilterBar)

## 7. Frontend — Autenticação e Páginas Base

- [x] 7.1 Criar `AuthService` em `core/auth/` com login, register, refresh, logout, token storage
- [x] 7.2 Criar `AuthGuard` que redireciona para login se não autenticado
- [x] 7.3 Criar JWT interceptor HTTP que anexa token e trata 401 com refresh automático
- [x] 7.4 Criar página Sign In em `features/auth/login/` com formulário email+senha, link para signup, validação
- [x] 7.5 Criar página Sign Up em `features/auth/signup/` com formulário nome+email+senha+confirmação
- [x] 7.6 Criar página Access Denied em `features/errors/access-denied/`
- [x] 7.7 Criar página 404 Not Found em `features/errors/not-found/`
- [x] 7.8 Configurar menu lateral com item "Consulta de Alíquotas"
- [x] 7.9 Escrever testes unitários para AuthService, AuthGuard e JWT interceptor
- [x] 7.10 Escrever testes unitários para páginas de login e signup (validação, submit, erros)

## 7b. Frontend — Controle de Acesso por Role (issue #109, #110)

- [x] 7b.1 Criar `RoleService` em `core/auth/` que decodifica o JWT e expõe o role do usuário logado
- [x] 7b.2 Criar `AdminGuard` que redireciona para access-denied se o usuário não tem role Admin
- [x] 7b.3 Adaptar `AppMenu` para exibir/ocultar itens com base no role (Admin vê Crawler, User não)
- [x] 7b.4 Tornar rotas de consulta públicas (acessíveis sem autenticação)
- [x] 7b.5 Escrever testes unitários para RoleService, AdminGuard e menu condicional

## 8. Frontend — Feature de Consulta (Pública)

- [x] 8.1 Criar SVG do mapa do Brasil com paths por estado e atributos `data-uf`
- [x] 8.2 Criar componente `BrazilMap` em `features/consulta/mapa/` com hover e click por estado
- [x] 8.3 Criar `ConsultaService` em `features/consulta/services/` para chamadas à API de estados, municípios e alíquotas
- [x] 8.4 Criar página de consulta (mapa) em `features/consulta/mapa/`
- [x] 8.5 Criar página de municípios por estado em `features/consulta/estado/` com busca por texto
- [x] 8.6 Criar página de listagem de alíquotas por município em `features/consulta/municipio/` com tabela paginada
- [x] 8.7 Implementar filtros na listagem: código de serviço, descrição, faixa de alíquota, competência
- [x] 8.8 Criar componente de detalhe de alíquota (dialog ou inline expand)
- [x] 8.9 Implementar breadcrumb na navegação da consulta (Consulta > Estado > Município)
- [x] 8.10 Implementar estados de loading, vazio, erro e retry em todas as páginas de consulta
- [x] 8.11 Escrever testes unitários para BrazilMap, ConsultaService e páginas de consulta

## 8b. Frontend — Gerenciamento do Crawler (Admin Only, issue #111)

- [x] 8b.1 Criar `CrawlerService` em `features/admin/crawler/services/` para chamadas à API do crawler
- [x] 8b.2 Criar página de status do crawler em `features/admin/crawler/status/` com última execução e métricas
- [x] 8b.3 Criar botão de execução manual com opção de forçar reprocessamento e filtro por UF
- [x] 8b.4 Criar página de gerenciamento de certificado em `features/admin/crawler/certificado/` com upload, status e remoção
- [x] 8b.5 Criar página de histórico de execuções em `features/admin/crawler/execucoes/` com listagem
- [x] 8b.6 Escrever testes unitários para CrawlerService e páginas admin do crawler

## 9. Integração e Validação

- [x] 9.1 Validar integração frontend ↔ backend com docker compose up (auth flow completo)
- [x] 9.2 Validar integração backend ↔ MongoDB (seed + consulta)
- [x] 9.3 Validar worker executando ciclo completo com API NFS-e (se certificado disponível) ou mock
- [x] 9.4 Testar fluxo completo: login → mapa → estado → município → listagem → filtros
- [x] 9.5 Revisar contratos da API vs implementação real (Swagger vs código)

## 10. Projeto E2E com Cypress

- [x] 10.1 Inicializar projeto Cypress em `cypress/` (corrigir typo `ceypress` → `cypress`)
- [x] 10.2 Configurar `cypress.config.ts` com baseUrl apontando para frontend dockerizado
- [x] 10.3 Criar seed de dados de teste (2 usuários, 3 estados com municípios, 10 alíquotas)
- [x] 10.4 Criar custom commands para login e setup de teste
- [x] 10.5 Escrever teste E2E: fluxo de cadastro
- [x] 10.6 Escrever teste E2E: fluxo de login (sucesso e falha)
- [x] 10.7 Escrever teste E2E: acesso negado (rota protegida sem auth)
- [x] 10.8 Escrever teste E2E: página 404
- [x] 10.9 Escrever teste E2E: navegação pelo menu
- [x] 10.10 Escrever teste E2E: visualização do mapa e seleção de estado
- [x] 10.11 Escrever teste E2E: seleção de município
- [x] 10.12 Escrever teste E2E: listagem de serviços e alíquotas
- [x] 10.13 Escrever teste E2E: aplicação e limpeza de filtros
- [x] 10.14 Escrever teste E2E: cenários de erro (API indisponível, dados vazios)
- [x] 10.15 Adicionar atributos `data-cy` nos componentes do frontend para seletores estáveis

## 11. Documentação Final e Refinamento

- [x] 11.1 Atualizar README.md com instruções de setup, docker compose e desenvolvimento
- [x] 11.2 Revisar e atualizar `docs/api-contracts.md` com contratos finais implementados
- [x] 11.3 Revisar e atualizar diagramas Mermaid na documentação técnica
- [x] 11.4 Validar que todos os documentos de `docs/` estão consistentes com a implementação
- [x] 11.5 Revisar Swagger/OpenAPI gerado vs documentação de contratos

---

## Mapa de Dependências e Paralelização

```
Fase 1 (paralelo):
  ├── Grupo 1.1-1.10 (Análise e Docs) ── sem dependência
  ├── Grupo 2 (Docker/Infra) ── sem dependência
  └── Grupo 3.1-3.2 (Backend base) ── sem dependência

Fase 1→2 (sequencial):
  Grupo 3 (Backend Auth) depende de 3.1-3.2
  Grupo 6 (Frontend Fundação) depende de 1.2 (análise template)

Fase 2 (paralelo entre trilhas):
  ├── Grupo 4 (Backend Consulta) depende de Grupo 3
  ├── Grupo 5 (Worker) depende de Grupo 3 (compartilha infra MongoDB)
  ├── Grupo 7 (Frontend Auth) depende de Grupo 6
  └── Grupos 4, 5, 7 podem rodar em paralelo (baixa colisão)

Fase 3 (paralelo entre trilhas):
  ├── Grupo 7b (Frontend Role Control) depende de Grupo 7
  ├── Grupo 8 (Frontend Consulta Pública) depende de Grupo 7 + Grupo 4
  ├── Grupo 8b (Frontend Crawler Admin) depende de Grupo 7b + Grupo 5
  └── Grupos 8 e 8b podem rodar em paralelo (baixa colisão — features diferentes)

Fase 4:
  Grupo 9 (Integração) depende de Grupos 4, 5, 8, 8b
  Grupo 10 (E2E) depende de Grupo 9
  Grupo 11 (Docs Final) depende de Grupo 9
```

## Checkpoints de Integração

| Checkpoint | Trilhas envolvidas | Critério |
|-----------|-------------------|----------|
| CP1 | Backend + Docker | Auth endpoints funcionando via docker compose |
| CP2 | Frontend + Backend | Login/signup integrado ponta a ponta |
| CP3 | Worker + Backend + MongoDB | Worker executa ciclo e dados aparecem na API |
| CP4 | Frontend + Backend | Fluxo completo mapa→estado→município→listagem (público) |
| CP5 | Frontend Admin + Backend | Admin vê menu crawler, User não; endpoints protegidos |
| CP6 | E2E + Todos | Testes E2E passando no ambiente dockerizado |
