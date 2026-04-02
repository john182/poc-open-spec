# Fundacao do Frontend - Mapa Tributario

## Analise do Template Sakai

O template Sakai do PrimeNG serve como base para o frontend. Cada componente foi avaliado individualmente para decidir entre reutilizar, adaptar ou descartar.

### Decisao por Componente

| Componente | Decisao | Justificativa |
|------------|---------|---------------|
| `AppLayout` | Adaptar | Estrutura solida com sidebar + topbar + content area. Remover configurador de tema e simplificar slots. |
| `AppTopbar` | Adaptar | Manter logo, toggle do menu lateral e dark mode. Remover seletor de preset e temas. Adicionar informacoes do usuario logado. |
| `AppSidebar` | Adaptar | Manter estrutura de sidebar colapsavel. Simplificar items para o contexto do Mapa Tributario. |
| `AppMenu` | Adaptar | Novo model com items especificos: Home, Consulta (Mapa, Estados, Municipios), Configuracoes. |
| `AppMenuitem` | Reutilizar | Componente recursivo funcional, nao requer alteracao significativa. |
| `AppFooter` | Reutilizar | Ajustar apenas o texto para "Mapa Tributario" e ano corrente. |
| `LayoutService` | Adaptar | Manter signals de estado do layout (sidebar aberta/fechada, dark mode). Remover configuracao de preset e escala de tema. |
| `AppConfigurator` | Descartar | Configurador de tema/preset nao e necessario no MVP. O tema sera fixo. |
| `AppFloatingConfigurator` | Descartar | Mesmo motivo do AppConfigurator. Botao flutuante de configuracao nao se aplica. |
| Login page | Adaptar | Base visual boa. Adicionar campos de registro, mensagens de erro, link para cadastro. Ajustar branding. |
| Error page (404) | Adaptar | Ajustar texto e navegacao. Manter layout visual. Adicionar botao de voltar para home. |
| Access page (403) | Adaptar | Ajustar texto para contexto de acesso negado. Manter layout visual. |
| Dashboard | Descartar | Nao ha dashboard no Mapa Tributario. A home sera o mapa de consulta. |
| Landing page | Descartar | Nao ha landing page publica no MVP. |
| UIKit demos | Descartar | Paginas de demonstracao de componentes nao fazem parte do produto. |

---

## Ordem Obrigatoria de Implementacao

A fundacao do frontend deve seguir esta sequencia. Nenhum passo pode ser pulado ou invertido.

### Passo 1 - Analisar o template de referencia

- Clonar o template Sakai
- Identificar componentes, servicos e estrutura de pastas
- Mapear dependencias entre componentes
- Documentar o que sera usado, adaptado e descartado (tabela acima)

### Passo 2 - Configurar o projeto Angular

- Criar projeto Angular 21 com standalone components
- Configurar PrimeNG como design system base
- Configurar Vitest para testes unitarios
- Configurar ESLint e Prettier
- Configurar paths de importacao (`@app/`, `@shared/`, `@core/`, `@layout/`)

### Passo 3 - Definir design tokens

- Definir paleta de cores do Mapa Tributario
- Definir tokens de espacamento, tipografia e bordas
- Configurar tema PrimeNG customizado com os tokens
- Definir variaveis CSS para dark mode

```
Tokens principais:
--mt-primary:         #1976D2  (azul institucional)
--mt-primary-light:   #42A5F5
--mt-primary-dark:    #1565C0
--mt-accent:          #FF9800  (destaque, alertas)
--mt-success:         #4CAF50
--mt-danger:          #F44336
--mt-warning:         #FFC107
--mt-text:            #212121
--mt-text-secondary:  #757575
--mt-background:      #FAFAFA
--mt-surface:         #FFFFFF
--mt-border:          #E0E0E0
--mt-radius-sm:       4px
--mt-radius-md:       8px
--mt-radius-lg:       12px
--mt-spacing-xs:      4px
--mt-spacing-sm:      8px
--mt-spacing-md:      16px
--mt-spacing-lg:      24px
--mt-spacing-xl:      32px
```

### Passo 4 - Construir layout base

