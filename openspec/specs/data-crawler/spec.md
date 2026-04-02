## ADDED Requirements

### Requirement: Scheduled data collection
The worker SHALL run as a .NET BackgroundService with configurable CRON schedule (default: daily at 02:00 UTC). The worker SHALL also support manual trigger via `POST /api/v1/crawler/executar`.

#### Scenario: Scheduled execution
- **WHEN** the CRON schedule triggers
- **THEN** the worker starts a new collection cycle, creates an execution record, and processes the work queue

#### Scenario: Manual trigger
- **WHEN** an admin user (role Admin) calls `POST /api/v1/crawler/executar`
- **THEN** the worker starts a new collection cycle immediately and returns HTTP 202 Accepted with execution ID

#### Scenario: Manual trigger with UF filter
- **WHEN** an admin user calls `POST /api/v1/crawler/executar` with `{ "ufs": ["SE", "MG"] }`
- **THEN** the worker starts a collection cycle only for the specified UFs

#### Scenario: Non-admin user attempts trigger
- **WHEN** a user without Admin role calls `POST /api/v1/crawler/executar`
- **THEN** the system returns HTTP 403 Forbidden

#### Scenario: Concurrent execution prevention
- **WHEN** a manual trigger is requested while a cycle is already running
- **THEN** the system returns HTTP 409 Conflict

---

### Requirement: Work queue management
The worker SHALL use a persistent work queue (MongoDB collection `fila_processamento`) to track items to be processed. Each queue item SHALL contain: codigoMunicipio, codigoServico, competencia, status (pendente/processando/concluido/erro), tentativas, ultimoErro, proximaTentativa.

#### Scenario: Queue generation
- **WHEN** a new collection cycle starts and the queue is empty
- **THEN** the worker generates queue items for all active municipality + service code combinations

#### Scenario: Queue resumption
- **WHEN** the worker starts and the queue has unprocessed items (from a previous interrupted cycle)
- **THEN** the worker resumes processing from pending and error items without regenerating the queue

#### Scenario: Item processing
- **WHEN** a queue item is picked up for processing
- **THEN** its status is set to "processando" before the API call begins

---

### Requirement: NFS-e API integration
The worker SHALL call the NFS-e API at `adn.nfse.gov.br` using HTTPS with client certificate (PFX). The primary endpoint is `GET /parametrizacao/{codigoMunicipio}/{codigoServico}/{competencia}/aliquota`. The worker SHALL also use `GET /parametrizacao/{codigoMunicipio}/convenio` to discover municipality adherence, and `GET /cnc/consulta/cad/{codigoMunicipio}` to discover registered services per municipality. The PFX certificate SHALL be managed via API endpoint (upload by admin), not mounted as a Docker volume.

#### Scenario: Successful tax rate fetch
- **WHEN** the API returns HTTP 200 with valid tax rate data
- **THEN** the worker upserts the result in the `aliquotas` collection and marks the queue item as "concluido"

#### Scenario: Municipality not found
- **WHEN** the API returns HTTP 404 for a municipality
- **THEN** the worker marks the queue item as "concluido" with a note and does NOT retry

#### Scenario: API transient error (5xx, timeout)
- **WHEN** the API returns HTTP 5xx or times out
- **THEN** the worker increments the retry counter, calculates next retry time with exponential backoff, and marks the item as "erro"

#### Scenario: API permanent error (4xx except 404)
- **WHEN** the API returns HTTP 400, 403, or other non-retryable error
- **THEN** the worker marks the item as "erro" with max retries reached

---

### Requirement: Concurrency control
The worker SHALL limit concurrent API calls using a configurable semaphore (default: 2 concurrent calls). The worker SHALL enforce a configurable rate limit (default: max 5 requests/second). These conservative defaults exist to protect the PFX certificate from being blocked by the API provider.

#### Scenario: Concurrency limit respected
- **WHEN** the worker is processing items
- **THEN** at most N items (default 2) are being fetched from the API simultaneously

#### Scenario: Rate limit enforcement
- **WHEN** requests would exceed the rate limit
- **THEN** the worker delays additional requests to stay within the limit

---

### Requirement: Certificate protection strategy
The worker SHALL implement a multi-layer strategy to protect the PFX certificate from being blocked by the API provider. This includes: conservative rate limiting, mandatory pauses between batches, adaptive throttling based on response signals, and daily request budget limits.

#### Scenario: Batch pause
- **WHEN** the worker finishes processing a batch of 50 items
- **THEN** it pauses for a configurable cool-down period (default: 30 seconds) before starting the next batch

#### Scenario: Adaptive throttling on warning signals
- **WHEN** the worker detects increasing response times (>5s avg over last 20 requests) OR receives HTTP 429 (Too Many Requests) OR receives unexpected 403
- **THEN** the worker reduces concurrency to 1, drops rate limit to 1 req/s, and increases batch pause to 2 minutes

