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

- [ ] 2.1 Criar `docker-compose.yml` na raiz com serviços: frontend, backend, mongodb, com health checks e volumes
- [ ] 2.2 Criar `.env.example` com variáveis documentadas (MongoDB URI, JWT secret, PFX path, CRON schedule)
- [ ] 2.3 Criar `backend/MapaTributário/Dockerfile` multi-stage (.NET SDK build + runtime)
- [ ] 2.4 Criar `frontend/MapaTributario-ui/Dockerfile` multi-stage (Node build + nginx serve)
- [ ] 2.5 Criar `frontend/MapaTributario-ui/nginx.conf` com proxy reverso para `/api/*`
- [ ] 2.6 Atualizar `.gitignore` para incluir `.env`, `*.pfx`, volumes docker, e arquivos de build
- [ ] 2.7 Validar `docker compose up` sobe todos os serviços corretamente

## 3. Backend — Estrutura Base e Autenticação

- [ ] 3.1 Corrigir typo na pasta `infrastructure/Rempository` → `infrastructure/Repository`
- [ ] 3.2 Configurar MongoDB Driver no `Program.cs` com connection string do `.env`/appsettings
- [ ] 3.3 Criar modelo de domínio `User` (email, passwordHash, nome, dataCriacao, ativo)
- [ ] 3.4 Criar repositório MongoDB para `User`
- [ ] 3.5 Criar `AuthService` com register (bcrypt hash) e login (validação + geração JWT)
- [ ] 3.6 Configurar JWT authentication/authorization no pipeline ASP.NET Core
- [ ] 3.7 Criar `AuthController` com endpoints: POST register, POST login, POST refresh
- [ ] 3.8 Criar DTOs de request/response para auth (RegisterRequest, LoginRequest, AuthResponse)
- [ ] 3.9 Criar middleware de tratamento de erros com formato padronizado `{ erro, detalhes, codigo }`
- [ ] 3.10 Criar health check endpoint `GET /health` com verificação de MongoDB
- [ ] 3.11 Configurar Swagger/OpenAPI com documentação de todos os endpoints de auth
- [ ] 3.12 Escrever testes unitários para `AuthService` (register, login, refresh, casos de erro)
- [ ] 3.13 Escrever testes de integração para `AuthController` (fluxo completo de registro e login)

## 4. Backend — Seed de Dados e Endpoints de Consulta

- [ ] 4.1 Criar modelos de domínio: `Estado`, `Municipio`, `Servico`, `Aliquota`
- [ ] 4.2 Criar repositórios MongoDB para cada modelo com índices compostos
- [ ] 4.3 Criar seed de estados (27 UFs) e municípios a partir de `context/municipios.json` (5.570 registros com Id, Codigo, Nome, Uf) executável no startup
- [ ] 4.4 Criar seed de códigos de serviço (LC 116/2003)
- [ ] 4.5 Criar `ConsultaService` com lógica de listagem paginada e filtros
- [ ] 4.6 Criar `ConsultaController` com endpoints: GET estados, GET municipios por UF, GET aliquotas por município, GET detalhe aliquota
- [ ] 4.7 Criar DTOs de response para consulta (EstadoResponse, MunicipioResponse, AliquotaResponse, AliquotaDetalheResponse, PaginatedResponse)
- [ ] 4.8 Implementar normalização de código de serviço (aceitar com/sem pontos, retornar formatado)
- [ ] 4.9 Documentar todos os endpoints de consulta no Swagger com exemplos
- [ ] 4.10 Escrever testes unitários para `ConsultaService` (filtros, paginação, normalização)
- [ ] 4.11 Escrever testes de integração para `ConsultaController` (endpoints com dados seeded)

## 5. Worker / Crawler

