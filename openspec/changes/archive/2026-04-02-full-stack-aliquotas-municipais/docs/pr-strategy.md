# Estrategia de PRs e Branches - Mapa Tributario

## Convencao de Branches

| Tipo | Padrao | Exemplo | Destino |
|------|--------|---------|---------|
| Principal | `main` | `main` | Producao |
| Release | `release/v{versao}-{feature}` | `release/v1.0-aliquotas-municipais` | `main` |
| Feature | `feature/{escopo}` | `feature/backend-auth` | Release branch |
| Fix | `fix/{descricao}` | `fix/token-refresh` | Release branch |
| Chore | `chore/{descricao}` | `chore/update-deps` | Release branch |

---

## Estrutura de PRs

### PR Principal (Release Branch)

Cada feature relevante possui uma unica PR principal que aponta para `main`. Esta PR consolida todos os micro PRs e representa a entrega completa da feature.

```
release/v1.0-aliquotas-municipais --> main (PR principal #1)
```

### Micro PRs (Feature Branches)

Cada micro PR aponta para o release branch, nunca diretamente para `main`. Cada um deve ser revisavel isoladamente e focado em uma parte clara do trabalho.

```
release/v1.0-aliquotas-municipais (PR principal --> main)
|
|-- feature/infra-docker          (micro PR --> release)
|-- feature/backend-auth          (micro PR --> release)
|-- feature/backend-consulta      (micro PR --> release)
|-- feature/worker-base           (micro PR --> release)
|-- feature/frontend-foundation   (micro PR --> release)
|-- feature/frontend-auth         (micro PR --> release)
|-- feature/frontend-consulta     (micro PR --> release)
|-- feature/e2e-tests             (micro PR --> release)
```

---

## Exemplo Concreto de Organizacao

### PR Principal

| PR | Branch | Base | Descricao |
|----|--------|------|-----------|
| #1 | `release/v1.0-aliquotas-municipais` | `main` | Release completa: consulta de aliquotas municipais |

### Micro PRs

| PR | Branch | Base | Trilha | Descricao |
|----|--------|------|--------|-----------|
| #2 | `feature/infra-docker` | release | Infra | Docker Compose, Dockerfiles, CI base |
| #3 | `feature/backend-auth` | release | Backend | Autenticacao JWT, endpoints de auth |
| #4 | `feature/backend-consulta` | release | Backend | Endpoints de consulta de aliquotas |
| #5 | `feature/worker-base` | release | Worker | Crawler base, estrategia de coleta |
| #6 | `feature/frontend-foundation` | release | Frontend | Layout, design system, tokens, componentes base |
| #7 | `feature/frontend-auth` | release | Frontend | Paginas de login, registro, guards |
| #8 | `feature/frontend-consulta` | release | Frontend | Mapa, selecao estado/municipio, listagem |
| #9 | `feature/e2e-tests` | release | E2E | Testes Cypress para fluxos criticos |

---

## Worktrees por Trilha

Cada trilha de trabalho deve usar um worktree separado para evitar conflitos e permitir paralelizacao segura.

```bash
# Criar worktrees por trilha
git worktree add ../wt-backend   feature/backend-auth
git worktree add ../wt-frontend  feature/frontend-foundation
git worktree add ../wt-worker    feature/worker-base
git worktree add ../wt-e2e       feature/e2e-tests
```

### Mapa de Worktrees

| Worktree | Trilha | Pastas Principais |
|----------|--------|-------------------|
| `wt-backend` | Backend | `backend/` |
| `wt-frontend` | Frontend | `frontend/` |
| `wt-worker` | Worker | `backend/Worker/` ou `worker/` |
| `wt-e2e` | E2E | `e2e/` ou `cypress/` |

---

## Regras de Merge

### Micro PR --> Release Branch

- **Estrategia:** Squash merge
- **Motivo:** Manter historico limpo no release branch, cada micro PR vira um unico commit
- **Quem aprova:** Pelo menos 1 revisor ou validacao automatizada

### Release Branch --> Main

- **Estrategia:** Merge commit (no fast-forward)
- **Motivo:** Preservar a historia de que houve uma release consolidada
- **Quem aprova:** Validacao completa (testes, review, CI green)

```bash
# Merge de micro PR (squash)
gh pr merge #3 --squash

# Merge de release para main (merge commit)
gh pr merge #1 --merge
```

---

## Convencao de Commits

