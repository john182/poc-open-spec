## ADDED Requirements

### Requirement: Docker Compose orchestration
The project SHALL provide a `docker-compose.yml` at the repository root that orchestrates all services: frontend (nginx), backend (.NET), MongoDB. The compose file SHALL support starting the full stack with a single `docker compose up` command.

#### Scenario: Full stack startup
- **WHEN** a developer runs `docker compose up`
- **THEN** all services start and the application is accessible at `http://localhost:4200`

#### Scenario: Service dependencies
- **WHEN** docker compose starts
- **THEN** MongoDB starts first, then backend (waits for MongoDB health), then frontend

---

### Requirement: Frontend Dockerfile
The frontend SHALL have a multi-stage Dockerfile: build stage (Node for Angular build) and serve stage (nginx:alpine for static file serving). The nginx config SHALL proxy API requests to the backend.

#### Scenario: Frontend build and serve
- **WHEN** the frontend Docker image is built
- **THEN** Angular is compiled to static files and served via nginx on port 4200

#### Scenario: API proxy
- **WHEN** the frontend makes a request to `/api/*`
- **THEN** nginx proxies the request to the backend service

---

### Requirement: Backend Dockerfile
The backend SHALL have a multi-stage Dockerfile: build/publish stage (.NET SDK) and runtime stage (.NET runtime). The worker runs as a BackgroundService within the same container.

#### Scenario: Backend build and run
- **WHEN** the backend Docker image is built and started
- **THEN** the ASP.NET Core application runs on port 5000 with the worker active

---

### Requirement: MongoDB configuration
The docker-compose SHALL include a MongoDB 7 service with: named volume for data persistence, configurable credentials via environment variables, health check.

#### Scenario: Data persistence
- **WHEN** docker compose is stopped and restarted
- **THEN** MongoDB data is preserved via named volume

#### Scenario: Health check
- **WHEN** other services depend on MongoDB
- **THEN** they wait until MongoDB health check passes before starting

---

### Requirement: Environment configuration
The project SHALL use `.env` file (gitignored) for environment-specific configuration: MongoDB connection string, JWT secret, API NFS-e certificate path, worker CRON schedule. A `.env.example` SHALL be provided with placeholder values.

#### Scenario: Environment file template
- **WHEN** a developer clones the repository
- **THEN** `.env.example` exists with all required variables documented

#### Scenario: Secret protection
- **WHEN** the project is committed to git
- **THEN** `.env` and PFX files are excluded by `.gitignore`

---

### Requirement: PFX certificate mounting
The docker-compose SHALL support mounting the PFX client certificate for the NFS-e API via a volume. The certificate path SHALL be configurable via environment variable.

#### Scenario: Certificate available to worker
- **WHEN** a PFX file is placed in the configured path and docker compose starts
- **THEN** the worker can use the certificate for mTLS connections to the NFS-e API
