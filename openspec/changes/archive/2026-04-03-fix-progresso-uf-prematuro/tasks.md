## 1. Domínio — ProgressoUf e ExecucaoCrawler

- [x] 1.1 Adicionar propriedade `MunicipiosAtivos` (int) à classe `ProgressoUf`
- [x] 1.2 Adicionar suporte aos status "Falha" e "Interrompido" no `ProgressoUf`
- [x] 1.3 Alterar `FinalizarProcessamentoUf` para receber `municipiosEncontrados` e `municipiosAtivos` separados
- [x] 1.4 Criar método `InterromperProcessamentoUf(string uf)` em `ExecucaoCrawler` para marcar UF como "Interrompido"
- [x] 1.5 Criar método `FalharProcessamentoUf(string uf, int municipiosEncontrados)` em `ExecucaoCrawler` para marcar UF como "Falha"

## 2. CrawlerService — Reestruturação do FaseConvenioAsync

- [x] 2.1 Reestruturar `FaseConvenioAsync` para unificar o loop por UF: ler municípios do banco e verificar convênio dentro do mesmo loop por UF
- [x] 2.2 Chamar `FinalizarProcessamentoUf` com contagens reais (encontrados + ativos) após as chamadas de convênio da UF
- [x] 2.3 Chamar `InterromperProcessamentoUf` quando CertificateProtection.ShouldHalt ou BudgetExhausted interrompe durante o processamento de uma UF
- [x] 2.4 Chamar `FalharProcessamentoUf` quando todas as chamadas de convênio de uma UF falham com exceção
- [x] 2.5 Manter UFs não processadas como "Pendente" quando o loop é interrompido antes de chegar a elas

## 3. Frontend — Cards de progresso por UF

- [x] 3.1 Adicionar campo `municipiosAtivos` ao modelo TypeScript `ProgressoUf` em `crawler.models.ts`
- [x] 3.2 Atualizar template `crawler-status.component.html` para exibir `municipiosAtivos` nos cards de UF
- [x] 3.3 Adicionar tratamento visual para os novos status "Falha" e "Interrompido" nos cards de UF

## 4. Testes unitários

- [x] 4.1 Teste: `Dado_TodasChamadasConvenioFalham_ProgressoUfDeveSerFalha` — quando todas as chamadas GetConvenioAsync falham com HttpRequestException, o ProgressoUf da UF deve ter status "Falha" e MunicipiosAtivos = 0
- [x] 4.2 Teste: `Dado_CertificateProtectionInterrompe_UfAtualDeveSerInterrompido` — quando CertificateProtection.ShouldHalt é true durante o processamento de uma UF, a UF atual deve ter status "Interrompido" e UFs restantes devem ficar "Pendente"
- [x] 4.3 Teste: `Dado_ZeroMunicipiosAtivos_ProgressoUfDeveRefletirContagens` — quando nenhum município da UF tem convênio ativo, MunicipiosEncontrados deve refletir o banco e MunicipiosAtivos deve ser 0
- [x] 4.4 Teste: `Dado_ConvenioAtivo_ProgressoUfDeveSerConcluido` — quando há municípios ativos, status deve ser "Concluido" e MunicipiosAtivos > 0
- [x] 4.5 Teste: `Dado_FalhasParciais_ProgressoUfDeveRefletirAtivos` — quando parte dos municípios falham e parte têm convênio ativo, MunicipiosAtivos reflete apenas os ativos
- [x] 4.6 Teste: `Dado_InterrupcaoNoMeio_UfsNaoIniciadasDevemFicarPendente` — quando CertProtection interrompe no meio, UFs posteriores devem permanecer "Pendente"
- [x] 4.7 Testes de `ExecucaoCrawler`: testar `InterromperProcessamentoUf` e `FalharProcessamentoUf` na entidade
