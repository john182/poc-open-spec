// ---------------------------------------------------------------------------
// auth.cy.ts — Testes E2E de autenticação (cadastro, login, logout)
// ---------------------------------------------------------------------------

describe('Autenticação', () => {
  const emailTeste = `e2e-${Date.now()}@teste.com`;
  const senhaTeste = '12345678';
  const nomeTeste = 'Usuário E2E';

  describe('Cadastro', () => {
    beforeEach(() => {
      cy.visit('/auth/signup');
    });

    it('Given_DadosValidos_Should_CriarContaERedirecionarParaConsulta', () => {
      // Arrange
      cy.getByDataCy('signup-page').should('be.visible');

      // Act
      cy.getByDataCy('signup-nome').clear().type(nomeTeste);
      cy.getByDataCy('signup-email').clear().type(emailTeste);
      cy.get('[data-cy="signup-senha"] input').clear().type(senhaTeste);
      cy.get('[data-cy="signup-confirmar"] input').clear().type(senhaTeste);
      cy.getByDataCy('signup-submit').click();

      // Assert
      cy.url().should('include', '/consulta');
      cy.getByDataCy('consulta-page').should('be.visible');
    });

    it('Given_EmailJaCadastrado_Should_ExibirMensagemDeErro', () => {
      // Arrange — usa credenciais do admin seed que já existe
      cy.getByDataCy('signup-page').should('be.visible');

      // Act
      cy.getByDataCy('signup-nome').clear().type('Admin Duplicado');
      cy.getByDataCy('signup-email').clear().type('admin@admin.com');
      cy.get('[data-cy="signup-senha"] input').clear().type('12345678');
      cy.get('[data-cy="signup-confirmar"] input').clear().type('12345678');
      cy.getByDataCy('signup-submit').click();

      // Assert
      cy.contains('Este email já está cadastrado').should('be.visible');
      cy.url().should('include', '/auth/signup');
    });

    it('Given_SenhasNaoCoincidem_Should_ExibirErroDeValidacao', () => {
      // Arrange
      cy.getByDataCy('signup-page').should('be.visible');

      // Act
      cy.getByDataCy('signup-nome').clear().type('Teste');
      cy.getByDataCy('signup-email').clear().type('teste-mismatch@teste.com');
      cy.get('[data-cy="signup-senha"] input').clear().type('12345678');
      cy.get('[data-cy="signup-confirmar"] input').clear().type('senhadiferente');
      cy.getByDataCy('signup-submit').click();

      // Assert
      cy.contains('As senhas não coincidem').should('be.visible');
      cy.url().should('include', '/auth/signup');
    });

    it('Given_CamposVazios_Should_ExibirErrosDeValidacao', () => {
      // Arrange
      cy.getByDataCy('signup-page').should('be.visible');

      // Act
      cy.getByDataCy('signup-submit').click();

      // Assert
      cy.contains('Nome é obrigatório').should('be.visible');
      cy.contains('Email é obrigatório').should('be.visible');
      cy.contains('Senha é obrigatória').should('be.visible');
    });

    it('Given_PaginaDeSignup_Should_NavegarParaLoginAoClicarEmEntrar', () => {
      // Arrange
      cy.getByDataCy('signup-page').should('be.visible');

      // Act
      cy.contains('a', 'Entrar').click();

      // Assert
      cy.url().should('include', '/auth/login');
      cy.getByDataCy('login-page').should('be.visible');
    });
  });

  describe('Login', () => {
    beforeEach(() => {
      cy.visit('/auth/login');
    });

    it('Given_CredenciaisValidas_Should_AutenticarERedirecionarParaConsulta', () => {
      // Arrange
      cy.getByDataCy('login-page').should('be.visible');

      // Act
      cy.getByDataCy('login-email').clear().type('admin@admin.com');
      cy.get('[data-cy="login-senha"] input').clear().type('12345678');
      cy.getByDataCy('login-submit').click();

      // Assert
      cy.url().should('include', '/consulta');
      cy.getByDataCy('consulta-page').should('be.visible');
      cy.getByDataCy('user-name').should('be.visible');
    });

    it('Given_SenhaErrada_Should_ExibirMensagemDeErro', () => {
      // Arrange
      cy.getByDataCy('login-page').should('be.visible');

      // Act
      cy.getByDataCy('login-email').clear().type('admin@admin.com');
      cy.get('[data-cy="login-senha"] input').clear().type('senhaerrada');
      cy.getByDataCy('login-submit').click();

      // Assert
      cy.contains('Email ou senha inválidos').should('be.visible');
      cy.url().should('include', '/auth/login');
    });

    it('Given_EmailInexistente_Should_ExibirMensagemDeErro', () => {
      // Arrange
      cy.getByDataCy('login-page').should('be.visible');

      // Act
      cy.getByDataCy('login-email').clear().type('inexistente@teste.com');
      cy.get('[data-cy="login-senha"] input').clear().type('12345678');
      cy.getByDataCy('login-submit').click();

      // Assert
      cy.contains('Email ou senha inválidos').should('be.visible');
      cy.url().should('include', '/auth/login');
    });

    it('Given_CamposVazios_Should_ExibirErrosDeValidacao', () => {
      // Arrange
      cy.getByDataCy('login-page').should('be.visible');

      // Act
      cy.getByDataCy('login-submit').click();

      // Assert
      cy.contains('Email é obrigatório').should('be.visible');
      cy.contains('Senha é obrigatória').should('be.visible');
    });

    it('Given_PaginaDeLogin_Should_NavegarParaSignupAoClicarEmCriarConta', () => {
      // Arrange
      cy.getByDataCy('login-page').should('be.visible');

      // Act
      cy.contains('a', 'Criar conta').click();

      // Assert
      cy.url().should('include', '/auth/signup');
      cy.getByDataCy('signup-page').should('be.visible');
    });
  });

  describe('Logout', () => {
    it('Given_UsuarioAutenticado_Should_DeslogarERedirecionarParaLogin', () => {
      // Arrange
      cy.login();
      cy.visit('/consulta');
      cy.getByDataCy('consulta-page').should('be.visible');

      // Act
      cy.getByDataCy('logout-button').click();

      // Assert
      cy.url().should('include', '/auth/login');
      cy.getByDataCy('login-page').should('be.visible');
    });
  });

  describe('Proteção de Rotas', () => {
    it('Given_UsuarioNaoAutenticado_Should_RedirecionarParaLoginAoAcessarRotaProtegida', () => {
      // Arrange — sem login
      cy.logout();

      // Act
      cy.visit('/consulta');

      // Assert
      cy.url().should('include', '/auth/login');
    });

    it('Given_UsuarioAutenticado_Should_RedirecionarParaConsultaAoAcessarLogin', () => {
      // Arrange
      cy.login();

      // Act
      cy.visit('/auth/login');

      // Assert — guestGuard redireciona para /
      cy.url().should('include', '/consulta');
    });
  });
});
