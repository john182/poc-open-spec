## MODIFIED Requirements

### Requirement: Concurrency control
The worker SHALL limit concurrent API calls using a configurable semaphore (default: 20 concurrent calls). The worker SHALL enforce a configurable rate limit (default: max 50 requests/second). The worker SHALL support parallel UF processing with a configurable degree of parallelism (default: 5 UFs simultaneous) via `MaxUfsParalelas`. The rate limiter and certificate protection are shared across all parallel UFs.

#### Scenario: Concurrency limit respected
- **WHEN** the worker is processing items
- **THEN** at most N items (default 20) are being fetched from the API simultaneously

#### Scenario: Rate limit enforcement
- **WHEN** requests would exceed the rate limit
- **THEN** the worker delays additional requests to stay within the limit

#### Scenario: UF parallelism control
- **WHEN** the worker starts Phase 1 with `MaxUfsParalelas = 5`
- **THEN** at most 5 UFs are being discovered simultaneously
- **AND** the global rate limiter is shared across all UF threads

---

### Requirement: Execution tracking
The worker SHALL record each execution cycle in the `execucoes_crawler` collection with: id, inicio, fim, status (em_andamento/concluido/falha_parcial/falha), tipo (agendado/manual), totalMunicipios, totalServicos, processados, erros, detalhesErro[], ufsProcessadas[], ufsEmAndamento[], progressoUfs{}. The field `ufsEmAndamento` replaces the previous `ufAtual` to support multiple simultaneous UFs.

#### Scenario: Execution completed
- **WHEN** all queue items are processed
- **THEN** the execution record is updated with final counts and status

#### Scenario: Execution status endpoint
- **WHEN** an admin user (role Admin) calls `GET /api/v1/crawler/status`
- **THEN** the system returns the latest execution record with `ufsEmAndamento` array showing currently processing UFs

#### Scenario: Execution history
- **WHEN** an admin user (role Admin) calls `GET /api/v1/crawler/execucoes`
- **THEN** the system returns the last 20 execution records

#### Scenario: Multiple UFs in progress
- **WHEN** the crawler is processing 3 UFs simultaneously
- **THEN** `ufsEmAndamento` contains 3 UF codes
- **AND** `progressoUfs` shows `EmAndamento` status for each
