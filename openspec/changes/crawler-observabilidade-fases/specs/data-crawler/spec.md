## MODIFIED Requirements

### Requirement: Execution tracking
The worker SHALL record each execution cycle in the `execucoes_crawler` collection with: id, inicio, fim, status (em_andamento/concluido/falha_parcial/falha), tipo (agendado/manual), totalMunicipios, totalServicos, processados, erros, detalhesErro[], **faseAtual (descoberta_convenios/sondagem/processamento_fila/concluido)**.

#### Scenario: Execution completed
- **WHEN** all queue items are processed
- **THEN** the execution record is updated with final counts, status, and faseAtual set to Concluido

#### Scenario: Execution status endpoint
- **WHEN** an admin user (role Admin) calls `GET /api/v1/crawler/status`
- **THEN** the system returns the latest execution record including the current phase (faseAtual)

#### Scenario: Execution history
- **WHEN** an admin user (role Admin) calls `GET /api/v1/crawler/execucoes`
- **THEN** the system returns the last 20 execution records

#### Scenario: Phase tracking during execution
- **WHEN** the crawler transitions between phases (convenio discovery → probe → queue processing)
- **THEN** the execution record SHALL be updated with the new faseAtual value and persisted to MongoDB before the new phase begins
