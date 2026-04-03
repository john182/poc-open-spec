## MODIFIED Requirements

### Requirement: PFX certificate management via API
The system SHALL allow administrators to upload, check, and remove the PFX certificate used for mTLS authentication with the NFS-e API. Certificate management endpoints SHALL require Admin role. The certificate SHALL be persisted in MongoDB (collection `certificados_digitais`) for durability across restarts. The system SHALL maintain an in-memory cache for fast access. On application startup, the system SHALL automatically load the certificate from MongoDB. The static file fallback (`NfseApi:CertificatePath`) SHALL be removed — MongoDB is the single source of truth.

#### Scenario: Upload certificate
- **WHEN** an admin user uploads a PFX file with password via `POST /api/v1/crawler/certificado`
- **THEN** the system validates the certificate, persists it in MongoDB with extracted metadata (thumbprint, subject, validity), updates the in-memory cache, and returns upload confirmation

#### Scenario: Invalid certificate upload
- **WHEN** an admin user uploads an invalid PFX file or provides wrong password
- **THEN** the system returns HTTP 400 with error details

#### Scenario: Check certificate status
- **WHEN** an admin user calls `GET /api/v1/crawler/certificado`
- **THEN** the system returns certificate availability, upload date, thumbprint, subject, and validity date

#### Scenario: Remove certificate
- **WHEN** an admin user calls `DELETE /api/v1/crawler/certificado`
- **THEN** the system removes the certificate from MongoDB, clears the in-memory cache, and returns HTTP 204

#### Scenario: Non-admin access to certificate endpoints
- **WHEN** a non-admin user attempts any certificate endpoint
- **THEN** the system returns HTTP 403 Forbidden

#### Scenario: Crawler requires certificate
- **WHEN** the crawler attempts to call the NFS-e API without a loaded certificate
- **THEN** the system logs the error and marks the execution as failed

#### Scenario: Certificate survives application restart
- **WHEN** the application restarts after a certificate was previously uploaded
- **THEN** the system automatically loads the certificate from MongoDB during startup
- **AND** the crawler can execute without requiring a new manual upload

#### Scenario: Unified availability check
- **WHEN** any component (Controller, BackgroundService, CrawlerService) checks certificate availability
- **THEN** all components SHALL use `ICertificadoStore.HasCertificate()` as the single source of truth
