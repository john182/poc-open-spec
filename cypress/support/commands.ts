Cypress.Commands.add('getByDataCy', (selector: string) => {
  return cy.get(`[data-cy="${selector}"]`);
});

Cypress.Commands.add('login', (email = 'admin@mapatributario.com', senha = 'Admin@123') => {
  cy.request({
    method: 'POST',
    url: '/api/v1/auth/login',
    body: { email, senha },
    failOnStatusCode: false,
  }).then((resp) => {
    if (resp.status === 200 && resp.body.token) {
      window.localStorage.setItem('token', resp.body.token);
      if (resp.body.refreshToken) {
        window.localStorage.setItem('refreshToken', resp.body.refreshToken);
      }
    }
  });
});

declare global {
  namespace Cypress {
    interface Chainable {
      getByDataCy(selector: string): Chainable<JQuery<HTMLElement>>;
      login(email?: string, senha?: string): Chainable<void>;
    }
  }
}

export {};
