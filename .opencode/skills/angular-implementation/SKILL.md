---
name: angular-implementation
description: Implementa código Angular/TypeScript seguindo estritamente o padrão oficial do projeto
---

# Objetivo

Gerar ou refatorar código Angular/TypeScript de produção seguindo estritamente os padrões do projeto, com foco em clareza, coesão, reutilização, domínio, baixo acoplamento e aderência total à arquitetura já adotada pelo projeto.

# Regras obrigatórias

- Sempre seguir SOLID com pragmatismo.
- Sempre seguir Clean Code sem overengineering.
- Preferir componentes, services, facades, directives, pipes e classes especializadas com nomes orientados ao domínio.
- Não criar `UseCase` por padrão.
- Para fluxos simples de comunicação com API, pode usar `Service` quando fizer sentido e o nome for específico.
- Evitar classes genéricas como `Helper`, `Utils`, `Manager`, `Processor`, `CommonService`.
- Evitar abstrações sem necessidade real.
- Evitar interfaces sem justificativa concreta.
- Preferir baixo acoplamento e alta coesão.
- Métodos privados devem ficar no final da classe.
- Antes de propor a solução, avaliar se ela está mais complexa do que o problema exige.
- Sempre que houver strings repetidas relevantes, extrair constantes.
- Não deixar números mágicos; extrair constantes, tipos ou enums apropriados.
- Sempre que um campo possuir conjunto finito de valores possíveis, preferir `enum` ou união de tipos quando fizer mais sentido no padrão do projeto.
- Preservar o comportamento existente em refatorações, salvo quando a mudança solicitada exigir alteração funcional explícita.

# Regras obrigatórias de aderência arquitetural

- Antes de implementar, identificar explicitamente qual arquitetura o projeto Angular já utiliza.
- A implementação deve seguir a arquitetura já existente no projeto, e não a arquitetura que o agente considera ideal.
- Se o projeto usar organização por feature, manter a organização por feature.
- Se o projeto usar organização por camada técnica, manter a organização por camada técnica.
- Se o projeto usar `standalone components`, manter esse padrão.
- Se o projeto usar `NgModules`, manter esse padrão.
- Se o projeto usar `signals`, manter esse padrão onde já for predominante.
- Se o projeto usar `RxJS` com `Observable`, manter esse padrão onde já for predominante.
- Se o projeto usar store global como NgRx, ComponentStore, Signals Store, Akita, NGXS ou facade pattern, manter o padrão já adotado.
- Não introduzir um novo estilo arquitetural em um projeto que já possui padrão estabelecido.
- Não misturar padrões arquiteturais incompatíveis sem solicitação explícita.
- Não transformar o projeto para outro modelo estrutural sem que isso tenha sido pedido.
- Respeitar a separação de módulos, features, diretórios, dependências e convenções já presentes no projeto.
- Respeitar os pontos de entrada e saída já definidos pela arquitetura atual.
- Respeitar a direção de dependências já adotada no projeto.
- Não mover código entre camadas, features ou módulos sem necessidade real.
- Não criar novas camadas, novos módulos ou novas bibliotecas sem necessidade clara e sem alinhamento com a estrutura já existente.
- Não introduzir abstrações arquiteturais novas apenas porque seriam “mais corretas” teoricamente.
- Se a arquitetura atual tiver limitações, ainda assim manter coerência com ela, salvo solicitação explícita de evolução arquitetural.
- Só propor alteração arquitetural quando isso for pedido explicitamente pelo usuário.
- Quando houver dúvida sobre a arquitetura dominante do projeto, primeiro inferir pela estrutura existente de pastas, módulos, dependências, convenções, componentes e serviços já implementados.
- Em caso de dúvida entre duas interpretações possíveis, preferir a abordagem que menos altera a arquitetura atual do projeto.
- Componentes novos devem nascer no mesmo estilo arquitetural dos componentes equivalentes já existentes.
- Components, services, facades, stores, guards, resolvers, interceptors, directives, pipes, models, DTOs, adapters e mappers devem ser posicionados de acordo com a arquitetura vigente no projeto, não com preferência pessoal do agente.

