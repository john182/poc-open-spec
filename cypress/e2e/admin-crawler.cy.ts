describe('Admin - Crawler (requer autenticacao)', () => {
  it('deve redirecionar para login se nao autenticado', () => {
    cy.visit('/admin/crawler/status');
    cy.url().should('include', '/auth/login');
  });

  it('deve redirecionar para acesso-negado se usuario nao admin', () => {
    // Simula token de usuario normal (sem role Admin)
    const tokenPayload = btoa(JSON.stringify({ sub: '1', email: 'user@test.com', role: 'User', exp: Math.floor(Date.now() / 1000) + 3600 }));
    const fakeToken = `eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.${tokenPayload}.fake-signature`;
    window.localStorage.setItem('accessToken', fakeToken);
    window.localStorage.setItem('rememberMe', 'true');
    cy.visit('/admin/crawler/status');
    cy.url().should('include', '/acesso-negado');
  });
});
