## ADDED Requirements

### Requirement: Persistência durável do certificado PFX no MongoDB
O sistema SHALL persistir o certificado PFX digital na collection `certificados_digitais` do MongoDB. A collection SHALL conter no máximo 1 documento ativo por vez. O documento SHALL armazenar: bytes PFX, senha, thumbprint, subject (CN), data de validade (NotAfter), data de upload.

#### Scenario: Upload persiste no MongoDB
- **WHEN** um admin faz upload de um certificado PFX válido via `POST /api/v1/crawler/certificado`
- **THEN** o sistema SHALL salvar os bytes PFX, senha e metadados extraídos na collection `certificados_digitais`
- **AND** o certificado anterior (se existir) SHALL ser substituído (upsert)

#### Scenario: Remoção apaga do MongoDB
- **WHEN** um admin remove o certificado via `DELETE /api/v1/crawler/certificado`
- **THEN** o documento SHALL ser removido da collection `certificados_digitais`
- **AND** o cache em memória SHALL ser limpo

#### Scenario: Metadados extraídos automaticamente no upload
- **WHEN** um certificado PFX é armazenado com sucesso
- **THEN** o sistema SHALL extrair e persistir: Thumbprint (hex string), Subject (CN do certificado), ValidoAte (NotAfter do certificado)

---

### Requirement: Cache em memória com carregamento automático na inicialização
O `CertificadoStore` SHALL manter um cache em memória do certificado para evitar round-trips ao MongoDB em cada request. Na inicialização da aplicação, o sistema SHALL carregar o certificado do MongoDB automaticamente para o cache.

#### Scenario: Carregamento automático na inicialização
- **WHEN** a aplicação inicia e existe um certificado salvo no MongoDB
- **THEN** o sistema SHALL carregar o certificado para o cache em memória antes de aceitar requests
- **AND** o crawler SHALL poder executar sem necessidade de novo upload manual

#### Scenario: Inicialização sem certificado no banco
- **WHEN** a aplicação inicia e não existe certificado salvo no MongoDB
- **THEN** o cache em memória SHALL permanecer vazio
- **AND** o sistema SHALL funcionar normalmente, apenas sem capacidade de chamar a API NFS-e

#### Scenario: Cache atualizado no upload
- **WHEN** um novo certificado é salvo no MongoDB via upload
- **THEN** o cache em memória SHALL ser atualizado imediatamente com o novo certificado

#### Scenario: Cache limpo na remoção
- **WHEN** o certificado é removido do MongoDB
- **THEN** o cache em memória SHALL ser limpo imediatamente

---

### Requirement: Metadados do certificado expostos no endpoint de status
O endpoint `GET /api/v1/crawler/certificado` SHALL retornar metadados do certificado além do status atual de disponibilidade.

#### Scenario: Status com certificado disponível retorna metadados
- **WHEN** um admin chama `GET /api/v1/crawler/certificado` e existe um certificado armazenado
- **THEN** o response SHALL incluir: `hasCertificate: true`, `uploadedAt`, `thumbprint`, `subject`, `validoAte`

#### Scenario: Status sem certificado retorna campos nulos
- **WHEN** um admin chama `GET /api/v1/crawler/certificado` e não existe certificado armazenado
- **THEN** o response SHALL incluir: `hasCertificate: false`, `uploadedAt: null`, `thumbprint: null`, `subject: null`, `validoAte: null`

---

### Requirement: Exibição de metadados do certificado no frontend
O frontend SHALL exibir os metadados do certificado na tela de gerenciamento (`/admin/crawler/certificado`) quando um certificado estiver disponível.

#### Scenario: Exibição de metadados quando certificado disponível
- **WHEN** o frontend carrega a tela de certificado e existe certificado armazenado
- **THEN** o frontend SHALL exibir: Subject (CN), Thumbprint, Validade (data formatada), Data de upload

#### Scenario: Certificado próximo do vencimento
- **WHEN** o certificado tem validade inferior a 30 dias
- **THEN** o frontend SHALL exibir um alerta visual (tag warning) indicando que o certificado está próximo do vencimento
