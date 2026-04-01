---
name: angular-review
description: Revisa tecnicamente a implementação Angular/TypeScript seguindo estritamente os padrões oficiais do projeto
---

# Objetivo

Revisar a implementação Angular/TypeScript e apontar, de forma objetiva e acionável, problemas de qualidade, padronização, aderência arquitetural, duplicação, riscos de regressão e violações dos padrões do projeto.

# Regras obrigatórias

- Revisar com foco em aderência real ao padrão do projeto.
- Priorizar problemas concretos em vez de sugestões cosméticas.
- Ser objetivo, técnico e acionável.
- Não sugerir mudanças sem ganho claro.
- Verificar se a solução está mais complexa do que o problema exige.
- Verificar se nomes estão coerentes com o domínio.
- Verificar se houve violação de Clean Code, SOLID com pragmatismo, coesão e baixo acoplamento.
- Verificar se métodos privados ficaram no final da classe.
- Verificar se strings repetidas relevantes deveriam ser constantes.
- Verificar se existem números mágicos.
- Verificar se campos com conjunto finito de valores deveriam ser `enum` ou união de tipos quando isso fizer mais sentido no padrão do projeto.
- Verificar se a arquitetura do projeto foi respeitada.
- Verificar se existem testes suficientes para a mudança.
- Quando houver formulários, validar se as regras de preenchimento, comportamento condicional, estados de erro e fluxo de submissão estão cobertos.
- Quando houver integração com API, validar se contratos, tratamento de erro, estados de loading e transformação de dados estão coerentes.

# Regras obrigatórias de aderência arquitetural

- Identificar a arquitetura já adotada pelo projeto Angular antes de avaliar a mudança.
- Verificar se a implementação seguiu a arquitetura já existente no projeto.
- Se o projeto usar organização por feature, validar se a mudança permaneceu coerente com esse padrão.
- Se o projeto usar organização por camada técnica, validar se a mudança permaneceu coerente com esse padrão.
- Se o projeto usar `standalone components`, validar se a mudança respeitou esse padrão.
- Se o projeto usar `NgModules`, validar se a mudança respeitou esse padrão.
- Se o projeto usar `signals`, validar se a mudança permaneceu coerente com esse padrão.
- Se o projeto usar `RxJS` com `Observable`, validar se a mudança permaneceu coerente com esse padrão.
- Se o projeto usar store global, facade pattern ou gerenciamento de estado específico, validar se a mudança respeitou esse padrão.
- Sinalizar como problema qualquer tentativa de introduzir um estilo arquitetural diferente do já adotado pelo projeto sem solicitação explícita.
- Sinalizar como problema qualquer mistura indevida de padrões incompatíveis.
- Verificar se novas pages, components, services, facades, stores, guards, resolvers, interceptors, directives, pipes, adapters, mappers, validators, DTOs e models foram posicionados corretamente conforme a arquitetura vigente.
- Verificar se a direção de dependências foi preservada.
- Sinalizar como problema quando componentes de apresentação passam a concentrar regra de negócio sem justificativa coerente com o padrão do projeto.
- Sinalizar como problema quando responsabilidades forem movidas para camadas, módulos ou features inadequadas.
- Sinalizar como problema quando a mudança criar novas camadas, novos módulos ou novas abstrações sem necessidade clara e sem alinhamento com a estrutura do projeto.
- Validar se o código novo parece nativo do projeto e segue o mesmo padrão estrutural dos componentes equivalentes já existentes.
- Em caso de dúvida sobre a arquitetura dominante, inferir pela estrutura de pastas, módulos, dependências, convenções, componentes e serviços existentes.
- Priorizar consistência com a arquitetura atual do projeto acima de preferência teórica por outro padrão.

# Regras obrigatórias de revisão Angular

- Verificar se componentes estão com responsabilidade adequada e não acumulam lógica excessiva.
- Verificar se lógica de apresentação, transformação de dados, integração com API e controle de estado estão separados de forma coerente com o padrão do projeto.
- Verificar se o template HTML está legível e sem excesso de lógica inline.
- Apontar como problema expressões extensas ou complexas no template quando prejudicarem a leitura, manutenção ou performance.
- Verificar se o projeto manteve consistência entre `@if`, `@for`, `@switch` e a sintaxe antiga com `*ngIf`, `*ngFor` e `ngSwitch`, sem misturas desnecessárias.
- Verificar se classes condicionais, atributos condicionais e renderização condicional seguem o padrão já adotado pelo projeto.
- Verificar se o uso de `signals`, `computed`, `effect`, `Observable`, `Subject`, `BehaviorSubject` ou store está consistente com a abordagem predominante do projeto.
- Apontar como problema `subscribe` manual desnecessário quando houver alternativa mais aderente ao padrão do projeto.
- Apontar como problema ausência de tratamento de unsubscribe quando ele for necessário.
- Verificar se chamadas HTTP estão tipadas corretamente e se o tratamento de erro segue o padrão do projeto.
- Verificar se DTOs, models e view models estão sendo usados de forma coerente, evitando acoplamento desnecessário entre contrato externo e estrutura interna da tela.
- Verificar se formulários seguem o padrão já adotado pelo projeto entre Reactive Forms e Template-driven Forms.
- Verificar se regras de validação estão no lugar correto e se mensagens de erro estão consistentes.
- Verificar se houve duplicação de lógica entre componentes, services, facades, stores, directives, pipes ou utilitários.
- Verificar se o código respeita acessibilidade básica quando houver interação do usuário:
  - associação adequada entre label e campo
  - uso correto de `aria-*` quando aplicável
  - semântica adequada nos elementos
  - mensagens de erro compreensíveis

