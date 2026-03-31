## Context

Este projeto é greenfield — não existe sistema anterior. A motivação é consolidar alíquotas municipais de ISS dispersas na API NFS-e do ADN em uma solução navegável.

**Estado atual:**
- A API NFS-e (`adn.nfse.gov.br`) expõe dados de parametrização tributária por município/serviço/competência
- A API requer certificado cliente PFX para acesso (evidenciado em `context/bruno.json`)
- Não há autenticação por token/API key nos endpoints — apenas mTLS
- Os arquivos Bruno em `context/` documentam 6 endpoints GET: alíquotas, histórico de alíquotas, convênio, retenções, benefício e consulta CNC
- O formato do código de serviço varia: `01.01.01.001` (com pontos) e `010301001` (sem pontos) aparecem nos exemplos
- A competência aparece como data completa (`2026-01-09`) nos Bruno files, mas o README indica formato `YYYYMM` — **premissa: aceitar ambos os formatos e normalizar internamente**
- Existem esqueletos iniciais do backend (.NET 10) e frontend (Angular 21 + PrimeNG 21)
- O template PrimeNG Sakai em `TEMPLATE_FRONT_PATH` usa Angular 21, Tailwind 4, standalone components, signals e zoneless change detection

**Stakeholders:** Equipe de desenvolvimento, usuários finais que precisam consultar alíquotas municipais.

**Constraints:**
- Solução deve ser dockerizada
- PRs devem ser pequenos e revisáveis
- Frontend deve seguir ordem obrigatória (fundação → auth → feature)
- Worker é parte central, não um loop ingênuo
- Documentação deve ser forte para evolução por agentes

---

## Goals / Non-Goals

**Goals:**
- Construir uma solução full stack dockerizada que permita consultar alíquotas de ISS por município e código de serviço
- Materializar dados da API NFS-e localmente para leitura rápida
- Oferecer experiência de navegação visual: mapa → estado → município → serviços/alíquotas
- Implementar autenticação básica viável (JWT) com caminho para evolução
- Garantir que o worker seja resiliente, incremental e retomável
- Manter documentação rastreável e útil para agentes
- Permitir paralelização segura entre trilhas (frontend, backend, worker, E2E)

**Non-Goals:**
- SSO, OAuth2 ou integração com provedores de identidade externos
- Edição ou cadastro de alíquotas pelo usuário (sistema é somente leitura)
- Cobertura de todos os 5.570 municípios no MVP — iniciar com conjunto controlado
- Notificações push ou real-time via WebSocket
- Multi-tenancy ou isolamento por organização
- Deploy em cloud ou CI/CD pipeline (foco é ambiente local dockerizado)
- Internacionalização (i18n) — aplicação em português

---

## Decisions

### D1: Materialização local vs. consulta real-time à API NFS-e

**Decisão:** Materialização local com worker assíncrono.

**Alternativas consideradas:**
1. **Proxy real-time**: Frontend → Backend → API NFS-e em tempo real
2. **Cache com TTL**: Consulta real-time com cache temporário
3. **Materialização local**: Worker coleta e persiste; backend serve do store local

**Justificativa:** A API NFS-e requer certificado PFX, tem latência variável e não há garantia de SLA. Materialização local oferece: leitura instantânea para o frontend, independência da disponibilidade da API externa, controle total sobre a frequência de atualização, e possibilidade de enriquecimento dos dados (descrições de serviço, metadados).

**Trade-off:** Dados podem estar defasados entre execuções do worker. Mitigação: registrar timestamp da última atualização e exibir no frontend.

---

### D2: Stack tecnológica

**Decisão:**
| Camada | Tecnologia | Justificativa |
|--------|-----------|---------------|
| Frontend | Angular 21 + PrimeNG 21 + Tailwind 4 | Já definido no projeto; template Sakai como referência |
| Backend | ASP.NET Core (.NET 10) + C# | Já definido no projeto; esqueleto existente |
| Worker | ASP.NET Core Background Service (mesmo projeto) | Compartilha domínio/infra com backend; simplifica deploy |
| Database | MongoDB | Esqueleto já aponta para Mongo; schema flexível para dados tributários variados |
| Auth | JWT com refresh token | Simples, stateless, bem suportado pelo .NET |
| E2E | Cypress (projeto separado) | Já definido; boa DX para testes de UI |
| Infra | Docker Compose | Orquestração local simples |

**Alternativa considerada para Worker:** Projeto separado. Descartada porque o worker compartilha modelos de domínio e acesso ao MongoDB com o backend. Manter no mesmo projeto .NET como `IHostedService` / BackgroundService simplifica significativamente.

