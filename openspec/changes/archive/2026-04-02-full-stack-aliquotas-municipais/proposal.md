## Why

O Brasil possui mais de 5.500 municípios, cada um com autonomia para definir alíquotas de ISS sobre centenas de códigos de serviço (LC 116/2003). A API NFS-e do ADN (`adn.nfse.gov.br`) disponibiliza essas informações, mas de forma fragmentada: cada consulta exige município + código de serviço + competência. Não existe hoje uma visão consolidada, navegável e atualizada dessas alíquotas.

Essa solução resolve o problema ao construir um **crawler inteligente** que coleta e materializa os dados localmente, um **backend API** que serve leitura rápida, e um **frontend Angular** com experiência de navegação visual por mapa → estado → município → serviços/alíquotas. O momento é agora porque a API NFS-e está estabilizada e há demanda real por uma ferramenta que simplifique a consulta tributária municipal.

## What Changes

- **Nova aplicação frontend Angular** com layout administrativo baseado no template PrimeNG Sakai (referência controlada), incluindo:
  - Autenticação (sign in, sign up)
  - Páginas de erro (acesso negado, 404)
  - Mapa interativo do Brasil com seleção de estado → município
  - Listagem de serviços/alíquotas com filtros
  - Design system e design tokens próprios
  - Estados de loading, vazio, erro e retry

- **Nova aplicação backend .NET** com API REST documentada (Swagger/OpenAPI), incluindo:
  - Autenticação e autorização (JWT)
  - Endpoints de consulta otimizados para leitura rápida
  - Persistência local dos dados consolidados (MongoDB)
  - Seed de estados e municípios brasileiros (IBGE)
  - Versionamento de API

- **Novo worker/crawler** como parte central da solução, incluindo:
  - Coleta incremental de alíquotas da API NFS-e com controle de concorrência
  - Materialização local com atualização por competência
  - Controle de status de execução, retomada e reprocessamento
  - Rate limiting e circuit breaker para proteção da API externa
  - Execução agendada e sob demanda

- **Novo projeto E2E com Cypress** separado, cobrindo:
  - Fluxos de autenticação
  - Navegação completa (mapa → estado → município → listagem)
  - Filtros e cenários de erro

- **Dockerização completa** com docker-compose para ambiente local integrado

- **Documentação forte** para evolução por agentes: produto, técnica, contratos, estratégias

## Capabilities

### New Capabilities

- `user-auth`: Autenticação e autorização de usuários (cadastro, login, JWT, guards de rota, acesso negado)
- `frontend-foundation`: Fundação visual do frontend — layout, design system, design tokens, componentes base, helpers de formulário, páginas de erro
- `aliquota-query`: Consulta de alíquotas municipais — mapa do Brasil, seleção de estado/município, listagem de serviços com filtros
- `backend-api`: API REST do backend — endpoints de consulta, DTOs, contratos, Swagger, persistência MongoDB, seed de dados
- `data-crawler`: Worker/crawler de coleta e consolidação — ingestão da API NFS-e, materialização local, controle de concorrência, retomada, agendamento
- `e2e-testing`: Projeto Cypress E2E — cobertura de fluxos críticos, massa de dados, seletores estáveis
- `docker-infra`: Infraestrutura dockerizada — Dockerfiles, docker-compose, rede, volumes, variáveis de ambiente

### Modified Capabilities

_(Nenhuma — projeto greenfield, todas as capabilities são novas)_

## Impact

- **Código**: Criação de 4 projetos novos (frontend Angular, backend .NET, worker, Cypress E2E) + infraestrutura Docker
- **APIs**: Nova API REST interna (backend → frontend) + integração com API externa NFS-e (worker → adn.nfse.gov.br)
- **Dependências externas**: API NFS-e do ADN (requer certificado PFX para acesso), API IBGE para dados de estados/municípios
- **Infraestrutura**: MongoDB como store principal, Docker Compose para orquestração local
- **Sistemas afetados**: Nenhum sistema existente — solução greenfield isolada
- **Riscos de integração**: A API NFS-e usa certificado cliente (PFX) e não tem documentação oficial pública; os arquivos Bruno em `context/` são a fonte primária de entendimento da integração
