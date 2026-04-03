## Context

O `CertificadoStore` atual é um singleton in-memory que armazena bytes PFX, senha e o objeto `X509Certificate2` parseado. Não há persistência — qualquer restart perde o certificado. O `HttpClientHandler` do `NfseApiClient` é configurado uma vez via DI com `ConfigurePrimaryHttpMessageHandler`, o que cria um acoplamento temporal: certificado uploadado após a criação do handler não é captado até reciclagem (~2min do `HttpClientFactory`).

Existem checks inconsistentes: `CrawlerController` e `CrawlerBackgroundService` verificam apenas o cert dinâmico, enquanto `CrawlerService.CertificadoDisponivel()` verifica também o fallback estático.

O projeto usa MongoDB para todas as entidades (collections: `usuarios`, `estados`, `municipios`, `servicos`, `aliquotas`, `execucoes_crawler`, `fila_processamento`, `configuracoes_crawler`). Repositórios são singletons que recebem `IMongoDatabase` via DI.

## Goals / Non-Goals

**Goals:**
- Persistir certificado PFX no MongoDB para sobreviver a restarts
- Carregar certificado automaticamente na inicialização da aplicação
- Expor metadados do certificado (thumbprint, subject, validade) no endpoint de status e no frontend
- Unificar check de disponibilidade de certificado (uma única fonte de verdade)
- Manter cache em RAM para evitar ir ao banco a cada request

**Non-Goals:**
- Criptografia dos bytes PFX no banco (poc sem auth no MongoDB — risco aceito)
- Suporte multi-instância com invalidação de cache em tempo real (restart resolve para o poc)
- Rotação automática de certificado próximo ao vencimento
- Migração de certificados existentes (não há estado a migrar — era in-memory)

## Decisions

### 1. Entidade `CertificadoDigital` como documento MongoDB

Criar entidade de domínio `CertificadoDigital` com: Id, PfxBytes (byte[]), Senha (string), Thumbprint, Subject, ValidoAte, DataUpload. Armazenar em collection `certificados_digitais`.

**Alternativa considerada**: Armazenar dentro de `ConfiguracaoCrawler` como subdocumento. Rejeitado porque o certificado é um conceito separado da configuração e os bytes PFX podem ter vários MB — não faz sentido carregar junto com a configuração em toda leitura.

**Alternativa considerada**: Armazenar no GridFS. Rejeitado porque o PFX é limitado a 10MB (já validado no controller) e documentos MongoDB suportam até 16MB. Documento simples é suficiente e mais simples.

### 2. Repositório `ICertificadoDigitalRepository` com operação de documento único

A collection terá no máximo 1 documento (o certificado ativo). O repositório expõe: `ObterAsync()`, `SalvarAsync(CertificadoDigital)`, `RemoverAsync()`. `SalvarAsync` faz upsert — remove o anterior e insere o novo.

### 3. `CertificadoStore` refatorado como cache sobre o repositório

O `CertificadoStore` continua sendo singleton e mantém o cache em RAM. As mudanças:
- `StoreAsync(bytes, senha)` → persiste no MongoDB via repositório, depois atualiza cache
- `Remove()` → remove do MongoDB via repositório, depois limpa cache
- `GetCertificate()` / `HasCertificate()` → lê do cache em RAM (sem round-trip ao banco)
- Novo método `CarregarDoBancoAsync()` — chamado na inicialização para popular o cache

O `CertificadoStore` passa a receber `ICertificadoDigitalRepository` via construtor.

### 4. Carregamento na inicialização via `RunSeedsAsync`

Adicionar chamada a `CertificadoStore.CarregarDoBancoAsync()` no pipeline de inicialização (junto com os seeds existentes em `InfrastructureServiceExtensions.RunSeedsAsync`). Isso garante que, se existir um certificado salvo, ele estará disponível antes do primeiro request.

### 5. Remoção do fallback estático

O fallback de arquivo (`NfseApi:CertificatePath` / `NfseApi:CertificatePassword`) será removido do `RegisterNfseApiClient`. A única fonte de certificado passa a ser o `ICertificadoStore` (que agora é backed por MongoDB). As propriedades de configuração `CertificatePath` e `CertificatePassword` no `NfseApiClientOptions` permanecem por ora, mas não são mais usadas para carregar certificado.

### 6. Metadados extraídos no momento do upload

Ao fazer `StoreAsync`, o `CertificadoStore` parseia o PFX e extrai: `Thumbprint`, `Subject` (CN do certificado), `NotAfter` (validade). Esses metadados são salvos na entidade `CertificadoDigital` e expostos no `CertificadoStatusResponse`.

### 7. `CertificadoStatusResponse` expandido

Adicionar campos: `Thumbprint` (string), `Subject` (string), `ValidoAte` (DateTime?). O frontend exibe esses dados na tela de certificado.

## Risks / Trade-offs

- **[Senha em plaintext no banco]** → Risco aceito para poc. Em produção, usar Data Protection API do .NET para criptografar antes de salvar.
- **[Cache stale em multi-instância]** → Se instância A faz upload e instância B ainda tem cache antigo, B não sabe. Mitigação: restart resolve. Para produção, usar polling ou change streams.
- **[HttpClient handler não atualiza imediatamente]** → O `ConfigurePrimaryHttpMessageHandler` é chamado uma vez. Após upload de novo cert, o handler antigo continua ativo até reciclagem (~2min). Isso é comportamento existente e aceitável — o handler é reciclado automaticamente pelo `HttpClientFactory`.
- **[Documento de até ~10MB no MongoDB]** → PFX files tipicamente são <10KB. O limite de upload (10MB) é conservador. Sem risco prático.
