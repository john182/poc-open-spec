## Context

A tela de status do crawler exibe progresso por UF na seção "Progresso por UF", mas o status "Concluído" de cada UF refere-se apenas à Fase 1 (descoberta de convênios). O crawler possui 3 fases sequenciais:

1. **Fase 1 — DescobertaConvenios**: Verifica quais municípios têm convênio ativo com o ambiente nacional NFS-e. Processa UFs em paralelo (`Parallel.ForEachAsync`).
2. **Fase 2 — Sondagem**: Testa municípios ativos com códigos de sondagem para confirmar se retornam dados de alíquota. Sequencial.
3. **Fase 3 — ProcessamentoFila**: Processa a fila completa de município × serviço para coletar alíquotas. Paralelo com semáforo.

Atualmente, o frontend não sabe em qual fase o crawler está. O campo `Status = EmAndamento` cobre as 3 fases sem distinção. O progresso por UF (com status `Concluido`) confunde o usuário porque aparenta que o trabalho da UF está completo.

**Arquivos impactados:**
- Backend: `ExecucaoCrawler.cs`, `CrawlerService.cs`, `StatusCrawlerResponse.cs`, `CrawlerController.cs`, `CrawlerMongoMappings.cs`
- Frontend: `crawler.models.ts`, `crawler-status.component.ts`, `crawler-status.component.html`
- Testes: `ExecucaoCrawlerTests.cs`, `CrawlerServiceTests.cs`, `CrawlerControllerTests.cs`

## Goals / Non-Goals

**Goals:**
- Permitir que o frontend exiba a fase atual do crawler (DescobertaConvenios, Sondagem, ProcessamentoFila)
- Contextualizar o status "Concluído" da UF para que o usuário entenda que se refere à fase de convênios
- Persistir a fase atual no MongoDB para que o polling do frontend capture transições

**Non-Goals:**
- Não adicionar progresso granular dentro de cada fase (ex: barra de progresso por UF na fase 3)
- Não alterar a lógica de execução das fases
- Não adicionar novas fases ao crawler

## Decisions

### D1: Enum `FaseCrawler` na entidade `ExecucaoCrawler`

**Decisão**: Adicionar enum `FaseCrawler` com valores `DescobertaConvenios`, `Sondagem`, `ProcessamentoFila`, `Concluido` e propriedade `FaseAtual` na entidade.

**Alternativas consideradas:**
- String livre: rejeitada — sem type-safety, risco de inconsistência
- Campo apenas no response (sem persistência): rejeitada — polling do frontend lê do MongoDB

**Justificativa**: Enum garante valores válidos e o mapeamento MongoDB/response é direto. A persistência no MongoDB permite que o polling capture a fase correta.

### D2: Transição de fase via método `AvancarFase(FaseCrawler)` na entidade

**Decisão**: Adicionar método `AvancarFase(FaseCrawler fase)` na entidade que atualiza `FaseAtual`. O `CrawlerService` chama esse método antes de cada fase e persiste com `UpdateAsync`.

**Justificativa**: Mantém a lógica de estado na entidade (DDD). O service já faz `UpdateAsync` entre fases, basta adicionar a chamada ao método antes.

### D3: Renomear label do status da UF no frontend

**Decisão**: No progresso por UF, mapear o status `Concluido` para o label `Convênios verificados` quando a fase atual do crawler for `DescobertaConvenios` ou posterior. O título da seção muda de "Progresso por UF" para "Fase 1 — Descoberta de Convênios".

**Alternativa considerada:**
- Manter "Concluído" e adicionar tooltip explicativo: rejeitada — não resolve o problema visual imediato

**Justificativa**: O label contextualizado elimina a ambiguidade sem exigir tooltip ou mudança de layout.

### D4: Indicador de fase no frontend

**Decisão**: Adicionar na seção "Última execução" um card/tag com a fase atual (ex: "Fase: Descoberta de Convênios" com ícone de spinner quando `EmAndamento`). Usar um stepper visual simples com 3 etapas.

**Justificativa**: O stepper dá visão completa do progresso geral. O usuário entende que "Concluído" na UF + "Fase 1" = ainda tem trabalho pela frente.

### D5: Mapeamento MongoDB

**Decisão**: Mapear `FaseAtual` como string no MongoDB (`.Representation(BsonType.String)`) usando o mesmo padrão já usado para `StatusExecucao` e `StatusProgressoUf`.

**Justificativa**: Consistência com mapeamentos existentes.

## Risks / Trade-offs

- **[Risco] Persist adicional entre fases**: O `UpdateAsync` já é chamado entre fases. Agora apenas adicionamos `AvancarFase()` antes. Impacto mínimo — 3 writes extras por execução.
  → Mitigação: Impacto negligível dado que uma execução faz milhares de requests à API.

- **[Trade-off] Label contextualizado vs status real**: Mostrar "Convênios verificados" em vez de "Concluído" pode confundir usuários que já se acostumaram com o label antigo.
  → Mitigação: O label é mais preciso e autoexplicativo. A transição é imediata.

- **[Risco] Frontend modelo desatualizado**: Se o frontend carregar antes do backend atualizado, o campo `faseAtual` será `undefined`.
  → Mitigação: Frontend deve tratar `faseAtual` como opcional com fallback.
