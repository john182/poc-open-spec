## Why

O crawler marca todas as UFs como "Concluído" com contagem de municípios antes de iniciar as chamadas à API externa de convênio. Quando as chamadas falham silenciosamente (certificado inválido, CertificateProtection, CircuitBreaker), a tela mostra 27/27 UFs concluídas com municípios contabilizados, mas o resumo da execução registra 0 municípios, 0 serviços, 0 processados. Isso gera divergência de informação e impede o administrador de identificar falhas.

## What Changes

- Corrigir o momento em que `FinalizarProcessamentoUf` é chamado: deve ocorrer após a verificação de convênio dos municípios daquela UF, não na leitura do banco local
- Separar a contagem de municípios do banco local (`MunicipiosEncontrados`) da contagem de municípios confirmados via API (`MunicipiosAtivos`) no `ProgressoUf`
- Adicionar status "Falha" e "Interrompido" ao `ProgressoUf` para refletir cenários de erro e interrupção por proteção de certificado
- Quando `CertificateProtection.ShouldHalt` ou `BudgetExhausted` interrompe o processamento, as UFs não verificadas devem permanecer como "Pendente" ou ser marcadas como "Interrompido"
- Adicionar testes de regressão que comprovem o bug e validem o comportamento correto
- Atualizar o frontend para exibir `MunicipiosAtivos` nos cards de progresso por UF

## Capabilities

### New Capabilities

_(nenhuma)_

### Modified Capabilities

- `data-crawler`: O requisito "Execution tracking" precisa ser atualizado para exigir que o progresso por UF reflita o resultado real das chamadas de convênio, não apenas a contagem de municípios no banco local. Novos cenários para UFs com falha e UFs interrompidas por proteção de certificado.

## Impact

- **Backend**: `CrawlerService.FaseConvenioAsync` — reestruturação do loop para rastrear progresso por UF durante as chamadas de convênio. `ExecucaoCrawler`/`ProgressoUf` — novo campo `MunicipiosAtivos`, novos status "Falha" e "Interrompido".
- **Frontend**: `crawler-status.component.html` — exibir `MunicipiosAtivos` nos cards de progresso por UF.
- **Testes**: Novos testes unitários em `CrawlerServiceTests` e `ExecucaoCrawlerTests` cobrindo cenários de falha silenciosa e interrupção por proteção de certificado.
- **API**: O campo `municipiosAtivos` será adicionado ao `ProgressoUf` na response de `GET /api/v1/crawler/status`. Campo aditivo, sem breaking change.
