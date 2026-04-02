# Design Tokens - Mapa Tributario

> Definicao dos design tokens do projeto Mapa Tributario, implementados como CSS custom properties e integrados com PrimeNG 21 e Tailwind 4.

---

## 1. O que sao Design Tokens

Design tokens sao os valores atomicos do design system: cores, tipografia, espacamento, bordas e sombras representados como variaveis reutilizaveis. Eles servem como a **unica fonte de verdade** para o vocabulario visual da aplicacao.

### Por que usar design tokens

- **Consistencia**: todos os componentes referenciam os mesmos valores
- **Manutenibilidade**: mudar um token atualiza toda a aplicacao
- **Tematizacao**: dark mode e temas alternativos sao apenas sobreescrita de tokens
- **Documentacao viva**: os tokens servem como referencia do design system
- **Integracao**: PrimeNG e Tailwind podem consumir os mesmos tokens

### Como usar

Os tokens sao definidos como **CSS custom properties** no seletor `:root` e podem ser sobrescritos em contextos especificos (como `.app-dark` para dark mode). Componentes Angular referenciam estes tokens via CSS, e o Tailwind 4 pode ser configurado para utiliza-los.

---

## 2. Implementacao

### Arquivo de tokens

Os tokens devem ser definidos em um arquivo CSS dedicado, importado globalmente:

```
src/
  styles/
    _tokens.css       <-- definicao dos tokens
    styles.css         <-- import global (inclui _tokens.css)
```

### Estrutura base

```css
:root {
  /* Tokens definidos aqui */
}

.app-dark {
  /* Sobreescrita de tokens para dark mode */
}
```

---

## 3. Tokens de Cor

### 3.1 Cores Primarias

```css
:root {
  --color-primary: #10b981;           /* emerald-500 */
  --color-primary-hover: #059669;     /* emerald-600 */
  --color-primary-active: #047857;    /* emerald-700 */
  --color-primary-light: #d1fae5;     /* emerald-100 */
  --color-primary-subtle: #ecfdf5;    /* emerald-50 */
  --color-primary-text: #ffffff;      /* texto sobre primary */
}
```

### 3.2 Cores de Surface

Escala completa de surface baseada em slate, usada para backgrounds, bordas e textos neutros:

```css
:root {
  --color-surface-0: #ffffff;         /* branco puro */
  --color-surface-50: #f8fafc;        /* slate-50 */
  --color-surface-100: #f1f5f9;       /* slate-100 */
  --color-surface-200: #e2e8f0;       /* slate-200 */
  --color-surface-300: #cbd5e1;       /* slate-300 */
  --color-surface-400: #94a3b8;       /* slate-400 */
  --color-surface-500: #64748b;       /* slate-500 */
  --color-surface-600: #475569;       /* slate-600 */
  --color-surface-700: #334155;       /* slate-700 */
  --color-surface-800: #1e293b;       /* slate-800 */
  --color-surface-900: #0f172a;       /* slate-900 */
  --color-surface-950: #020617;       /* slate-950 */
}
```

### 3.3 Cores de Texto

```css
:root {
  --color-text-primary: #1e293b;      /* slate-800 - texto principal */
  --color-text-secondary: #475569;    /* slate-600 - texto secundario */
  --color-text-muted: #94a3b8;        /* slate-400 - texto desabilitado/muted */
}
```

### 3.4 Cores Semanticas

```css
:root {
  --color-success: #22c55e;           /* green-500 */
  --color-success-bg: #f0fdf4;        /* green-50 */
  --color-success-border: #bbf7d0;    /* green-200 */
  --color-success-text: #15803d;      /* green-700 */

  --color-warning: #f59e0b;           /* amber-500 */
  --color-warning-bg: #fffbeb;        /* amber-50 */
  --color-warning-border: #fde68a;    /* amber-200 */
  --color-warning-text: #b45309;      /* amber-700 */

  --color-danger: #ef4444;            /* red-500 */
  --color-danger-bg: #fef2f2;         /* red-50 */
  --color-danger-border: #fecaca;     /* red-200 */
  --color-danger-text: #b91c1c;       /* red-700 */

  --color-info: #3b82f6;              /* blue-500 */
  --color-info-bg: #eff6ff;           /* blue-50 */
  --color-info-border: #bfdbfe;       /* blue-200 */
  --color-info-text: #1d4ed8;         /* blue-700 */
}
```

