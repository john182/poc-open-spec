// ---------------------------------------------------------------------------
// Custom commands para testes E2E do Mapa Tributário
// ---------------------------------------------------------------------------

/**
 * Seleciona elemento pelo atributo data-cy.
 */
Cypress.Commands.add('getByDataCy', (seletor: string) => {
  return cy.get(`[data-cy="${seletor}"]`);
});

/**
 * Login programático via API.
 * Armazena tokens no localStorage para que o app reconheça o usuário autenticado.
 * Credenciais padrão: admin seed (admin@admin.com / 12345678).
 */
Cypress.Commands.add('login', (email = 'admin@admin.com', senha = '12345678') => {
  cy.request({
    method: 'POST',
    url: '/api/v1/auth/login',
    body: { email, senha },
  }).then((resposta) => {
    expect(resposta.status).to.eq(200);
    window.localStorage.setItem('rememberMe', 'true');
    window.localStorage.setItem('accessToken', resposta.body.accessToken);
    if (resposta.body.refreshToken) {
      window.localStorage.setItem('refreshToken', resposta.body.refreshToken);
    }
  });
});

/**
 * Logout programático — limpa tokens do storage.
 */
Cypress.Commands.add('logout', () => {
  window.localStorage.removeItem('accessToken');
  window.localStorage.removeItem('refreshToken');
  window.localStorage.removeItem('rememberMe');
  window.sessionStorage.removeItem('accessToken');
  window.sessionStorage.removeItem('refreshToken');
});

// ---------------------------------------------------------------------------
// Declaração dos tipos para os custom commands
// ---------------------------------------------------------------------------
declare global {
  namespace Cypress {
    interface Chainable {
      /** Seleciona elemento por data-cy */
      getByDataCy(seletor: string): Chainable<JQuery<HTMLElement>>;
      /** Login programático via API */
      login(email?: string, senha?: string): Chainable<void>;
      /** Logout programático */
      logout(): Chainable<void>;
    }
  }
}

export {};
