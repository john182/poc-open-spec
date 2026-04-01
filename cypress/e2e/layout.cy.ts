describe('Layout', () => {
  beforeEach(() => {
    cy.visit('/');
  });

  it('deve exibir o topbar com o nome da aplicacao', () => {
    cy.findByText('Mapa Tributário').should('be.visible');
  });

  it('deve exibir o menu lateral com item de consulta', () => {
    cy.findByText('Consulta de Alíquotas').should('be.visible');
  });

  it('deve exibir o footer', () => {
    cy.get('[data-cy="app-footer"]').should('contain.text', 'Mapa Tributário © 2026');
  });

  it('deve navegar para consulta ao clicar no menu', () => {
    cy.findByText('Consulta de Alíquotas').click();
    cy.url().should('include', '/consulta');
  });

  it('deve ter botao de toggle dark mode no topbar', () => {
    cy.get('[data-cy="app-topbar"]').find('.pi-moon, .pi-sun').should('exist');
  });
});