# Regras obrigatórias de leitura estrutural do projeto

- Antes de criar qualquer arquivo novo, localizar componentes equivalentes já existentes no projeto.
- Observar onde o projeto normalmente coloca:
  - components
  - pages
  - layouts
  - services
  - facades
  - stores
  - guards
  - resolvers
  - interceptors
  - directives
  - pipes
  - models
  - DTOs
  - mappers
  - adapters
  - validators
  - utilitários já centralizados
- Repetir o padrão estrutural já adotado.
- Repetir o padrão de injeção de dependência já adotado.
- Repetir o padrão de composição de respostas, erros e validações já adotado.
- Repetir o padrão de organização de namespaces, aliases, imports e pastas já adotado.
- Não inventar nova convenção de nomes, agrupamento ou separação de responsabilidade se já existir padrão claro no projeto.

# Regras obrigatórias de reutilização e centralização

- Antes de criar qualquer método auxiliar para formatação, normalização, parsing, conversão, limpeza ou composição de valores, procurar se já existe implementação equivalente no projeto.
- Não duplicar métodos para comportamentos recorrentes como máscara, CEP, telefone, documento, código postal, texto, números, datas, moeda, identificadores e normalizações semelhantes.
- Quando houver comportamento recorrente e reutilizável, centralizar em um ponto único apropriado, com nome orientado ao domínio.
- Não criar uma nova implementação local apenas para resolver uma necessidade pontual se já existir comportamento equivalente ou muito semelhante no projeto.
- Se já existir uma implementação semelhante, preferir reutilizar diretamente.
- Se a implementação existente não atender completamente ao novo cenário, preferir refatorar e consolidar em vez de criar outra versão paralela.
- Não espalhar lógica compartilhada em components, services, facades, stores, directives, pipes, validators, mappers ou adapters.
- Evitar métodos locais duplicados como `formatCep`, `normalizeCep`, `applyCepMask`, `formatPhone`, `normalizePhone`, `cleanDocument` e equivalentes quando representarem o mesmo comportamento.
- Só criar um novo componente para esse tipo de lógica quando ficar claro que não existe ponto reutilizável já disponível no projeto.
- Quando precisar criar um ponto central novo, escolher nome coeso, específico e orientado ao domínio, evitando nomes genéricos.

# Regras obrigatórias de implementação Angular/TypeScript

- Respeitar a arquitetura existente do projeto.
- Não mover responsabilidade para camadas inadequadas.
- Não misturar regra de negócio com detalhe de UI sem necessidade.
- Preferir componentes enxutos, com foco em apresentação e orquestração de interações.
- Sempre que fizer sentido, mover regras reutilizáveis para services, facades, stores ou classes específicas já compatíveis com o padrão do projeto.
- Preferir métodos curtos e com responsabilidade única.
- Preferir nomes que expressem intenção.
- Evitar duplicação de lógica.
- Em fluxos de transformação, manter clareza entre entrada, processamento e saída.
- Quando houver mapeamento entre contrato da API e modelo da tela, evitar acoplamento excessivo entre DTO externo e estrutura interna da feature.
- Não colocar lógica complexa diretamente no template HTML.
- Evitar expressões extensas no template; preferir computed values, getters com parcimônia, signals, observables tratados previamente ou propriedades derivadas conforme o padrão existente.
- Não criar métodos no template que provoquem processamento desnecessário em ciclos de detecção de mudança, salvo quando isso já for padrão aceito e controlado no projeto.
- Quando usar RxJS, evitar `subscribe` manual desnecessário; preferir `async pipe`, composição reativa e limpeza adequada quando houver inscrição manual.
- Quando usar Signals, manter consistência com o padrão já adotado no projeto.
- Em formulários, seguir o padrão já existente entre Reactive Forms e Template-driven Forms.
- Em chamadas HTTP, manter o padrão atual de tratamento de erro, interceptação, adapters e tipagem.
- Em componentes, preservar separação clara entre:
  - estado da tela
  - eventos do usuário
  - integração com serviços
  - transformação de dados para exibição
