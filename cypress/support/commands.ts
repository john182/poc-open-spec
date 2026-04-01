Cypress.Commands.add('getByDataCy', (selector: string) => {
  return cy.get(`[data-cy="${selector}"]`);
});

declare global {
  namespace Cypress {
    interface Chainable {
      getByDataCy(selector: string): Chainable<JQuery<HTMLElement>>;
    }
  }
}

export {};
