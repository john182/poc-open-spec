describe('Consulta - Municipios por Estado', () => {
  const municipiosMock = [
    { codigoIbge: 3106200, nome: 'Belo Horizonte', siglaEstado: 'MG' },
    { codigoIbge: 3118601, nome: 'Contagem', siglaEstado: 'MG' },
    { codigoIbge: 3170206, nome: 'Uberlândia', siglaEstado: 'MG' },
  ];

  beforeEach(() => {
    cy.intercept('GET', '/api/v1/estados/MG/municipios', municipiosMock).as('listarMunicipios');
    cy.visit('/consulta/estado/MG');
    cy.wait('@listarMunicipios');
  });

  it('deve exibir lista de municipios', () => {
    cy.getByDataCy('estado-municipios').should('be.visible');
    cy.findByText('Belo Horizonte').should('be.visible');
    cy.findByText('Contagem').should('be.visible');
    cy.findByText('3 municípios').should('be.visible');
  });

  it('deve filtrar municipios por texto', () => {
    cy.getByDataCy('busca-municipio').clear().type('Belo');
    cy.findByText('Belo Horizonte').should('be.visible');
    cy.findByText('1 municípios').should('be.visible');
  });

  it('deve navegar para aliquotas ao clicar em municipio', () => {
    cy.getByDataCy('municipio-3106200').click();
    cy.url().should('include', '/consulta/municipio/3106200');
  });
});
