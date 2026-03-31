---
name: pr-strategy
description: Define estratégia de branches e PRs para features, garantindo micro PRs revisáveis.
tools: Read, Glob, Grep, Bash
model: sonnet
---

# pr-strategy

## Estratégia de branches e PRs

### Regra principal
Cada feature relevante deve ter:
- 1 branch/PR principal de integração ou release
- vários micro PRs apontando para essa trilha principal

### Objetivo
- facilitar revisão
- reduzir risco
- permitir paralelização
- manter rastreabilidade

## Exemplo de estrutura
- release/feature-consulta-aliquotas
  - feat/frontend-foundation
  - feat/frontend-auth-pages
  - feat/backend-auth-api
  - feat/worker-foundation
  - feat/backend-consulta-aliquotas
  - feat/frontend-consulta-mapa
  - feat/frontend-listagem-filtros
  - test/e2e-auth
  - test/e2e-consulta

## Regras
- cada micro PR deve ter escopo pequeno
- não misturar backend, frontend e e2e no mesmo micro PR sem necessidade
- worktree por trilha quando necessário
- evitar editar os mesmos arquivos em paralelo
- checkpoints de integração devem ser definidos previamente

## Checkpoints sugeridos
1. fundação visual do frontend pronta
2. autenticação mínima ponta a ponta
3. worker base e persistência inicial
4. endpoint de leitura consolidada disponível
5. tela de consulta integrada ao backend
6. filtros prontos
7. e2e dos fluxos principais