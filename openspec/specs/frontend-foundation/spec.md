## ADDED Requirements

### Requirement: Template analysis and controlled reuse
The frontend MUST use the PrimeNG Sakai template as a controlled reference. The team SHALL analyze the template to decide what to reuse, adapt, or discard. The template MUST NOT be copied blindly.

#### Scenario: Template analysis completed
- **WHEN** the frontend foundation work begins
- **THEN** a document `frontend-foundation.md` SHALL exist mapping each Sakai component to a reuse decision (reuse/adapt/discard) with justification

---

### Requirement: Admin layout with sidebar navigation
The frontend SHALL provide an authenticated admin layout with: topbar with logo and user actions, collapsible sidebar with menu items, main content area, and footer. The layout SHALL support static and overlay menu modes. The layout SHALL be responsive (desktop and mobile).

#### Scenario: Desktop layout
- **WHEN** a user accesses the application on a desktop screen (>1024px)
- **THEN** the layout displays a static sidebar, topbar, and content area

#### Scenario: Mobile layout
- **WHEN** a user accesses the application on a mobile screen (<1024px)
- **THEN** the sidebar becomes an overlay triggered by a hamburger button in the topbar

#### Scenario: Menu navigation
- **WHEN** a user clicks a menu item in the sidebar
- **THEN** the application navigates to the corresponding route and highlights the active menu item

---

### Requirement: Design system definition
The project SHALL define a design system document (`design-system.md`) covering: color palette, typography scale, spacing scale, component patterns, icon usage, and interaction patterns. The design system MUST be based on PrimeNG components and Tailwind utilities.

#### Scenario: Design system document exists
- **WHEN** the frontend foundation phase is completed
- **THEN** a `design-system.md` document SHALL exist with all defined patterns

---

### Requirement: Design tokens definition
The project SHALL define design tokens (`design-tokens.md`) as CSS custom properties covering: primary/secondary/accent colors, surface colors, text colors, border radius, spacing units, shadow definitions, and breakpoints. Tokens MUST support dark mode.

#### Scenario: Design tokens applied
- **WHEN** the frontend application loads
- **THEN** all UI elements SHALL use design tokens instead of hardcoded values

#### Scenario: Dark mode
- **WHEN** a user toggles dark mode
- **THEN** the application switches color tokens and all components update accordingly

---

### Requirement: Base reusable components
The frontend SHALL provide base reusable components: LoadingSpinner, EmptyState, ErrorState with retry button, PageHeader, FilterBar, DataTable wrapper, ConfirmDialog, and Toast notifications.

#### Scenario: Loading state
- **WHEN** an async operation is in progress
- **THEN** the appropriate component displays a loading spinner with optional message

#### Scenario: Error state with retry
- **WHEN** an API call fails
- **THEN** the ErrorState component displays an error message and a retry button that re-executes the failed operation

#### Scenario: Empty state
- **WHEN** a query returns zero results
- **THEN** the EmptyState component displays a friendly message with optional action

---

### Requirement: Form helpers
The frontend SHALL provide form helper utilities: form field wrapper with label/error display, validation message mapping, and common validators (required, email, minLength, pattern).

#### Scenario: Field validation display
- **WHEN** a form field is invalid and touched
- **THEN** the field wrapper displays the appropriate validation error message below the input

---

### Requirement: Sign In page
The frontend SHALL provide a sign in page at `/auth/login` with email and password fields, a submit button, a link to sign up, and validation feedback.

#### Scenario: Successful login
- **WHEN** a user enters valid credentials and clicks sign in
- **THEN** the system authenticates and redirects to the main application

#### Scenario: Login validation
- **WHEN** a user submits the form with empty fields
- **THEN** validation errors are displayed inline

#### Scenario: Login error
- **WHEN** the server returns 401
- **THEN** a toast or inline message shows "Credenciais inválidas"

---

### Requirement: Sign Up page
The frontend SHALL provide a sign up page at `/auth/signup` with name, email, password, and password confirmation fields, a submit button, and a link back to sign in.

#### Scenario: Successful registration
- **WHEN** a user fills all fields correctly and submits
- **THEN** the system creates the account and redirects to the main application

#### Scenario: Password mismatch
- **WHEN** password and confirmation do not match
- **THEN** a validation error is displayed on the confirmation field

---

### Requirement: 404 Not Found page
The frontend SHALL display a 404 page for unmatched routes with a message and link to navigate home.

#### Scenario: Unknown route
- **WHEN** a user navigates to a route that does not exist
- **THEN** the 404 page is displayed with a "Voltar ao início" link

---

### Requirement: Access Denied page
The frontend SHALL display an access denied page with a message and link to navigate home.

#### Scenario: Forbidden access
- **WHEN** a user is redirected to the access denied page
- **THEN** the page displays "Acesso Negado" and a link to the main area