- [ ] 5.1 Criar modelo `ExecucaoCrawler` e repositório MongoDB
- [ ] 5.2 Criar modelo `FilaProcessamento` e repositório MongoDB com índices por status
- [ ] 5.3 Criar `NfseApiClient` com HttpClient configurado para mTLS (PFX), timeout 30s e headers
- [ ] 5.4 Implementar `RateLimiter` configurável (default 5 req/s — conservador para proteger certificado)
- [ ] 5.5 Implementar `CircuitBreaker` (threshold 50%, janela 1min, pausa 5min)
- [ ] 5.6 Criar `CrawlerService` com lógica de: geração de fila, processamento de item, upsert de alíquota
- [ ] 5.7 Implementar retry com exponential backoff (30s, 2min, 8min, max 3 tentativas)
- [ ] 5.8 Implementar processamento incremental (skip de combinações já coletadas na competência atual)
- [ ] 5.9 Implementar descoberta de municípios via endpoint convênio (Fase 1: filtro — skip municípios sem convênio)
- [ ] 5.9.1 Implementar probe de município (Fase 2: testar 5 serviços representativos de grupos diferentes — se todos falharem, marcar "sem_dados_adn" e skip inteiro)
- [ ] 5.10 Criar `CrawlerBackgroundService` (IHostedService) com CRON configurável
- [ ] 5.11 Criar `CrawlerController` com endpoints: POST executar, GET status, GET execuções
- [ ] 5.12 Implementar concurrency control com SemaphoreSlim (default 2 — conservador para proteger certificado)
- [ ] 5.13 Registrar execução completa com métricas (total, processados, erros)
- [ ] 5.14 Implementar early-stop na iteração de subdivisões de código de serviço (parar após 9 misses consecutivos no grupo XX.XX.XX)
- [ ] 5.15 Implementar estratégia de proteção do certificado: pausa entre batches (30s a cada 50 itens), budget diário (50K req), throttling adaptativo (detectar 429/403/latência alta e reduzir velocidade), detecção de bloqueio (3× 403 consecutivos = halt + alerta)
- [ ] 5.16 Escrever testes unitários para `CrawlerService` (geração de fila, processamento, retry, circuit breaker, early-stop, proteção do certificado)
- [ ] 5.17 Escrever testes de integração para endpoints do crawler

## 6. Frontend — Fundação Visual (Fase Obrigatória)

- [ ] 6.1 Adaptar layout do Sakai: criar `layout/` com AppLayout, AppTopbar, AppSidebar, AppMenu, AppFooter
- [ ] 6.2 Adaptar `LayoutService` com signals para estado do menu (remover configurador de preset)
- [ ] 6.3 Configurar design tokens como CSS custom properties em `styles.scss` (cores, superfícies, tipografia, espaçamento)
- [ ] 6.4 Configurar suporte a dark mode via classe `app-dark`
- [ ] 6.5 Criar componente `LoadingSpinner` em `shared/components/`
- [ ] 6.6 Criar componente `EmptyState` em `shared/components/`
- [ ] 6.7 Criar componente `ErrorState` com botão de retry em `shared/components/`
- [ ] 6.8 Criar componente `PageHeader` com título e breadcrumb em `shared/components/`
- [ ] 6.9 Criar componente `FilterBar` genérico em `shared/components/`
- [ ] 6.10 Criar form helpers: field wrapper com label/erro, validation message mapping
- [ ] 6.11 Configurar rotas base: layout autenticado, auth (fora do layout), wildcard 404
- [ ] 6.12 Escrever testes unitários para componentes base (LoadingSpinner, EmptyState, ErrorState, FilterBar)

## 7. Frontend — Autenticação e Páginas Base

- [ ] 7.1 Criar `AuthService` em `core/auth/` com login, register, refresh, logout, token storage
- [ ] 7.2 Criar `AuthGuard` que redireciona para login se não autenticado
- [ ] 7.3 Criar JWT interceptor HTTP que anexa token e trata 401 com refresh automático
- [ ] 7.4 Criar página Sign In em `features/auth/login/` com formulário email+senha, link para signup, validação
- [ ] 7.5 Criar página Sign Up em `features/auth/signup/` com formulário nome+email+senha+confirmação
- [ ] 7.6 Criar página Access Denied em `features/errors/access-denied/`
- [ ] 7.7 Criar página 404 Not Found em `features/errors/not-found/`
- [ ] 7.8 Configurar menu lateral com item "Consulta de Alíquotas"
- [ ] 7.9 Escrever testes unitários para AuthService, AuthGuard e JWT interceptor
- [ ] 7.10 Escrever testes unitários para páginas de login e signup (validação, submit, erros)

## 8. Frontend — Feature de Consulta

