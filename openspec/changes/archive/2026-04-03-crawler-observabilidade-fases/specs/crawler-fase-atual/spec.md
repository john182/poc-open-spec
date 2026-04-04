## ADDED Requirements

### Requirement: Fase atual da execução do crawler
A entidade `ExecucaoCrawler` SHALL rastrear a fase atual do processamento através de uma propriedade `FaseAtual` do tipo enum `FaseCrawler`. Os valores possíveis são: `DescobertaConvenios`, `Sondagem`, `ProcessamentoFila`, `Concluido`. O valor inicial ao criar uma execução SHALL ser `DescobertaConvenios`.

#### Scenario: Fase inicial ao criar execução
- **WHEN** uma nova `ExecucaoCrawler` é criada via `Create(TipoExecucao)`
- **THEN** a propriedade `FaseAtual` SHALL ser `DescobertaConvenios`

#### Scenario: Transição para fase de sondagem
- **WHEN** a Fase 1 (descoberta de convênios) termina e a Fase 2 inicia
- **THEN** `FaseAtual` SHALL ser atualizada para `Sondagem`
- **AND** a execução SHALL ser persistida no MongoDB

#### Scenario: Transição para fase de processamento da fila
- **WHEN** a Fase 2 (sondagem) termina e a Fase 3 inicia
- **THEN** `FaseAtual` SHALL ser atualizada para `ProcessamentoFila`
- **AND** a execução SHALL ser persistida no MongoDB

#### Scenario: Transição para concluído
- **WHEN** a execução é finalizada (todas as fases terminam)
- **THEN** `FaseAtual` SHALL ser atualizada para `Concluido`

---

### Requirement: Exposição da fase atual no endpoint de status
O endpoint `GET /api/v1/crawler/status` SHALL incluir o campo `faseAtual` no response, representando a fase atual da execução como string.

#### Scenario: Status retorna fase atual durante execução
- **WHEN** o crawler está na Fase 2 (sondagem) e o frontend chama `GET /api/v1/crawler/status`
- **THEN** o response SHALL incluir `"faseAtual": "Sondagem"`

#### Scenario: Status retorna fase concluída após execução
- **WHEN** o crawler finalizou a execução e o frontend chama `GET /api/v1/crawler/status`
- **THEN** o response SHALL incluir `"faseAtual": "Concluido"`

---

### Requirement: Exibição da fase atual no frontend
O frontend SHALL exibir a fase atual da execução na seção "Última execução" com indicador visual (stepper ou tag). O frontend SHALL tratar `faseAtual` como campo opcional com fallback para não exibir quando ausente.

#### Scenario: Exibição durante execução em andamento
- **WHEN** o status do crawler é `EmAndamento` e `faseAtual` é `Sondagem`
- **THEN** o frontend SHALL exibir um indicador visual mostrando que a fase atual é "Sondagem" com as 3 etapas visíveis

#### Scenario: Campo faseAtual ausente (backend antigo)
- **WHEN** o response do status não contém o campo `faseAtual`
- **THEN** o frontend SHALL não exibir o indicador de fase e manter o comportamento atual

---

### Requirement: Label contextualizado do status da UF
O frontend SHALL exibir o status da UF na seção "Progresso por UF" com label contextualizado. Quando o status de uma UF for `Concluido`, o label exibido SHALL ser `Convênios verificados` em vez de `Concluído`. O título da seção SHALL incluir referência à fase (ex: "Fase 1 — Descoberta de Convênios").

#### Scenario: UF concluída exibe label contextualizado
- **WHEN** uma UF tem status `Concluido` no progresso
- **THEN** o frontend SHALL exibir o label `Convênios verificados` com severidade `success`

#### Scenario: UF em andamento mantém label original
- **WHEN** uma UF tem status `EmAndamento` no progresso
- **THEN** o frontend SHALL exibir o label `Em andamento` com severidade `info`

#### Scenario: UF com falha mantém label original
- **WHEN** uma UF tem status `Falha` no progresso
- **THEN** o frontend SHALL exibir o label `Falha` com severidade `danger`

#### Scenario: UF interrompida mantém label original
- **WHEN** uma UF tem status `Interrompido` no progresso
- **THEN** o frontend SHALL exibir o label `Interrompido` com severidade `warn`
