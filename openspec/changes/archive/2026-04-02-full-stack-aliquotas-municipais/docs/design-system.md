# Design System - Mapa Tributario

> Guia de design system para o frontend Angular 21 + PrimeNG 21 + Tailwind 4 do projeto Mapa Tributario.

---

## 1. Principios Visuais

O design system do Mapa Tributario se apoia em tres principios fundamentais:

### 1.1 Clareza

- Hierarquia visual bem definida entre titulos, subtitulos, corpo de texto e elementos interativos
- Uso intencional de espacamento para separar blocos logicos de informacao
- Contraste adequado entre texto e fundo em todos os contextos (light e dark mode)
- Elementos interativos devem ser imediatamente reconheciveis como clicaveis

### 1.2 Consistencia

- Todos os componentes seguem o mesmo vocabulario visual: mesmas cores, mesmos arredondamentos, mesmas sombras
- Padroes de interacao identicos em contextos similares (ex: todo formulario usa os mesmos helpers, toda tabela segue o mesmo layout)
- PrimeNG Aura theme como base unica de estilo para componentes de UI
- Tailwind 4 como utilitario de layout e espacamento, nunca conflitando com o theme do PrimeNG

### 1.3 Acessibilidade

- Conformidade com WCAG 2.1 nivel AA como objetivo minimo
- Contraste de cor minimo de 4.5:1 para texto normal e 3:1 para texto grande
- Todos os elementos interativos devem ser acessiveis por teclado
- Focus visible obrigatorio em todos os componentes interativos
- ARIA labels em componentes que nao tem texto visivel autoexplicativo
- Suporte a navegacao por teclado (Tab, Shift+Tab, Enter, Escape)
- Textos alternativos em imagens e icones decorativos marcados com `aria-hidden="true"`

---

## 2. Paleta de Cores

### 2.1 Primary: Emerald (PrimeNG Aura Default)

A cor primaria do projeto segue o preset Aura do PrimeNG, baseada na escala emerald. Esta cor e usada em:

- Botoes primarios
- Links e elementos de acao principal
- Indicadores de estado ativo/selecionado
- Elementos de destaque na navegacao

| Token               | Uso                              |
| -------------------- | -------------------------------- |
| `emerald-500`        | Cor primaria padrao              |
| `emerald-600`        | Hover em elementos primarios     |
| `emerald-700`        | Active/pressed                   |
| `emerald-400`        | Variante mais clara (badges, bg) |
| `emerald-50`         | Background sutil primario        |

### 2.2 Surface: Slate (Neutro)

A escala slate e utilizada para superficies, bordas e textos neutros:

| Token          | Uso                                      |
| -------------- | ---------------------------------------- |
| `slate-50`     | Background principal (light mode)        |
| `slate-100`    | Background de cards e areas elevadas     |
| `slate-200`    | Bordas sutis                             |
| `slate-300`    | Bordas padrao                            |
| `slate-400`    | Texto desabilitado / muted               |
| `slate-500`    | Texto secundario                         |
| `slate-600`    | Texto secundario enfatizado              |
| `slate-700`    | Texto primario (light mode)              |
| `slate-800`    | Titulos e texto forte                    |
| `slate-900`    | Background principal (dark mode)         |
| `slate-950`    | Background mais profundo (dark mode)     |

### 2.3 Cores Semanticas

Cores com significado especifico para feedback ao usuario:

| Semantica   | Cor base | Uso                                         |
| ----------- | -------- | ------------------------------------------- |
| **Success** | Green    | Confirmacoes, operacoes bem-sucedidas       |
| **Warning** | Amber    | Alertas, acoes que requerem atencao         |
| **Danger**  | Red      | Erros, acoes destrutivas, validacoes        |
| **Info**    | Blue     | Informacoes contextuais, dicas, onboarding  |

Cada cor semantica deve ter variantes para:
- Background sutil (ex: `green-50`, `red-50`)
- Texto/icone (ex: `green-600`, `red-600`)
- Borda (ex: `green-200`, `red-200`)
- Hover (ex: `green-700`, `red-700`)

