## Context

O sistema MapaTributário possui autenticação JWT completa (registro, login, refresh token) mas não oferece funcionalidade de edição de perfil. A entidade `User` é imutável após criação (somente `Create`, `SetId`, `Deactivate`). O `IUserRepository` não possui método de atualização. A topbar exibe o nome do usuário e ícone de forma estática sem interação além do dark mode toggle.

Componentes existentes relevantes:
- `User.cs` — entidade de domínio com private setters
- `IUserRepository` / `UserRepository` — somente `CreateAsync`, `GetByEmailAsync`, `GetByIdAsync`
- `JwtTokenProvider` (implementa `ITokenProvider`) — já gera tokens com claims de nome, email, role
- `AuthController` — endpoints públicos de login/register/refresh
- `app-topbar.component` — layout estático com ícone de usuário
- `AuthService` (frontend) — decode JWT, signals para nome/email/role

## Goals / Non-Goals

**Goals:**
- Permitir que o usuário autenticado visualize e edite seu nome
- Permitir alteração opcional de senha com verificação da senha atual
- Fornecer dropdown interativo na topbar para acesso ao perfil
- Atualizar a topbar imediatamente após mudança de nome (via novo JWT)
- Manter consistência com os padrões existentes do projeto (controllers, DTOs, validadores)

**Non-Goals:**
- Edição de email (email é somente leitura)
- Upload de avatar ou foto de perfil
- Gestão de preferências ou configurações do usuário
- Administração de perfis de outros usuários
- Refresh token na resposta de atualização de perfil (somente accessToken)

## Decisions

### 1. Controller dedicado `PerfilController` vs endpoints no `AuthController`

**Decisão**: Criar `PerfilController` separado.

**Alternativa considerada**: Adicionar endpoints em `AuthController`. Rejeitada porque `AuthController` é público (sem `[Authorize]`), e os endpoints de perfil exigem autenticação. Separar mantém coesão — auth trata de autenticação, perfil trata de dados do usuário autenticado.

### 2. Endpoint único `PUT /api/v1/perfil` vs endpoints separados para nome e senha

**Decisão**: Endpoint único `PUT /api/v1/perfil` que trata nome e senha opcionalmente.

**Alternativa considerada**: `PUT /api/v1/perfil/nome` e `PUT /api/v1/perfil/senha` separados. Rejeitada porque o cenário mais comum é atualizar tudo de uma vez num único formulário, e o custo de dois endpoints para uma feature simples não se justifica.

**Contrato do request:**
```json
{
  "nome": "string (obrigatório)",
  "senhaAtual": "string (opcional — obrigatório somente se novaSenha preenchida)",
  "novaSenha": "string (opcional — mínimo 8 caracteres se preenchida)"
}
```

**Contrato do response (200):**
```json
{
  "id": "string",
  "nome": "string",
  "email": "string",
  "accessToken": "string"
}
```

### 3. Emissão de novo JWT na resposta

**Decisão**: O `PUT /api/v1/perfil` retorna um novo `accessToken` no corpo da resposta sempre que a atualização for bem-sucedida.

**Justificativa**: O nome do usuário está na claim do JWT. Sem novo token, a topbar e outros pontos que leem o nome do token ficariam desatualizados até o próximo refresh. Retornar o token no corpo da resposta é mais simples que forçar um refresh flow.

**Frontend**: Após receber a resposta, o `AuthService` atualiza o token no localStorage e re-decodifica as claims para atualizar os signals.

### 4. Métodos de domínio na entidade `User`

**Decisão**: Adicionar `AtualizarNome(string nome)` e `AtualizarSenha(string novoHash)` na entidade `User`.

**Justificativa**: Mantém a lógica de domínio na entidade (DDD lite, consistente com `Deactivate()`). `AtualizarNome` valida nome não-vazio. `AtualizarSenha` recebe hash pronto (o hashing é responsabilidade da camada de aplicação/controller, como já feito no registro).

### 5. OverlayPanel do PrimeNG para dropdown na topbar

**Decisão**: Usar `OverlayPanel` do PrimeNG para o dropdown de usuário na topbar.

**Alternativa considerada**: `Menu` ou `TieredMenu` do PrimeNG. Rejeitada porque OverlayPanel é mais flexível para layout customizado (pode conter qualquer template, não apenas itens de menu).

**Interação**: Clicar no ícone/nome do usuário abre o OverlayPanel com dois itens: "Meu Perfil" (navegação via routerLink) e "Sair" (executa `logout()`).

### 6. Verificação de senha no controller

**Decisão**: A verificação de `senhaAtual` contra o hash armazenado é feita no controller/action, usando `BCrypt.Net.BCrypt.Verify()` diretamente (mesmo padrão usado no login pelo `AuthController`).

**Justificativa**: Não há camada de application services no projeto — controllers fazem a orquestração diretamente. Manter consistência com o padrão existente.

### 7. Identificação do usuário no endpoint

**Decisão**: Extrair o ID do usuário autenticado via `User.FindFirst(ClaimTypes.NameIdentifier)` no controller (mesmo padrão do refresh token no `AuthController`).

**Justificativa**: Não depende de dados no body, usa a claim do JWT — seguro e consistente.

## Risks / Trade-offs

- **[Risco] Token antigo no localStorage após atualização** → Mitigação: Frontend atualiza imediatamente o token no localStorage e re-decodifica claims após resposta 200 do PUT.
- **[Risco] Concorrência na atualização** → Mitigação: Aceitável para POC. Última escrita vence. Não há cenário multi-sessão crítico.
- **[Trade-off] Sem refresh token na resposta de perfil** → Simplifica o fluxo. O refresh token existente continua válido. Se expirar, o flow normal de refresh resolve.
- **[Risco] OverlayPanel pode conflitar com o layout mobile** → Mitigação: Testar responsividade. OverlayPanel do PrimeNG já é responsivo por padrão.
