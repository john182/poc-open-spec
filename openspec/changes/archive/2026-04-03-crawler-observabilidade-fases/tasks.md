## 1. Backend — Enum e Entidade

- [x] 1.1 Criar enum `FaseCrawler` com valores `DescobertaConvenios`, `Sondagem`, `ProcessamentoFila`, `Concluido` em `ExecucaoCrawler.cs`
- [x] 1.2 Adicionar propriedade `FaseAtual` na entidade `ExecucaoCrawler` com valor inicial `DescobertaConvenios` no método `Create()`
- [x] 1.3 Criar método `AvancarFase(FaseCrawler fase)` na entidade
- [x] 1.4 Mapear `FaseAtual` no `CrawlerMongoMappings.cs` como `BsonType.String`

## 2. Backend — CrawlerService (transições de fase)

- [x] 2.1 Chamar `AvancarFase(DescobertaConvenios)` + `UpdateAsync` antes de `FaseConvenioAsync` no `ExecutarAsync`
- [x] 2.2 Chamar `AvancarFase(Sondagem)` + `UpdateAsync` antes de `FaseProbeAsync`
- [x] 2.3 Chamar `AvancarFase(ProcessamentoFila)` + `UpdateAsync` antes de `ProcessarFilaAsync`
- [x] 2.4 Chamar `AvancarFase(Concluido)` dentro do método `Finalizar()` da entidade

## 3. Backend — Response e Controller

- [x] 3.1 Adicionar campo `FaseAtual` (string) no `StatusCrawlerResponse`
- [x] 3.2 Mapear `FaseAtual` no `CrawlerController.MapToResponse()` (enum → string)

## 4. Backend — Testes unitários

- [x] 4.1 Testar que `Create()` define `FaseAtual = DescobertaConvenios`
- [x] 4.2 Testar que `AvancarFase()` transiciona corretamente entre fases
- [x] 4.3 Testar que `Finalizar()` define `FaseAtual = Concluido`
- [x] 4.4 Atualizar testes do `CrawlerService` para verificar chamadas de `AvancarFase` nas transições

## 5. Frontend — Modelo e serviço

- [x] 5.1 Adicionar campo `faseAtual` (string opcional) na interface `StatusCrawler` em `crawler.models.ts`

## 6. Frontend — Componente de status

- [x] 6.1 Adicionar indicador visual de fase atual na seção "Última execução" (stepper ou tags com 3 etapas)
- [x] 6.2 Renomear título da seção "Progresso por UF" para "Fase 1 — Descoberta de Convênios"
- [x] 6.3 Mapear label do status da UF: `Concluido` → `Convênios verificados`, mantendo os demais status com labels originais
- [x] 6.4 Tratar `faseAtual` como opcional — não exibir indicador quando ausente (compatibilidade com backend antigo)