Adotar [Conventional Commits](https://www.conventionalcommits.org/) em todos os commits.

### Formato

```
<tipo>(<escopo>): <descricao curta>

[corpo opcional]

[rodape opcional]
```

### Tipos Permitidos

| Tipo | Uso |
|------|-----|
| `feat` | Nova funcionalidade |
| `fix` | Correcao de bug |
| `chore` | Tarefas de manutencao, configuracao, infra |
| `docs` | Alteracao apenas em documentacao |
| `test` | Adicao ou correcao de testes |
| `refactor` | Refatoracao sem alterar comportamento |
| `style` | Formatacao, espacos, ponto-e-virgula |
| `ci` | Alteracoes em CI/CD |

### Escopos Recomendados

| Escopo | Exemplo |
|--------|---------|
| `backend` | `feat(backend): adicionar endpoint de consulta por municipio` |
| `frontend` | `feat(frontend): criar componente EmptyState` |
| `worker` | `feat(worker): implementar retry com backoff exponencial` |
| `e2e` | `test(e2e): cobrir fluxo de login e logout` |
| `infra` | `chore(infra): adicionar docker compose para MongoDB` |
| `docs` | `docs(api): atualizar contrato de consulta` |

---

## Checkpoints de Integracao

Os checkpoints definem momentos em que as trilhas devem sincronizar para garantir que a integracao entre frontend, backend, worker e e2e esteja funcionando.

### CP1 - Infraestrutura Base

**Quando:** Apos `feature/infra-docker` ser mergeado no release branch.

**Validacao:**
- [ ] Docker Compose sobe todos os servicos (API, MongoDB, frontend)
- [ ] Health check da API responde OK
- [ ] Frontend build funciona
- [ ] MongoDB acessivel

**Trilhas liberadas apos CP1:** Backend, Worker, Frontend (foundation)

### CP2 - Backend Auth + Frontend Foundation

**Quando:** Apos `feature/backend-auth` e `feature/frontend-foundation` serem mergeados.

**Validacao:**
- [ ] Endpoints de auth funcionam (register, login, refresh)
- [ ] Layout base do frontend renderiza corretamente
- [ ] Design system e tokens aplicados
- [ ] Componentes base criados e testados

**Trilhas liberadas apos CP2:** Frontend Auth, Worker Base

### CP3 - Autenticacao Integrada

**Quando:** Apos `feature/frontend-auth` ser mergeado.

**Validacao:**
- [ ] Login/registro funciona end-to-end
- [ ] Guards de rota protegem paginas autenticadas
- [ ] Token JWT e enviado nas requisicoes
- [ ] Interceptor de auth funciona

**Trilhas liberadas apos CP3:** Frontend Consulta, E2E (auth tests)

### CP4 - Consulta Integrada

**Quando:** Apos `feature/backend-consulta`, `feature/worker-base` e `feature/frontend-consulta` serem mergeados.

**Validacao:**
- [ ] Worker popula dados no MongoDB
- [ ] Endpoints de consulta retornam dados reais
- [ ] Frontend exibe mapa, estados, municipios e aliquotas
- [ ] Filtros funcionam

**Trilhas liberadas apos CP4:** E2E (consulta tests)

### CP5 - Release Candidate

**Quando:** Todos os micro PRs mergeados no release branch.

**Validacao:**
- [ ] Todos os testes E2E passam
- [ ] Todos os testes unitarios passam
- [ ] Todos os testes de integracao passam
- [ ] Docker Compose sobe o ambiente completo
- [ ] Documentacao atualizada
- [ ] Review final da PR principal

**Acao:** Merge do release branch para `main`.

---

## Paralelizacao Segura

### Trilhas que podem rodar em paralelo

| Combinacao | Risco de Colisao | Observacao |
|------------|------------------|------------|
| Backend + Frontend | Baixo | Pastas completamente separadas |
| Backend + Worker | Medio | Compartilham modelos de dominio e acesso ao MongoDB |
| Backend + E2E | Baixo | E2E depende de endpoints prontos, mas nao edita backend |
| Frontend + E2E | Baixo | E2E depende de UI pronta, mas nao edita frontend |
| Frontend + Worker | Baixo | Nenhum arquivo compartilhado |
| Worker + E2E | Baixo | Sem sobreposicao |

### Regras de Paralelizacao

1. **Nunca editar o mesmo arquivo em duas trilhas ao mesmo tempo**
2. **Contratos de API devem ser definidos antes de paralelizar backend e frontend**
3. **Worker e backend devem alinhar modelos de dominio antes de paralelizar**
4. **E2E so inicia apos os endpoints e telas que vai testar estarem no release branch**

---

## Prevencao de Conflitos (Ownership por Arquivo)

### Mapa de Ownership

| Pasta / Arquivo | Trilha Responsavel |
|-----------------|--------------------|
| `backend/API/` | Backend |
| `backend/Domain/` | Backend (compartilhado com Worker) |
| `backend/Infrastructure/` | Backend |
| `backend/Worker/` ou `worker/` | Worker |
| `frontend/src/app/layout/` | Frontend |
| `frontend/src/app/pages/` | Frontend |
| `frontend/src/app/shared/` | Frontend |
| `e2e/` ou `cypress/` | E2E |
| `docker-compose*.yml` | Infra (merge manual se conflito) |
| `openspec/` | Documentacao (qualquer trilha pode atualizar) |

### Regras de Ownership

- Cada trilha e responsavel por seus arquivos
- Arquivos compartilhados (`docker-compose.yml`, modelos de dominio) requerem alinhamento antes de editar
- Se duas trilhas precisam alterar o mesmo arquivo, uma deve esperar a outra mergear primeiro
- Documentacao (`openspec/`) pode ser atualizada por qualquer trilha, mas cada uma atualiza apenas sua parte

---

## Review Checklist

Antes de aprovar qualquer micro PR, verificar:

### Geral
- [ ] PR esta pequeno e focado (maximo ~400 linhas de codigo novo)
- [ ] Titulo e descricao seguem convencao
- [ ] Branch aponta para o destino correto (release, nao main)
- [ ] Nao ha arquivos acidentais (node_modules, bin, .env)

### Codigo
- [ ] Nomes semanticos e claros
- [ ] Sem duplicacao desnecessaria
- [ ] Responsabilidades bem separadas
- [ ] Sem logica de negocio fora da camada correta

### Testes
- [ ] Testes unitarios para logica nova
- [ ] Testes de integracao para endpoints novos
- [ ] Nenhum teste quebrado

### Documentacao
- [ ] Contrato de API atualizado se houve mudanca de endpoint
- [ ] Doc tecnica atualizada se houve decisao arquitetural
- [ ] Tasks/backlog atualizado se a execucao ainda esta em curso

### Integracao
- [ ] CI passou (build, lint, testes)
- [ ] Docker Compose continua subindo corretamente
- [ ] Nao introduz conflito com outros micro PRs no release branch
