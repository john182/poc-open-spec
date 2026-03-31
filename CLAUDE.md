# CLAUDE.md

## Global Project Rule
Neste projeto, toda implementação deve respeitar:
- especificação antes de implementação relevante
- micro PRs apontando para uma PR principal de release
- paralelização apenas com baixo risco de colisão
- frontend começando obrigatoriamente por template + design system + design tokens + páginas base
- worker/crawler tratado como parte central da solução
- documentação viva e rastreável
- testes proporcionais ao risco
- separação clara entre frontend, backend, worker e e2e

## Objetivo deste repositório
Este repositório faz parte de uma solução full stack para consulta e consolidação de alíquotas municipais da API NFS-e, com foco em:
- backend API documentada
- worker/crawler para coleta e atualização de dados
- frontend Angular com experiência de navegação por mapa/estado/município
- testes E2E com Cypress
- arquitetura preparada para pequenas entregas revisáveis
- documentação forte para evolução por agentes

---

## Princípios obrigatórios

### 1. Sempre trabalhar orientado por especificação
Antes de implementar qualquer feature relevante:
- gerar ou consultar proposta
- registrar decisões arquiteturais
- registrar escopo, premissas, riscos e critérios de aceite
- quebrar o trabalho em PBIs e tasks pequenas

### 2. Pequenos PRs sempre
Este projeto adota a estratégia de:
- 1 PR principal por feature ou release branch
- vários micro PRs pequenos apontando para a PR principal
- cada micro PR deve ser revisável isoladamente
- evitar PRs grandes e difusos

### 3. Paralelização com baixo conflito
Quando possível:
- separar trabalho por trilha
- usar worktree por feature/trilha
- não permitir edição concorrente no mesmo arquivo
- só paralelizar quando a colisão for baixa
- definir checkpoints de integração entre frontend, backend, worker e e2e

### 4. Documentação como contexto vivo
Toda mudança relevante deve atualizar, quando fizer sentido:
- documentação de produto
- documentação técnica
- contratos de API
- estratégia do worker
- estratégia de testes
- decisões arquiteturais
- backlog/tarefas quando a execução ainda estiver em curso

### 5. Clareza acima de excesso de abstração
Preferir:
- nomes semânticos
- componentes com responsabilidade clara
- baixo acoplamento
- alta coesão
- arquitetura limpa sem superengenharia

Evitar:
- helpers genéricos sem contexto
- services com responsabilidade ampla demais
- abstrações prematuras
- duplicação desnecessária
- lógica espalhada sem dono claro

---

## Regras de arquitetura

### Backend
- manter separação clara entre domínio, aplicação, infraestrutura e API
- contratos explícitos
- DTOs e responses claros
- validações centralizadas
- logs estruturados
- tratamento consistente de erros
- OpenAPI/Swagger bem documentado
- versionamento de API quando fizer sentido
- endpoints pensados para leitura rápida pelo frontend

### Worker / Crawler
- tratar o worker como parte central da solução
- não disparar chamadas externas sem controle
- controlar concorrência
- usar timeout e retry com critério
- registrar status de execução
- permitir retomada
- lidar com falhas parciais
- justificar estratégia de atualização, cache e materialização local
- preferir persistência de resultados consolidados quando isso reduzir acoplamento com API externa

### Frontend Angular
- a primeira trilha do frontend é obrigatoriamente a fundação visual e estrutural
- usar template de referência apenas como base controlada
- não copiar o template inteiro cegamente
- definir primeiro:
  - layout base
  - design system
  - design tokens
  - componentes reutilizáveis
  - helpers de formulário
  - páginas base de autenticação e erro
- só depois iniciar a feature de consulta com mapa/estado/município/listagem/filtros
- priorizar acessibilidade, consistência visual e testabilidade

### E2E com Cypress
- manter projeto E2E desacoplado e organizado
- testar fluxos críticos
- usar seletores estáveis
- evitar acoplamento frágil com detalhes visuais irrelevantes
- cobrir login, cadastro, acesso negado, 404, navegação, mapa, seleção de estado/município, filtros e listagem

