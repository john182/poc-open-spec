## Why

A tela de status do crawler exibe "Concluído" no progresso por UF quando a Fase 1 (descoberta de convênios) termina, dando a impressão ao usuário de que o processamento daquela UF está 100% finalizado. Na realidade, ainda faltam a Fase 2 (sondagem) e a Fase 3 (processamento da fila de alíquotas). O usuário não tem visibilidade de qual fase está em execução, o que torna a tela confusa e pouco informativa.

## What Changes

- Adicionar campo `FaseAtual` na entidade `ExecucaoCrawler` para rastrear em qual fase o crawler está (DescobertaConvenios, Sondagem, ProcessamentoFila, Concluido)
- Expor `FaseAtual` no `StatusCrawlerResponse` e no endpoint `GET /api/v1/crawler/status`
- Atualizar a `ExecucaoCrawler` no MongoDB ao transitar entre fases para que o polling capture o estado correto
- Renomear o label do status da UF na seção "Progresso por UF" do frontend para contextualizar que se refere à fase de convênios (ex: "Convênios verificados" em vez de "Concluído")
- Exibir a fase atual na seção "Última execução" do frontend com indicador visual

## Capabilities

### New Capabilities
- `crawler-fase-atual`: Rastreamento e exibição da fase atual de execução do crawler (DescobertaConvenios, Sondagem, ProcessamentoFila)

### Modified Capabilities
- `data-crawler`: Adicionar requisito de rastreamento de fase na entidade de execução e no endpoint de status

## Impact

- **Backend**: `ExecucaoCrawler` (nova propriedade + enum), `CrawlerService` (setar fase ao entrar em cada fase), `StatusCrawlerResponse` (novo campo), `CrawlerController` (mapear campo), `CrawlerMongoMappings` (mapear novo campo)
- **Frontend**: `crawler.models.ts` (novo campo no modelo), `crawler-status.component.html` (exibir fase atual, renomear labels de UF), `crawler-status.component.ts` (computed/helper para fase)
- **Testes**: Testes unitários do backend para nova propriedade e transições de fase, testes do frontend para exibição
- **API**: Campo adicional `faseAtual` no response de `GET /api/v1/crawler/status` — **não é breaking** (campo adicionado, não removido)
