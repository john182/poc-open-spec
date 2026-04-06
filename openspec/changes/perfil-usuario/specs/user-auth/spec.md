## ADDED Requirements

### Requirement: Métodos de mutação na entidade User
A entidade `User` SHALL fornecer métodos de domínio para atualizar nome e senha. O método `AtualizarNome` SHALL validar que o nome não é vazio. O método `AtualizarSenha` SHALL receber o novo hash de senha.

#### Scenario: Atualizar nome com valor válido
- **WHEN** `AtualizarNome("Novo Nome")` é chamado em uma instância de User
- **THEN** a propriedade `Nome` é atualizada para "Novo Nome"

#### Scenario: Atualizar nome com valor vazio
- **WHEN** `AtualizarNome("")` é chamado em uma instância de User
- **THEN** o método lança exceção de validação

#### Scenario: Atualizar senha
- **WHEN** `AtualizarSenha(novoHash)` é chamado em uma instância de User
- **THEN** a propriedade `PasswordHash` é atualizada para o novo hash

---

### Requirement: Persistência de atualização do usuário
O repositório de usuários SHALL fornecer método `AtualizarAsync(User)` que persiste as alterações de uma entidade User existente no MongoDB.

#### Scenario: Atualizar usuário existente
- **WHEN** `AtualizarAsync(user)` é chamado com uma entidade User válida
- **THEN** o documento correspondente no MongoDB é atualizado com os novos valores de Nome e PasswordHash

---

## MODIFIED Requirements

### Requirement: Password security
The system SHALL enforce minimum password requirements and store passwords securely.

#### Scenario: Password hashing
- **WHEN** a user registers or changes their password
- **THEN** the system stores the password as a bcrypt hash with a cost factor of at least 12

#### Scenario: Password minimum requirements
- **WHEN** a user submits a password shorter than 8 characters
- **THEN** the system rejects the request with HTTP 400

#### Scenario: Validação de senha atual ao alterar senha
- **WHEN** um usuário autenticado solicita alteração de senha via `PUT /api/v1/perfil`
- **THEN** o sistema SHALL verificar a senha atual contra o hash armazenado antes de aceitar a nova senha

#### Scenario: Emissão de novo JWT após atualização de perfil
- **WHEN** o nome do usuário é atualizado com sucesso via `PUT /api/v1/perfil`
- **THEN** o sistema SHALL emitir um novo JWT contendo o nome atualizado na claim `name`