### 3.5 Cor de Borda

```css
:root {
  --color-border: #e2e8f0;            /* slate-200 */
}
```

---

## 4. Tokens de Dark Mode

Sob a classe `.app-dark`, a escala de surface e invertida e as cores de texto sao ajustadas:

```css
.app-dark {
  /* Surface invertido */
  --color-surface-0: #0f172a;         /* slate-900 */
  --color-surface-50: #1e293b;        /* slate-800 */
  --color-surface-100: #334155;       /* slate-700 */
  --color-surface-200: #475569;       /* slate-600 */
  --color-surface-300: #64748b;       /* slate-500 */
  --color-surface-400: #94a3b8;       /* slate-400 */
  --color-surface-500: #cbd5e1;       /* slate-300 */
  --color-surface-600: #e2e8f0;       /* slate-200 */
  --color-surface-700: #f1f5f9;       /* slate-100 */
  --color-surface-800: #f8fafc;       /* slate-50 */
  --color-surface-900: #ffffff;       /* branco puro */
  --color-surface-950: #ffffff;       /* branco puro */

  /* Texto ajustado */
  --color-text-primary: #f1f5f9;      /* slate-100 */
  --color-text-secondary: #cbd5e1;    /* slate-300 */
  --color-text-muted: #64748b;        /* slate-500 */

  /* Borda ajustada */
  --color-border: #334155;            /* slate-700 */

  /* Primary mantida (boa legibilidade em dark) */
  --color-primary: #10b981;           /* emerald-500 */
  --color-primary-hover: #34d399;     /* emerald-400 (mais clara em dark) */
  --color-primary-active: #10b981;    /* emerald-500 */
  --color-primary-light: #064e3b;     /* emerald-900 */
  --color-primary-subtle: #022c22;    /* emerald-950 */

  /* Semanticas - backgrounds ajustados para dark */
  --color-success-bg: #052e16;        /* green-950 */
  --color-success-border: #166534;    /* green-800 */
  --color-success-text: #86efac;      /* green-300 */

  --color-warning-bg: #451a03;        /* amber-950 */
  --color-warning-border: #92400e;    /* amber-800 */
  --color-warning-text: #fcd34d;      /* amber-300 */

  --color-danger-bg: #450a0a;         /* red-950 */
  --color-danger-border: #991b1b;     /* red-800 */
  --color-danger-text: #fca5a5;       /* red-300 */

  --color-info-bg: #172554;           /* blue-950 */
  --color-info-border: #1e40af;       /* blue-800 */
  --color-info-text: #93c5fd;         /* blue-300 */
}
```

---

## 5. Tokens de Tipografia

### 5.1 Familia

```css
:root {
  --font-family: 'Inter', system-ui, -apple-system, sans-serif;
}
```

### 5.2 Tamanhos

```css
:root {
  --font-size-xs: 0.75rem;            /* 12px */
  --font-size-sm: 0.875rem;           /* 14px */
  --font-size-base: 1rem;             /* 16px */
  --font-size-lg: 1.125rem;           /* 18px */
  --font-size-xl: 1.25rem;            /* 20px */
  --font-size-2xl: 1.5rem;            /* 24px */
  --font-size-3xl: 1.875rem;          /* 30px */
}
```

### 5.3 Pesos

```css
:root {
  --font-weight-normal: 400;
  --font-weight-medium: 500;
  --font-weight-semibold: 600;
  --font-weight-bold: 700;
}
```

### 5.4 Alturas de Linha

```css
:root {
  --line-height-tight: 1.25;
  --line-height-normal: 1.5;
  --line-height-relaxed: 1.75;
}
```

---

## 6. Tokens de Espacamento

Escala de 4px como unidade base:

