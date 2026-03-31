## ADDED Requirements

### Requirement: Scheduled data collection
The worker SHALL run as a .NET BackgroundService with configurable CRON schedule (default: daily at 02:00 UTC). The worker SHALL also support manual trigger via `POST /api/v1/crawler/executar`.

#### Scenario: Scheduled execution
- **WHEN** the CRON schedule triggers
- **THEN** the worker starts a new collection cycle, creates an execution record, and processes the work queue

#### Scenario: Manual trigger
- **WHEN** an authenticated user calls `POST /api/v1/crawler/executar`
- **THEN** the worker starts a new collection cycle immediately and returns HTTP 202 Accepted with execution ID

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
The worker SHALL call the NFS-e API at `adn.nfse.gov.br` using HTTPS with client certificate (PFX). The primary endpoint is `GET /parametrizacao/{codigoMunicipio}/{codigoServico}/{competencia}/aliquota`. The worker SHALL also use `GET /parametrizacao/{codigoMunicipio}/convenio` to discover municipality adherence.

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
- **WHEN** an authenticated user calls `GET /api/v1/crawler/status`
- **THEN** the system returns the latest execution record

#### Scenario: Execution history
- **WHEN** an authenticated user calls `GET /api/v1/crawler/execucoes`
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

### Requirement: Service code discovery with subdivision early-stop
The worker SHALL maintain a list of known service codes (seeded from LC 116/2003). The service code format is `XX.XX.XX.XXX` where the last 3 digits (XXX) are subdivisions of the service item. The worker SHALL iterate subdivisions starting at 001, and MUST stop iterating subdivisions for a given service item group (`XX.XX.XX`) after 9 consecutive misses (no data returned). This early-stop strategy drastically reduces the number of API calls. The worker SHALL use the convênio endpoint to discover which municipalities are active. The worker SHOULD use the CNC endpoint if it provides service discovery capability.

#### Scenario: Subdivision iteration with early-stop
- **WHEN** the worker iterates subdivisions for service item `01.01.01` starting at `01.01.01.001`
- **AND** subdivisions 001 through 009 all return 404 (no data)
- **THEN** the worker stops iterating that service item group and moves to the next (`01.01.02`)

#### Scenario: Subdivision found then gap
- **WHEN** the worker finds data for `01.01.01.001` but then 002 through 010 return 404 (9 consecutive misses)
- **THEN** the worker stops iterating that service item group since no more subdivisions are expected

#### Scenario: Municipality discovery via convênio
- **WHEN** the worker calls `GET /parametrizacao/{municipio}/convenio` and receives a successful response
- **THEN** the municipality is marked as active and proceeds to the probe phase

#### Scenario: Municipality not adherent
- **WHEN** the convênio endpoint returns 404 or empty response
- **THEN** the municipality is marked as "sem_convenio" and skipped in subsequent cycles

---

### Requirement: Municipality probe before full crawl
Before queueing all service codes for a municipality, the worker SHALL run a probe phase: test 3-5 representative service codes (from different groups). If ALL probe calls return 404/error, the municipality SHALL be marked as "sem_dados_adn" and skipped entirely. This prevents wasting thousands of requests on municipalities that are technically conveniados but have no parametrization data in the ADN. Only municipalities that pass the probe (at least 1 successful response) SHALL have their full service code list enqueued.

#### Scenario: Probe succeeds
- **WHEN** the worker probes municipality 3106200 with 5 representative service codes
- **AND** at least 1 probe returns HTTP 200 with data
- **THEN** the municipality passes the probe and its full service code list is enqueued for processing

#### Scenario: Probe fails completely
- **WHEN** the worker probes a municipality with 5 representative service codes
- **AND** all 5 return 404 or error
- **THEN** the municipality is marked as "sem_dados_adn" and skipped — no further service codes are tested

#### Scenario: Probe representative codes
- **WHEN** the worker selects probe service codes
- **THEN** it SHALL use codes from different groups (e.g., 01.01.01.001, 07.02.01.001, 14.01.01.001, 17.01.01.001, 25.01.01.001) to maximize coverage across the LC 116 table

#### Scenario: Municipality skip persists across cycles
- **WHEN** a municipality was marked as "sem_dados_adn" in a previous cycle
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