- Antes de finalizar, revisar se a implementação introduziu duplicação, complexidade desnecessária, violação arquitetural ou violação de padrão do projeto.

# Regras obrigatórias para HTML e template Angular

- O template deve ser legível e orientado à intenção.
- Evitar excesso de lógica inline em bindings.
- Repetir o padrão já adotado no projeto para:
  - classes condicionais
  - renderização condicional
  - loops
  - bindings de atributos
  - organização de componentes filhos
- Se o projeto usar a nova sintaxe de controle do Angular como `@if`, `@for` e `@switch`, manter esse padrão.
- Se o projeto usar `*ngIf`, `*ngFor` e `ngSwitch`, manter esse padrão.
- Não misturar os dois estilos sem necessidade.
- Priorizar acessibilidade quando o contexto indicar interação do usuário:
  - labels corretos
  - associação entre campos e mensagens
  - `aria-*` quando aplicável
  - semântica adequada nos elementos HTML
- Classes CSS no template devem seguir o padrão já existente do projeto, como BEM, utility-first, design system próprio ou convenção local.

# Regras obrigatórias para estilos

- Respeitar a estratégia já usada no projeto:
  - SCSS
  - CSS
  - Tailwind
  - Angular Material
  - design system próprio
- Não introduzir nova abordagem de estilização sem necessidade.
- Não espalhar regras de estilo locais quando já existir componente base, design token ou classe reutilizável apropriada.
- Reutilizar variáveis, mixins, tokens, utilitários e componentes visuais já existentes antes de criar novos.

# Regras obrigatórias de decisão

- Sempre perguntar internamente: “como este projeto já resolve problemas parecidos?”
- A nova implementação deve parecer nativa do projeto.
- O código novo não deve parecer importado de outra arquitetura ou de outro estilo de projeto.
- Se existir componente equivalente, copiar o padrão estrutural e de dependência dele.
- Priorizar consistência com o projeto acima de preferência teórica de arquitetura.

# Saída esperada

Ao implementar:
1. gerar código pronto para uso
2. manter aderência aos padrões do projeto
3. manter aderência total à arquitetura existente
4. minimizar impacto desnecessário
5. reutilizar o que já existe antes de criar novas abstrações
6. explicar de forma objetiva qualquer decisão técnica relevante

## Naming obrigatório

- Nunca usar nomes genéricos ou de uma letra para parâmetros, variáveis locais, métodos privados e resultados intermediários, exceto em lambdas extremamente curtas e óbvias.
- Parâmetros devem ter nome semântico completo, representando claramente o papel no contexto.
- Métodos privados devem ter nomes que expressem claramente intenção e resultado.
- Variáveis intermediárias devem receber nomes que expliquem o conteúdo ou propósito, evitando expressões complexas inline quando isso prejudicar legibilidade.
- Components, services, facades, stores, directives, pipes e models devem ter nomes que deixem claro seu papel funcional e de domínio.

### Exemplos proibidos
- `r`
- `x`
- `obj`
- `data`
- `value`
- `item`
- `res`
- `result` quando houver um nome mais específico
- métodos como `build`, `handle`, `process`, `execute`, `loadData` sem contexto suficiente

### Exemplos esperados
- `customerSummaryResponse`
- `invoiceFilterForm`
- `selectedCompanyId`
- `salesReportRequest`
- `formattedAccessKey`
- `loadAuthorizedInvoices`
- `mapCompanyResponseToViewModel`
- `buildRestrictionFormGroup`
- `resolveValidationMessage`