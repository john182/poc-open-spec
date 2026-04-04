// ---------------------------------------------------------------------------
// consultation.cy.ts — Testes E2E de consulta de alíquotas, filtros, detalhe
// ---------------------------------------------------------------------------

const aliquotasMock = {
  items: [
    {
      codigoServico: '010101001',
      codigoServicoFormatado: '01.01.01.001',
      descricaoServico: 'Análise e desenvolvimento de sistemas',
      aliquota: 2.0,
      competencia: '2026-03',
    },
    {
      codigoServico: '010201001',
      codigoServicoFormatado: '01.02.01.001',
      descricaoServico: 'Programação',
      aliquota: 3.5,
      competencia: '2026-03',
    },
    {
      codigoServico: '010301001',
      codigoServicoFormatado: '01.03.01.001',
      descricaoServico: 'Consultoria em informática',
      aliquota: 5.0,
      competencia: '2026-03',
    },
  ],
  pagina: 1,
  tamanhoPagina: 20,
  totalItens: 3,
  totalPaginas: 1,
};

const aliquotasVazias = {
  items: [],
  pagina: 1,
  tamanhoPagina: 20,
  totalItens: 0,
  totalPaginas: 0,
};

describe('Consulta de Alíquotas', () => {
  describe('Listagem com interceptação', () => {
    beforeEach(() => {
      cy.login();
      cy.intercept('GET', '/api/v1/municipios/3550308/aliquotas*', aliquotasMock).as('listarAliquotas');
      cy.visit('/consulta/municipio/3550308?uf=SP&nome=São%20Paulo');
      cy.wait('@listarAliquotas');
    });

    it('Given_MunicipioComAliquotas_Should_ExibirTabelaDeAliquotas', () => {
      // Assert
      cy.getByDataCy('municipio-aliquotas').should('be.visible');
      cy.getByDataCy('tabela-aliquotas').should('be.visible');
      cy.getByDataCy('filtros-aliquotas').should('be.visible');
    });

    it('Given_MunicipioComAliquotas_Should_ExibirDadosNaTabela', () => {
      // Assert
      cy.contains('td', '01.01.01.001').should('be.visible');
      cy.contains('td', 'Análise e desenvolvimento de sistemas').should('be.visible');
      cy.contains('td', '2,00').should('be.visible');
    });

    it('Given_CliqueEmDetalhe_Should_AbrirDialogComInformacoes', () => {
      // Act
      cy.getByDataCy('btn-detalhe').first().click();

      // Assert
      cy.getByDataCy('dialog-detalhe').should('be.visible');
      cy.contains('Detalhe da Alíquota').should('be.visible');
    });

    it('Given_FiltroPreenchido_Should_AplicarEFazerNovaRequisicao', () => {
      // Arrange
      cy.intercept('GET', '/api/v1/municipios/3550308/aliquotas*', aliquotasMock).as('listarAliquotasFiltrado');

      // Act
      cy.get('#descricao').clear().type('Programação');
      cy.getByDataCy('btn-filtrar').click();
      cy.wait('@listarAliquotasFiltrado');

      // Assert
      cy.getByDataCy('tabela-aliquotas').should('be.visible');
    });

    it('Given_FiltroAplicado_Should_LimparFiltrosERecarregar', () => {
      // Arrange
      cy.get('#descricao').clear().type('Programação');
      cy.getByDataCy('btn-filtrar').click();

      cy.intercept('GET', '/api/v1/municipios/3550308/aliquotas*', aliquotasMock).as('listarAliquotasLimpo');

      // Act
      cy.getByDataCy('btn-limpar').click();
      cy.wait('@listarAliquotasLimpo');

      // Assert
      cy.get('#descricao').should('have.value', '');
      cy.getByDataCy('tabela-aliquotas').should('be.visible');
    });

    it('Given_PaginaDeAliquotas_Should_ExibirCabecalhoComNomeDoMunicipio', () => {
      // Assert
      cy.getByDataCy('page-header').should('be.visible');
      cy.contains('São Paulo').should('be.visible');
    });
  });

  describe('Estado vazio', () => {
    it('Given_MunicipioSemAliquotas_Should_ExibirMensagemDeListaVazia', () => {
      // Arrange
      cy.login();
      cy.intercept('GET', '/api/v1/municipios/3550308/aliquotas*', aliquotasVazias).as('listarAliquotasVazias');

      // Act
      cy.visit('/consulta/municipio/3550308');
      cy.wait('@listarAliquotasVazias');

      // Assert
      cy.contains('Nenhuma alíquota encontrada').should('be.visible');
    });
  });
});
