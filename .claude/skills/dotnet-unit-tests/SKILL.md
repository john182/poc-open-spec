---
name: dotnet-unit-tests
description: Gera testes unitários em C#/.NET com foco em comportamento, isolamento, legibilidade e padronização técnica
---

# Objetivo

Gerar testes unitários em C#/.NET seguindo estritamente o padrão do projeto, com foco principal em:
- comportamento observado
- isolamento da unidade testada
- legibilidade
- baixo boilerplate
- manutenção
- cobertura de cenários relevantes
- prevenção de regressão

Não priorizar contexto de negócio quando ele não for necessário para validar o comportamento técnico.

# Quando usar

Use esta skill quando:
- precisar criar testes unitários novos em C#
- precisar refatorar testes existentes
- precisar padronizar testes fora do padrão do projeto
- houver objetos complexos com muito setup
- houver necessidade de melhorar clareza, isolamento e reutilização da massa de teste
- houver necessidade de testar transformação de dados, serialização, mapeamento, handlers, services, clients, factories, validators, parsers, strategies, repositories fakeados e fluxos condicionais

# Regras obrigatórias

Ao gerar testes, siga obrigatoriamente:

- usar xUnit
- usar Shouldly para todas as asserções
- não usar `Assert.*`, exceto se o usuário pedir explicitamente
- usar Moq para dependências mockáveis
- usar Bogus quando ajudar a reduzir boilerplate de dados
- usar Fixture para cenários base reutilizáveis
- usar Builder quando houver muitas variações de entrada
- usar RichardSzalay.MockHttp para mockar requests HTTP em testes unitários
- nomear testes obrigatoriamente no padrão:

`Given_<context>_Should_<expected_behavior>`

- organizar todos os testes com comentários explícitos:
  - Arrange
  - Act
  - Assert
- cada teste deve validar um único comportamento
- os testes devem ser curtos, determinísticos, legíveis e focados
- evitar duplicação de setup
- evitar acoplamento desnecessário à implementação interna
- validar comportamento observável e saída relevante
- em XML, JSON, DTO, request, command, response ou payload, validar estrutura relevante e não apenas existência

# Prioridade de validação

Ao escrever testes, priorize nesta ordem:

1. comportamento observado
2. resultado final retornado
3. interações relevantes com dependências
4. transformação correta dos dados
5. cenários alternativos e de falha
6. bordas e defaults
7. detalhes estruturais relevantes da saída

Não gastar esforço com contexto funcional desnecessário quando o teste é puramente técnico.

# Estratégia para massa de teste

## Regra geral

- não montar objetos grandes inline repetidamente
- encapsular setup repetido
- deixar o teste mostrar intenção, não ruído
- a montagem deve ficar fora do caminho principal de leitura do teste

## Quando montar inline

Pode montar inline somente quando:
- o cenário for simples
- o objeto for pequeno
- não houver repetição relevante
- isso deixar o teste mais claro do que criar abstrações

## Quando usar Fixture

Usar Fixture quando:
- existir um cenário base válido reutilizado
- vários testes precisarem do mesmo objeto inicial
- o objetivo for fornecer massa estável, simples e conhecida

## Quando usar Builder

Usar Builder quando:
- houver muitas variações de um mesmo objeto
- o objeto tiver muitos campos
- houver cenários mínimos, completos e alternativos
- a leitura do teste piorar com setup manual repetido

## Regra de escolha

Use esta lógica:
- cenário pequeno e único: inline
- cenário base reutilizado: Fixture
- múltiplas variações: Builder
- objeto complexo: Builder com objeto válido por padrão
- objeto complexo com base comum: Fixture + Builder

# Regras para Fixture

A Fixture deve:
- fornecer dados válidos por padrão
- ser simples
- evitar lógica desnecessária
- representar cenários reutilizáveis
- expor métodos claros, por exemplo:
  - `CreateValidDefault()`
  - `CreateValidMinimal()`
  - `CreateComplete()`

