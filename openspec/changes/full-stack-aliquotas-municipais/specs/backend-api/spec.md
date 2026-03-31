## ADDED Requirements

### Requirement: States endpoint
The backend SHALL expose `GET /api/v1/estados` returning a list of all 27 Brazilian states with: codigo, nome, sigla, regiao. The endpoint MUST require authentication (valid JWT).

#### Scenario: List all states
- **WHEN** an authenticated user calls `GET /api/v1/estados`
- **THEN** the system returns HTTP 200 with an array of 27 states sorted alphabetically by name

#### Scenario: Unauthenticated access
- **WHEN** a request without a valid JWT calls `GET /api/v1/estados`
- **THEN** the system returns HTTP 401 Unauthorized

---

### Requirement: Municipalities by state endpoint
The backend SHALL expose `GET /api/v1/estados/:uf/municipios` returning municipalities for the given state. Each municipality SHALL include: codigoIbge, nome, siglaEstado.

#### Scenario: List municipalities
- **WHEN** an authenticated user calls `GET /api/v1/estados/MG/municipios`
- **THEN** the system returns HTTP 200 with municipalities of Minas Gerais sorted alphabetically

#### Scenario: Invalid state code
- **WHEN** a user calls `GET /api/v1/estados/XX/municipios` with invalid UF
- **THEN** the system returns HTTP 404 Not Found

---

### Requirement: Tax rates by municipality endpoint
The backend SHALL expose `GET /api/v1/municipios/:codigoIbge/aliquotas` returning a paginated list of services and tax rates for the given municipality. The endpoint SHALL support query parameters: pagina, tamanhoPagina, codigoServico, descricao, aliquotaMin, aliquotaMax, competencia.

#### Scenario: List tax rates with defaults
- **WHEN** an authenticated user calls `GET /api/v1/municipios/3106200/aliquotas`
- **THEN** the system returns HTTP 200 with page 1, default 20 items, sorted by service code

#### Scenario: Filter by service code partial match
- **WHEN** a user calls `GET /api/v1/municipios/3106200/aliquotas?codigoServico=01.01`
- **THEN** the system returns only services whose code starts with "01.01"

#### Scenario: Filter by description
- **WHEN** a user calls `GET /api/v1/municipios/3106200/aliquotas?descricao=analise`
- **THEN** the system returns only services whose description contains "analise" (case-insensitive)

#### Scenario: Filter by tax rate range
- **WHEN** a user calls `GET /api/v1/municipios/3106200/aliquotas?aliquotaMin=2&aliquotaMax=5`
- **THEN** the system returns only services with tax rate between 2.0 and 5.0 inclusive

#### Scenario: Pagination
- **WHEN** a user calls `GET /api/v1/municipios/3106200/aliquotas?pagina=2&tamanhoPagina=10`
- **THEN** the system returns page 2 with up to 10 items and includes total count in response headers or body

#### Scenario: Municipality with no data
- **WHEN** a user calls the endpoint for a municipality with no collected data
- **THEN** the system returns HTTP 200 with an empty array and total count 0

---

### Requirement: Tax rate detail endpoint
The backend SHALL expose `GET /api/v1/municipios/:codigoIbge/aliquotas/:codigoServico` returning detailed tax rate information for a specific service in a municipality.

#### Scenario: Detail found
- **WHEN** an authenticated user calls `GET /api/v1/municipios/3106200/aliquotas/01.01.01.001`
- **THEN** the system returns HTTP 200 with: codigoMunicipio, nomeMunicipio, codigoServico, codigoServicoFormatado, descricaoServico, aliquota, competencia, coletadoEm

#### Scenario: Detail not found
- **WHEN** the requested service code has no data for that municipality
- **THEN** the system returns HTTP 404 Not Found

---

### Requirement: Swagger/OpenAPI documentation
The backend SHALL expose Swagger UI at `/swagger` and OpenAPI JSON at `/swagger/v1/swagger.json`. All endpoints MUST be documented with: descriptions, parameter types, response schemas, and example responses.

#### Scenario: Swagger UI accessible
- **WHEN** a user navigates to `/swagger`
- **THEN** the Swagger UI is displayed with all API endpoints documented

---

### Requirement: Service code normalization
The backend SHALL accept service codes in both formats (`01.01.01.001` and `010101001`) and normalize internally. Response bodies SHALL always return the formatted version (`XX.XX.XX.XXX`).

#### Scenario: Dotted format input
- **WHEN** a request includes `codigoServico=01.01.01.001`
- **THEN** the system processes it correctly

#### Scenario: Numeric format input
- **WHEN** a request includes `codigoServico=010101001`
- **THEN** the system normalizes it and processes correctly

---

### Requirement: Paginated response format
All paginated endpoints SHALL return a consistent response format with: `{ items: [...], pagina, tamanhoPagina, totalItens, totalPaginas }`.

#### Scenario: Consistent pagination metadata
- **WHEN** any paginated endpoint is called
- **THEN** the response body includes pagination metadata alongside the items array

---

### Requirement: Error response format
All error responses SHALL follow a consistent format: `{ erro: string, detalhes?: string[], codigo?: string }`.

#### Scenario: Validation error
- **WHEN** a request fails validation
- **THEN** the response body includes `{ erro: "Validação falhou", detalhes: ["campo X é obrigatório"] }`

#### Scenario: Not found error
- **WHEN** a resource is not found
- **THEN** the response body includes `{ erro: "Recurso não encontrado" }`

---

### Requirement: Health check endpoint
The backend SHALL expose `GET /health` (unauthenticated) returning service health status including MongoDB connectivity.

#### Scenario: Healthy service
- **WHEN** MongoDB is reachable
- **THEN** returns HTTP 200 with `{ status: "healthy", mongodb: "connected" }`

#### Scenario: Unhealthy service
- **WHEN** MongoDB is unreachable
- **THEN** returns HTTP 503 with `{ status: "unhealthy", mongodb: "disconnected" }`

---

### Requirement: IBGE data seed from context file
The backend SHALL include a seed mechanism to populate states and municipalities using the file `context/municipios.json` as the primary data source. This file contains 5.570 municipalities with fields: Id, Codigo (IBGE code), Nome, Uf. States SHALL be derived from the distinct UF values in this file. Municipalities SHALL include: codigoIbge (from Codigo), nome (from Nome), siglaEstado (from Uf).

#### Scenario: First startup seed
- **WHEN** the application starts and the estados collection is empty
- **THEN** the system reads `context/municipios.json`, derives 27 states from distinct UF values, and seeds all states and 5.570 municipalities

#### Scenario: Seed idempotent
- **WHEN** the seed runs and data already exists
- **THEN** the system skips seeding without duplicating data

#### Scenario: Seed data format
- **WHEN** the seed reads `context/municipios.json`
- **THEN** each municipality is stored with codigoIbge as string (7 digits from Codigo field), nome, and siglaEstado (from Uf field)
