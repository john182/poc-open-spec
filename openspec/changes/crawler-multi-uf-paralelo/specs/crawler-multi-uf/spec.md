## ADDED Requirements

### Requirement: Processamento paralelo de UFs na Fase 1 (convênio)
O crawler SHALL processar múltiplas UFs simultaneamente na Fase 1 (descoberta de municípios via convênio). O grau de paralelismo SHALL ser controlado pelo campo `MaxUfsParalelas` da configuração (default: 5). O processamento SHALL usar `Parallel.ForEachAsync` com `MaxDegreeOfParallelism` igual a `MaxUfsParalelas`.

#### Scenario: Processamento paralelo com default
- **WHEN** o crawler inicia a Fase 1 com 27 UFs e `MaxUfsParalelas = 5`
- **THEN** até 5 UFs são processadas simultaneamente
- **AND** cada UF executa seu loop de municípios independentemente
- **AND** os resultados (municípios ativos) são acumulados de forma thread-safe

#### Scenario: Processamento com filtro de UFs
- **WHEN** o crawler inicia a Fase 1 com filtro `["SE", "MG", "SP"]` e `MaxUfsParalelas = 5`
- **THEN** as 3 UFs filtradas são processadas simultaneamente (3 < 5)

#### Scenario: MaxUfsParalelas = 1 equivale ao comportamento sequencial
- **WHEN** o crawler inicia a Fase 1 com `MaxUfsParalelas = 1`
- **THEN** as UFs são processadas uma por vez (comportamento idêntico ao sequencial)

---

### Requirement: Thread-safety no tracking de progresso de UFs
O `ExecucaoCrawler` SHALL usar estruturas thread-safe para tracking de UFs quando múltiplas UFs são processadas em paralelo. O `ProgressoUfs` SHALL usar `ConcurrentDictionary`. O campo `UfAtual` SHALL ser substituído por `UfsEmAndamento` (lista thread-safe de UFs em processamento).

#### Scenario: Múltiplas UFs em andamento simultaneamente
- **WHEN** 3 UFs estão sendo processadas em paralelo
- **THEN** `UfsEmAndamento` contém as 3 siglas das UFs em processamento
- **AND** `ProgressoUfs` contém entrada com `Status = EmAndamento` para cada uma

#### Scenario: UF finalizada é removida de UfsEmAndamento
- **WHEN** a UF "SE" termina o processamento com sucesso
- **THEN** "SE" é removido de `UfsEmAndamento`
- **AND** `ProgressoUfs["SE"].Status` é atualizado para `Concluido`

#### Scenario: Acesso concorrente ao ProgressoUfs
- **WHEN** duas threads chamam `IniciarProcessamentoUf` e `FinalizarProcessamentoUf` simultaneamente para UFs diferentes
- **THEN** não há exceção de concorrência
- **AND** ambos os progresso são registrados corretamente

---

### Requirement: Interrupção coordenada via CancellationToken
O processamento paralelo de UFs SHALL propagar interrupção de proteção de certificado para todas as UFs em andamento via `CancellationTokenSource`. Quando `_certificateProtection.ShouldHalt` ou `BudgetExhausted` for detectado por qualquer UF, todas as demais SHALL ser notificadas via cancelamento do token derivado.

#### Scenario: Proteção de certificado interrompe todas as UFs
- **WHEN** a UF "MG" detecta `ShouldHalt = true` durante processamento paralelo
- **THEN** o token derivado é cancelado
- **AND** as UFs restantes em andamento terminam graciosamente
- **AND** UFs que já terminaram mantêm status `Concluido`
- **AND** UFs interrompidas recebem status `Interrompido`
- **AND** UFs que não iniciaram mantêm status `Pendente`

#### Scenario: Cancelamento externo propaga para UFs paralelas
- **WHEN** o `CancellationToken` externo (da aplicação) é cancelado
- **THEN** todas as UFs em processamento paralelo terminam com `OperationCanceledException`

---

### Requirement: Configuração de MaxUfsParalelas
O campo `MaxUfsParalelas` SHALL ser adicionado a `ConfiguracaoCrawler` com default 5. SHALL ser validado entre 1 e 27 (total de UFs do Brasil). SHALL ser configurável via PATCH API (`/api/v1/crawler/configuracao`).

#### Scenario: Seed com valor default
- **WHEN** a aplicação inicia pela primeira vez e executa o seed
- **THEN** `ConfiguracaoCrawler` é criada com `MaxUfsParalelas = 5`

#### Scenario: Atualização via PATCH
- **WHEN** admin envia `PATCH /api/v1/crawler/configuracao` com `{ "maxUfsParalelas": 10 }`
- **THEN** o campo é atualizado para 10
- **AND** a próxima execução do crawler usa 10 UFs paralelas

#### Scenario: Validação de limites
- **WHEN** admin envia `PATCH /api/v1/crawler/configuracao` com `{ "maxUfsParalelas": 0 }`
- **THEN** a API retorna HTTP 400 com erro de validação
- **WHEN** admin envia `PATCH /api/v1/crawler/configuracao` com `{ "maxUfsParalelas": 28 }`
- **THEN** a API retorna HTTP 400 com erro de validação

---

### Requirement: Acumulação thread-safe de municípios ativos
Os resultados da Fase 1 (municípios ativos por UF) SHALL ser acumulados em estrutura thread-safe (`ConcurrentBag<Municipio>`). A lista final SHALL ser convertida para `List<Municipio>` antes de passar para a Fase 2.

#### Scenario: Resultados de múltiplas UFs acumulados corretamente
- **WHEN** 5 UFs paralelas encontram respectivamente 10, 25, 3, 50, 8 municípios ativos
- **THEN** a lista final contém exatamente 96 municípios
- **AND** nenhum município é perdido ou duplicado