---

### D3: Modelo de dados MongoDB

**Decisão:** Coleções otimizadas para leitura rápida.

```
Collections:
├── estados              # 27 documentos (UFs brasileiras)
│   { codigo, nome, sigla, regiao }
│
├── municipios           # ~5.570 documentos (IBGE)
│   { codigoIbge, nome, codigoEstado, siglaEstado, ativo }
│
├── servicos             # Códigos de serviço LC 116/2003
│   { codigo, codigoFormatado, descricao, subitem }
│
├── aliquotas            # Dados consolidados (documento principal)
│   { codigoMunicipio, codigoServico, competencia,
│     aliquota, descricaoServico, coletadoEm,
│     fonteExecucaoId }
│   índices: { codigoMunicipio+codigoServico+competencia: unique }
│
├── execucoes_crawler    # Registro de execuções do worker
│   { id, inicio, fim, status, tipo,
│     totalMunicipios, totalServicos,
│     processados, erros, detalhesErro[] }
│
└── fila_processamento   # Fila de trabalho do worker
    { codigoMunicipio, codigoServico, competencia,
      status, tentativas, ultimoErro, proximaTentativa }
```

**Justificativa:** MongoDB permite schema flexível para acomodar variações nos dados da API NFS-e. Índices compostos em `aliquotas` garantem leitura rápida por município+serviço. A coleção `fila_processamento` permite retomada e reprocessamento sem depender de estado em memória.

---

### D4: Estratégia do Worker/Crawler

**Decisão:** Worker como BackgroundService com fila persistente e processamento incremental.

**Fluxo do Worker:**

```mermaid
flowchart TD
    A[Worker Inicia] --> B{Verificar Fila}
    B -->|Fila vazia| C[Gerar Fila de Trabalho]
    B -->|Fila com itens| D[Processar Próximo Item]
    C --> D
    D --> E[Consultar API NFS-e]
    E -->|Sucesso| F[Salvar Alíquota no MongoDB]
    E -->|Erro transitório| G[Incrementar tentativas + backoff]
    E -->|Erro permanente| H[Marcar como falha definitiva]
    F --> I[Atualizar status na fila]
    G --> I
    H --> I
    I --> J{Mais itens na fila?}
    J -->|Sim| D
    J -->|Não| K[Registrar Execução Completa]
    K --> L[Aguardar próximo ciclo]
    L --> B
```

**Controles:**
- **Concorrência**: Semáforo configurável (default: 2 chamadas simultâneas — conservador para proteger o certificado)
- **Rate limiting**: Max 5 req/s para a API NFS-e (configurável — conservador por padrão)
- **Retry**: Exponential backoff com max 3 tentativas por item
- **Circuit breaker**: Se >50% das chamadas falharem em 1 minuto, pausar por 5 minutos
- **Timeout**: 30s por chamada à API NFS-e
- **Pausa entre batches**: A cada 50 itens processados, pausar 30s (protege o certificado)
- **Budget diário**: Max 50.000 requests/dia — após atingir, aguarda o próximo ciclo
- **Throttling adaptativo**: Se response time médio > 5s ou receber 429/403, reduz para 1 req/s e pausa 2min entre batches
- **Detecção de bloqueio**: 3 erros 403 consecutivos = halt imediato com alerta crítico
- **Retomada**: Fila persistente no MongoDB — reinício do worker retoma de onde parou
- **Reprocessamento**: Endpoint administrativo para re-enfileirar municípios/serviços específicos
- **Agendamento**: CRON configurável (default: diário às 02:00) + trigger manual via endpoint

**Contexto NFS-e Nacional e adesão municipal:**

Nem todos os 5.570 municípios do IBGE (arquivo `context/municipios.json`) terão dados no ADN. A LC 214/2025 tornou a adesão obrigatória a partir de 01/01/2026, mas adesão ≠ parametrização completa. Dos ~5.565 entes que aderiram, há dois modos: (1) convênio completo (ADN + Emissor Público) e (2) apenas ADN com sistema próprio. Municípios que não completaram a parametrização não terão dados de alíquota disponíveis na API.

**Estratégia de 3 fases para cada município:**

```mermaid
flowchart TD
    M[Município do IBGE] --> F1{Fase 1: Convênio}
    F1 -->|GET /parametrizacao/{mun}/convenio| C1{Resposta OK?}
    C1 -->|Não| SKIP1[Marcar 'sem_convenio' — SKIP]
    C1 -->|Sim| F2{Fase 2: Probe}
    F2 -->|Testar 5 serviços representativos| C2{Pelo menos 1 retornou dados?}
    C2 -->|Não| SKIP2[Marcar 'sem_dados_adn' — SKIP]
    C2 -->|Sim| F3[Fase 3: Crawl completo com early-stop]
    F3 --> DONE[Processar todos os serviços]
```

