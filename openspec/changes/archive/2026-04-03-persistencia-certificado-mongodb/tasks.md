## 1. Entidade e Repositório

- [x] 1.1 Criar entidade de domínio `CertificadoDigital` com propriedades: Id, PfxBytes (byte[]), Senha (string), Thumbprint (string), Subject (string), ValidoAte (DateTime), DataUpload (DateTime). Adicionar factory method `Criar(byte[] pfxBytes, string senha, string thumbprint, string subject, DateTime validoAte)`.
- [x] 1.2 Criar interface `ICertificadoDigitalRepository` no domínio com métodos: `ObterAsync()`, `SalvarAsync(CertificadoDigital)`, `RemoverAsync()`.
- [x] 1.3 Criar implementação `CertificadoDigitalRepository` na camada de infraestrutura. Collection `certificados_digitais`. `SalvarAsync` faz `DeleteMany` + `InsertOne` (upsert de documento único). `ObterAsync` retorna `FindAsync().FirstOrDefaultAsync()`. `RemoverAsync` faz `DeleteMany`.
- [x] 1.4 Registrar mapeamento MongoDB para `CertificadoDigital` em `CrawlerMongoMappings` (Id como ObjectId string, PfxBytes como binary, demais campos mapeados com camelCase).
- [x] 1.5 Registrar `ICertificadoDigitalRepository` como singleton em `InfrastructureServiceExtensions`.

## 2. Refatorar CertificadoStore

- [x] 2.1 Adicionar dependência de `ICertificadoDigitalRepository` no construtor de `CertificadoStore`. Atualizar registro DI.
- [x] 2.2 Refatorar `StoreAsync(byte[] pfxBytes, string senha)`: parsear PFX, extrair metadados (thumbprint, subject, notAfter), criar `CertificadoDigital`, salvar via repositório, atualizar cache em RAM.
- [x] 2.3 Refatorar `Remove()` para `RemoveAsync()`: chamar `_repositorio.RemoverAsync()` e limpar cache. Atualizar interface `ICertificadoStore` e todos os chamadores.
- [x] 2.4 Adicionar método `CarregarDoBancoAsync()` em `ICertificadoStore` e implementação: busca documento do MongoDB, popula cache (bytes, senha, certificate, uploadedAt).
- [x] 2.5 Adicionar propriedades de metadados em `ICertificadoStore`: `Thumbprint`, `Subject`, `ValidoAte`. Implementar leitura do cache.

## 3. Carregamento na Inicialização

- [x] 3.1 Adicionar chamada a `certificadoStore.CarregarDoBancoAsync()` em `InfrastructureServiceExtensions.RunSeedsAsync` (após os seeds existentes).

## 4. Remover Fallback Estático e Unificar Checks

- [x] 4.1 Remover lógica de fallback estático (`NfseApi:CertificatePath`) do `RegisterNfseApiClient` em `InfrastructureServiceExtensions`. O handler SHALL usar apenas `ICertificadoStore.GetCertificate()`.
- [x] 4.2 Remover `CertificadoDisponivel()` do `CrawlerService` — substituir por `_certificadoStore.HasCertificate()` em todos os pontos de verificação.
- [x] 4.3 Verificar que `CrawlerController.Executar`, `CrawlerBackgroundService` e `ConsultaService` usam `ICertificadoStore.HasCertificate()` de forma unificada.

## 5. Expandir CertificadoStatusResponse e Controller

- [x] 5.1 Adicionar campos `Thumbprint` (string?), `Subject` (string?), `ValidoAte` (DateTime?) em `CertificadoStatusResponse`.
- [x] 5.2 Atualizar `CertificadoController.Status()` para preencher os novos campos a partir do `ICertificadoStore`.
- [x] 5.3 Atualizar `CertificadoController.Remove()` para chamar `RemoveAsync()` em vez de `Remove()`.

## 6. Frontend — Metadados do Certificado

- [x] 6.1 Atualizar interface `CertificadoStatus` em `crawler.models.ts` com campos: `thumbprint`, `subject`, `validoAte`.
- [x] 6.2 Atualizar `crawler-certificado.component.html` para exibir metadados: Subject (CN), Thumbprint, Validade (formatada), Data de upload.
- [x] 6.3 Adicionar lógica de alerta de vencimento próximo (< 30 dias) com tag warning no componente.

## 7. Testes

- [x] 7.1 Criar testes unitários para `CertificadoDigital` (factory method, propriedades).
- [x] 7.2 Criar testes unitários para `CertificadoStore` refatorado: StoreAsync persiste e atualiza cache, RemoveAsync limpa banco e cache, CarregarDoBancoAsync popula cache, GetCertificate retorna do cache, metadados acessíveis.
- [x] 7.3 Atualizar testes existentes de `CertificadoController` para verificar novos campos no response e chamada a RemoveAsync.
- [x] 7.4 Atualizar testes de `CrawlerService`, `CrawlerBackgroundService` e `ConsultaService` se houver mudança na interface de `ICertificadoStore`.
- [x] 7.5 Atualizar testes do frontend (`crawler-certificado.component.spec.ts`) para verificar exibição de metadados e alerta de vencimento.
