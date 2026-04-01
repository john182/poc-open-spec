describe('Paginas de Autenticacao', () => {
  describe('Login', () => {
    beforeEach(() => {
      cy.visit('/auth/login');
    });

    it('deve exibir pagina de login com layout full-screen Sakai', () => {
      cy.get('[data-cy="login-page"]').should('be.visible');
      cy.get('[data-cy="login-page"]')
        .should('have.class', 'bg-surface-50')
        .and('have.class', 'min-h-screen')
        .and('have.class', 'flex');
    });

    it('deve exibir titulo e subtitulo', () => {
      cy.findByText('Mapa Tributário').should('be.visible');
      cy.findByText('Entre para continuar').should('be.visible');
    });

    it('deve ter campos de email e senha', () => {
      cy.findByLabelText('Email').should('be.visible');
      cy.get('p-password#senha').should('exist');
    });

    it('deve ter checkbox lembrar-me', () => {
      cy.get('p-checkbox#lembrar').should('exist');
    });

    it('deve ter botao Entrar', () => {
      cy.findByText('Entrar').should('be.visible');
    });

    it('deve navegar para signup ao clicar em Criar conta', () => {
      cy.findByText('Criar conta').should('be.visible').click();
      cy.url().should('include', '/auth/signup');
    });
  });

  describe('Signup', () => {
    beforeEach(() => {
      cy.visit('/auth/signup');
    });

    it('deve exibir pagina de signup com layout full-screen Sakai', () => {
      cy.get('[data-cy="signup-page"]').should('be.visible');
      cy.get('[data-cy="signup-page"]')
        .should('have.class', 'bg-surface-50')
        .and('have.class', 'min-h-screen')
        .and('have.class', 'flex');
    });

    it('deve exibir titulo Criar Conta', () => {
      cy.findByText('Criar Conta').should('be.visible');
    });

    it('deve ter campos nome, email, senha e confirmacao', () => {
      cy.findByLabelText('Nome').should('be.visible');
      cy.findByLabelText('Email').should('be.visible');
      cy.get('p-password#senha').should('exist');
      cy.get('p-password#confirmar').should('exist');
    });

    it('deve navegar para login ao clicar em Entrar', () => {
      cy.findByText('Entrar').click();
      cy.url().should('include', '/auth/login');
    });
  });
});