**Fase 1 — Filtro por convênio:** Chama `GET /parametrizacao/{municipio}/convenio`. Se 404/erro → skip. Custo: 1 request por município.

**Fase 2 — Probe com serviços representativos:** Testa 5 códigos de serviço de grupos diferentes (ex: `01.01.01.001`, `07.02.01.001`, `14.01.01.001`, `17.01.01.001`, `25.01.01.001`). Se todos falharem → município marcado como "sem_dados_adn" e pulado inteiro. Custo: max 5 requests. Isso evita enfileirar centenas de serviços para um município que não tem dado algum.

**Fase 3 — Crawl completo com early-stop por subdivisão:** O código de serviço segue o formato `XX.XX.XX.XXX` onde os 3 últimos dígitos são subdivisões. Para cada grupo `XX.XX.XX`:
- Começar em `001`
- Se 9 misses consecutivos (404) → parar e pular para o próximo grupo
- Isso reduz de ~999 tentativas por grupo para ~9

**Impacto na estimativa de volume:**

| Cenário | Sem otimização | Com convênio + probe + early-stop |
|---------|---------------|-----------------------------------|
| 5.570 municípios (raw) | Milhões de requests | ~5.570 convênio + ~5.570×5 probe = ~33K |
| ~2.000 municípios passam probe | — | ~2.000 × ~200 itens × ~9 subdivisões = ~3.6M |
| MVP (27 capitais) | ~5.4M | ~27 + 135 probe + ~48.600 crawl = ~49K |

**Seed de municípios:** Usar diretamente o arquivo `context/municipios.json` (5.570 municípios com Id, Codigo IBGE, Nome, UF) como fonte de seed no startup do backend. Não depender de API externa do IBGE.

---

### D5: Autenticação (JWT)

**Decisão:** JWT com access token (curta duração) + refresh token (longa duração).

**Fluxo:**

```mermaid
sequenceDiagram
    participant U as Usuário
    participant F as Frontend
    participant B as Backend

    U->>F: Preenche email + senha
    F->>B: POST /api/v1/auth/login
    B->>B: Valida credenciais (bcrypt)
    B-->>F: { accessToken, refreshToken, expiresIn }
    F->>F: Armazena tokens (httpOnly cookie ou localStorage)

    Note over F,B: Requisições autenticadas
    F->>B: GET /api/v1/aliquotas?... (Authorization: Bearer {token})
    B->>B: Valida JWT
    B-->>F: Dados

    Note over F,B: Token expirado
    F->>B: POST /api/v1/auth/refresh
    B-->>F: Novo accessToken
```

**Modelo de usuário:** email, senha (hash bcrypt), nome, dataCriacao, ativo.
**Sem roles no MVP** — todos os usuários autenticados têm acesso igual. Caminho para evolução: adicionar claims/roles ao JWT.

---

### D6: Arquitetura do Frontend Angular

**Decisão:** Estrutura modular com lazy loading, baseada no template Sakai como referência controlada.

**Estrutura de pastas:**

```
src/app/
├── core/                    # Singleton services, guards, interceptors
│   ├── auth/               # AuthService, AuthGuard, JWT interceptor
│   ├── http/               # HTTP interceptors (error, loading)
│   └── services/           # API services singleton
├── layout/                  # Layout administrativo (adaptado do Sakai)
│   ├── components/         # Topbar, Sidebar, Menu, Footer
│   └── services/           # LayoutService (estado do menu, tema)
├── shared/                  # Componentes reutilizáveis, pipes, directives
│   ├── components/         # Loading, Empty, Error, Retry, FilterBar
│   ├── directives/
│   └── pipes/
├── features/               # Feature modules (lazy loaded)
│   ├── auth/               # Login, SignUp (fora do layout)
│   │   ├── login/
│   │   └── signup/
│   ├── consulta/           # Feature principal
│   │   ├── mapa/          # Mapa do Brasil (SVG interativo)
│   │   ├── estado/        # Lista de municípios do estado
│   │   ├── municipio/     # Listagem de serviços/alíquotas
│   │   └── services/      # ConsultaService, AliquotaService
│   └── errors/            # 404, Acesso Negado
├── design-system/          # Design tokens, tema, variáveis
└── app.routes.ts
```