```css
:root {
  --spacing-1: 0.25rem;               /* 4px */
  --spacing-2: 0.5rem;                /* 8px */
  --spacing-3: 0.75rem;               /* 12px */
  --spacing-4: 1rem;                  /* 16px */
  --spacing-5: 1.25rem;               /* 20px */
  --spacing-6: 1.5rem;                /* 24px */
  --spacing-8: 2rem;                  /* 32px */
  --spacing-10: 2.5rem;               /* 40px */
  --spacing-12: 3rem;                 /* 48px */
  --spacing-16: 4rem;                 /* 64px */
}
```

---

## 7. Tokens de Borda

### 7.1 Arredondamento

```css
:root {
  --border-radius-sm: 0.25rem;        /* 4px */
  --border-radius: 0.5rem;            /* 8px - padrao */
  --border-radius-lg: 0.75rem;        /* 12px */
  --border-radius-full: 9999px;       /* circular */
}
```

### 7.2 Largura

```css
:root {
  --border-width: 1px;
}
```

---

## 8. Tokens de Sombra

```css
:root {
  --shadow-sm: 0 1px 2px 0 rgb(0 0 0 / 0.05);
  --shadow: 0 1px 3px 0 rgb(0 0 0 / 0.1), 0 1px 2px -1px rgb(0 0 0 / 0.1);
  --shadow-lg: 0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1);
}

.app-dark {
  --shadow-sm: 0 1px 2px 0 rgb(0 0 0 / 0.2);
  --shadow: 0 1px 3px 0 rgb(0 0 0 / 0.3), 0 1px 2px -1px rgb(0 0 0 / 0.3);
  --shadow-lg: 0 10px 15px -3px rgb(0 0 0 / 0.3), 0 4px 6px -4px rgb(0 0 0 / 0.3);
}
```

---

## 9. Tokens de Breakpoint

```css
:root {
  --breakpoint-sm: 640px;
  --breakpoint-md: 768px;
  --breakpoint-lg: 1024px;
  --breakpoint-xl: 1280px;
}
```

> **Nota**: breakpoints em CSS custom properties nao podem ser usados diretamente em `@media` queries. Estes tokens servem como referencia documentacional. Na pratica, usar as classes responsivas do Tailwind (`sm:`, `md:`, `lg:`, `xl:`) ou as media queries do PrimeNG.

---

## 10. Integracao com PrimeNG e Tailwind

### 10.1 PrimeNG Aura Theme

O PrimeNG 21 com Aura theme define seus proprios tokens CSS internos (prefixados com `--p-`). Os tokens do projeto devem **complementar** os tokens do PrimeNG, nao substituir.

A relacao e a seguinte:

| Token do projeto        | Token PrimeNG correspondente     |
| ----------------------- | -------------------------------- |
| `--color-primary`       | `--p-primary-color`              |
| `--color-surface-*`     | `--p-surface-*`                  |
| `--border-radius`       | `--p-border-radius`              |
| `--font-family`         | `--p-font-family`                |

O theme Aura do PrimeNG e configurado para usar emerald como cor primaria. Os tokens do projeto existem para uso em componentes customizados e classes Tailwind que nao passam pelo PrimeNG.

### 10.2 Tailwind 4

No Tailwind 4, a configuracao de theme e feita diretamente no CSS usando `@theme`:

```css
@theme {
  --color-primary: var(--color-primary);
  --color-primary-hover: var(--color-primary-hover);
  --color-surface-50: var(--color-surface-50);
  --color-surface-100: var(--color-surface-100);
  --color-surface-200: var(--color-surface-200);
  /* ... demais tokens mapeados */
}
```

Isso permite usar classes como `bg-primary`, `text-primary`, `border-surface-200` diretamente nos templates Angular, mantendo sincronizacao com os tokens.

### 10.3 Sincronizacao entre os dois

A estrategia de integracao:

1. **Tokens do projeto** (`--color-*`, `--font-*`, etc.) sao a fonte de verdade
2. **PrimeNG Aura** consome seus proprios tokens (`--p-*`) que sao configurados via preset para alinhar com a paleta emerald/slate
3. **Tailwind 4** referencia os tokens do projeto via `@theme` para gerar classes utilitarias
4. **Componentes customizados** usam `var(--color-*)` diretamente no CSS ou classes Tailwind

