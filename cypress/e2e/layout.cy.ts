describe('Layout', () => {
  beforeEach(() => {
    cy.visit('/');
  });

  it('deve exibir o topbar com o nome da aplicacao', () => {
    cy.get('[data-cy="app-topbar"]').should('be.visible');
    cy.findByText('Mapa Tributário').should('be.visible');
  });

  it('deve exibir o menu lateral com item de consulta', () => {
    cy.get('[data-cy="app-sidebar"]').should('exist');
    cy.get('[data-cy="app-menu"]').should('exist');
    cy.findByText('Consulta de Alíquotas').should('be.visible');
  });

  it('deve exibir o footer', () => {
    cy.get('[data-cy="app-footer"]').should('contain.text', 'Mapa Tributário © 2026');
  });

  it('deve navegar para consulta ao clicar no menu', () => {
    cy.findByText('Consulta de Alíquotas').click();
    cy.url().should('include', '/consulta');
    cy.get('[data-cy="consulta-page"]').should('be.visible');
  });

  it('deve ter botao de toggle dark mode no topbar', () => {
    cy.get('[data-cy="app-topbar"]').find('.pi-moon, .pi-sun').should('exist');
  });

  it('deve usar classes de layout do Sakai', () => {
    cy.get('.layout-wrapper').should('exist');
    cy.get('.layout-topbar').should('exist');
    cy.get('.layout-sidebar').should('exist');
    cy.get('.layout-main-container').should('exist');
    cy.get('.layout-footer').should('exist');
  });

  it('deve ter menu com classes Sakai', () => {
    cy.get('.layout-menu').should('exist');
    cy.get('.layout-root-menuitem').should('exist');
    cy.get('.layout-menuitem-root-text').should('exist');
    cy.get('.layout-menuitem-icon').should('exist');
  });

  it('deve ter botao de menu toggle funcional', () => {
    cy.get('.layout-menu-button').should('be.visible').click();
    cy.get('.layout-wrapper').should('have.class', 'layout-static-inactive');
    cy.get('.layout-menu-button').click();
    cy.get('.layout-wrapper').should('not.have.class', 'layout-static-inactive');
  });
});