---

## 3. Tipografia

### 3.1 Familia Tipografica

O projeto utiliza **Inter** como fonte primaria, com fallbacks para fontes do sistema:

```
font-family: 'Inter', system-ui, -apple-system, sans-serif;
```

A fonte Inter deve ser carregada via Google Fonts ou self-hosted para garantir disponibilidade.

### 3.2 Escala Tipografica

A escala segue os utilitarios do Tailwind 4:

| Classe Tailwind | Tamanho | Uso                                  |
| --------------- | ------- | ------------------------------------ |
| `text-xs`       | 12px    | Labels pequenos, footnotes, badges   |
| `text-sm`       | 14px    | Texto secundario, captions, helpers  |
| `text-base`     | 16px    | Corpo de texto padrao                |
| `text-lg`       | 18px    | Subtitulos, destaques               |
| `text-xl`       | 20px    | Titulos de secao                     |
| `text-2xl`      | 24px    | Titulos de pagina                    |
| `text-3xl`      | 30px    | Titulos principais, hero             |

### 3.3 Pesos Tipograficos

| Peso         | Valor | Uso                                    |
| ------------ | ----- | -------------------------------------- |
| `normal`     | 400   | Corpo de texto                         |
| `medium`     | 500   | Labels, texto com leve enfase          |
| `semibold`   | 600   | Subtitulos, headers de tabela          |
| `bold`       | 700   | Titulos, botoes, destaques fortes      |

### 3.4 Alturas de Linha

| Tipo       | Valor | Uso                                       |
| ---------- | ----- | ----------------------------------------- |
| `tight`    | 1.25  | Titulos e textos curtos                   |
| `normal`   | 1.5   | Corpo de texto padrao                     |
| `relaxed`  | 1.75  | Textos longos, paragrafos explicativos    |

---

## 4. Espacamento

O sistema de espacamento segue uma escala de **4px** como unidade base, alinhada com o Tailwind 4:

| Classe Tailwind | Valor | Uso                                         |
| --------------- | ----- | ------------------------------------------- |
| `gap-1`         | 4px   | Espacamento minimo (entre icone e texto)    |
| `gap-2`         | 8px   | Espacamento pequeno (entre elementos inline)|
| `gap-3`         | 12px  | Espacamento medio-pequeno                   |
| `gap-4`         | 16px  | Espacamento padrao (entre blocos)           |
| `gap-6`         | 24px  | Espacamento medio-grande                    |
| `gap-8`         | 32px  | Espacamento entre secoes                    |
| `gap-12`        | 48px  | Espacamento entre blocos grandes            |
| `gap-16`        | 64px  | Espacamento entre secoes de pagina          |

A mesma escala se aplica a `padding` (`p-*`), `margin` (`m-*`) e `space` (`space-x-*`, `space-y-*`).

### Regras gerais de espacamento

- Padding interno de cards: `p-4` (16px) ou `p-6` (24px)
- Gap entre campos de formulario: `gap-4` (16px)
- Margem entre secoes de pagina: `gap-8` (32px) ou `gap-12` (48px)
- Padding do layout principal: `p-4` em mobile, `p-6` em desktop

---

## 5. Bordas e Arredondamento

### 5.1 Arredondamento

| Classe Tailwind        | Valor  | Uso                                       |
| ---------------------- | ------ | ----------------------------------------- |
| `rounded-border`       | Variavel PrimeNG | Arredondamento padrao dos componentes PrimeNG |
| `rounded` (Tailwind)   | 4px    | Arredondamento sutil                      |
| `rounded-lg`           | 8px    | Cards, dialogs, containers               |
| `rounded-xl`           | 12px   | Cards destacados, modais                  |
| `rounded-full`         | 9999px | Avatares, badges circulares, chips        |

O token `rounded-border` do PrimeNG deve ser respeitado para todos os componentes PrimeNG. Para elementos customizados, usar `rounded-lg` como padrao.

### 5.2 Bordas

