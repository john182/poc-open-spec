describe('Navegacao', () => {
  it('deve redirecionar raiz para /consulta', () => {
    cy.visit('/');
    cy.url().should('include', '/consulta');
  });

  it('deve exibir pagina 404 com layout full-screen Sakai', () => {
    cy.visit('/rota-que-nao-existe', { failOnStatusCode: false });
    cy.get('[data-cy="not-found-page"]').should('be.visible');
    cy.get('[data-cy="not-found-page"]')
      .should('have.class', 'bg-surface-50')
      .and('have.class', 'min-h-screen')
      .and('have.class', 'flex');
    cy.findByText('404').should('be.visible');
    cy.findByText('Página não encontrada').should('be.visible');
  });

  it('deve ter link para voltar ao inicio na pagina 404', () => {
    cy.visit('/rota-inexistente', { failOnStatusCode: false });
    cy.findByText('Voltar ao início').should('be.visible').click();
    cy.url().should('include', '/consulta');
  });

  it('deve exibir pagina de acesso negado com layout full-screen Sakai', () => {
    cy.visit('/acesso-negado');
    cy.get('[data-cy="access-denied-page"]').should('be.visible');
    cy.get('[data-cy="access-denied-page"]')
      .should('have.class', 'bg-surface-50')
      .and('have.class', 'min-h-screen')
      .and('have.class', 'flex');
    cy.findByText('Acesso Negado').should('be.visible');
  });

  it('deve ter icone de cadeado na pagina de acesso negado', () => {
    cy.visit('/acesso-negado');
    cy.get('.pi-lock').should('be.visible');
  });

  it('deve ter link para voltar ao inicio na pagina de acesso negado', () => {
    cy.visit('/acesso-negado');
    cy.findByText('Voltar ao início').click();
    cy.url().should('include', '/consulta');
  });
});
