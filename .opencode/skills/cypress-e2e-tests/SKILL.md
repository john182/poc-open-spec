---
name: cypress-e2e-tests
description: Escreve testes end-to-end com Cypress seguindo estritamente o padrão oficial do projeto
---

# Objetivo

Gerar testes end-to-end com Cypress seguindo estritamente o padrão oficial do projeto, com foco em:
- comportamento real do usuário
- validação de fluxos completos
- clareza
- baixo boilerplate
- legibilidade
- cobertura de regras de negócio visíveis na interface
- reutilização correta de commands, fixtures, builders e helpers
- estabilidade
- evitar overengineering

# Quando usar esta skill

Use esta skill quando:
- for necessário criar testes end-to-end com Cypress
- for necessário validar fluxos completos entre tela, navegação, backend e feedback visual
- houver formulários com regras condicionais
- houver listagens, filtros, ações em tabela, modais, toasts e navegação entre páginas
- houver necessidade de validar permissões, bloqueios, estados visuais e fluxos críticos do usuário
- houver integração entre frontend e backend que precise ser validada do ponto de vista do usuário
- houver necessidade de validar regressão funcional em jornadas principais do sistema

# Regras obrigatórias

Ao gerar testes, seguir obrigatoriamente estas regras:

- Usar Cypress
- Nomear os testes obrigatoriamente no padrão:

`Given_<context>_Should_<expected_behavior>`

- Organizar os testes com comentários explícitos de:
  - Arrange
  - Act
  - Assert
- Cada teste deve validar um único comportamento principal
- Todo teste relevante deve validar:
  1. comportamento esperado do usuário
  2. efeito visível na interface
  3. integração funcional do fluxo testado
- O teste deve refletir a jornada real do usuário sempre que isso não tornar o cenário desnecessariamente lento ou frágil
- Preferir fluxos estáveis, objetivos e legíveis
- Não testar detalhe interno de implementação do Angular
- Não validar estado interno de componente, service, facade ou store em teste end-to-end
- Validar somente o que é observável pelo usuário, pela navegação, pela resposta visual e pelos efeitos funcionais do sistema

# Regra mandatória de comportamento observável

Para testes end-to-end com Cypress:

- Não basta validar apenas chamada de API ou existência de elemento isolado
- Cada teste deve validar o efeito funcional completo do cenário
- Não considerar suficiente um teste genérico como `should load page`
- A validação do comportamento observável faz parte da definição de pronto do teste
- Não considerar o teste concluído enquanto o cenário não validar o resultado funcional esperado na interface ou no fluxo do usuário

# Regras obrigatórias de E2E

- Os testes devem representar ações reais do usuário:
  - acessar página
  - preencher campos
  - clicar em botões
  - navegar entre páginas
  - aplicar filtros
  - confirmar ações
  - visualizar mensagens
  - validar bloqueios e permissões
- Validar presença, ausência, habilitação, desabilitação, visibilidade e conteúdo quando fizer sentido
- Validar mensagens de erro, sucesso, alerta e confirmação quando fizer sentido
- Validar navegação e redirecionamento quando fizer sentido
- Validar impacto funcional da ação executada
- Quando houver tabela, filtro ou listagem, validar comportamento do filtro e reflexo real dos dados apresentados
- Quando houver formulário, validar preenchimento, validação, submissão e resultado
- Quando houver modal, validar abertura, conteúdo principal, ação executada e fechamento quando fizer sentido
- Quando houver permissões, validar o comportamento esperado para perfil autorizado e não autorizado quando aplicável
- Quando houver integração com backend, usar interceptação de forma pragmática para estabilizar o teste sem perder a essência end-to-end do fluxo
- Não depender de temporizações fixas como `cy.wait(2000)` sem necessidade real
- Preferir espera por comportamento observável, requisição interceptada, mudança de rota, mudança de estado visual ou conclusão de ação

# Regra obrigatória de seletor