- Largura padrao: `border` (1px)
- Cor padrao light mode: `slate-200` ou `slate-300`
- Cor padrao dark mode: `slate-700` ou `slate-600`
- Bordas devem ser sutis e nunca competir visualmente com o conteudo

---

## 6. Sombras

| Classe Tailwind | Uso                                              |
| --------------- | ------------------------------------------------ |
| `shadow-sm`     | Elevacao sutil (inputs, chips)                   |
| `shadow`        | Elevacao padrao (cards, dropdowns)               |
| `shadow-lg`     | Elevacao forte (dialogs, modais, menus flutuantes)|

### Regras de sombra

- Cards em light mode: `shadow-sm` ou `shadow`
- Cards em dark mode: sombra reduzida ou substituida por borda sutil
- Dialogs e overlays: `shadow-lg`
- Elementos inline (inputs, selects): `shadow-sm` ou nenhuma sombra

---

## 7. Icones

O projeto utiliza **PrimeIcons** como biblioteca padrao de icones, integrada nativamente ao PrimeNG.

### Uso

```html
<i class="pi pi-search"></i>
<i class="pi pi-user"></i>
<i class="pi pi-map-marker"></i>
<i class="pi pi-filter"></i>
<i class="pi pi-refresh"></i>
<i class="pi pi-exclamation-triangle"></i>
<i class="pi pi-check-circle"></i>
<i class="pi pi-times-circle"></i>
```

### Icones frequentes no projeto

| Icone                         | Contexto                          |
| ----------------------------- | --------------------------------- |
| `pi pi-map-marker`            | Municipio, localizacao            |
| `pi pi-search`                | Busca, filtro                     |
| `pi pi-filter`                | Filtros avancados                 |
| `pi pi-refresh`               | Retry, atualizar                  |
| `pi pi-exclamation-triangle`  | Warning, alerta                   |
| `pi pi-check-circle`          | Sucesso, confirmacao              |
| `pi pi-times-circle`          | Erro, remocao                     |
| `pi pi-info-circle`           | Informacao contextual             |
| `pi pi-spin pi-spinner`       | Loading                           |
| `pi pi-chevron-right`         | Navegacao, breadcrumb             |
| `pi pi-sign-in`               | Login                             |
| `pi pi-sign-out`              | Logout                            |
| `pi pi-user`                  | Usuario, perfil                   |
| `pi pi-home`                  | Home, dashboard                   |
| `pi pi-list`                  | Listagem                          |
| `pi pi-table`                 | Tabela de dados                   |

### Regras de uso de icones

- Icones decorativos devem ter `aria-hidden="true"`
- Icones com funcao semantica devem ter um `aria-label` ou texto adjacente
- Tamanho padrao: herda do `font-size` do elemento pai
- Cor padrao: herda do `color` do elemento pai

---

## 8. Componentes Padrao PrimeNG

O projeto utiliza os seguintes componentes do PrimeNG 21 com o theme Aura:

### 8.1 Formularios e Inputs

| Componente       | Uso                                          |
| ---------------- | -------------------------------------------- |
| `p-inputtext`    | Campos de texto simples                      |
| `p-password`     | Campo de senha com toggle de visibilidade    |
| `p-dropdown`     | Selecao unica (estado, municipio, servico)   |
| `p-inputnumber`  | Campos numericos (aliquotas, valores)        |
| `p-checkbox`     | Selecoes multiplas em filtros                |

### 8.2 Botoes

| Componente  | Variantes                                          |
| ----------- | -------------------------------------------------- |
| `p-button`  | `severity="primary"` - acao principal              |
| `p-button`  | `severity="secondary"` - acao secundaria           |
| `p-button`  | `severity="success"` - confirmacao                 |
| `p-button`  | `severity="danger"` - acao destrutiva              |
| `p-button`  | `[text]="true"` - botao sem background             |
| `p-button`  | `[outlined]="true"` - botao com borda              |
| `p-button`  | `[loading]="true"` - estado de carregamento        |

### 8.3 Dados e Tabelas

| Componente     | Uso                                           |
| -------------- | --------------------------------------------- |
| `p-table`      | Listagem de aliquotas, servicos, municipios   |
| `p-paginator`  | Paginacao de resultados                       |

