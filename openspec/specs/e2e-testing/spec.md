## ADDED Requirements

### Requirement: Authentication E2E flows
The Cypress E2E project SHALL cover authentication flows including: successful registration, successful login, login with invalid credentials, and logout.

#### Scenario: E2E successful registration
- **WHEN** a user fills the signup form with valid data and submits
- **THEN** the user is redirected to the authenticated area

#### Scenario: E2E successful login
- **WHEN** a user fills the login form with valid credentials and submits
- **THEN** the user is redirected to the authenticated area

#### Scenario: E2E invalid login
- **WHEN** a user submits invalid credentials
- **THEN** an error message is displayed and the user remains on the login page

---

### Requirement: Navigation E2E flows
The Cypress E2E project SHALL cover navigation flows: accessing the sidebar menu, navigating to the consultation page, and using breadcrumbs.

#### Scenario: E2E menu navigation
- **WHEN** an authenticated user clicks the consultation menu item
- **THEN** the application navigates to the map view

#### Scenario: E2E breadcrumb navigation
- **WHEN** a user is on the municipality level and clicks the state breadcrumb
- **THEN** the application navigates back to the municipality list

---

### Requirement: Consultation flow E2E
The Cypress E2E project SHALL cover the full consultation flow: map display, state selection, municipality selection, service listing, and filtering.

#### Scenario: E2E map interaction
- **WHEN** an authenticated user views the map
- **THEN** the SVG map is rendered with clickable states

#### Scenario: E2E state selection
- **WHEN** a user clicks a state on the map
- **THEN** the municipality list for that state is displayed

#### Scenario: E2E municipality selection
- **WHEN** a user selects a municipality from the list
- **THEN** the service/tax rate listing is displayed

#### Scenario: E2E filter application
- **WHEN** a user applies a filter on the listing
- **THEN** the results update to match the filter criteria

#### Scenario: E2E clear filters
- **WHEN** a user clears all filters
- **THEN** the full unfiltered listing is displayed

---

### Requirement: Error pages E2E
The Cypress E2E project SHALL cover error pages: 404 for unknown routes and access denied.

#### Scenario: E2E 404 page
- **WHEN** a user navigates to a non-existent route
- **THEN** the 404 page is displayed with a link to return home

#### Scenario: E2E access denied page
- **WHEN** an unauthenticated user tries to access a protected route
- **THEN** the user is redirected to the login page

---

### Requirement: Test data strategy
The E2E project SHALL use a seeded test database with known data for deterministic assertions. The seed SHALL include: at least 2 test users, at least 3 states with municipalities, and at least 10 service/tax rate records.

#### Scenario: Test environment setup
- **WHEN** the E2E test suite runs
- **THEN** the test database is seeded with known data before tests execute

#### Scenario: Test isolation
- **WHEN** each test spec runs
- **THEN** the database state is consistent (either reset per spec or isolated)

---

### Requirement: Stable selectors
The E2E project SHALL use `data-cy` attributes for element selection instead of CSS classes or text content.

#### Scenario: Selector stability
- **WHEN** a frontend component is restyled or text is changed
- **THEN** E2E tests continue to pass because they rely on `data-cy` attributes