---

## Estratégia de frontend obrigatória

### Ordem obrigatória de execução no frontend
1. analisar o template de referência
2. decidir o que será reutilizado, adaptado ou descartado
3. construir layout/template base
4. definir design system
5. definir design tokens
6. criar componentes base reutilizáveis
7. criar helpers de formulário
8. implementar páginas base:
   - sign in
   - sign up
   - acesso negado
   - 404
9. só então implementar a feature principal de consulta

### O frontend deve ter
- shell/layout autenticado
- navegação clara
- estados de loading, vazio, erro e retry
- filtros na listagem
- separação entre pages, layouts, shared UI e componentes de domínio
- boa organização para evolução futura

---

## Estratégia de testes

### Backend
- testes unitários para regras e serviços centrais
- testes de integração para endpoints, persistência e fluxos relevantes
- cobrir cenários de falha e sucesso

### Frontend
- testes unitários para componentes, helpers, guards, services e páginas importantes
- foco em comportamento, não implementação interna

### E2E
Cobrir no mínimo:
- cadastro
- login
- acesso negado
- 404
- navegação básica
- seleção no mapa
- seleção de estado
- seleção de município
- listagem de serviços/alíquotas
- filtros
- cenários de erro relevantes

---

## Convenções de implementação

### Nomeação
- usar nomes específicos e semânticos
- evitar nomes genéricos demais como Helper, Manager, Processor sem contexto
- service só quando realmente fizer sentido
- componentes e classes devem transmitir a responsabilidade

### Código
- preferir funções e métodos pequenos
- reduzir parâmetros excessivos
- evitar duplicação
- usar objetos literais e mapeamentos quando isso simplificar condicionais
- não jogar regra de negócio em controllers/pages/components quando ela pertence a outra camada

### Angular
- evitar lógica desnecessária no template, mas aproveitar recursos nativos do Angular
- preferir soluções idiomáticas do framework
- não centralizar tudo em componentes gigantes
- manter componentes com responsabilidade clara
- formularios e UI base devem ser padronizados

### Documentação e contratos
- sempre que alterar API, atualizar contrato
- sempre que alterar fluxo principal, atualizar doc técnica correspondente
- diagramas devem refletir o desenho real, não um desenho idealizado e desatualizado

---

## Fluxo esperado para os agentes

### Antes de implementar
- entender escopo
- consultar proposta/design/tasks quando existirem
- identificar dependências
- identificar risco de colisão
- propor fatiamento pequeno quando a entrega estiver grande

### Durante a implementação
- manter foco na task atual
- não expandir escopo sem registrar
- atualizar documentação quando necessário
- manter rastreabilidade

### Antes de concluir
- validar critérios de aceite
- validar impacto em testes
- revisar se houve quebra de arquitetura
- revisar se o PR ficou pequeno e revisável

---

## Estratégia de PRs
Para cada feature relevante:
- criar uma PR principal de release/integration
- criar micro PRs ligados a essa trilha
- cada micro PR deve focar em uma parte clara:
  - fundação visual
  - autenticação frontend
  - contratos backend
  - worker base
  - listagem
  - filtros
  - e2e etc.
- preferir integração gradual ao invés de megabranch sem revisão

---

## O que evitar
- começar pela tela final sem antes preparar a base do frontend
- chamar a API externa de forma massiva sem estratégia de controle
- criar PR gigante misturando frontend, backend, worker e e2e sem separação
- deixar documentação para o final
- abstrair demais no início
- copiar template sem critério
- implementar sem critérios de aceite claros

---

## Critério de qualidade esperado
Toda entrega deve buscar:
- clareza de arquitetura
- consistência visual
- baixo acoplamento
- rastreabilidade
- cobertura de testes compatível com a criticidade
- documentação útil para evolução por novos agentes