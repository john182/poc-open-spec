// ---------------------------------------------------------------------------
// errors.cy.ts — Testes E2E de páginas de erro (404, acesso negado, erros API)
// ---------------------------------------------------------------------------

describe('Páginas de Erro', () => {
  describe('Página 404', () => {
    it('Given_RotaInexistente_Should_ExibirPagina404', () => {
      // Act
      cy.visit('/rota-que-nao-existe', { failOnStatusCode: false });

      // Assert
      cy.getByDataCy('not-found-page').should('be.visible');
      cy.contains('404').should('be.visible');
      cy.contains('Página não encontrada').should('be.visible');
      cy.contains('O recurso solicitado não está disponível').should('be.visible');
    });

    it('Given_Pagina404_Should_TerLinkParaVoltarAoInicio', () => {
      // Arrange
      cy.visit('/rota-inexistente', { failOnStatusCode: false });
      cy.getByDataCy('not-found-page').should('be.visible');

      // Act
      cy.contains('Voltar ao início').click();

      // Assert — redireciona para login (pois não autenticado) ou consulta (se autenticado)
      cy.url().should('match', /\/(consulta|auth\/login)/);
    });
  });

  describe('Acesso Negado', () => {
    it('Given_PaginaDeAcessoNegado_Should_ExibirMensagemEIconeDeCadeado', () => {
      // Act
      cy.visit('/acesso-negado');

      // Assert
      cy.getByDataCy('access-denied-page').should('be.visible');
      cy.contains('Acesso Negado').should('be.visible');
      cy.contains('Você não tem permissão para acessar esta página').should('be.visible');
      cy.get('.pi-lock').should('be.visible');
    });

    it('Given_PaginaDeAcessoNegado_Should_TerLinkParaVoltarAoInicio', () => {
      // Arrange
      cy.visit('/acesso-negado');
      cy.getByDataCy('access-denied-page').should('be.visible');

      // Act
      cy.contains('Voltar ao início').click();

      // Assert
      cy.url().should('match', /\/(consulta|auth\/login)/);
    });

    it('Given_UsuarioNaoAutenticado_Should_RedirecionarParaLoginAoAcessarRotaAdmin', () => {
      // Arrange
      cy.logout();

      // Act
      cy.visit('/admin/crawler/status');

      // Assert
      cy.url().should('include', '/auth/login');
    });
  });

  describe('Erros de API', () => {
    it('Given_FalhaAoCarregarEstados_Should_ExibirEstadoDeErro', () => {
      // Arrange
      cy.login();
      cy.intercept('GET', '/api/v1/estados', { statusCode: 500, body: {} }).as('estadosErro');

      // Act
      cy.visit('/consulta');
      cy.wait('@estadosErro');

      // Assert
      cy.getByDataCy('error-state').should('be.visible');
      cy.contains('Tentar novamente').should('be.visible');
    });

    it('Given_FalhaAoCarregarMunicipios_Should_ExibirEstadoDeErro', () => {
      // Arrange
      cy.login();
      cy.intercept('GET', '/api/v1/estados/SP/municipios', { statusCode: 500, body: {} }).as('municipiosErro');

      // Act
      cy.visit('/consulta/estado/SP');
      cy.wait('@municipiosErro');

      // Assert
      cy.getByDataCy('error-state').should('be.visible');
    });

    it('Given_FalhaAoCarregarAliquotas_Should_ExibirEstadoDeErro', () => {
      // Arrange
      cy.login();
      cy.intercept('GET', '/api/v1/municipios/3550308/aliquotas*', { statusCode: 500, body: {} }).as('aliquotasErro');

      // Act
      cy.visit('/consulta/municipio/3550308');
      cy.wait('@aliquotasErro');

      // Assert
      cy.getByDataCy('error-state').should('be.visible');
    });

    it('Given_TimeoutDeAPI_Should_ExibirEstadoDeErro', () => {
      // Arrange
      cy.login();
      cy.intercept('GET', '/api/v1/estados', { forceNetworkError: true }).as('estadosTimeout');

      // Act
      cy.visit('/consulta');
      cy.wait('@estadosTimeout');

      // Assert
      cy.getByDataCy('error-state').should('be.visible');
    });
  });
});