- Adaptar `AppLayout` do Sakai
- Adaptar `AppTopbar` (logo, toggle, dark mode, usuario)
- Adaptar `AppSidebar` e `AppMenu` com items do Mapa Tributario
- Reutilizar `AppMenuitem` e `AppFooter`
- Adaptar `LayoutService` (remover preset config)
- Testar layout com conteudo placeholder

### Passo 5 - Definir design system (componentes PrimeNG)

- Configurar tema PrimeNG com os design tokens
- Definir padroes de uso para:
  - Botoes (p-button): primario, secundario, texto, perigo
  - Inputs (p-inputText, p-dropdown, p-calendar)
  - Tabelas (p-table): paginacao, ordenacao, filtro
  - Cards (p-card)
  - Dialogs (p-dialog)
  - Toasts (p-toast): sucesso, erro, aviso
  - Loading (p-progressSpinner)
- Documentar padroes de uso para consistencia

### Passo 6 - Criar componentes base reutilizaveis

Componentes que serao usados em multiplas paginas:

#### LoadingSpinner

```typescript
// Inputs: message?: string, overlay?: boolean
// Exibe spinner centralizado com mensagem opcional
// Modo overlay cobre o container pai
```

#### EmptyState

```typescript
// Inputs: icon: string, title: string, message: string, actionLabel?: string
// Output: action: EventEmitter<void>
// Exibe estado vazio com icone, titulo, mensagem e acao opcional
```

#### ErrorState

```typescript
// Inputs: title: string, message: string, retryLabel?: string
// Output: retry: EventEmitter<void>
// Exibe estado de erro com opcao de tentar novamente
```

#### PageHeader

```typescript
// Inputs: title: string, subtitle?: string, breadcrumbs?: Breadcrumb[]
// Content projection para acoes (botoes, filtros)
```

#### FilterBar

```typescript
// Inputs: filters: FilterConfig[]
// Output: filterChange: EventEmitter<FilterValues>
// Barra de filtros configuravel, emite evento quando filtros mudam
```

#### FormField

```typescript
// Inputs: label: string, controlName: string, required?: boolean, errorMessages?: Record<string, string>
// Content projection para o input
// Exibe label, input e mensagem de erro automaticamente
```

### Passo 7 - Criar form helpers

- Wrapper de campo de formulario com exibicao automatica de erros
- Mapeamento padrao de mensagens de validacao

```typescript
// Mapeamento padrao de mensagens
const VALIDATION_MESSAGES: Record<string, (params?: any) => string> = {
  required: () => 'Campo obrigatorio',
  email: () => 'E-mail invalido',
  minlength: (p) => `Minimo de ${p.requiredLength} caracteres`,
  maxlength: (p) => `Maximo de ${p.requiredLength} caracteres`,
  pattern: () => 'Formato invalido',
  min: (p) => `Valor minimo: ${p.min}`,
  max: (p) => `Valor maximo: ${p.max}`,
};
```

- Helper para criar FormGroups com tipagem

### Passo 8 - Implementar pagina de Sign In

- Formulario com email e senha
- Validacao de campos obrigatorios
- Mensagem de erro para credenciais invalidas
- Link para pagina de cadastro
- Integracao com `AuthService`
- Loading state durante autenticacao
- Redirecionar para home apos login

### Passo 9 - Implementar pagina de Sign Up

- Formulario com nome, email, senha e confirmacao de senha
- Validacao de campos (email valido, senha forte, senhas iguais)
- Mensagem de erro para email duplicado
- Link para pagina de login
- Integracao com `AuthService`
- Redirecionar para login apos registro

### Passo 10 - Implementar pagina de Acesso Negado (403)

- Layout visual adaptado do template Sakai
- Mensagem clara de acesso negado
- Botao para voltar a home
- Rota: `/access`

### Passo 11 - Implementar pagina 404

- Layout visual adaptado do template Sakai
- Mensagem de pagina nao encontrada
- Botao para voltar a home
- Wildcard route no router

### Passo 12 - Validar fundacao completa

Criterios de aceite antes de iniciar a feature de consulta:

- [ ] Layout base renderiza corretamente (sidebar, topbar, content, footer)
- [ ] Dark mode funciona
- [ ] Menu lateral navega entre rotas
- [ ] Design tokens aplicados em todos os componentes
- [ ] Componentes base criados e testados
- [ ] Form helpers funcionam com validacao
- [ ] Sign in funciona (ou mock se backend nao esta pronto)
- [ ] Sign up funciona (ou mock)
- [ ] Pagina 403 renderiza
- [ ] Pagina 404 renderiza
- [ ] Testes unitarios dos componentes base passam
- [ ] Build de producao funciona sem erros

---

## Estrutura de Pastas do Frontend

```
frontend/
  src/
    app/
      core/                          # Singleton services, guards, interceptors
        guards/
          auth.guard.ts
        interceptors/
          auth.interceptor.ts
          error.interceptor.ts
        services/
          auth.service.ts
          consulta.service.ts
          storage.service.ts
        models/
          user.model.ts
          estado.model.ts
          municipio.model.ts
          servico.model.ts
          aliquota.model.ts
          api-response.model.ts

      layout/                        # Shell da aplicacao
        components/
          app-layout/
            app-layout.component.ts
          app-topbar/
            app-topbar.component.ts
          app-sidebar/
            app-sidebar.component.ts
          app-menu/
            app-menu.component.ts
          app-menuitem/
            app-menuitem.component.ts
          app-footer/
            app-footer.component.ts
        services/
          layout.service.ts
          menu.service.ts

      shared/                        # Componentes e utilitarios reutilizaveis
        components/
          loading-spinner/
            loading-spinner.component.ts
            loading-spinner.component.spec.ts
          empty-state/
            empty-state.component.ts
            empty-state.component.spec.ts
          error-state/
            error-state.component.ts
            error-state.component.spec.ts
          page-header/
            page-header.component.ts
            page-header.component.spec.ts
          filter-bar/
            filter-bar.component.ts
            filter-bar.component.spec.ts
          form-field/
            form-field.component.ts
            form-field.component.spec.ts
        helpers/
          form.helpers.ts
          form.helpers.spec.ts
          validation-messages.ts
        pipes/
          aliquota-format.pipe.ts
          codigo-municipio.pipe.ts

      pages/                         # Paginas da aplicacao
        auth/
          sign-in/
            sign-in.component.ts
            sign-in.component.spec.ts
          sign-up/
            sign-up.component.ts
            sign-up.component.spec.ts
        consulta/
          mapa/
            mapa.component.ts
            mapa.component.spec.ts
          estado/
            estado.component.ts
            estado.component.spec.ts
          municipio/
            municipio.component.ts
            municipio.component.spec.ts
        errors/
          not-found/
            not-found.component.ts
          access-denied/
            access-denied.component.ts

    assets/
      images/
        logo.svg
        mapa-brasil.svg
      styles/
        _tokens.scss
        _theme.scss
        _layout.scss
        styles.scss

    environments/
      environment.ts
      environment.development.ts
```

---

## Componentes Base - Detalhamento

### LoadingSpinner

**Responsabilidade:** Exibir indicador de carregamento.

**API:**
| Input | Tipo | Default | Descricao |
|-------|------|---------|-----------|
| `message` | `string` | `'Carregando...'` | Mensagem exibida abaixo do spinner |
| `overlay` | `boolean` | `false` | Se true, exibe overlay sobre o container pai |
| `size` | `'sm' \| 'md' \| 'lg'` | `'md'` | Tamanho do spinner |

### EmptyState

**Responsabilidade:** Exibir estado vazio com orientacao ao usuario.

**API:**
| Input | Tipo | Default | Descricao |
|-------|------|---------|-----------|
| `icon` | `string` | `'pi pi-inbox'` | Classe do icone PrimeIcons |
| `title` | `string` | obrigatorio | Titulo principal |
| `message` | `string` | obrigatorio | Mensagem descritiva |
| `actionLabel` | `string` | `undefined` | Label do botao de acao (se presente) |

| Output | Tipo | Descricao |
|--------|------|-----------|
| `action` | `EventEmitter<void>` | Emitido quando o botao de acao e clicado |