**Mapa do Brasil:** SVG inline com paths por estado, cada path com `data-uf` para interação. Ao clicar, navega para rota `/consulta/estado/:uf`. Não usar biblioteca de mapas pesada — SVG simples com hover/click é suficiente para o MVP.

**Reuso do template Sakai:**
| Componente | Decisão | Justificativa |
|-----------|---------|---------------|
| Layout wrapper | Reutilizar/adaptar | Estrutura sólida de static/overlay sidebar |
| Topbar | Adaptar | Remover configurador de tema, manter toggle dark mode |
| Sidebar + Menu | Adaptar | Simplificar itens, manter estrutura recursiva |
| Footer | Reutilizar | Mínimas alterações |
| LayoutService | Adaptar | Manter signals, remover preset configurator |
| Auth pages (login, error, access) | Adaptar | Boa base, adicionar signup e ajustar formulários |
| Dashboard | Descartar | Não é relevante — substituir pela tela de consulta |
| UIKit demos | Descartar | São exemplos, não são parte do produto |
| Landing page | Descartar | Não faz parte do escopo |
| Configurator | Descartar | Superengenharia para o MVP |

---

### D7: API REST — Contratos do Backend

**Decisão:** API versionada com prefixo `/api/v1/`, contratos claros e Swagger completo.

**Endpoints principais:**

```
Auth:
  POST   /api/v1/auth/register     # Cadastro
  POST   /api/v1/auth/login        # Login
  POST   /api/v1/auth/refresh      # Refresh token

Consulta (autenticado):
  GET    /api/v1/estados                              # Lista estados
  GET    /api/v1/estados/:uf/municipios               # Municípios por UF
  GET    /api/v1/municipios/:codigoIbge/aliquotas     # Alíquotas do município
  GET    /api/v1/municipios/:codigoIbge/aliquotas/:codigoServico  # Detalhe

  Query params para listagem:
    ?pagina=1&tamanhoPagina=20
    &codigoServico=01.01
    &descricao=analise
    &aliquotaMin=2.0&aliquotaMax=5.0
    &competencia=202603

Admin/Worker (futuro: protegido por role):
  POST   /api/v1/crawler/executar          # Trigger manual
  GET    /api/v1/crawler/status            # Status da última execução
  GET    /api/v1/crawler/execucoes         # Histórico de execuções
```

---

### D8: Docker Compose

**Decisão:** 4 serviços + 1 database.

```mermaid
graph TB
    subgraph Docker Compose
        FE[Frontend<br/>nginx:alpine<br/>:4200]
        BE[Backend API<br/>.NET 10<br/>:5000]
        WK[Worker<br/>.NET 10<br/>BackgroundService]
        MG[(MongoDB<br/>:27017)]
        CY[Cypress<br/>apenas em dev/CI]
    end

    FE -->|HTTP| BE
    BE -->|MongoDB Driver| MG
    WK -->|MongoDB Driver| MG
    WK -->|HTTPS + mTLS| EXT[API NFS-e<br/>adn.nfse.gov.br]
    CY -->|HTTP| FE
```

**Nota sobre Worker:** O worker roda como BackgroundService dentro do mesmo container do backend. Em docker-compose, ambos compartilham a mesma imagem .NET mas podem ser separados futuramente com profiles ou imagens distintas.

---

### D9: Estratégia de Observabilidade

**Decisão:** Logs estruturados + health checks + métricas básicas.

- **Logs**: Serilog no backend/worker com output JSON estruturado
- **Health checks**: `/health` e `/health/ready` no backend (MongoDB connectivity, worker status)
- **Métricas do worker**: Registradas na coleção `execucoes_crawler` (duração, processados, erros)
- **Frontend**: Console errors + interceptor HTTP que loga falhas

**Non-goal para MVP:** APM, Prometheus, Grafana, distributed tracing.

---

## Risks / Trade-offs

### R1: API NFS-e indisponível ou instável
**Risco:** A API externa pode ficar fora do ar, retornar erros ou mudar formato sem aviso.
**Mitigação:** Circuit breaker no worker, materialização local garante que o frontend continua funcionando com dados já coletados, alerta no log quando circuit breaker ativa.

### R2: Formato de código de serviço inconsistente
**Risco:** Os arquivos Bruno mostram formatos diferentes (`01.01.01.001` vs `010301001`). O README diz LC 116/2003 com pontos.
**Premissa:** Armazenar internamente sem pontos (numérico puro) e formatar para exibição. Normalizar na entrada tanto do worker quanto da API.
**Mitigação:** Validação centralizada no domínio com regex que aceita ambos os formatos.

