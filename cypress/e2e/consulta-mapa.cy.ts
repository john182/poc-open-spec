describe('Consulta - Mapa de Estados', () => {
  const estadosMock = [
    { codigo: 31, nome: 'Minas Gerais', sigla: 'MG', regiao: 'Sudeste' },
    { codigo: 35, nome: 'São Paulo', sigla: 'SP', regiao: 'Sudeste' },
    { codigo: 33, nome: 'Rio de Janeiro', sigla: 'RJ', regiao: 'Sudeste' },
  ];

  beforeEach(() => {
    cy.intercept('GET', '/api/v1/estados', estadosMock).as('listarEstados');
  });

  it('deve exibir mapa do Brasil e lista de estados', () => {
    cy.visit('/consulta');
    cy.wait('@listarEstados');
    cy.getByDataCy('consulta-page').should('be.visible');
    cy.getByDataCy('brazil-map').should('be.visible');
    cy.findByText('Minas Gerais').should('be.visible');
    cy.findByText('São Paulo').should('be.visible');
  });

  it('deve navegar para municipios ao clicar em estado na lista', () => {
    cy.visit('/consulta');
    cy.wait('@listarEstados');
    cy.getByDataCy('estado-MG').click();
    cy.url().should('include', '/consulta/estado/MG');
  });

  it('deve exibir erro quando API falha', () => {
    cy.intercept('GET', '/api/v1/estados', { statusCode: 500 }).as('listarEstadosErro');
    cy.visit('/consulta');
    cy.wait('@listarEstadosErro');
    cy.findByText(/Erro ao carregar estados/).should('be.visible');
  });
});
