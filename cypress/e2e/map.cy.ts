// ---------------------------------------------------------------------------
// map.cy.ts — Testes E2E do mapa de estados e seleção de estado/município
// ---------------------------------------------------------------------------

describe('Mapa de Estados', () => {
  beforeEach(() => {
    cy.login();
    cy.visit('/consulta');
    cy.getByDataCy('consulta-page').should('be.visible');
  });

  describe('Renderização', () => {
    it('Given_PaginaDeConsulta_Should_ExibirMapaDoBrasil', () => {
      // Assert
      cy.getByDataCy('brazil-map').should('be.visible');
      cy.getByDataCy('brazil-map').find('svg').should('exist');
    });

    it('Given_PaginaDeConsulta_Should_ExibirCabecalhoDaPagina', () => {
      // Assert
      cy.getByDataCy('page-header').should('be.visible');
      cy.contains('h1', 'Consulta de Alíquotas').should('be.visible');
    });

    it('Given_PaginaDeConsulta_Should_ExibirInstrucaoDeSelecao', () => {
      // Assert
      cy.contains('Selecione um estado no mapa').should('be.visible');
    });

    it('Given_PaginaDeConsulta_Should_ExibirListaDeEstados', () => {
      // Assert — verifica que pelo menos alguns estados conhecidos aparecem
      cy.getByDataCy('estado-SP').should('exist');
      cy.getByDataCy('estado-RJ').should('exist');
      cy.getByDataCy('estado-MG').should('exist');
    });
  });

  describe('Seleção de Estado', () => {
    it('Given_CliqueEmEstadoNaLista_Should_NavegarParaMunicipios', () => {
      // Act
      cy.getByDataCy('estado-SP').click();

      // Assert
      cy.url().should('include', '/consulta/estado/SP');
      cy.getByDataCy('estado-municipios').should('be.visible');
    });

    it('Given_CliqueEmEstadoNoSvg_Should_NavegarParaMunicipios', () => {
      // Act — clica no path SVG do estado
      cy.getByDataCy('brazil-map').find('g[data-uf="SP"]').click({ force: true });

      // Assert
      cy.url().should('include', '/consulta/estado/SP');
    });
  });
});

describe('Municípios por Estado', () => {
  beforeEach(() => {
    cy.login();
    cy.visit('/consulta/estado/SP');
    cy.getByDataCy('estado-municipios').should('be.visible');
  });

  it('Given_EstadoComMunicipios_Should_ExibirListaDeMunicipios', () => {
    // Assert — SP tem municípios (dados reais do seed/crawler)
    cy.get('[data-cy^="municipio-"]').should('have.length.greaterThan', 0);
  });

  it('Given_CampoDeBusca_Should_FiltrarMunicipiosPorNome', () => {
    // Arrange — capturar o nome do primeiro município para usar como termo de busca
    cy.get('[data-cy^="municipio-"]').first().invoke('text').then((textoMunicipio) => {
      const nomeMunicipio = textoMunicipio.trim().split('\n')[0].trim();
      const termoBusca = nomeMunicipio.substring(0, 4);

      // Act
      cy.getByDataCy('busca-municipio').clear().type(termoBusca);

      // Assert — deve filtrar e ainda mostrar pelo menos o município original
      cy.get('[data-cy^="municipio-"]').should('have.length.greaterThan', 0);
      cy.get('[data-cy^="municipio-"]').first().should('contain.text', termoBusca);
    });
  });

  it('Given_CliqueEmMunicipio_Should_NavegarParaAliquotas', () => {
    // Arrange — buscar um município específico para garantir que existe
    cy.get('[data-cy^="municipio-"]').first().then(($el) => {
      const dataCy = $el.attr('data-cy') || '';
      const codigoIbge = dataCy.replace('municipio-', '');

      // Act
      cy.getByDataCy(dataCy).click();

      // Assert
      cy.url().should('include', `/consulta/municipio/${codigoIbge}`);
    });
  });

  it('Given_BuscaSemResultado_Should_ExibirMensagemVazia', () => {
    // Act
    cy.getByDataCy('busca-municipio').clear().type('XYZ-municipio-inexistente-123');

    // Assert
    cy.get('[data-cy^="municipio-"]').should('have.length', 0);
  });

  it('Given_PaginaDeMunicipios_Should_ExibirCabecalhoComUf', () => {
    // Assert
    cy.getByDataCy('page-header').should('be.visible');
    cy.contains('SP').should('be.visible');
  });
});
