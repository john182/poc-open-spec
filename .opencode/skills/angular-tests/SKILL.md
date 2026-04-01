---
name: angular-tests
description: Escreve testes Angular/TypeScript com validação funcional, estrutural e comportamental seguindo o padrão oficial do projeto
---

# Objetivo

Gerar testes para código Angular/TypeScript seguindo estritamente o padrão oficial do projeto, com foco em:
- comportamento funcional correto
- clareza
- baixo boilerplate
- legibilidade
- cobertura das regras de negócio da tela, componente, service, facade, store, directive ou pipe
- reutilização correta de fixtures, builders, factories e helpers
- evitar overengineering

# Quando usar esta skill

Use esta skill quando:
- for necessário criar testes para components Angular
- for necessário criar testes para services, facades, stores, guards, resolvers, directives ou pipes
- houver formulários com regras condicionais
- houver fluxo de carregamento, erro, sucesso ou estado vazio
- houver transformação de dados entre API e tela
- houver necessidade de validar comportamento de template, estado e integração ao mesmo tempo

# Regras obrigatórias

Ao gerar testes, seguir obrigatoriamente estas regras:

- Usar o framework de teste já adotado no projeto
- Se o projeto usar Jest, manter Jest
- Se o projeto usar Jasmine/Karma, manter Jasmine/Karma
- Usar Testing Library quando isso já fizer parte do padrão do projeto ou quando fizer sentido para testar comportamento do usuário
- Não usar `Assert.*`, exceto se o projeto exigir explicitamente ou o usuário pedir explicitamente
- Nomear os testes obrigatoriamente no padrão:

`Given_<context>_Should_<expected_behavior>`

- Organizar todos os testes com comentários explícitos de:
  - Arrange
  - Act
  - Assert
- Cada teste deve validar um único comportamento principal
- Todo teste relevante deve validar:
  1. comportamento esperado
  2. efeito observável relevante
  3. estado, renderização, evento, transformação ou integração afetada pelo cenário

# Regra mandatória de comportamento observável

Para testes Angular/TypeScript:

- Não basta validar apenas método interno da classe quando o comportamento relevante é visível por template, estado, saída de evento ou integração
- Cada teste deve validar o efeito real esperado do cenário
- Não considerar suficiente um teste genérico isolado como `should create`
- A validação do comportamento observável faz parte da definição de pronto do teste
- Não considerar o teste concluído enquanto o cenário testado não validar o efeito funcional relevante no próprio contexto testado

# Regras obrigatórias de teste Angular

- Não validar apenas detalhes internos quando o comportamento pode ser validado pela interface pública do componente, service, facade, store, directive ou pipe
- Não testar implementação privada diretamente
- Validar presença, ausência, renderização condicional, estados visuais, emissão de eventos, chamadas de dependências e transformação de dados quando fizer sentido
- Quando houver componente com template, validar o comportamento do template junto ao cenário funcional relevante
- Quando houver formulário, validar estrutura, preenchimento, validação, habilitação, desabilitação, mensagens de erro e submissão quando aplicável
- Quando houver integração com serviço ou backend, validar tipagem, chamada, tratamento de erro e propagação de estado quando aplicável
- A lógica de setup de mocks, spies e factories deve ser reutilizável e não copiada manualmente em cada teste

# Regra obrigatória de organização da classe de teste

Toda classe ou arquivo de teste deve seguir esta ordem:

1. constantes
2. mocks reutilizáveis
3. factories/fixtures/builders
4. variáveis do contexto do teste
5. `beforeEach` quando aplicável
6. testes `it`, `[Fact]`, `test` ou equivalente do framework adotado
7. métodos privados/helpers

Regras obrigatórias:
- Nenhum método privado deve aparecer antes dos testes
- Todo helper privado deve ficar no final da classe ou arquivo
- Isso inclui helpers de renderização, criação de componente, criação de formulário, mock de resposta, query de elementos e métodos utilitários privados
- Se houver apenas um helper privado, ainda assim ele deve ficar no final

# Estratégia de criação de massa de teste

Seguir esta ordem de preferência:

1. montagem simples inline, quando o objeto for pequeno e o teste continuar claro
2. Fixture/Factory, quando houver cenário base reutilizável
3. Builder, quando houver muitas variações sobre a mesma estrutura
4. geração artificial de dados, apenas quando reduzir boilerplate sem prejudicar clareza

# Quando usar Fixture ou Factory

Usar Fixture/Factory quando:
- existir uma resposta base válida reutilizada por vários testes
- existir um estado mínimo válido recorrente
- houver necessidade de um cenário completo conhecido
- a intenção for centralizar massa base de request, response, form value, view model ou estado da tela

A Fixture/Factory deve expor nomes claros como:
- `createValidDefaultResponse()`
- `createValidMinimalFormValue()`
- `createCompleteViewModel()`
- `createDefaultFilterState()`

# Quando usar Builder