- [ ] 8.1 Criar SVG do mapa do Brasil com paths por estado e atributos `data-uf`
- [ ] 8.2 Criar componente `BrazilMap` em `features/consulta/mapa/` com hover e click por estado
- [ ] 8.3 Criar `ConsultaService` em `features/consulta/services/` para chamadas à API de estados, municípios e alíquotas
- [ ] 8.4 Criar página de consulta (mapa) em `features/consulta/mapa/`
- [ ] 8.5 Criar página de municípios por estado em `features/consulta/estado/` com busca por texto
- [ ] 8.6 Criar página de listagem de alíquotas por município em `features/consulta/municipio/` com tabela paginada
- [ ] 8.7 Implementar filtros na listagem: código de serviço, descrição, faixa de alíquota, competência
- [ ] 8.8 Criar componente de detalhe de alíquota (dialog ou inline expand)
- [ ] 8.9 Implementar breadcrumb na navegação da consulta (Consulta > Estado > Município)
- [ ] 8.10 Implementar estados de loading, vazio, erro e retry em todas as páginas de consulta
- [ ] 8.11 Escrever testes unitários para BrazilMap, ConsultaService e páginas de consulta

## 9. Integração e Validação

- [ ] 9.1 Validar integração frontend ↔ backend com docker compose up (auth flow completo)
- [ ] 9.2 Validar integração backend ↔ MongoDB (seed + consulta)
- [ ] 9.3 Validar worker executando ciclo completo com API NFS-e (se certificado disponível) ou mock
- [ ] 9.4 Testar fluxo completo: login → mapa → estado → município → listagem → filtros
- [ ] 9.5 Revisar contratos da API vs implementação real (Swagger vs código)

## 10. Projeto E2E com Cypress

- [ ] 10.1 Inicializar projeto Cypress em `cypress/` (corrigir typo `ceypress` → `cypress`)
- [ ] 10.2 Configurar `cypress.config.ts` com baseUrl apontando para frontend dockerizado
- [ ] 10.3 Criar seed de dados de teste (2 usuários, 3 estados com municípios, 10 alíquotas)
- [ ] 10.4 Criar custom commands para login e setup de teste
- [ ] 10.5 Escrever teste E2E: fluxo de cadastro
- [ ] 10.6 Escrever teste E2E: fluxo de login (sucesso e falha)
- [ ] 10.7 Escrever teste E2E: acesso negado (rota protegida sem auth)
- [ ] 10.8 Escrever teste E2E: página 404
- [ ] 10.9 Escrever teste E2E: navegação pelo menu
- [ ] 10.10 Escrever teste E2E: visualização do mapa e seleção de estado
- [ ] 10.11 Escrever teste E2E: seleção de município
- [ ] 10.12 Escrever teste E2E: listagem de serviços e alíquotas
- [ ] 10.13 Escrever teste E2E: aplicação e limpeza de filtros
- [ ] 10.14 Escrever teste E2E: cenários de erro (API indisponível, dados vazios)
- [ ] 10.15 Adicionar atributos `data-cy` nos componentes do frontend para seletores estáveis

## 11. Documentação Final e Refinamento

- [ ] 11.1 Atualizar README.md com instruções de setup, docker compose e desenvolvimento
- [ ] 11.2 Revisar e atualizar `docs/api-contracts.md` com contratos finais implementados
- [ ] 11.3 Revisar e atualizar diagramas Mermaid na documentação técnica
- [ ] 11.4 Validar que todos os documentos de `docs/` estão consistentes com a implementação
- [ ] 11.5 Revisar Swagger/OpenAPI gerado vs documentação de contratos

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

Fase 3 (sequencial):
  Grupo 8 (Frontend Consulta) depende de Grupo 7 + Grupo 4
  Grupo 9 (Integração) depende de Grupos 4, 5, 8

Fase 4:
  Grupo 10 (E2E) depende de Grupo 9
  Grupo 11 (Docs Final) depende de Grupo 9
```

## Checkpoints de Integração

| Checkpoint | Trilhas envolvidas | Critério |
|-----------|-------------------|----------|
| CP1 | Backend + Docker | Auth endpoints funcionando via docker compose |
| CP2 | Frontend + Backend | Login/signup integrado ponta a ponta |
| CP3 | Worker + Backend + MongoDB | Worker executa ciclo e dados aparecem na API |
| CP4 | Frontend + Backend | Fluxo completo mapa→estado→município→listagem |
| CP5 | E2E + Todos | Testes E2E passando no ambiente dockerizado |