# Regras obrigatórias de naming e legibilidade

- Apontar como problema parâmetros de uma letra fora de lambdas triviais e óbvias.
- Apontar como problema variáveis locais com nomes genéricos ou sem contexto semântico claro.
- Apontar como problema métodos privados com nomes vagos, genéricos ou sem intenção explícita.
- Apontar como problema nomes como `data`, `value`, `item`, `result`, `obj`, `res`, `x` e equivalentes quando houver nome semântico melhor.
- Apontar como problema nomes genéricos para componentes, services, facades, stores, directives, pipes ou models quando não deixarem claro o papel funcional e de domínio.
- Apontar como problema expressões complexas inline quando prejudicarem a leitura.
- Validar se o código favorece legibilidade por meio de nomes claros e variáveis intermediárias bem nomeadas quando necessário.

# Regras obrigatórias de reutilização e centralização

- Verificar se foram criados métodos auxiliares duplicados para formatação, normalização, parsing, conversão, limpeza ou composição de valores.
- Apontar quando existir lógica repetida para CEP, telefone, documento, máscaras, datas, textos, identificadores, códigos, moeda ou comportamentos equivalentes.
- Sinalizar quando o código criou novo método local em vez de reutilizar ou consolidar implementação já existente.
- Sinalizar quando houver múltiplas variações do mesmo comportamento espalhadas em componentes, services, facades, stores, directives, pipes ou utilitários diferentes.
- Verificar se o agente criou métodos locais por conveniência em vez de buscar o ponto central já existente.
- Recomendar centralização quando houver comportamento compartilhável repetido em mais de um ponto.
- Apontar quando components, services, facades, stores, validators, mappers, adapters ou pipes passaram a carregar lógica duplicada que deveria estar centralizada.
- Verificar se a solução introduziu mais uma versão paralela de algo que já existia no projeto.
- Quando houver comportamento recorrente reutilizável, recomendar reutilização ou refatoração para consolidação em vez de duplicação.

# Regras adicionais para revisão de UI, estado e integração

Quando a mudança envolver tela, componente, fluxo visual, estado ou integração com backend:

- verificar se o estado da tela foi modelado de forma clara
- verificar se loading, empty state, success state e error state foram tratados quando aplicável
- verificar se houve separação adequada entre evento do usuário, orquestração da tela e acesso a dados
- apontar como problema quando a tela depender diretamente de detalhes da API sem adaptação coerente com o padrão do projeto
- apontar como problema quando a lógica de transformação de dados para exibição estiver espalhada no template
- validar se componentes filhos receberam responsabilidades compatíveis e inputs/outputs com boa coesão
- validar se houve consistência visual e estrutural com componentes equivalentes já existentes no projeto
- validar se mudanças em template e estilo preservam clareza, reutilização e aderência ao design system ou padrão visual já adotado

# Saída esperada

A revisão deve retornar:

1. O que está bom
2. Problemas encontrados
3. Correções recomendadas
4. Lacunas de teste ou risco
5. Veredito final

# Critérios de reprovação

A revisão deve reprovar ou sinalizar fortemente quando encontrar:

- duplicação de regra de negócio
- criação de métodos auxiliares redundantes
- lógica compartilhada espalhada em vários pontos
- complexidade desnecessária
- nomes genéricos ou pouco orientados ao domínio
- ausência de reutilização quando já havia implementação semelhante
- regressão de padrão arquitetural
- introdução indevida de outro estilo estrutural
- posicionamento incorreto de components, services, facades, stores, guards, resolvers, interceptors, directives, pipes, models, DTOs ou validators
- quebra da direção de dependências da arquitetura existente
- componente concentrando regra demais sem necessidade
- template com lógica excessiva
- subscribe manual desnecessário ou sem limpeza adequada
- lacunas importantes de teste