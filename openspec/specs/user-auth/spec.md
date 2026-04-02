## ADDED Requirements

### Requirement: User registration
The system SHALL allow new users to register with email, name, and password. The email MUST be unique. The password MUST be stored as a bcrypt hash. Upon successful registration, the system SHALL return JWT access and refresh tokens.

#### Scenario: Successful registration
- **WHEN** a user submits valid email, name, and password to `POST /api/v1/auth/register`
- **THEN** the system creates the user account, returns HTTP 201 with `{ accessToken, refreshToken, expiresIn }`

#### Scenario: Duplicate email
- **WHEN** a user submits an email that already exists
- **THEN** the system returns HTTP 409 Conflict with error message

#### Scenario: Invalid input
- **WHEN** a user submits missing or malformed fields (empty email, password < 8 chars, empty name)
- **THEN** the system returns HTTP 400 Bad Request with validation errors

---

### Requirement: User login
The system SHALL authenticate users by email and password. Upon successful authentication, the system SHALL return JWT access token (short-lived) and refresh token (long-lived).

#### Scenario: Successful login
- **WHEN** a user submits valid email and password to `POST /api/v1/auth/login`
- **THEN** the system returns HTTP 200 with `{ accessToken, refreshToken, expiresIn }`

#### Scenario: Invalid credentials
- **WHEN** a user submits incorrect email or password
- **THEN** the system returns HTTP 401 Unauthorized with generic error message (no detail on which field is wrong)

#### Scenario: Inactive user
- **WHEN** a user whose account is marked as inactive attempts to login
- **THEN** the system returns HTTP 403 Forbidden

---

### Requirement: Token refresh
The system SHALL allow refreshing an expired access token using a valid refresh token.

#### Scenario: Successful refresh
- **WHEN** a client submits a valid refresh token to `POST /api/v1/auth/refresh`
- **THEN** the system returns HTTP 200 with a new `{ accessToken, expiresIn }`

#### Scenario: Invalid refresh token
- **WHEN** a client submits an expired or invalid refresh token
- **THEN** the system returns HTTP 401 Unauthorized

---

### Requirement: Route protection (frontend)
The frontend SHALL protect authenticated routes with an AuthGuard. Unauthenticated users MUST be redirected to the login page. The frontend SHALL include a JWT interceptor that attaches the access token to all API requests.

#### Scenario: Unauthenticated access to protected route
- **WHEN** a user without a valid token navigates to a protected route
- **THEN** the frontend redirects the user to `/auth/login`

#### Scenario: Token expired during session
- **WHEN** an API call returns HTTP 401 and a refresh token is available
- **THEN** the frontend automatically refreshes the token and retries the original request

#### Scenario: Refresh fails
- **WHEN** the token refresh also fails (refresh token expired)
- **THEN** the frontend clears stored tokens and redirects to `/auth/login`

---

### Requirement: Access denied page
The frontend SHALL display an access denied page when a user attempts to access a resource they are not authorized for.

#### Scenario: Forbidden response
- **WHEN** an API call returns HTTP 403
- **THEN** the frontend navigates to the access denied page with a clear message and a link back to the dashboard

---

### Requirement: Password security
The system SHALL enforce minimum password requirements and store passwords securely.

#### Scenario: Password hashing
- **WHEN** a user registers or changes their password
- **THEN** the system stores the password as a bcrypt hash with a cost factor of at least 12

#### Scenario: Password minimum requirements
- **WHEN** a user submits a password shorter than 8 characters
- **THEN** the system rejects the request with HTTP 400

---

### Requirement: Role-based access and frontend navigation
The system SHALL assign roles to users based on the `Admin:Emails` configuration. Users whose email is in this list receive role "Admin"; all others receive role "User". The frontend SHALL adapt navigation based on the user's role.

#### Scenario: Admin user sees crawler menu
- **WHEN** a user with Admin role logs into the frontend
- **THEN** the sidebar navigation displays the "Crawler Management" menu section (executar, status, certificado)

#### Scenario: Regular user does not see crawler menu
- **WHEN** a user with role "User" logs into the frontend
- **THEN** the sidebar navigation does NOT display the "Crawler Management" menu section

#### Scenario: Guest user can browse consultation
- **WHEN** an unauthenticated user navigates to the consultation pages (mapa, estados, municipios, aliquotas)
- **THEN** the frontend allows access without requiring login — consultation is public

#### Scenario: Guest guard prevents re-login
- **WHEN** an already-authenticated user navigates to the login page
- **THEN** the frontend redirects them to the dashboard/consultation page