### R3: Formato de competência divergente
**Risco:** Bruno files usam data completa (`2026-01-09`), README usa `YYYYMM`.
**Premissa:** A API NFS-e aceita data completa no path, mas a competência efetiva é mensal (YYYYMM). O worker usará formato `YYYY-MM-01` (primeiro dia do mês).
**Mitigação:** Testar ambos os formatos na análise da API externa e documentar o comportamento real.

### R4: Volume de dados para crawling completo
**Risco:** 5.570 municípios × ~600 códigos de serviço × competências = milhões de combinações possíveis.
**Mitigação:** Iniciar com subconjunto controlado (capitais + municípios com convênio ativo). Expandir progressivamente. Worker com fila permite controle granular.

### R5: Certificado PFX para API NFS-e
**Risco:** O acesso à API requer certificado cliente. O gerenciamento do certificado em Docker precisa ser seguro.
**Mitigação:** Montar certificado via volume ou secret do Docker. Nunca versionar o PFX (já no `.gitignore`). Documentar processo de configuração.

### R6: Bloqueio do certificado PFX por excesso de requests
**Risco:** A API NFS-e pode bloquear o certificado PFX se detectar volume excessivo de chamadas, causando parada total da coleta.
**Mitigação:** Múltiplas camadas de proteção: rate limit conservador (5 req/s default), pausa entre batches (30s a cada 50 itens), budget diário (50K requests), throttling adaptativo (reduz automaticamente ao detectar sinais de stress: response time alto, 429, 403), detecção de bloqueio (3 erros 403 consecutivos = halt + alerta crítico), e early-stop na iteração de subdivisões (reduz volume em ~99%).

### R7: Colisão entre agentes em desenvolvimento paralelo
**Risco:** Frontend, backend, worker e E2E sendo desenvolvidos em paralelo podem gerar conflitos.
**Mitigação:** Worktrees separadas por trilha, micro PRs focados, checkpoints de integração definidos, conflict-guard agent ativo.

---

## Diagrama de Arquitetura Geral

```mermaid
C4Context
    title Arquitetura da Solução - Mapa Tributário

    Person(user, "Usuário", "Consulta alíquotas municipais")

    System_Boundary(solution, "Mapa Tributário") {
        Container(frontend, "Frontend Angular", "Angular 21, PrimeNG, Tailwind", "UI com mapa, navegação e filtros")
        Container(backend, "Backend API", ".NET 10, ASP.NET Core", "API REST autenticada com Swagger")
        Container(worker, "Worker/Crawler", ".NET 10 BackgroundService", "Coleta e consolida alíquotas")
        ContainerDb(mongodb, "MongoDB", "MongoDB 7", "Dados consolidados de alíquotas")
    }

    System_Ext(nfse, "API NFS-e ADN", "adn.nfse.gov.br - Parametrização tributária")

    Rel(user, frontend, "Navega", "HTTPS")
    Rel(frontend, backend, "Consome API", "HTTP/JSON")
    Rel(backend, mongodb, "Lê dados", "MongoDB Driver")
    Rel(worker, mongodb, "Escreve dados", "MongoDB Driver")
    Rel(worker, nfse, "Coleta alíquotas", "HTTPS + mTLS/PFX")
```

---

## Diagrama de Fluxo do Usuário

```mermaid
flowchart LR
    A[Login/Cadastro] --> B[Layout Admin]
    B --> C[Menu: Consulta de Alíquotas]
    C --> D[Mapa do Brasil]
    D -->|Clique no estado| E[Lista de Municípios]
    E -->|Seleciona município| F[Listagem de Serviços/Alíquotas]
    F -->|Aplica filtros| G[Resultados Filtrados]
    F -->|Clique em serviço| H[Detalhe da Alíquota]
```

---

## Open Questions

1. **Quais municípios incluir no seed inicial?** Premissa: 27 capitais + municípios com convênio ativo (descobertos via endpoint `/parametrizacao/{municipio}/convenio`).

2. **A API NFS-e tem rate limit documentado?** Não encontrado nos Bruno files. Premissa conservadora: max 10 req/s.

3. **O endpoint CNC (`/cnc/consulta/cad/{municipio}`) retorna lista de serviços do município?** Se sim, pode ser usado para descobrir serviços válidos por município ao invés de iterar toda a tabela LC 116. Necessita teste real.

4. **Qual o comportamento da API quando município não tem convênio?** Premissa: retorna 404 ou resposta vazia. O worker deve tratar ambos os cenários.

5. **O certificado PFX será fornecido pela equipe ou existe um de desenvolvimento/sandbox?** Premissa: será fornecido e montado via volume Docker.
