## 1. ConfiguracaoCrawler — Novo campo MaxUfsParalelas

- [x] 1.1 Adicionar propriedade `MaxUfsParalelas` em `ConfiguracaoCrawler.cs` com default 5, incluir no `CriarPadrao()`, `Atualizar()` e `AtualizarParcial()`
- [x] 1.2 Adicionar campo `MaxUfsParalelas` nos DTOs: `ConfiguracaoCrawlerResponse`, `AtualizarConfiguracaoCrawlerRequest`, `AtualizarParcialConfiguracaoCrawlerRequest`
- [x] 1.3 Adicionar validação no `AtualizarConfiguracaoCrawlerRequestValidator` e `AtualizarParcialConfiguracaoCrawlerRequestValidator`: `MaxUfsParalelas` entre 1 e 27
- [x] 1.4 Atualizar mapeamento MongoDB em `CrawlerMongoMappings.cs` para incluir `MaxUfsParalelas`
- [x] 1.5 Escrever testes unitários para `MaxUfsParalelas` em `ConfiguracaoCrawlerTests`, `ConfiguracaoCrawlerAppServiceTests` e `AtualizarConfiguracaoCrawlerRequestValidatorTests`

## 2. ExecucaoCrawler — Thread-safety e UfsEmAndamento

- [x] 2.1 Substituir `Dictionary<string, ProgressoUf>` por `ConcurrentDictionary<string, ProgressoUf>` em `ExecucaoCrawler`
- [x] 2.2 Substituir `UfAtual: string?` por `UfsEmAndamento: List<string>` protegido por lock (adicionar/remover thread-safe)
- [x] 2.3 Atualizar métodos `IniciarProcessamentoUf`, `FinalizarProcessamentoUf`, `FalharProcessamentoUf`, `InterromperProcessamentoUf` para usar `ConcurrentDictionary` e lock em `UfsEmAndamento`
- [x] 2.4 Atualizar `StatusCrawlerResponse` para incluir `UfsEmAndamento: List<string>` (substituindo `UfAtual`)
- [x] 2.5 Atualizar mapeamento MongoDB em `CrawlerMongoMappings.cs` para `UfsEmAndamento` e `ConcurrentDictionary`
- [x] 2.6 Escrever testes unitários de thread-safety: múltiplas threads chamando `IniciarProcessamentoUf`/`FinalizarProcessamentoUf` simultaneamente sem exceção

## 3. CrawlerService — Paralelismo na Fase 1

- [x] 3.1 Refatorar `FaseConvenioAsync` para usar `Parallel.ForEachAsync` com `MaxDegreeOfParallelism = _configuracao.MaxUfsParalelas`
- [x] 3.2 Substituir `List<Municipio> todosAtivos` por `ConcurrentBag<Municipio>` e converter para `List` no retorno
- [x] 3.3 Implementar `CancellationTokenSource` derivado para propagar interrupção de proteção de certificado entre UFs paralelas
- [x] 3.4 Ajustar o bloco de determinação de status da UF (interrompida/falha/concluída) para funcionar dentro do contexto paralelo
- [x] 3.5 Validar que `ValidarConfiguracao()` inclui fallback para `MaxUfsParalelas` quando valor inválido (<=0)
- [x] 3.6 Escrever testes unitários: `FaseConvenioAsync` com múltiplas UFs verifica que todas são processadas e resultados acumulados corretamente

## 4. Testes e Validação

- [x] 4.1 Garantir que todos os testes existentes continuam passando após as mudanças (rodar `dotnet test` completo)
- [x] 4.2 Escrever teste de integração: executar crawler com `MaxUfsParalelas > 1` e verificar que `UfsEmAndamento` é populado corretamente durante execução
- [x] 4.3 Verificar compilação e ausência de warnings críticos