### 8.4 Containers e Layout

| Componente     | Uso                                           |
| -------------- | --------------------------------------------- |
| `p-card`       | Agrupamento de conteudo relacionado           |
| `p-dialog`     | Modais de confirmacao, detalhes               |

### 8.5 Feedback e Notificacoes

| Componente  | Uso                                              |
| ----------- | ------------------------------------------------ |
| `p-toast`   | Notificacoes temporarias (sucesso, erro, info)   |

### 8.6 Navegacao

| Componente      | Uso                                          |
| --------------- | -------------------------------------------- |
| `p-menu`        | Menu lateral, menu de contexto               |
| `p-breadcrumb`  | Trilha de navegacao (Home > Estado > Municipio) |

---

## 9. Padroes de Interacao

### 9.1 Loading States

Todo componente que depende de dados assincronos deve exibir um estado de carregamento:

- **Tabelas**: skeleton rows ou spinner centralizado com mensagem "Carregando..."
- **Cards**: skeleton placeholder com animacao pulse
- **Botoes**: `[loading]="true"` no p-button enquanto a acao esta em processamento
- **Pagina inteira**: spinner centralizado com texto contextual

Regra: nunca deixar o usuario sem feedback visual durante uma operacao assincrona.

### 9.2 Empty States

Quando nao ha dados para exibir:

- Icone contextual (ex: `pi pi-inbox` ou `pi pi-search`)
- Mensagem descritiva (ex: "Nenhum municipio encontrado para o filtro selecionado")
- Acao sugerida quando aplicavel (ex: botao "Limpar filtros" ou "Tentar novamente")

Regra: empty states nao devem ser apenas texto. Devem ter icone + mensagem + acao (quando cabivel).

### 9.3 Error States + Retry

Quando ocorre um erro ao carregar dados:

- Icone de erro (`pi pi-exclamation-triangle`)
- Mensagem amigavel (nunca expor stack traces ou mensagens tecnicas brutas)
- Botao de retry (`pi pi-refresh` + "Tentar novamente")
- Toast de erro para erros em acoes do usuario (salvar, filtrar, etc.)

Padrao de retry:

```html
<div class="flex flex-col items-center gap-4 p-8">
  <i class="pi pi-exclamation-triangle text-4xl text-red-500"></i>
  <p class="text-slate-600">Nao foi possivel carregar os dados.</p>
  <p-button label="Tentar novamente" icon="pi pi-refresh" (click)="retry()" />
</div>
```

---

## 10. Responsividade

O projeto segue uma abordagem **mobile-first** com os seguintes breakpoints do Tailwind 4:

| Breakpoint | Largura minima | Uso                                    |
| ---------- | -------------- | -------------------------------------- |
| `sm`       | 640px          | Smartphones em landscape               |
| `md`       | 768px          | Tablets em portrait                    |
| `lg`       | 1024px         | Tablets em landscape, laptops          |
| `xl`       | 1280px         | Desktops                               |

### Regras de responsividade

- **Layout principal**: sidebar colapsavel em `< lg`, fixa em `>= lg`
- **Tabelas**: scroll horizontal em mobile, colunas completas em desktop
- **Formularios**: campos empilhados em mobile (`flex-col`), lado a lado em desktop (`flex-row`)
- **Cards**: grid de 1 coluna em mobile, 2 em `md`, 3-4 em `lg`/`xl`
- **Mapa**: largura total em mobile, ao lado de filtros em desktop
- **Dialogs**: fullscreen em mobile (`[maximizable]="true"`), centralizado em desktop
- **Navegacao**: hamburger menu em mobile, sidebar completa em desktop

---

## 11. Dark Mode

### Ativacao

O dark mode e ativado pela classe `app-dark` no elemento raiz da aplicacao. Esta classe controla:

- O theme do PrimeNG (Aura dark variant)
- As classes `dark:` do Tailwind 4

### Implementacao

```html
<!-- Elemento raiz -->
<div [class.app-dark]="isDarkMode">
  <router-outlet />
</div>
```