#### Scenario: Daily request budget
- **WHEN** the worker has made more than the configured daily limit of requests (default: 50.000)
- **THEN** the worker stops processing and resumes in the next cycle

#### Scenario: Certificate block detection
- **WHEN** the worker receives 3 consecutive 403 responses
- **THEN** the worker halts all API calls, logs a critical alert, and does NOT retry until the next manual trigger or scheduled cycle

---

### Requirement: Circuit breaker
The worker SHALL implement a circuit breaker that pauses API calls when the error rate exceeds a threshold. Default: if >50% of calls fail within a 1-minute window, pause for 5 minutes.

#### Scenario: Circuit opens
- **WHEN** the error rate exceeds 50% in the last minute
- **THEN** the worker pauses all API calls and logs a warning

#### Scenario: Circuit closes
- **WHEN** the pause period ends
- **THEN** the worker resumes processing with a single probe request before returning to full throughput

---

### Requirement: Retry with exponential backoff
The worker SHALL retry failed items up to a configurable maximum (default: 3 attempts). Retry delay SHALL follow exponential backoff: 30s, 2min, 8min.

#### Scenario: Retry schedule
- **WHEN** an item fails for the 1st time
- **THEN** its `proximaTentativa` is set to now + 30 seconds

#### Scenario: Max retries exceeded
- **WHEN** an item fails for the 3rd time
- **THEN** the item is marked as permanent failure and not retried in this cycle

---

### Requirement: Execution tracking
The worker SHALL record each execution cycle in the `execucoes_crawler` collection with: id, inicio, fim, status (em_andamento/concluido/falha_parcial/falha), tipo (agendado/manual), totalMunicipios, totalServicos, processados, erros, detalhesErro[].

#### Scenario: Execution completed
- **WHEN** all queue items are processed
- **THEN** the execution record is updated with final counts and status

#### Scenario: Execution status endpoint
- **WHEN** an admin user (role Admin) calls `GET /api/v1/crawler/status`
- **THEN** the system returns the latest execution record

#### Scenario: Execution history
- **WHEN** an admin user (role Admin) calls `GET /api/v1/crawler/execucoes`
- **THEN** the system returns the last 20 execution records

---

### Requirement: Incremental processing
The worker SHALL support incremental processing — only fetch data that has not been collected for the current competência or that has previously failed.

#### Scenario: Skip already collected
- **WHEN** generating the work queue and a municipality+service+competência combination already exists in `aliquotas` with the current competência
- **THEN** the combination is NOT added to the queue

#### Scenario: Force reprocess
- **WHEN** a manual trigger includes `{ forcarReprocessamento: true }`
- **THEN** the worker regenerates the full queue ignoring already collected data

---

### Requirement: Service code discovery via CNC with municipal complement early-stop
The worker SHALL use the CNC endpoint (`GET /cnc/consulta/cad/{municipio}`) as the primary mechanism for discovering which services a municipality has registered. This replaces the previous strategy of iterating all ~391 national codes with probe and early-stop. The worker SHALL maintain a seed of ~391 national tax codes (cTribNac, format `ii.ss.dd`) sourced from the gov.br/nfse portal as fallback reference. The full code format is `ii.ss.dd.xxx` where `ii.ss.dd` is the national code and `xxx` is the municipal complement (cTribMun), which varies by municipality. When iterating municipal complements for codes not covered by CNC, the worker SHALL use aggressive early-stop: start at `000`, advance to `001`, and if `001` returns no data, stop immediately for that national code.

#### Scenario: CNC discovery replaces brute-force iteration
- **WHEN** the worker starts processing a municipality that passed the convênio or CNC check
- **THEN** the worker calls `GET /cnc/consulta/cad/{municipio}` to get the list of registered services
- **AND** enqueues only the services returned by the CNC endpoint

#### Scenario: Municipal complement iteration with aggressive early-stop
- **WHEN** the worker iterates complements for a national code not covered by CNC, starting at `000`
- **AND** complement `001` returns 404 (no data)
- **THEN** the worker stops iterating that national code and moves to the next

#### Scenario: Complement found then gap
- **WHEN** the worker finds data for `01.01.01.000` but `01.01.01.001` returns 404
- **THEN** the worker stops iterating that national code immediately (aggressive early-stop)

#### Scenario: Municipality discovery via convênio
- **WHEN** the worker calls `GET /parametrizacao/{municipio}/convenio` and receives a successful response
- **THEN** the municipality is marked as having convênio and proceeds to CNC discovery

#### Scenario: Municipality without convênio but with CNC data
- **WHEN** the convênio endpoint returns 404 or empty response
- **AND** the CNC endpoint `GET /cnc/consulta/cad/{municipio}` returns services
- **THEN** the municipality is still processed with the services discovered via CNC

