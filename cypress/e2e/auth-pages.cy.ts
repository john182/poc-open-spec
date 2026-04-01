describe('Paginas de Autenticacao', () => {
  it('deve exibir pagina de login', () => {
    cy.visit('/auth/login');
    cy.get('[data-cy="login-page"]').should('be.visible');
    cy.findByText('Mapa Tributário').should('be.visible');
    cy.findByText('Entre para continuar').should('be.visible');
  });

  it('deve ter campos de email e senha no login', () => {
    cy.visit('/auth/login');
    cy.findByLabelText('Email').should('be.visible');
    cy.findByLabelText('Senha').should('be.visible');
  });

  it('deve ter link para criar conta no login', () => {
    cy.visit('/auth/login');
    cy.findByText('Criar conta').should('be.visible');
    cy.findByText('Criar conta').click();
    cy.url().should('include', '/auth/signup');
  });

  it('deve exibir pagina de signup', () => {
    cy.visit('/auth/signup');
    cy.get('[data-cy="signup-page"]').should('be.visible');
    cy.findByText('Criar Conta').should('be.visible');
  });

  it('deve ter campos nome, email, senha e confirmacao no signup', () => {
    cy.visit('/auth/signup');
    cy.findByLabelText('Nome').should('be.visible');
    cy.findByLabelText('Email').should('be.visible');
    cy.findByLabelText('Senha').should('be.visible');
    cy.findByLabelText('Confirmar Senha').should('be.visible');
  });

  it('deve ter link para entrar no signup', () => {
    cy.visit('/auth/signup');
    cy.findByText('Entrar').click();
    cy.url().should('include', '/auth/login');
  });
});
