import { MENSAGENS_VALIDACAO } from './mensagens-validacao';

describe('MENSAGENS_VALIDACAO', () => {
  it('deve ter mensagem para required', () => {
    expect(MENSAGENS_VALIDACAO['required']).toBe('Campo obrigatório');
  });

  it('deve ter mensagem para email', () => {
    expect(MENSAGENS_VALIDACAO['email']).toBe('Email inválido');
  });

  it('deve ter mensagem para minlength', () => {
    expect(MENSAGENS_VALIDACAO['minlength']).toBe('Tamanho mínimo não atingido');
  });

  it('deve ter mensagem para passwordMismatch', () => {
    expect(MENSAGENS_VALIDACAO['passwordMismatch']).toBe('Senhas não conferem');
  });
});
