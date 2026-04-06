## Why

Atualmente o sistema não oferece nenhuma forma para o usuário autenticado visualizar ou editar seus dados de perfil (nome, senha). O ícone de usuário na topbar é estático e não oferece interação além do logout. Isso impede que usuários corrijam seu nome ou alterem sua senha sem intervenção direta no banco de dados.

## What Changes

- Adicionar menu dropdown (OverlayPanel) no ícone do usuário na topbar com opções "Meu Perfil" e "Sair"
- Criar página de perfil (`/perfil`) com formulário para visualização e edição dos dados do usuário
  - Nome: editável
  - Email: somente leitura (exibido para informação)
  - Alteração de senha: opcional — campos aparecem mas só são processados se preenchidos
  - Exige senha atual para alterar a senha
- Criar endpoints REST no backend para obter e atualizar perfil do usuário autenticado
- Após atualização do nome, o backend emite novo JWT para que a topbar reflita a mudança imediatamente
- Adicionar métodos de domínio na entidade `User` para atualização de nome e senha

## Capabilities

### New Capabilities
- `perfil-usuario`: Capacidade de visualizar e editar perfil do usuário autenticado (nome e senha), incluindo endpoints backend, página frontend e interação via topbar

### Modified Capabilities
- `user-auth`: Adiciona requisito de emissão de novo JWT após atualização de nome, e adiciona métodos de mutação na entidade User (AtualizarNome, AtualizarSenha)
- `frontend-foundation`: Adiciona requisito de interação no ícone do usuário na topbar (dropdown com navegação para perfil e logout)

## Impact

- **Backend**:
  - `User.cs` — novos métodos de domínio (`AtualizarNome`, `AtualizarSenha`)
  - `IUserRepository` / `UserRepository` — novo método `AtualizarAsync`
  - Novo `PerfilController` com endpoints `GET /api/v1/perfil` e `PUT /api/v1/perfil`
  - Novos DTOs: `PerfilResponse`, `AtualizarPerfilRequest`
  - Novos validadores para os DTOs
  - Reutiliza `JwtTokenGenerator` para emitir novo token após atualização
- **Frontend**:
  - `app-topbar.component` — adicionar OverlayPanel com dropdown de opções
  - Novo `PerfilComponent` — página de edição de perfil
  - Novo `PerfilService` — chamadas HTTP para os endpoints de perfil
  - Nova rota `/perfil` em `app.routes.ts`
- **Testes**:
  - Testes unitários backend para novos métodos de domínio, controller, e validadores
  - Testes unitários frontend para componentes e serviço
  - Testes E2E Cypress para fluxo completo de edição de perfil
- **Documentação**:
  - `docs/api-contracts.md` — documentar novos endpoints de perfil
