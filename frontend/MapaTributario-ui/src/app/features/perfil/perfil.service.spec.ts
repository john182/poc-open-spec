import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PerfilService } from './perfil.service';
import { AtualizarPerfilRequest } from './perfil.models';

describe('PerfilService', () => {
  let service: PerfilService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PerfilService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('deve obter perfil do usuario', () => {
    const perfilMock = { id: '123', nome: 'João', email: 'joao@test.com' };
    service.obterPerfil().subscribe(perfil => {
      expect(perfil).toEqual(perfilMock);
    });
    const req = httpTesting.expectOne('/api/v1/perfil');
    expect(req.request.method).toBe('GET');
    req.flush(perfilMock);
  });

  it('deve atualizar perfil apenas com nome', () => {
    const dados: AtualizarPerfilRequest = { nome: 'João Atualizado' };
    const respostaMock = { id: '123', nome: 'João Atualizado', email: 'joao@test.com', accessToken: 'novo-token' };

    service.atualizarPerfil(dados).subscribe(resposta => {
      expect(resposta).toEqual(respostaMock);
    });

    const req = httpTesting.expectOne('/api/v1/perfil');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ nome: 'João Atualizado' });
    req.flush(respostaMock);
  });

  it('deve atualizar perfil com nome e senha', () => {
    const dados: AtualizarPerfilRequest = { nome: 'João', senhaAtual: 'senha123', novaSenha: 'novaSenha123' };
    const respostaMock = { id: '123', nome: 'João', email: 'joao@test.com', accessToken: 'novo-token' };

    service.atualizarPerfil(dados).subscribe(resposta => {
      expect(resposta).toEqual(respostaMock);
    });

    const req = httpTesting.expectOne('/api/v1/perfil');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ nome: 'João', senhaAtual: 'senha123', novaSenha: 'novaSenha123' });
    req.flush(respostaMock);
  });

  it('deve propagar erro ao obter perfil', () => {
    let statusErro = 0;
    service.obterPerfil().subscribe({
      error: (err) => { statusErro = err.status; },
    });

    httpTesting.expectOne('/api/v1/perfil').flush(
      { erro: 'Não autorizado' },
      { status: 401, statusText: 'Unauthorized' },
    );
    expect(statusErro).toBe(401);
  });

  it('deve propagar erro ao atualizar perfil', () => {
    let statusErro = 0;
    service.atualizarPerfil({ nome: 'João' }).subscribe({
      error: (err) => { statusErro = err.status; },
    });

    httpTesting.expectOne('/api/v1/perfil').flush(
      { erro: 'Senha incorreta' },
      { status: 400, statusText: 'Bad Request' },
    );
    expect(statusErro).toBe(400);
  });
});