- Priorizar seletores estáveis e intencionais
- Não usar seletores frágeis baseados em estrutura visual, CSS acidental ou texto excessivamente volátil quando houver alternativa melhor
- Repetir o padrão já adotado no projeto para seleção de elementos
- Se o projeto usar `data-cy`, `data-testid` ou atributo equivalente, manter esse padrão
- Se o projeto usar seletores acessíveis por `role`, `label` ou texto estável, manter esse padrão quando apropriado
- Não introduzir um padrão novo de seleção sem necessidade
- Quando for necessário recomendar melhoria no código para suportar teste estável, preferir atributo semântico e previsível

# Regra obrigatória de organização do arquivo de teste

Todo arquivo de teste deve seguir esta ordem:

1. constantes
2. mocks e fixtures reutilizáveis
3. funções auxiliares reutilizáveis
4. hooks como `beforeEach`, quando aplicável
5. blocos `describe`
6. blocos `it`
7. helpers privados no final, se fizer sentido no padrão adotado

Regras obrigatórias:
- Não espalhar helpers no meio dos cenários sem necessidade
- Helpers reutilizáveis devem ficar organizados e com nomes orientados ao domínio
- Se o projeto já tiver `commands` customizados, reutilizar antes de criar novos
- Se o projeto já tiver padrão de `page object`, `app actions` ou helpers de domínio, manter esse padrão
- Não criar abstração desnecessária para fluxos simples
- O teste deve continuar legível mesmo com reutilização

# Estratégia de criação de massa de teste

Seguir esta ordem de preferência:

1. massa mínima inline, quando o cenário continuar claro
2. fixture, quando houver resposta base reutilizável
3. builder/factory, quando houver muitas variações sobre o mesmo contexto
4. comandos customizados, quando houver fluxo recorrente de autenticação, navegação ou preparação do cenário
5. geração artificial de dados, apenas quando reduzir boilerplate sem prejudicar clareza

# Quando usar fixture

Usar fixture quando:
- existir resposta base reutilizável
- existir payload recorrente de listagem, detalhe, formulário ou autenticação
- houver cenários com variações previsíveis sobre um conjunto conhecido de dados
- a intenção for centralizar massa base de teste

A fixture deve expor nomes claros como:
- `authorizedInvoices.json`
- `emptyInvoiceList.json`
- `salesReportSuccess.json`
- `blockedUserProfile.json`

# Quando usar builder ou factory

Usar builder ou factory quando:
- houver muitas variações sobre o mesmo cenário base
- houver diferentes combinações de permissões, estados ou respostas
- houver cenários como:
  - usuário autorizado vs bloqueado
  - lista vazia vs lista preenchida
  - retorno com erro vs retorno com sucesso
  - filtro simples vs filtro completo
  - formulário válido vs inválido
  - ação permitida vs negada
  - modal de confirmação aceito vs cancelado

# Regras para builder ou factory

O builder ou factory deve:
- criar cenário válido por padrão
- permitir sobrescrita incremental
- retornar estrutura clara e previsível
- expor métodos orientados ao domínio
- esconder complexidade de inicialização
- evitar nomes genéricos como `setX`, `setY`

Exemplos desejados:
- `withAuthorizedUser()`
- `withBlockedUser()`
- `withEmptyResults()`
- `withCompletedFilter()`
- `withValidationError()`
- `withSuccessfulSave()`
- `withServerFailure()`

# Estratégia de implementação para Cypress

Ao gerar testes:

- reutilizar `commands` existentes do projeto antes de criar novos
- criar `commands` customizados apenas quando houver ganho claro de legibilidade e repetição real
- reutilizar fixtures e helpers já existentes antes de criar versões paralelas
- usar `cy.intercept` com pragmatismo para:
  - estabilizar cenários
  - simular respostas previsíveis
  - validar integração esperada
  - reduzir fragilidade externa desnecessária
- evitar interceptar tudo sem necessidade
- manter equilíbrio entre confiabilidade e aderência ao fluxo real
- preferir assertions claras e orientadas ao comportamento
- preferir encadear o mínimo necessário para manter legibilidade
- evitar comandos excessivamente genéricos que escondam a intenção do teste

# Estratégia para cenários

Ao gerar testes, priorizar cobertura destes grupos:

## 1. Cenário mínimo válido
Validar o fluxo mínimo necessário para o usuário concluir a ação principal com sucesso.

## 2. Cenário completo
Validar um fluxo completo com preenchimento relevante, navegação, resposta visual e conclusão funcional.