---

## 11. Exemplo de Uso em Componente Angular

### Componente: Card de Aliquota Municipal

```typescript
// aliquota-card.component.ts
import { Component, input } from '@angular/core';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-aliquota-card',
  standalone: true,
  imports: [CardModule, ButtonModule],
  templateUrl: './aliquota-card.component.html',
  styleUrl: './aliquota-card.component.css',
})
export class AliquotaCardComponent {
  municipio = input.required<string>();
  servico = input.required<string>();
  aliquota = input.required<number>();
  competencia = input.required<string>();
}
```

```html
<!-- aliquota-card.component.html -->
<p-card styleClass="aliquota-card">
  <ng-template #header>
    <div class="flex items-center gap-2 p-4 pb-0">
      <i class="pi pi-map-marker text-primary" aria-hidden="true"></i>
      <h3 class="text-lg font-semibold text-slate-800 dark:text-slate-100">
        {{ municipio() }}
      </h3>
    </div>
  </ng-template>

  <div class="flex flex-col gap-3">
    <div class="flex justify-between items-center">
      <span class="text-sm text-slate-500 dark:text-slate-400">Servico</span>
      <span class="text-sm font-medium text-slate-700 dark:text-slate-200">
        {{ servico() }}
      </span>
    </div>

    <div class="flex justify-between items-center">
      <span class="text-sm text-slate-500 dark:text-slate-400">Competencia</span>
      <span class="text-sm font-medium text-slate-700 dark:text-slate-200">
        {{ competencia() }}
      </span>
    </div>

    <div class="flex justify-between items-center pt-2 border-t border-slate-200 dark:border-slate-700">
      <span class="text-base font-semibold text-slate-800 dark:text-slate-100">Aliquota</span>
      <span class="text-xl font-bold" style="color: var(--color-primary)">
        {{ aliquota() }}%
      </span>
    </div>
  </div>

  <ng-template #footer>
    <div class="flex justify-end">
      <p-button
        label="Ver historico"
        icon="pi pi-history"
        [text]="true"
        severity="primary"
        aria-label="Ver historico de aliquotas para {{ municipio() }}"
      />
    </div>
  </ng-template>
</p-card>
```

```css
/* aliquota-card.component.css */
:host {
  display: block;
}

.aliquota-card {
  border: var(--border-width) solid var(--color-border);
  border-radius: var(--border-radius-lg);
  box-shadow: var(--shadow-sm);
  transition: box-shadow 0.2s ease;
}

.aliquota-card:hover {
  box-shadow: var(--shadow);
}
```

### Pontos de destaque do exemplo

1. **Tokens CSS usados diretamente**: `var(--color-primary)`, `var(--color-border)`, `var(--border-radius-lg)`, `var(--shadow-sm)`
2. **Classes Tailwind para layout**: `flex`, `gap-2`, `p-4`, `text-lg`, `font-semibold`
3. **Classes Tailwind para cores**: `text-slate-800`, `dark:text-slate-100`, `border-slate-200`, `dark:border-slate-700`
4. **PrimeNG components**: `p-card`, `p-button` com suas propriedades nativas
5. **Acessibilidade**: `aria-hidden="true"` no icone decorativo, `aria-label` no botao
6. **Dark mode**: prefixo `dark:` nas classes de cor
7. **Angular signals**: uso de `input.required()` e chamada com `()` no template

---

## 12. Checklist de Validacao

Ao criar ou revisar um componente, validar:

- [ ] Cores referenciam tokens (nunca valores hardcoded como `#10b981` direto no CSS do componente)
- [ ] Tipografia usa a escala definida (nao inventar tamanhos fora da escala)
- [ ] Espacamento segue a escala de 4px
- [ ] Dark mode funciona corretamente (testar com classe `app-dark`)
- [ ] Bordas e sombras usam os tokens definidos
- [ ] Componentes PrimeNG seguem o theme Aura sem customizacao inline
- [ ] Classes Tailwind sao usadas para layout, tokens CSS para valores semanticos
