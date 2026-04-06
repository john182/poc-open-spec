## 1. Backend — Entidade e Repositório

- [x] 1.1 Adicionar métodos `AtualizarNome(string nome)` e `AtualizarSenha(string novoHash)` na entidade `User.cs` com validação de nome não-vazio
- [x] 1.2 Adicionar método `AtualizarAsync(User user)` na interface `IUserRepository` e implementar em `UserRepository` (update de Nome e PasswordHash no MongoDB)
- [x] 1.3 Criar testes unitários para `AtualizarNome` e `AtualizarSenha` (cenários de sucesso e validação)
- [x] 1.4 Criar teste de integração para `AtualizarAsync` no `UserRepository`

## 2. Backend — DTOs e Validadores

- [x] 2.1 Criar DTOs: `PerfilResponse` (id, nome, email) e `AtualizarPerfilRequest` (nome, senhaAtual?, novaSenha?)
- [x] 2.2 Criar validador FluentValidation para `AtualizarPerfilRequest` (nome obrigatório, novaSenha mínimo 8 chars se preenchida, senhaAtual obrigatória se novaSenha preenchida)
- [x] 2.3 Criar testes unitários para o validador

## 3. Backend — Controller de Perfil

- [x] 3.1 Criar `PerfilController` com `[Authorize]` e endpoints `GET /api/v1/perfil` e `PUT /api/v1/perfil`
- [x] 3.2 Implementar `GET` — extrair userId do JWT, buscar usuário, retornar `PerfilResponse`
- [x] 3.3 Implementar `PUT` — validar request, verificar senha atual se necessário (BCrypt.Verify), atualizar entidade, salvar, emitir novo JWT, retornar response com accessToken
- [x] 3.4 Criar testes unitários para o `PerfilController` (obter perfil, atualizar nome, atualizar nome+senha, senha incorreta, validações)

## 4. Frontend — Serviço de Perfil

- [x] 4.1 Criar `PerfilService` com métodos `obterPerfil()` e `atualizarPerfil(dados)` usando HttpClient
- [x] 4.2 Adicionar método no `AuthService` para atualizar token local e re-decodificar claims (`atualizarToken(novoToken)`)
- [x] 4.3 Criar testes unitários para `PerfilService` e para o novo método do `AuthService`

## 5. Frontend — Componente de Perfil

- [x] 5.1 Criar `PerfilComponent` com formulário reativo: campo nome (editável), campo email (readonly), seção de senha opcional (senhaAtual, novaSenha)
- [x] 5.2 Implementar lógica de submit: chamar `PerfilService.atualizarPerfil()`, atualizar token via `AuthService`, exibir toast de sucesso/erro
- [x] 5.3 Adicionar rota `/perfil` em `app.routes.ts` protegida por AuthGuard
- [x] 5.4 Criar testes unitários para `PerfilComponent` (renderização, validação, submit com sucesso, tratamento de erros)

## 6. Frontend — Dropdown da Topbar

- [x] 6.1 Modificar `app-topbar.component` para adicionar Popover do PrimeNG no ícone/nome do usuário
- [x] 6.2 Implementar itens do dropdown: "Meu Perfil" (routerLink `/perfil`) e "Sair" (executa logout)
- [x] 6.3 Criar testes unitários para o comportamento do dropdown na topbar

## 7. Testes E2E

- [x] 7.1 Criar spec Cypress para fluxo completo: login → dropdown topbar → navegar para perfil → editar nome → verificar topbar atualizada
- [x] 7.2 Criar cenários E2E para alteração de senha (sucesso e senha incorreta)

## 8. Documentação

- [x] 8.1 Atualizar `docs/api-contracts.md` com os novos endpoints `GET /api/v1/perfil` e `PUT /api/v1/perfil`
