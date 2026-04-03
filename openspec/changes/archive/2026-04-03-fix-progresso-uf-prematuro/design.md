## Context

O `CrawlerService.FaseConvenioAsync` executa dois loops sequenciais:
1. **Loop por UF**: percorre as 27 UFs, lê municípios do MongoDB local, e chama `FinalizarProcessamentoUf(uf, count)` — marcando a UF como "Concluido" com a contagem de municípios do banco.
2. **Loop por município**: percorre todos os municípios coletados e chama a API externa `GetConvenioAsync` para verificar se são ativos.

O problema é que o Loop 1 já marca todas as UFs como "Concluido" antes do Loop 2 iniciar. Se as chamadas de convênio falham (certificado inválido, CertificateProtection, CircuitBreaker), nenhum município é ativado, mas o `ProgressoUfs` já está salvo como concluído.

Arquivos impactados:
- `CrawlerService.cs` (linhas 214-318 — `FaseConvenioAsync`)
- `ExecucaoCrawler.cs` (entidade de domínio)
- `ProgressoUf` (classe embedded no `ExecucaoCrawler`)
- `crawler-status.component.html` (template do frontend)
- `crawler.models.ts` (modelos TypeScript)

## Goals / Non-Goals

**Goals:**
- O `ProgressoUf` de cada UF deve refletir o resultado real das chamadas de convênio à API externa
- Diferenciar "municípios encontrados no banco" de "municípios confirmados ativos via API"
- UFs cujas chamadas falharam devem ter status "Falha"
- UFs não processadas por interrupção (CertificateProtection) devem manter status "Pendente" ou ter status "Interrompido"
- Testes de regressão devem provar o bug e validar o comportamento correto

**Non-Goals:**
- Não alterar o fluxo das Fases 2 (Probe) e 3 (ProcessarFila)
- Não alterar a lógica de CertificateProtection ou CircuitBreaker em si
- Não mudar a API de execução (`POST /api/v1/crawler/executar`)

## Decisions

### 1. Unificar o loop por UF com as chamadas de convênio

**Decisão**: Reestruturar `FaseConvenioAsync` para agrupar as chamadas de convênio por UF dentro do mesmo loop. Cada UF é iniciada, tem seus municípios lidos do banco, cada município é verificado via convênio, e a UF é finalizada com o resultado real.

**Alternativa considerada**: Manter os dois loops separados e atualizar o `ProgressoUf` após o Loop 2 com base na UF de cada município ativo. Rejeitada porque não capturaria corretamente UFs interrompidas (a informação de "qual UF estava sendo processada quando CertProtection interrompeu" se perderia).

**Estrutura do novo loop**:
```
para cada UF:
  1. IniciarProcessamentoUf(uf)
  2. Ler municípios do banco → todos da UF
  3. Aplicar filtro de capital (se houver)
  4. Para cada município da UF:
     - Verificar CertProtection.ShouldHalt / BudgetExhausted → se sim, marcar UF como "Interrompido" e sair
     - Chamar GetConvenioAsync
     - Se ativo, adicionar à lista
  5. FinalizarProcessamentoUf(uf, encontrados, ativos)
```

### 2. Adicionar campo `MunicipiosAtivos` ao `ProgressoUf`

**Decisão**: Manter `MunicipiosEncontrados` (contagem do banco local) e adicionar `MunicipiosAtivos` (contagem de municípios confirmados via API). Isso permite ao administrador entender "de X encontrados no banco, Y foram confirmados ativos".

**Alternativa considerada**: Substituir `MunicipiosEncontrados` pelo valor real. Rejeitada porque a informação de "quantos existem no banco" é útil para diagnóstico.

### 3. Novos status no `ProgressoUf`

**Decisão**: Adicionar os status "Falha" e "Interrompido":
- **"Falha"**: Todas as chamadas de convênio para a UF falharam com exceção (HttpRequestException)
- **"Interrompido"**: O processamento foi interrompido por CertificateProtection antes de completar a UF
- **"Concluido"**: Todas as chamadas de convênio da UF foram executadas (independentemente de quantos estão ativos)

### 4. Ordenação: capitais primeiro dentro de cada UF

**Decisão**: A priorização de capitais será mantida, mas agora ocorrerá dentro do loop por UF. Capitais de cada UF serão processadas antes dos demais municípios daquela UF, e entre UFs, a ordem alfabética será mantida.

**Trade-off**: Na versão anterior, TODAS as capitais de todas as UFs eram processadas antes de qualquer município interior. Agora, dentro de cada UF as capitais vêm primeiro, mas a UF "AC" completa antes de "AL" começar. Isso é aceitável porque o progresso por UF fica mais preciso.

## Risks / Trade-offs

- **[Mudança de ordem de processamento]** → Capitais eram processadas todas juntas antes; agora são processadas por UF. Mitigação: a priorização de capitais dentro de cada UF é mantida; o comportamento da funcionalidade "Executar capitais primeiro" (`filtroCapital=true`) não é afetado.
- **[Campo novo na response]** → `MunicipiosAtivos` é adicionado ao `ProgressoUf`. Mitigação: campo aditivo, não quebra clientes existentes. O frontend será atualizado para exibir.
- **[Persistência intermediária]** → Antes, o `ProgressoUfs` era salvo uma vez após o Loop 1 completo. Agora, pode ser salvo após cada UF ou ao final de todas as UFs. Decisão: salvar ao final de todas as UFs (mesma frequência atual) para não aumentar writes no MongoDB.
