## MODIFIED Requirements

### Requirement: Execution tracking
The worker SHALL record each execution cycle in the `execucoes_crawler` collection with: id, inicio, fim, status (em_andamento/concluido/falha_parcial/falha), tipo (agendado/manual), totalMunicipios, totalServicos, processados, erros, detalhesErro[]. The worker SHALL also track per-UF progress in `progressoUfs`, where each UF entry SHALL include: uf, status (Pendente/EmAndamento/Concluido/Falha/Interrompido), municipiosEncontrados (count from local database), municipiosAtivos (count confirmed via external API), inicio, fim. The per-UF status SHALL reflect the actual result of the external API calls for that UF, not merely the local database read.

#### Scenario: Execution completed
- **WHEN** all queue items are processed
- **THEN** the execution record is updated with final counts and status

#### Scenario: Execution status endpoint
- **WHEN** an admin user (role Admin) calls `GET /api/v1/crawler/status`
- **THEN** the system returns the latest execution record including per-UF progress with both municipiosEncontrados and municipiosAtivos

#### Scenario: Execution history
- **WHEN** an admin user (role Admin) calls `GET /api/v1/crawler/execucoes`
- **THEN** the system returns the last 20 execution records

#### Scenario: UF progress reflects actual API result
- **WHEN** the worker verifies convenio for all municipalities of a UF and at least one is active
- **THEN** the UF progress status SHALL be "Concluido" with municipiosAtivos reflecting the count of active municipalities

#### Scenario: UF progress when all convenio calls fail
- **WHEN** the worker attempts to verify convenio for all municipalities of a UF and ALL calls fail with HTTP errors
- **THEN** the UF progress status SHALL be "Falha" with municipiosAtivos = 0

#### Scenario: UF progress when no municipalities are active
- **WHEN** the worker verifies convenio for all municipalities of a UF and none are active (all return inactive or null)
- **THEN** the UF progress status SHALL be "Concluido" with municipiosAtivos = 0

#### Scenario: UF progress when processing is interrupted
- **WHEN** the CertificateProtection halts or budget is exhausted during processing of a UF
- **THEN** the current UF progress status SHALL be "Interrompido" and remaining unprocessed UFs SHALL remain "Pendente"

#### Scenario: UF progress municipiosEncontrados reflects local database
- **WHEN** the worker reads municipalities from the local database for a UF
- **THEN** municipiosEncontrados SHALL reflect the count of municipalities found in the local database, regardless of API call results
