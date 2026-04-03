## Why

O certificado PFX usado para autenticação mTLS com a API NFS-e é armazenado exclusivamente em memória (singleton `CertificadoStore`). Quando a aplicação reinicia — deploy, crash, recriação de container — o certificado é perdido e o admin precisa fazer upload novamente. Além disso, em cenário multi-instância, cada container tem seu próprio singleton, impossibilitando compartilhamento do certificado entre réplicas.

## What Changes

- Criar entidade `CertificadoDigital` no domínio com bytes PFX, senha, metadados (thumbprint, subject, validade, dataUpload)
- Criar repositório MongoDB (`certificados_digitais`) para persistência durável
- Refatorar `CertificadoStore` para usar MongoDB como backend com cache em RAM
- Na inicialização da aplicação, carregar certificado do banco automaticamente
- Expandir `CertificadoStatusResponse` com metadados: thumbprint, subject, validoAte
- Atualizar frontend para exibir metadados do certificado (subject, thumbprint, validade)
- Corrigir check inconsistente: Controller e BackgroundService passam a verificar disponibilidade de forma unificada
- Remover fallback estático de arquivo (`/certs/client.pfx`) — o banco passa a ser a única fonte de verdade

## Capabilities

### New Capabilities
- `certificado-persistencia`: Persistência durável do certificado PFX no MongoDB com cache em memória e carregamento automático na inicialização

### Modified Capabilities
- `data-crawler`: Atualizar requisito de gerenciamento de certificado PFX — o certificado passa a ser persistido no MongoDB em vez de mantido apenas em memória. O endpoint de status passa a retornar metadados do certificado (thumbprint, subject, validade).

## Impact

- **Backend**: `CertificadoStore`, `CertificadoController`, `CertificadoStatusResponse`, `InfrastructureServiceExtensions` (registro DI e HttpClient handler), `CrawlerMongoMappings`
- **Frontend**: `CertificadoStatus` model, `crawler-certificado.component` (exibição de metadados)
- **Banco**: Nova collection `certificados_digitais` no MongoDB
- **Testes**: Novos testes unitários para entidade e repositório, atualização dos testes existentes de `CertificadoStore` e `CertificadoController`
- **Breaking**: Fallback de arquivo estático (`NfseApi:CertificatePath`) será removido — certificado deve ser enviado via API
