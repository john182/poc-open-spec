// ---------------------------------------------------------------------------
// navigation.cy.ts — Testes E2E de navegação (menu, layout, toggle)
// ---------------------------------------------------------------------------

describe('Navegação e Layout', () => {
  beforeEach(() => {
    cy.login();
    cy.visit('/consulta');
    cy.getByDataCy('consulta-page').should('be.visible');
  });

  describe('Layout principal', () => {
    it('Given_UsuarioAutenticado_Should_ExibirTopbarComNomeDaAplicacao', () => {
      // Arrange — já autenticado no beforeEach

      // Act — nenhuma ação adicional

      // Assert
      cy.getByDataCy('app-topbar').should('be.visible');
      cy.getByDataCy('app-topbar').contains('Mapa Tributário').should('be.visible');
    });

    it('Given_UsuarioAutenticado_Should_ExibirNomeDoUsuarioNoTopbar', () => {
      // Assert
      cy.getByDataCy('user-name').should('be.visible');
    });

    it('Given_UsuarioAutenticado_Should_ExibirMenuLateral', () => {
      // Assert
      cy.getByDataCy('app-sidebar').should('exist');
      cy.getByDataCy('app-menu').should('exist');
      cy.contains('Consulta de Alíquotas').should('be.visible');
    });

    it('Given_UsuarioAutenticado_Should_ExibirFooterComAnoCorrente', () => {
      // Arrange
      const anoAtual = new Date().getFullYear();

      // Assert
      cy.getByDataCy('app-footer').should('contain.text', `Mapa Tributário © ${anoAtual}`);
    });

    it('Given_UsuarioAutenticado_Should_ExibirEstruturaDeCssDoLayout', () => {
      // Assert
      cy.get('.layout-wrapper').should('exist');
      cy.get('.layout-topbar').should('exist');
      cy.get('.layout-sidebar').should('exist');
      cy.get('.layout-main-container').should('exist');
      cy.get('.layout-footer').should('exist');
    });
  });

  describe('Menu', () => {
    it('Given_CliqueNoMenuConsulta_Should_NavegarParaConsulta', () => {
      // Act
      cy.contains('Consulta de Alíquotas').click();

      // Assert
      cy.url().should('include', '/consulta');
      cy.getByDataCy('consulta-page').should('be.visible');
    });

    it('Given_BotaoDeToggleMenu_Should_ColapsarEExpandirMenuLateral', () => {
      // Act — colapsar
      cy.get('.layout-menu-button').should('be.visible').click();

      // Assert — menu colapsado
      cy.get('.layout-wrapper').should('have.class', 'layout-static-inactive');

      // Act — expandir
      cy.get('.layout-menu-button').click();

      // Assert — menu expandido novamente
      cy.get('.layout-wrapper').should('not.have.class', 'layout-static-inactive');
    });
  });

  describe('Redirecionamento', () => {
    it('Given_AcessoARaiz_Should_RedirecionarParaConsulta', () => {
      // Act
      cy.visit('/');

      // Assert
      cy.url().should('include', '/consulta');
    });
  });
});
