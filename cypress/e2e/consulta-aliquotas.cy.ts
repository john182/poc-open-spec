describe('Consulta - Aliquotas por Municipio', () => {
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
    ],
    pagina: 1,
    tamanhoPagina: 20,
    totalItens: 2,
    totalPaginas: 1,
  };

  const aliquotasVazias = {
    items: [],
    pagina: 1,
    tamanhoPagina: 20,
    totalItens: 0,
    totalPaginas: 0,
  };

  it('deve exibir tabela de aliquotas', () => {
    cy.intercept('GET', '/api/v1/municipios/3106200/aliquotas*', aliquotasMock).as('listarAliquotas');
    cy.visit('/consulta/municipio/3106200');
    cy.wait('@listarAliquotas');
    cy.getByDataCy('municipio-aliquotas').should('be.visible');
    cy.getByDataCy('filtros-aliquotas').should('be.visible');
  });

  it('deve exibir empty state quando nao ha aliquotas', () => {
    cy.intercept('GET', '/api/v1/municipios/3106200/aliquotas*', aliquotasVazias).as('listarAliquotas');
    cy.visit('/consulta/municipio/3106200');
    cy.wait('@listarAliquotas');
    cy.findByText('Nenhuma alíquota encontrada').should('be.visible');
  });

  it('deve aplicar filtros e limpar', () => {
    cy.intercept('GET', '/api/v1/municipios/3106200/aliquotas*', aliquotasMock).as('listarAliquotas');
    cy.visit('/consulta/municipio/3106200');
    cy.wait('@listarAliquotas');

    cy.get('#codigoServico').type('01.01');
    cy.getByDataCy('btn-filtrar').click();
    cy.wait('@listarAliquotas');

    cy.getByDataCy('btn-limpar').click();
    cy.wait('@listarAliquotas');
    cy.get('#codigoServico').should('have.value', '');
  });

  it('deve exibir erro quando API falha', () => {
    cy.intercept('GET', '/api/v1/municipios/3106200/aliquotas*', { statusCode: 500 }).as('listarAliquotasErro');
    cy.visit('/consulta/municipio/3106200');
    cy.wait('@listarAliquotasErro');
    cy.findByText(/Erro ao carregar alíquotas/).should('be.visible');
  });
});
