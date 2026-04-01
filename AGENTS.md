## Regra obrigatória de idioma no código

Neste repositório, a linguagem padrão para nomeação de elementos do código é **português**.

### Regra geral
Todo código novo deve usar **português** de forma consistente em:
- nomes de classes
- nomes de interfaces
- nomes de métodos
- nomes de propriedades
- nomes de variáveis
- nomes de parâmetros
- nomes de arquivos
- nomes de componentes
- nomes de serviços
- nomes de DTOs, requests, responses, commands, queries, filtros e modelos
- nomes de testes

Não misturar português e inglês no mesmo contexto.

### Obrigatório
- preferir nomes completos e semânticos em português
- manter consistência com os nomes já existentes no projeto
- se a entidade de domínio já estiver em português, todo o restante relacionado também deve ficar em português
- métodos devem ter nomes em português orientados à intenção
- propriedades devem ter nomes em português claros e diretos
- variáveis locais e parâmetros também devem ficar em português
- testes devem continuar seguindo o padrão do projeto, mas com nomes em português quando possível dentro da convenção adotada

### Evitar
- criar método em inglês em classe em português
- criar propriedade em inglês em DTO em português
- misturar nomes como:
  - `BuscarAliquota` com `taxRate`
  - `municipio` com `city`
  - `AtualizarCache` com `updateResult`
- usar abreviações desnecessárias
- traduzir parcialmente nomes relacionados

### Exceções permitidas
Somente manter em inglês quando houver motivo técnico real, como:
- nomes de bibliotecas, frameworks ou tipos externos
- contratos exigidos por API externa
- propriedades cujo nome precisa refletir exatamente o payload externo
- termos técnicos consagrados impossíveis ou inadequados de traduzir no contexto
- namespaces de libs, classes de infraestrutura externa e nomes vindos de vendor

### Regra para integrações externas
Quando a API externa usar nomes em inglês:
- no domínio interno, preferir português
- na borda de integração, mapear o contrato externo para nomes internos em português
- evitar espalhar nomes em inglês por todo o sistema só porque a API externa usa inglês

### Regra para Angular
No Angular, usar português em:
- nomes de componentes
- propriedades do componente
- métodos
- variáveis
- services
- models internos
- helpers internos

Exceções:
- bindings ou contratos externos que precisem refletir integração real
- APIs de framework e bibliotecas

### Regra para backend
No backend, usar português em:
- entidades
- casos de uso quando existirem
- serviços
- métodos
- propriedades
- parâmetros
- DTOs internos
- responses
- filtros
- handlers
- validadores

Exceções:
- contratos externos
- bibliotecas
- nomes impostos por protocolo ou tecnologia

### Regra para testes
Os testes devem seguir o padrão definido pelo projeto, mas o vocabulário do cenário deve permanecer em português sempre que possível.

Exemplo desejado:
- `Dado_MunicipioInvalido_Deve_RetornarErroDeValidacao`
- `Given_MunicipioInvalido_Should_RetornarErroDeValidacao`

Evitar:
- `Given_InvalidCity_Should_ReturnValidationError`
quando o restante do projeto estiver em português.

### Regra de revisão
Antes de concluir qualquer implementação, revisar:
- se houve mistura de português e inglês
- se novos nomes seguem o padrão predominante do projeto
- se termos externos foram corretamente isolados apenas na borda de integração