### ErrorState

**Responsabilidade:** Exibir estado de erro com opcao de retry.

**API:**
| Input | Tipo | Default | Descricao |
|-------|------|---------|-----------|
| `title` | `string` | `'Erro inesperado'` | Titulo do erro |
| `message` | `string` | `'Tente novamente mais tarde.'` | Mensagem descritiva |
| `retryLabel` | `string` | `'Tentar novamente'` | Label do botao de retry |
| `showRetry` | `boolean` | `true` | Se true, exibe botao de retry |

| Output | Tipo | Descricao |
|--------|------|-----------|
| `retry` | `EventEmitter<void>` | Emitido quando o botao de retry e clicado |

### PageHeader

**Responsabilidade:** Cabecalho padronizado de pagina com breadcrumb.

**API:**
| Input | Tipo | Default | Descricao |
|-------|------|---------|-----------|
| `title` | `string` | obrigatorio | Titulo da pagina |
| `subtitle` | `string` | `undefined` | Subtitulo opcional |
| `breadcrumbs` | `Breadcrumb[]` | `[]` | Lista de breadcrumbs |

Content projection: `<ng-content select="[actions]">` para botoes e acoes no cabecalho.

### FilterBar

**Responsabilidade:** Barra de filtros configuravel para listagens.

**API:**
| Input | Tipo | Default | Descricao |
|-------|------|---------|-----------|
| `filters` | `FilterConfig[]` | `[]` | Configuracao dos filtros |
| `loading` | `boolean` | `false` | Desabilita filtros durante loading |

| Output | Tipo | Descricao |
|--------|------|-----------|
| `filterChange` | `EventEmitter<FilterValues>` | Emitido quando qualquer filtro muda |
| `clear` | `EventEmitter<void>` | Emitido quando limpar filtros e clicado |

```typescript
interface FilterConfig {
  key: string;
  label: string;
  type: 'text' | 'dropdown' | 'number-range';
  options?: { label: string; value: any }[];  // para dropdown
  placeholder?: string;
}
```

### FormField

**Responsabilidade:** Wrapper de campo de formulario com label e erro automatico.

**API:**
| Input | Tipo | Default | Descricao |
|-------|------|---------|-----------|
| `label` | `string` | obrigatorio | Label do campo |
| `controlName` | `string` | obrigatorio | Nome do FormControl no FormGroup pai |
| `required` | `boolean` | `false` | Exibe asterisco no label |
| `hint` | `string` | `undefined` | Texto de ajuda abaixo do campo |
| `errorMessages` | `Record<string, string>` | `{}` | Mensagens customizadas (sobrescreve padrao) |

Content projection: `<ng-content>` para o input/dropdown/etc.

---

## Form Helpers

### Validation Messages

```typescript
// shared/helpers/validation-messages.ts

export const DEFAULT_VALIDATION_MESSAGES: Record<string, (params?: any) => string> = {
  required: () => 'Campo obrigatorio',
  email: () => 'E-mail invalido',
  minlength: (p) => `Minimo de ${p.requiredLength} caracteres`,
  maxlength: (p) => `Maximo de ${p.requiredLength} caracteres`,
  pattern: () => 'Formato invalido',
  min: (p) => `Valor minimo: ${p.min}`,
  max: (p) => `Valor maximo: ${p.max}`,
  passwordMismatch: () => 'As senhas nao coincidem',
};

export function getValidationMessage(
  errorKey: string,
  errorParams?: any,
  customMessages?: Record<string, string>
): string {
  if (customMessages?.[errorKey]) {
    return customMessages[errorKey];
  }
  const messageFn = DEFAULT_VALIDATION_MESSAGES[errorKey];
  return messageFn ? messageFn(errorParams) : 'Campo invalido';
}
```

### Form Builder Helper

```typescript
// shared/helpers/form.helpers.ts

export function markFormAsTouched(form: FormGroup): void {
  Object.keys(form.controls).forEach(key => {
    form.get(key)?.markAsTouched();
  });
}

export function isFieldInvalid(form: FormGroup, field: string): boolean {
  const control = form.get(field);
  return !!control && control.invalid && (control.dirty || control.touched);
}

export function getFieldErrors(form: FormGroup, field: string): string[] {
  const control = form.get(field);
  if (!control || !control.errors) return [];
  return Object.keys(control.errors).map(key =>
    getValidationMessage(key, control.errors?.[key])
  );
}
```

