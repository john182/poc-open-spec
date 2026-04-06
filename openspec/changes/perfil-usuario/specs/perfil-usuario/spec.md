## ADDED Requirements

### Requirement: Obter perfil do usuário autenticado
O sistema SHALL expor `GET /api/v1/perfil` que retorna os dados do perfil do usuário autenticado. O endpoint SHALL exigir autenticação (qualquer role). Os dados retornados SHALL incluir: id, nome, email.

#### Scenario: Obter perfil com sucesso
- **WHEN** um usuário autenticado chama `GET /api/v1/perfil`
- **THEN** o sistema retorna HTTP 200 com `{ id, nome, email }`

#### Scenario: Usuário não autenticado tenta obter perfil
- **WHEN** um usuário sem token válido chama `GET /api/v1/perfil`
- **THEN** o sistema retorna HTTP 401 Unauthorized

---

### Requirement: Atualizar perfil do usuário autenticado
O sistema SHALL expor `PUT /api/v1/perfil` que permite ao usuário autenticado atualizar seu nome e, opcionalmente, sua senha. O email NÃO SHALL ser editável. Se os campos de senha forem omitidos ou vazios, somente o nome SHALL ser atualizado. Se os campos de senha forem preenchidos, o sistema SHALL validar a senha atual antes de aceitar a nova senha.

#### Scenario: Atualizar somente o nome
- **WHEN** um usuário autenticado chama `PUT /api/v1/perfil` com `{ nome: "Novo Nome" }` sem campos de senha
- **THEN** o sistema atualiza o nome, emite novo JWT com o nome atualizado, e retorna HTTP 200 com `{ id, nome, email, accessToken }`

#### Scenario: Atualizar nome e senha
- **WHEN** um usuário autenticado chama `PUT /api/v1/perfil` com `{ nome: "Novo Nome", senhaAtual: "12345678", novaSenha: "87654321" }`
- **THEN** o sistema valida a senha atual, atualiza nome e hash da senha, emite novo JWT, e retorna HTTP 200 com `{ id, nome, email, accessToken }`

#### Scenario: Senha atual incorreta
- **WHEN** um usuário autenticado chama `PUT /api/v1/perfil` com senha atual incorreta
- **THEN** o sistema retorna HTTP 400 Bad Request com `{ erro: "Senha atual incorreta" }`

#### Scenario: Nova senha não atende requisitos mínimos
- **WHEN** um usuário autenticado chama `PUT /api/v1/perfil` com nova senha com menos de 8 caracteres
- **THEN** o sistema retorna HTTP 400 Bad Request com erro de validação

#### Scenario: Nome vazio
- **WHEN** um usuário autenticado chama `PUT /api/v1/perfil` com nome vazio ou em branco
- **THEN** o sistema retorna HTTP 400 Bad Request com erro de validação

#### Scenario: Nova senha preenchida sem senha atual
- **WHEN** um usuário autenticado chama `PUT /api/v1/perfil` com `novaSenha` preenchida mas `senhaAtual` vazia
- **THEN** o sistema retorna HTTP 400 Bad Request com `{ erro: "Senha atual é obrigatória para alterar a senha" }`

---

### Requirement: Página de perfil no frontend
O frontend SHALL fornecer uma página em `/perfil` acessível a qualquer usuário autenticado. A página SHALL exibir um card único com: campo de nome (editável), campo de email (somente leitura), seção de alteração de senha (opcional). O formulário SHALL ter botão "Salvar" que envia as alterações.

#### Scenario: Exibir dados do perfil
- **WHEN** um usuário autenticado navega para `/perfil`
- **THEN** a página carrega os dados do perfil via `GET /api/v1/perfil` e exibe nome (editável), email (somente leitura)

#### Scenario: Editar somente o nome
- **WHEN** o usuário altera o nome e clica "Salvar" sem preencher os campos de senha
- **THEN** o sistema envia `PUT /api/v1/perfil` com apenas o nome, atualiza o token local, e a topbar reflete o novo nome

#### Scenario: Editar nome e senha
- **WHEN** o usuário altera o nome, preenche senha atual e nova senha, e clica "Salvar"
- **THEN** o sistema envia `PUT /api/v1/perfil` com nome e senhas, atualiza o token local, e exibe mensagem de sucesso

#### Scenario: Erro de validação no formulário
- **WHEN** o usuário tenta salvar com nome vazio
- **THEN** o formulário exibe erro inline no campo nome e não envia a requisição

#### Scenario: Erro de senha atual incorreta
- **WHEN** o backend retorna erro de senha atual incorreta
- **THEN** o formulário exibe mensagem de erro no campo de senha atual

#### Scenario: Rota protegida
- **WHEN** um usuário não autenticado tenta acessar `/perfil`
- **THEN** o frontend redireciona para `/auth/login`

---

### Requirement: Serviço de perfil no frontend
O frontend SHALL fornecer um `PerfilService` que encapsula as chamadas HTTP para os endpoints de perfil (`GET /api/v1/perfil` e `PUT /api/v1/perfil`).

#### Scenario: Obter perfil
- **WHEN** o `PerfilService.obterPerfil()` é chamado
- **THEN** ele faz `GET /api/v1/perfil` e retorna um Observable com os dados do perfil

#### Scenario: Atualizar perfil
- **WHEN** o `PerfilService.atualizarPerfil(dados)` é chamado
- **THEN** ele faz `PUT /api/v1/perfil` com os dados e retorna um Observable com a resposta incluindo novo token