# Regras para Builder

O Builder deve:
- criar um objeto válido por padrão
- permitir sobrescrita incremental
- retornar `this`
- esconder complexidade de inicialização
- ter nomes claros e intencionais
- evitar `SetX`, `SetY` quando existir nome melhor
- facilitar alteração apenas do que importa para o teste

Exemplos de nomes adequados:
- `WithNullValue()`
- `WithEmptyItems()`
- `WithInvalidDocument()`
- `WithHttpSuccess()`
- `WithHttpFailure()`
- `WithoutOptionalSection()`
- `WithOptionalSection()`
- `WithTimeout()`
- `WithUnauthorizedResponse()`

# Organização recomendada

Preferir:
- `<ClasseOuEntidade>Tests`
- `<ClasseOuEntidade>TestFixture`
- `<ClasseOuEntidade>Builder`

Os testes devem consumir `Fixture` e `Builder` sempre que isso reduzir ruído e duplicação.

# Cenários que devem ser priorizados

Ao gerar testes, priorizar estes grupos:

## 1. Cenário mínimo válido
Validar o fluxo mínimo necessário para sucesso.

## 2. Cenário completo
Validar um cenário com preenchimento mais completo e saída relevante.

## 3. Cenários condicionais
Validar mudanças de comportamento conforme entrada, estado ou retorno de dependências.

## 4. Cenários de borda
Validar nulo, vazio, default, coleção vazia, status alternativo, retorno parcial, dado ausente e combinações que possam quebrar o fluxo.

## 5. Cenários de falha
Validar erro esperado, exceção esperada, retorno inválido, falha de dependência, timeout, status inesperado, fallback e short-circuit.

## 6. Estrutura de saída relevante
Quando existir saída estruturada, validar os blocos importantes que realmente representam comportamento.

# Interações com dependências

Quando aplicável, validar:
- se a dependência foi chamada
- se foi chamada a quantidade esperada
- se foi chamada com argumentos relevantes
- se não foi chamada quando o fluxo deveria encerrar antes

Não verificar interação sem motivo claro.

# XML, JSON e serialização

Quando o teste envolver XML, JSON ou serialização:
- validar o comportamento geral
- validar elementos, atributos, blocos ou campos relevantes
- não validar detalhes irrelevantes que tornem o teste frágil
- quando aplicável, validar estrutura produzida e conteúdo importante

# O que evitar

Evitar:
- objetos enormes montados inline em vários testes
- duplicação extensa de Arrange
- uso de `Assert.*`
- mais de uma intenção principal no mesmo teste
- nomes vagos
- nomes excessivamente presos ao negócio quando o teste é técnico
- builders inválidos por padrão
- builders genéricos demais
- testes frágeis baseados em detalhe interno irrelevante
- excesso de mocks sem necessidade
- testar implementação em vez de comportamento

# Saída esperada

Ao responder, gerar:
1. testes no padrão `Given_<context>_Should_<expected_behavior>`
2. `Shouldly` em todas as asserções
3. comentários `Arrange / Act / Assert`
4. testes focados em comportamento
5. cenários mínimos, completos, condicionais, borda e falha
6. `Fixture` e/ou `Builder` quando fizer sentido
7. baixo boilerplate
8. código pronto para uso no projeto

# Forma de atuação

Ao criar os testes:
- primeiro identificar o comportamento principal da unidade
- depois mapear cenários obrigatórios
- depois decidir se usa inline, Fixture ou Builder
- depois gerar testes com foco em clareza e isolamento
- por fim revisar duplicação, nomes e fragilidade

# Exemplo correto

```csharp
[Fact]
public void Given_ValidRequest_Should_ReturnMappedResponse()
{
    // Arrange
    var request = RequestTestFixture.CreateValidDefault();

    // Act
    var result = _sut.Execute(request);

    // Assert
    result.ShouldNotBeNull();
    result.Success.ShouldBeTrue();
}