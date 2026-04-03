## Context

O crawler do MapaTributário processa UFs sequencialmente num `foreach` nas fases de descoberta (Fase 1: convênio, Fase 2: probe). A Fase 3 (processamento da fila) já é paralela por item via `SemaphoreSlim(MaxItensParalelos)`.

Estado atual:
- `CrawlerService.FaseConvenioAsync`: `foreach (string uf in ufsParaProcessar)` — sequencial
- `CrawlerService.FaseProbeAsync`: `foreach (Municipio municipio in municipiosAtivos)` — sequencial (já flat, sem conceito de UF)
- `ExecucaoCrawler.UfAtual`: `string?` — assume uma única UF por vez
- Métodos de tracking de UF (`IniciarProcessamentoUf`, `FinalizarProcessamentoUf`, etc.) **não são thread-safe** — acessam `Dictionary<string, ProgressoUf>` e `UfAtual` sem lock
- O benchmark provou: API suporta 50+ req/s, zero rate limiting, latência estável ~60ms independente de quantas UFs simultâneas

## Goals / Non-Goals

**Goals:**
- Processar N UFs simultaneamente na Fase 1 (convênio), reduzindo o tempo de descoberta proporcionalmente
- Tornar `ExecucaoCrawler` thread-safe para tracking simultâneo de múltiplas UFs
- Manter a Fase 3 inalterada (já é paralela por item)
- Novo campo configurável `MaxUfsParalelas` em `ConfiguracaoCrawler` (default: 5)
- Manter compatibilidade com filtro de UFs e filtro de capital

**Non-Goals:**
- Não mudar a Fase 3 (`ProcessarFilaAsync`) — já é paralela por item
- Não mudar a Fase 2 (`FaseProbeAsync`) — já processa lista flat de municípios, paralelizar por UF aqui não faz sentido porque a lista já é flat
- Não implementar retomada de UFs interrompidas entre execuções (escopo futuro)
- Não criar lógica de priorização de UFs (ex: por tamanho ou importância)
- Não mudar a lógica de fire-and-forget "capitais primeiro" no controller

## Decisions

### Decisão 1: `Parallel.ForEachAsync` para iterar UFs na Fase 1

**Escolha**: Substituir `foreach` por `Parallel.ForEachAsync` com `MaxDegreeOfParallelism = _configuracao.MaxUfsParalelas`.

**Alternativa considerada**: `SemaphoreSlim` + `Task.WhenAll` manual. Descartada porque `Parallel.ForEachAsync` já encapsula semáforo + backpressure + cancelamento + particionamento, e é a API idiomática do .NET 6+ para este cenário.

**Rationale**: O loop interno de cada UF (iterar municípios, chamar API, registrar resultados) é I/O-bound. `Parallel.ForEachAsync` limita o grau de paralelismo sem criar N tasks de uma vez.

### Decisão 2: Thread-safety via `ConcurrentDictionary` + lock nos métodos de UF

**Escolha**: 
- Trocar `Dictionary<string, ProgressoUf>` por `ConcurrentDictionary<string, ProgressoUf>` em `ExecucaoCrawler`
- Trocar `UfAtual: string?` por `UfsEmAndamento: List<string>` protegido por lock
- Todos os métodos de tracking de UF (`IniciarProcessamentoUf`, `FinalizarProcessamentoUf`, etc.) usam lock para manipular a lista `UfsEmAndamento`

**Alternativa considerada**: Manter `Dictionary` + lock global em todos os métodos. Descartada porque `ConcurrentDictionary` reduz contenção e é mais expressivo.

**Rationale**: Com N UFs paralelas, múltiplas threads chamam `IniciarProcessamentoUf` e `FinalizarProcessamentoUf` simultaneamente. `ConcurrentDictionary` garante segurança no acesso ao dicionário, e lock protege a lista `UfsEmAndamento`.

### Decisão 3: `todosAtivos` como `ConcurrentBag<Municipio>` na Fase 1

**Escolha**: Trocar `List<Municipio> todosAtivos` por `ConcurrentBag<Municipio>` para acumular resultados de múltiplas UFs concorrentes.

**Rationale**: Com `Parallel.ForEachAsync`, múltiplas UFs adicionam municípios ativos simultaneamente. `ConcurrentBag` é lock-free e otimizado para cenários de adicionar-depois-consumir.

### Decisão 4: Flag `interrompidoPorProtecao` como `volatile` ou via `CancellationTokenSource`

**Escolha**: Usar `CancellationTokenSource` ligado ao `CancellationToken` original para propagar interrupção de proteção para todas as UFs em paralelo.

**Alternativa considerada**: `volatile bool`. Funciona, mas é menos composável — cada loop interno precisa checar a flag. Com `CancellationTokenSource`, a propagação é automática via token.

**Rationale**: Quando `_certificateProtection.ShouldHalt`, cancela o token derivado, e todas as UFs em andamento terminam graciosamente via `OperationCanceledException`.

### Decisão 5: `MaxUfsParalelas` default = 5

**Escolha**: Default de 5 UFs simultâneas.

**Rationale**: Com 27 UFs, processar 5 simultaneamente divide o tempo de descoberta por ~5. Ir além de 5 tem retorno marginal decrescente porque o gargalo passa a ser o rate limiter global (50 req/s compartilhado). Com 5 UFs × ~75 municípios médios × 60ms latência, cada UF gera ~1.2 req/s de pressão, totalizando ~6 req/s — bem dentro do limite. UFs grandes (MG: 853, SP: 645) podem gerar mais pressão, mas o rate limiter global garante o teto.

### Decisão 6: Manter a Fase 2 (Probe) como está

**Escolha**: Não paralelizar a Fase 2 por UF.

**Rationale**: A Fase 2 recebe uma lista flat de municípios (já sem conceito de UF) e itera sequencialmente. Paralelizar aqui exigiria reestruturar a assinatura e não traria ganho significativo — a Fase 2 faz 1 request por município com os códigos de sondagem, e os municípios já vêm da Fase 1 que agora será paralela.

## Risks / Trade-offs

- **[Risco] Pressão no rate limiter global**: Com N UFs paralelas, cada uma gerando requisições, a pressão no rate limiter compartilhado aumenta. → **Mitigação**: O rate limiter global (`SemaphoreSlim(1,1)` com delay) serializa as requisições independente da origem. O default de 5 UFs é conservador.

- **[Risco] CertificateProtection e interrupção parcial**: Se a proteção de certificado disparar durante processamento paralelo, UFs em andamento podem estar em estados inconsistentes. → **Mitigação**: Usar `CancellationTokenSource` derivado para propagar halt, e cada UF trata seu estado no bloco catch/finally.

- **[Risco] Persistência de progresso no MongoDB**: O `ExecucaoCrawler` é persistido periodicamente via `_execucaoRepository.UpdateAsync(execucao)`. Com múltiplas UFs escrevendo no mesmo objeto, updates frequentes podem perder dados. → **Mitigação**: Persistir progresso após `Parallel.ForEachAsync` completar (um update após toda a Fase 1), não durante.

- **[Trade-off] `UfAtual` → `UfsEmAndamento`**: Quebra de contrato no response de status. → **Mitigação**: Manter campo `UfAtual` no response como alias do primeiro item de `UfsEmAndamento` para compatibilidade, deprecar gradualmente.

- **[Trade-off] Ordem de processamento não-determinística**: Com paralelismo, UFs não seguem mais ordem alfabética. → **Aceitável**: Ordem não é um requisito funcional.
