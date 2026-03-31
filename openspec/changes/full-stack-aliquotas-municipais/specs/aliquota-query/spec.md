## ADDED Requirements

### Requirement: Brazil map display
The frontend SHALL display an interactive SVG map of Brazil showing all 27 states (26 estados + DF). Each state SHALL be a clickable region with hover effect. The map MUST be responsive.

#### Scenario: Map loads
- **WHEN** a user navigates to the consultation page
- **THEN** the SVG map of Brazil is displayed with all states visible

#### Scenario: State hover
- **WHEN** a user hovers over a state on the map
- **THEN** the state region visually highlights and a tooltip shows the state name

#### Scenario: State click
- **WHEN** a user clicks a state on the map
- **THEN** the application navigates to the municipality list for that state

---

### Requirement: Municipality list by state
The frontend SHALL display a list of municipalities for the selected state. The list SHALL show municipality name and IBGE code. The list SHALL support text search filtering. The list SHALL show loading and empty states.

#### Scenario: Municipalities loaded
- **WHEN** a user selects a state (e.g., "MG")
- **THEN** the frontend calls `GET /api/v1/estados/MG/municipios` and displays the municipality list

#### Scenario: Municipality search filter
- **WHEN** a user types in the search field
- **THEN** the municipality list filters in real-time by name (case-insensitive)

#### Scenario: Municipality selection
- **WHEN** a user clicks a municipality in the list
- **THEN** the application navigates to the service/tax rate listing for that municipality

#### Scenario: No municipalities
- **WHEN** the selected state has no municipalities with data
- **THEN** the EmptyState component is displayed

---

### Requirement: Service/tax rate listing
The frontend SHALL display a paginated table of services and their tax rates for the selected municipality. The table SHALL show: service code (formatted), service description, tax rate (%), and reference period (competência). The table SHALL support pagination with configurable page size.

#### Scenario: Listing loaded
- **WHEN** a user selects a municipality (e.g., IBGE code 3106200)
- **THEN** the frontend calls `GET /api/v1/municipios/3106200/aliquotas` and displays the data table

#### Scenario: Pagination
- **WHEN** there are more results than the page size
- **THEN** pagination controls are displayed and functional

#### Scenario: Service code display format
- **WHEN** a service code is displayed
- **THEN** it SHALL be formatted as `XX.XX.XX.XXX` regardless of how it is stored

---

### Requirement: Listing filters
The frontend SHALL provide filters on the tax rate listing: text search by service code or description, tax rate range (min/max), and competência period. Filters SHALL be applied via query params to the backend API. Filters SHALL be clearable individually or all at once.

#### Scenario: Filter by service description
- **WHEN** a user types "análise" in the description filter
- **THEN** the listing reloads showing only services whose description contains "análise"

#### Scenario: Filter by tax rate range
- **WHEN** a user sets min=2.0 and max=5.0
- **THEN** the listing shows only services with tax rates between 2.0% and 5.0%

#### Scenario: Clear filters
- **WHEN** a user clicks "Limpar filtros"
- **THEN** all filters are reset and the full listing is reloaded

#### Scenario: Combined filters
- **WHEN** a user applies multiple filters simultaneously
- **THEN** all filters are combined with AND logic

---

### Requirement: Service detail view
The frontend SHALL allow viewing detailed information for a specific service/tax rate, showing: full service code, description, current tax rate, competência, municipality name, and last update timestamp.

#### Scenario: Detail loaded
- **WHEN** a user clicks on a service row in the listing
- **THEN** the frontend calls `GET /api/v1/municipios/:codigoIbge/aliquotas/:codigoServico` and displays the detail

---

### Requirement: Breadcrumb navigation
The frontend SHALL display breadcrumb navigation reflecting the user's position in the consultation flow: Consulta > Estado > Município > [Serviço]. Each breadcrumb segment SHALL be clickable to navigate back.

#### Scenario: Breadcrumb at municipality level
- **WHEN** a user is viewing municipalities of "Minas Gerais"
- **THEN** breadcrumb shows "Consulta > Minas Gerais" with "Consulta" clickable

#### Scenario: Breadcrumb navigation back
- **WHEN** a user clicks "Consulta" in the breadcrumb
- **THEN** the application navigates back to the map view

---

### Requirement: Loading, error, and retry states
Every data-fetching view in the consultation flow SHALL handle loading, error, and retry states using the shared base components.

#### Scenario: API error on municipality list
- **WHEN** the municipalities API call fails
- **THEN** the ErrorState component is displayed with a retry button

#### Scenario: Retry after error
- **WHEN** a user clicks retry after an error
- **THEN** the original API call is re-executed