## 3. Cenários condicionais
Validar regras como:
- usuário autorizado vs sem permissão
- lista vazia vs lista com registros
- carregamento bem-sucedido vs erro
- botão habilitado vs desabilitado
- modal aberto vs cancelado
- formulário válido vs inválido
- filtro aplicado vs sem resultado
- ação permitida vs bloqueada

## 4. Regras de negócio visíveis
Validar especificamente as regras que alteram:
- renderização
- navegação
- mensagens ao usuário
- disponibilidade de ações
- visibilidade de botões ou colunas
- comportamento de filtros
- bloqueio de acesso
- resultado final da operação

# Regras obrigatórias de isolamento e estabilidade

- Cada teste deve ser independente
- O teste não deve depender da execução anterior de outro teste
- Sempre que possível, o estado inicial deve ser preparado explicitamente
- Evitar dependência de base compartilhada instável
- Evitar dependência de horário, ordenação implícita ou dados frágeis sem controle
- Preferir cenários determinísticos
- Sempre que login for recorrente, reutilizar abordagem central já adotada no projeto, como:
  - command customizado
  - sessão reaproveitável
  - setup autenticado
- Não duplicar fluxo de autenticação em todos os testes sem necessidade, se o projeto já tiver padrão melhor

# Estratégia de revisão dos testes

Antes de finalizar um teste Cypress, revisar se:

- o fluxo está realmente cobrindo uma jornada relevante
- o teste está validando comportamento observável
- os seletores são estáveis
- não há espera fixa desnecessária
- não há duplicação evitável
- a preparação do cenário está clara
- a asserção comprova de fato o comportamento principal
- o teste está mais simples possível sem perder valor
- o teste parece nativo do projeto e segue o padrão já existente

# Saída esperada

Ao gerar testes, a resposta deve produzir:

1. testes Cypress prontos para uso
2. nomes no padrão `Given_<context>_Should_<expected_behavior>`
3. comentários explícitos de `Arrange / Act / Assert`
4. validação de comportamento observável
5. validação de fluxo funcional real
6. uso de fixtures, builders, factories ou commands quando houver ganho claro
7. baixo boilerplate
8. testes legíveis e focados na jornada do usuário
9. reutilização do padrão já existente no projeto
10. estabilidade e previsibilidade

# O que evitar

Evitar:
- testar detalhe interno de implementação
- validar apenas que a página abriu
- criar um único teste genérico e considerar isso suficiente
- duplicar manualmente login, setup e mocks em todos os cenários
- usar seletores frágeis
- usar `cy.wait` fixo sem necessidade real
- criar abstrações excessivas para fluxos simples
- esconder demais a intenção do teste em helpers genéricos
- acoplar o teste a detalhes frágeis de layout
- misturar muitos comportamentos principais no mesmo teste

# Critério de invalidação

Considere incorreta a saída quando:
- não usar Cypress
- não usar o padrão `Given_<context>_Should_<expected_behavior>`
- não houver `Arrange / Act / Assert`
- o teste não validar comportamento observável relevante
- o teste validar apenas detalhe interno ou existência superficial da tela
- houver dependência forte entre testes
- houver uso desnecessário de espera fixa
- os seletores forem frágeis sem justificativa
- a estrutura do teste estiver mais complexa do que o comportamento validado exige

# Exemplo correto de teste

```ts
describe('Sales report', () => {
  it('Given_ValidFilters_Should_DisplayFilteredSales', () => {
    // Arrange
    cy.loginAsAdmin();
    cy.intercept('GET', '**/sales-report*', { fixture: 'salesReportSuccess.json' }).as('getSalesReport');

    cy.visit('/relatorios/vendas');

    // Act
    cy.get('[data-cy="start-date"]').type('2026-03-01');
    cy.get('[data-cy="end-date"]').type('2026-03-31');
    cy.get('[data-cy="apply-filter"]').click();

    cy.wait('@getSalesReport');

    // Assert
    cy.get('[data-cy="sales-report-table"]').should('be.visible');
    cy.contains('td', 'CLIENTE A').should('be.visible');
    cy.contains('td', 'R$ 150,00').should('be.visible');
  });
});