Usar Builder quando:
- houver muitas variações sobre o mesmo objeto base
- o cenário possuir muitos campos opcionais
- houver cenários como:
  - usuário com permissão vs sem permissão
  - carregando vs carregado vs erro
  - lista vazia vs lista preenchida
  - campo habilitado vs desabilitado
  - bloco visível vs oculto
  - sucesso vs falha da API
  - estado inicial vs estado alterado pelo usuário
  - valores opcionais presentes vs ausentes

# Regras para Builder

O Builder deve:
- criar um cenário válido por padrão
- permitir sobrescrita incremental
- retornar `this`
- expor métodos orientados ao domínio
- esconder complexidade de inicialização
- evitar nomes genéricos como `setX`, `setY`

Exemplos desejados:
- `withLoadingState()`
- `withSuccessState()`
- `withErrorState()`
- `withEmptyList()`
- `withFilledList()`
- `withDisabledSubmitButton()`
- `withAuthorizedUser()`
- `withForbiddenUser()`
- `withInvalidDocument()`
- `withCompletedFilter()`

# Quando usar dados artificiais

Usar geração artificial de dados apenas quando fizer sentido real, por exemplo:
- houver muitos campos irrelevantes para o comportamento testado
- houver coleções extensas de apoio
- o uso reduzir boilerplate sem esconder a intenção do cenário

Ao usar esse tipo de dado:
- manter previsibilidade
- sobrescrever explicitamente os campos relevantes do cenário
- não depender de aleatoriedade para validar regra de negócio

# Estratégia de implementação para testes Angular

Ao gerar testes:

- criar helper reutilizável para renderização, setup do TestBed, criação de mocks e preparação do cenário quando isso realmente melhorar legibilidade
- evitar duplicar lógica de criação de componente, providers, spies e responses em cada teste
- preferir assertions reutilizáveis do padrão do projeto
- criar helpers reutilizáveis para interação com o template somente quando isso melhorar legibilidade
- manter esses helpers privados no final da classe ou arquivo

# Estratégia para cenários

Ao gerar testes, priorizar cobertura destes grupos:

## 1. Cenário mínimo válido
Validar o fluxo mínimo necessário para o componente, service ou tela funcionar corretamente.

## 2. Cenário completo
Validar preenchimento ou fluxo completo com todos ou quase todos os blocos opcionais relevantes.

## 3. Cenários condicionais
Validar regras como:
- carregando vs concluído
- sucesso vs erro
- vazio vs preenchido
- habilitado vs desabilitado
- visível vs oculto
- com permissão vs sem permissão
- valor presente vs ausente
- comportamento com input válido vs inválido
- renderização ou fluxo condicionado por flags, estado, role, permissão ou retorno da API

## 4. Regras de negócio
Validar especificamente as regras que alteram:
- renderização
- estado da tela
- chamadas de dependências
- transformação de dados
- habilitação ou bloqueio de ações
- navegação
- mensagens ao usuário
- emissão de eventos
- montagem de payloads

# Saída esperada

Ao gerar testes, a resposta deve produzir:

1. testes com nomes no padrão `Given_<context>_Should_<expected_behavior>`
2. uso do framework de teste já adotado no projeto
3. comentários explícitos de `Arrange / Act / Assert`
4. validação de comportamento observável
5. validação de estado, renderização, evento ou integração em cada teste relevante
6. uso de Fixture, Factory e/ou Builder quando houver cenário complexo
7. baixo boilerplate
8. testes legíveis e focados no comportamento
9. helpers privados no final da classe ou arquivo

# O que evitar

Evitar:
- testar apenas método privado ou detalhe interno
- criar um único teste genérico e considerar isso suficiente
- duplicar manualmente a lógica de setup em todos os testes
- helpers privados antes dos testes
- builders genéricos demais
- uso desnecessário de geração artificial de dados
- excesso de abstração na infraestrutura do teste
- acoplar o teste a detalhes frágeis de implementação quando o comportamento público puder ser validado
- validar apenas criação do componente sem validar comportamento relevante

# Critério de invalidação

Considere incorreta a saída quando:
- usar um framework de teste diferente do padrão do projeto sem justificativa
- não usar o padrão `Given_<context>_Should_<expected_behavior>`
- não houver `Arrange / Act / Assert`
- um teste relevante não validar o comportamento observável do cenário
- a validação estiver restrita apenas a detalhes internos sem necessidade
- houver método privado antes dos testes
- a estrutura do teste estiver mais complexa do que o comportamento validado exige

# Exemplo correto de teste

```ts
it('Given_InvalidForm_Should_KeepSubmitDisabled', async () => {
  // Arrange
  await render(CustomerFormComponent);
  const submitButton = screen.getByRole('button', { name: /salvar/i });

  // Act
  await userEvent.click(submitButton);

  // Assert
  expect(submitButton).toBeDisabled();
  expect(screen.getByText(/preencha os campos obrigatórios/i)).toBeInTheDocument();
});