---

## Estrategia de Roteamento

### Lazy Loading

Todas as paginas devem usar lazy loading para otimizar o bundle inicial.

```typescript
// app.routes.ts

export const routes: Routes = [
  {
    path: '',
    component: AppLayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        redirectTo: 'consulta',
        pathMatch: 'full'
      },
      {
        path: 'consulta',
        loadChildren: () => import('./pages/consulta/consulta.routes')
          .then(m => m.CONSULTA_ROUTES)
      }
    ]
  },
  {
    path: 'auth',
    children: [
      {
        path: 'sign-in',
        loadComponent: () => import('./pages/auth/sign-in/sign-in.component')
          .then(m => m.SignInComponent)
      },
      {
        path: 'sign-up',
        loadComponent: () => import('./pages/auth/sign-up/sign-up.component')
          .then(m => m.SignUpComponent)
      }
    ]
  },
  {
    path: 'access',
    loadComponent: () => import('./pages/errors/access-denied/access-denied.component')
      .then(m => m.AccessDeniedComponent)
  },
  {
    path: '**',
    loadComponent: () => import('./pages/errors/not-found/not-found.component')
      .then(m => m.NotFoundComponent)
  }
];
```

### Rotas de Consulta

```typescript
// pages/consulta/consulta.routes.ts

export const CONSULTA_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'mapa',
    pathMatch: 'full'
  },
  {
    path: 'mapa',
    loadComponent: () => import('./mapa/mapa.component')
      .then(m => m.MapaComponent),
    data: { breadcrumb: 'Mapa' }
  },
  {
    path: 'estado/:uf',
    loadComponent: () => import('./estado/estado.component')
      .then(m => m.EstadoComponent),
    data: { breadcrumb: 'Estado' }
  },
  {
    path: 'municipio/:codigo',
    loadComponent: () => import('./municipio/municipio.component')
      .then(m => m.MunicipioComponent),
    data: { breadcrumb: 'Municipio' }
  }
];
```

### Guards

```typescript
// core/guards/auth.guard.ts

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/auth/sign-in'], {
    queryParams: { returnUrl: state.url }
  });
};
```

---

## Gerenciamento de Estado

### Angular Signals (sem biblioteca externa)

O projeto usa Angular Signals como mecanismo de estado reativo. Nenhuma biblioteca externa de state management (NgRx, NGXS, etc.) e necessaria para o escopo do MVP.

### Onde o estado vive

| Estado | Responsavel | Tipo |
|--------|-------------|------|
| Autenticacao (usuario, token) | `AuthService` | Signal global |
| Layout (sidebar, dark mode) | `LayoutService` | Signal global |
| Lista de estados | `ConsultaService` | Signal local ou cache |
| Municipios do estado selecionado | Componente `EstadoComponent` | Signal local |
| Servicos do municipio selecionado | Componente `MunicipioComponent` | Signal local |
| Filtros ativos | Componente da pagina | Signal local |

### Padrao de Service com Signals

```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  private currentUser = signal<User | null>(null);
  private token = signal<string | null>(null);

  // Computed signals (read-only para consumidores)
  readonly user = this.currentUser.asReadonly();
  readonly isAuthenticated = computed(() => !!this.token());
  readonly userName = computed(() => this.currentUser()?.name ?? '');

  login(credentials: LoginRequest): Observable<void> { /* ... */ }
  logout(): void { /* ... */ }
  register(data: RegisterRequest): Observable<void> { /* ... */ }
}
```

### Regras de Estado

1. **Signals globais** apenas para estado compartilhado entre multiplas paginas (auth, layout)
2. **Signals locais** para estado especifico de uma pagina ou componente
3. **Nao duplicar estado** - cada dado tem um unico dono
4. **Computed signals** para derivar estado, nunca armazenar dados derivados
5. **Effects** com cautela - preferir computed quando possivel
