// ---------------------------------------------------------------------------
// perfil.cy.ts — Testes E2E do perfil de usuário
// ---------------------------------------------------------------------------

describe('Perfil de Usuário', () => {
  const nomeOriginal = 'Admin';
  const senhaAtual = '12345678';

  beforeEach(() => {
    cy.login();
    cy.visit('/consulta');
    cy.getByDataCy('consulta-page').should('be.visible');
  });

  describe('Navegação via Dropdown da Topbar', () => {
    it('Given_UsuarioAutenticado_Should_ExibirDropdownComMeuPerfilELogout', () => {
      // Act — abrir popover do menu de usuário
      cy.getByDataCy('user-menu-trigger').click();

      // Assert
      cy.getByDataCy('menu-meu-perfil').should('be.visible');
      cy.getByDataCy('menu-sair').should('be.visible');
    });

    it('Given_CliqueEmMeuPerfil_Should_NavegarParaPaginaDePerfil', () => {
      // Act
      cy.getByDataCy('user-menu-trigger').click();
      cy.getByDataCy('menu-meu-perfil').click();

      // Assert
      cy.url().should('include', '/perfil');
      cy.getByDataCy('perfil-page').should('be.visible');
    });
  });

  describe('Visualização do Perfil', () => {
    beforeEach(() => {
      cy.visit('/perfil');
      cy.getByDataCy('perfil-page').should('be.visible');
    });

    it('Given_PaginaDePerfil_Should_ExibirNomeEEmailDoUsuario', () => {
      // Assert
      cy.getByDataCy('perfil-nome').should('be.visible');
      cy.getByDataCy('perfil-nome').should('not.have.value', '');
      cy.getByDataCy('perfil-email').should('be.visible');
      cy.getByDataCy('perfil-email').should('have.value', 'admin@admin.com');
    });

    it('Given_PaginaDePerfil_Should_ExibirEmailComoSomenteLeitura', () => {
      // Assert
      cy.getByDataCy('perfil-email').should('have.attr', 'readonly');
    });
  });

  describe('Edição de Nome', () => {
    const nomeAlterado = `Teste E2E ${Date.now()}`;

    beforeEach(() => {
      cy.visit('/perfil');
      cy.getByDataCy('perfil-page').should('be.visible');
    });

    it('Given_NomeValido_Should_AtualizarNomeEExibirMensagemDeSucesso', () => {
      // Act
      cy.getByDataCy('perfil-nome').clear().type(nomeAlterado);
      cy.getByDataCy('perfil-salvar').click();

      // Assert — mensagem de sucesso
      cy.contains('Perfil atualizado com sucesso').should('be.visible');

      // Assert — topbar deve refletir o novo nome
      cy.getByDataCy('user-name').should('contain.text', nomeAlterado);
    });

    it('Given_NomeVazio_Should_ExibirErroDeValidacao', () => {
      // Act
      cy.getByDataCy('perfil-nome').clear();
      cy.getByDataCy('perfil-salvar').click();

      // Assert
      cy.contains('Nome é obrigatório').should('be.visible');
    });

    afterEach(() => {
      // Restaurar nome original via API para não afetar outros testes
      cy.window().then((win) => {
        const token = win.localStorage.getItem('accessToken');
        if (token) {
          cy.request({
            method: 'PUT',
            url: '/api/v1/perfil',
            headers: { Authorization: `Bearer ${token}` },
            body: { nome: nomeOriginal },
          });
        }
      });
    });
  });

  describe('Alteração de Senha', () => {
    beforeEach(() => {
      cy.visit('/perfil');
      cy.getByDataCy('perfil-page').should('be.visible');
    });

    it('Given_SenhaAtualCorretaENovaSenhaValida_Should_AlterarSenhaComSucesso', () => {
      const novaSenha = 'novaSenha123';

      // Act — alterar senha
      cy.get('[data-cy="perfil-senha-atual"] input').clear().type(senhaAtual);
      cy.get('[data-cy="perfil-nova-senha"] input').clear().type(novaSenha);
      cy.get('[data-cy="perfil-confirmar-nova-senha"] input').clear().type(novaSenha);
      cy.getByDataCy('perfil-salvar').click();

      // Assert
      cy.contains('Perfil atualizado com sucesso').should('be.visible');

      // Restaurar — alterar senha de volta para a original
      cy.get('[data-cy="perfil-senha-atual"] input').clear().type(novaSenha);
      cy.get('[data-cy="perfil-nova-senha"] input').clear().type(senhaAtual);
      cy.get('[data-cy="perfil-confirmar-nova-senha"] input').clear().type(senhaAtual);
      cy.getByDataCy('perfil-salvar').click();
      cy.contains('Perfil atualizado com sucesso').should('be.visible');
    });

    it('Given_SenhaAtualIncorreta_Should_ExibirMensagemDeErro', () => {
      // Act
      cy.get('[data-cy="perfil-senha-atual"] input').clear().type('senhaErrada');
      cy.get('[data-cy="perfil-nova-senha"] input').clear().type('novaSenha123');
      cy.get('[data-cy="perfil-confirmar-nova-senha"] input').clear().type('novaSenha123');
      cy.getByDataCy('perfil-salvar').click();

      // Assert
      cy.contains('Senha atual incorreta').should('be.visible');
    });

    it('Given_NovaSenhaCurtaDemais_Should_ExibirErroDeValidacao', () => {
      // Act — preencher senha atual mas nova senha com menos de 8 caracteres
      cy.get('[data-cy="perfil-senha-atual"] input').clear().type(senhaAtual);
      cy.get('[data-cy="perfil-nova-senha"] input').clear().type('curta');
      cy.get('[data-cy="perfil-confirmar-nova-senha"] input').clear().type('curta');
      cy.getByDataCy('perfil-salvar').click();

      // Assert — backend retorna erro de validação (min 8 chars)
      cy.contains(/senha|mínimo|caracteres/i).should('be.visible');
    });

    it('Given_SenhasNaoCoincidem_Should_ExibirErroDeValidacao', () => {
      // Act — nova senha e confirmação diferentes
      cy.get('[data-cy="perfil-senha-atual"] input').clear().type(senhaAtual);
      cy.get('[data-cy="perfil-nova-senha"] input').clear().type('novaSenha123');
      cy.get('[data-cy="perfil-confirmar-nova-senha"] input').clear().type('senhaDiferente');
      cy.getByDataCy('perfil-salvar').click();

      // Assert
      cy.contains('As senhas não coincidem').should('be.visible');
    });
  });

  describe('Proteção de Rota', () => {
    it('Given_UsuarioNaoAutenticado_Should_RedirecionarParaLoginAoAcessarPerfil', () => {
      // Arrange — limpar autenticação
      cy.logout();

      // Act
      cy.visit('/perfil');

      // Assert
      cy.url().should('include', '/auth/login');
    });
  });
});