#### Scenario: Municipality without convênio and without CNC data
- **WHEN** the convênio endpoint returns 404 or empty response
- **AND** the CNC endpoint also returns 404 or empty response
- **THEN** the municipality is marked as "sem_dados" and skipped in subsequent cycles

---

### Requirement: Municipality validation before full crawl
Before queueing all service codes for a municipality, the worker SHALL validate the municipality using a two-step process: (1) check convênio via `GET /parametrizacao/{municipio}/convenio`, (2) discover services via CNC `GET /cnc/consulta/cad/{municipio}`. A municipality is eligible for crawling if EITHER the convênio check succeeds OR the CNC returns services. This ensures municipalities that may have data without formal convênio are not missed.

#### Scenario: Municipality with convênio and CNC data
- **WHEN** the worker checks municipality 3106200
- **AND** the convênio endpoint returns HTTP 200
- **AND** the CNC endpoint returns a list of services
- **THEN** the municipality passes validation and its CNC-discovered services are enqueued for processing

#### Scenario: Municipality without convênio but with CNC data
- **WHEN** the worker checks a municipality
- **AND** the convênio endpoint returns 404
- **AND** the CNC endpoint returns a list of services
- **THEN** the municipality passes validation and its CNC-discovered services are enqueued

#### Scenario: Municipality fails both checks
- **WHEN** the worker checks a municipality
- **AND** the convênio endpoint returns 404 or error
- **AND** the CNC endpoint returns 404 or empty
- **THEN** the municipality is marked as "sem_dados" and skipped — no further service codes are tested

#### Scenario: Municipality skip persists across cycles
- **WHEN** a municipality was marked as "sem_dados" in a previous cycle
- **THEN** it is skipped in subsequent cycles unless a force reprocess is triggered

---

### Requirement: Competência management
The worker SHALL process data for the current competência (current month, format YYYY-MM-01). The worker SHALL support configuration to also collect historical competências.

#### Scenario: Current competência
- **WHEN** a new cycle starts in March 2026
- **THEN** the worker processes competência "2026-03-01"

#### Scenario: Historical collection
- **WHEN** configured to collect historical data
- **THEN** the worker uses the `historicoaliquotas` endpoint and stores results with their respective competências

---

### Requirement: PFX certificate management via API
The system SHALL allow administrators to upload, check, and remove the PFX certificate used for mTLS authentication with the NFS-e API. Certificate management endpoints SHALL require Admin role.

#### Scenario: Upload certificate
- **WHEN** an admin user uploads a PFX file with password via `POST /api/v1/crawler/certificado`
- **THEN** the system validates the certificate, stores it securely, and returns upload confirmation

#### Scenario: Invalid certificate upload
- **WHEN** an admin user uploads an invalid PFX file or provides wrong password
- **THEN** the system returns HTTP 400 with error details

#### Scenario: Check certificate status
- **WHEN** an admin user calls `GET /api/v1/crawler/certificado`
- **THEN** the system returns whether a certificate is loaded and when it was uploaded

#### Scenario: Remove certificate
- **WHEN** an admin user calls `DELETE /api/v1/crawler/certificado`
- **THEN** the system removes the stored certificate and returns HTTP 204

#### Scenario: Non-admin access to certificate endpoints
- **WHEN** a non-admin user attempts any certificate endpoint
- **THEN** the system returns HTTP 403 Forbidden

#### Scenario: Crawler requires certificate
- **WHEN** the crawler attempts to call the NFS-e API without a loaded certificate
- **THEN** the system logs the error and marks the execution as failed

---

### Requirement: Admin role authorization for crawler endpoints
All crawler management endpoints (executar, status, execucoes, certificado) SHALL require the Admin role. The admin role is determined by the `Admin:Emails` configuration. Users whose email is in this list receive the "Admin" role claim in their JWT token. Regular users (role "User") SHALL receive HTTP 403 Forbidden when accessing crawler endpoints.

#### Scenario: Admin user accesses crawler
- **WHEN** a user with Admin role calls any crawler endpoint
- **THEN** the request is processed normally

#### Scenario: Regular user blocked from crawler
- **WHEN** a user with role "User" calls any crawler endpoint
- **THEN** the system returns HTTP 403 Forbidden

#### Scenario: Unauthenticated user blocked from crawler
- **WHEN** a request without JWT calls any crawler endpoint
- **THEN** the system returns HTTP 401 Unauthorized

#### Scenario: Admin emails configuration
- **WHEN** the application starts
- **THEN** it reads `Admin:Emails` from configuration to determine which users receive the Admin role

#### Scenario: Frontend navigation visibility
- **WHEN** a regular user logs into the frontend
- **THEN** the crawler management menu items are NOT displayed in the sidebar navigation
