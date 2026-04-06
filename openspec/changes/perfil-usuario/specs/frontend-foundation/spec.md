## MODIFIED Requirements

### Requirement: Admin layout with sidebar navigation
The frontend SHALL provide an authenticated admin layout with: topbar with logo and user actions, collapsible sidebar with menu items, main content area, and footer. The layout SHALL support static and overlay menu modes. The layout SHALL be responsive (desktop and mobile).

#### Scenario: Desktop layout
- **WHEN** a user accesses the application on a desktop screen (>1024px)
- **THEN** the layout displays a static sidebar, topbar, and content area

#### Scenario: Mobile layout
- **WHEN** a user accesses the application on a mobile screen (<1024px)
- **THEN** the sidebar becomes an overlay triggered by a hamburger button in the topbar

#### Scenario: Menu navigation
- **WHEN** a user clicks a menu item in the sidebar
- **THEN** the application navigates to the corresponding route and highlights the active menu item

#### Scenario: Dropdown do usuário na topbar
- **WHEN** um usuário autenticado clica no ícone/nome do usuário na topbar
- **THEN** um OverlayPanel/dropdown é exibido com as opções "Meu Perfil" (navega para `/perfil`) e "Sair" (executa logout)

#### Scenario: Navegação para perfil via dropdown
- **WHEN** o usuário clica em "Meu Perfil" no dropdown da topbar
- **THEN** o sistema navega para a rota `/perfil` e o dropdown é fechado

#### Scenario: Logout via dropdown
- **WHEN** o usuário clica em "Sair" no dropdown da topbar
- **THEN** o sistema executa o logout e redireciona para `/auth/login`