### Regras de dark mode

- Todo componente customizado deve ter variantes `dark:` definidas
- Cores de surface sao invertidas (slate-50 vira slate-900, etc.)
- Sombras sao reduzidas ou substituidas por bordas sutis em dark mode
- Contraste de texto deve ser mantido acima de 4.5:1 em ambos os modos
- Icones e elementos semanticos mantem as mesmas cores (green, red, amber, blue)
- Imagens e ilustracoes podem precisar de ajustes de opacidade

### Exemplos de uso do prefixo dark:

```html
<div class="bg-white dark:bg-slate-900 text-slate-800 dark:text-slate-100">
  <h1 class="text-slate-900 dark:text-white">Titulo</h1>
  <p class="text-slate-600 dark:text-slate-400">Texto secundario</p>
</div>

<div class="border border-slate-200 dark:border-slate-700 shadow dark:shadow-none">
  <!-- Card content -->
</div>
```

---

## 12. Acessibilidade

### 12.1 ARIA Labels

- Todo elemento interativo sem texto visivel deve ter `aria-label` ou `aria-labelledby`
- Regioes de pagina devem ter `role` e `aria-label` quando necessario
- Tabelas devem ter `aria-label` descrevendo seu conteudo
- Icones funcionais devem ter `aria-label`; icones decorativos devem ter `aria-hidden="true"`

### 12.2 Focus Visible

- Todos os elementos interativos devem ter um indicador visual de foco (outline)
- Usar `focus-visible:ring-2 focus-visible:ring-emerald-500` como padrao
- Nunca remover o outline de foco sem substituir por outro indicador visual
- O anel de foco deve ter contraste suficiente contra o fundo

### 12.3 Navegacao por Teclado

- Toda funcionalidade acessivel por mouse deve ser acessivel por teclado
- Tab order deve seguir a ordem logica da interface
- Escape deve fechar dialogs, dropdowns e overlays
- Enter deve ativar botoes e links
- Arrow keys devem navegar em menus, dropdowns e tabelas
- Skip links devem estar disponiveis para pular navegacao repetitiva

### 12.4 Contraste de Cores

- Texto normal (< 18px): ratio minimo de 4.5:1
- Texto grande (>= 18px bold ou >= 24px): ratio minimo de 3:1
- Elementos graficos e bordas de input: ratio minimo de 3:1
- Nao usar cor como unico meio de transmitir informacao (sempre acompanhar com icone ou texto)

### 12.5 Checklist de Acessibilidade por Componente

| Componente       | Requisitos                                                   |
| ---------------- | ------------------------------------------------------------ |
| `p-button`       | Label claro, `aria-label` se so icone, foco visivel         |
| `p-inputtext`    | Label associado via `for`/`id`, mensagem de erro acessivel  |
| `p-table`        | `aria-label`, headers com scope, paginacao acessivel        |
| `p-dialog`       | Focus trap, Escape para fechar, `aria-labelledby`           |
| `p-toast`        | `aria-live="polite"`, role="alert"                          |
| `p-menu`         | Arrow key navigation, `aria-expanded`                       |
| `p-dropdown`     | Label associado, arrow key navigation, `aria-expanded`      |
| `p-breadcrumb`   | `nav` com `aria-label="Breadcrumb"`                         |

---

## 13. Resumo de Decisoes

| Decisao                     | Escolha                                |
| --------------------------- | -------------------------------------- |
| Framework CSS               | Tailwind 4 (utilitarios)               |
| Biblioteca de componentes   | PrimeNG 21 (Aura theme)               |
| Cor primaria                | Emerald (PrimeNG Aura default)         |
| Cor de surface              | Slate                                  |
| Fonte                       | Inter                                  |
| Icones                      | PrimeIcons                             |
| Abordagem responsiva        | Mobile-first                           |
| Dark mode                   | Classe `app-dark` + prefixo `dark:`    |
| Acessibilidade alvo         | WCAG 2.1 AA                            |
| Escala de espacamento       | 4px base (Tailwind padrao)             |
