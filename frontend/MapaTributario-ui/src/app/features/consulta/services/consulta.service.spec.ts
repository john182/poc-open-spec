import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ConsultaService } from './consulta.service';

describe('ConsultaService', () => {
  let service: ConsultaService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ConsultaService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('deve listar estados', () => {
    const estadosMock = [{ codigo: 31, nome: 'Minas Gerais', sigla: 'MG', regiao: 'Sudeste' }];
    service.listarEstados().subscribe(estados => {
      expect(estados).toEqual(estadosMock);
    });
    const req = httpTesting.expectOne('/api/v1/estados');
    expect(req.request.method).toBe('GET');
    req.flush(estadosMock);
  });

  it('deve listar municipios por UF', () => {
    const respostaMock = {
      statusProcessamento: 'concluido',
      ultimoProcessamento: '2026-03-28T10:00:00Z',
      municipios: [{ codigoIbge: '3106200', nome: 'Belo Horizonte', siglaEstado: 'MG', possuiAliquotas: true }],
    };
    service.listarMunicipios('MG').subscribe(resposta => {
      expect(resposta).toEqual(respostaMock);
    });
    const req = httpTesting.expectOne('/api/v1/estados/MG/municipios');
    expect(req.request.method).toBe('GET');
    req.flush(respostaMock);
  });

  it('deve listar aliquotas sem filtros', () => {
    const respostaMock = { items: [], pagina: 1, tamanhoPagina: 20, totalItens: 0, totalPaginas: 0 };
    service.listarAliquotas('3106200').subscribe(resp => {
      expect(resp).toEqual(respostaMock);
    });
    const req = httpTesting.expectOne('/api/v1/municipios/3106200/aliquotas');
    expect(req.request.method).toBe('GET');
    req.flush(respostaMock);
  });

  it('deve listar aliquotas com filtros', () => {
    const respostaMock = { items: [], pagina: 1, tamanhoPagina: 10, totalItens: 0, totalPaginas: 0 };
    service.listarAliquotas('3106200', {
      pagina: 1,
      tamanhoPagina: 10,
      codigoServico: '01.01',
      aliquotaMin: 2,
      aliquotaMax: 5,
    }).subscribe(resp => {
      expect(resp).toEqual(respostaMock);
    });
    const req = httpTesting.expectOne(r =>
      r.url === '/api/v1/municipios/3106200/aliquotas'
      && r.params.get('codigoServico') === '01.01'
      && r.params.get('aliquotaMin') === '2'
      && r.params.get('aliquotaMax') === '5',
    );
    expect(req.request.method).toBe('GET');
    req.flush(respostaMock);
  });

  it('deve obter detalhe de aliquota', () => {
    const detalheMock = {
      codigoServico: '010101001',
      codigoServicoFormatado: '01.01.01.001',
      descricaoServico: 'Analise',
      aliquota: 2.0,
      competencia: '2026-03-01',
      codigoMunicipio: '3106200',
      nomeMunicipio: 'Belo Horizonte',
      coletadoEm: '2026-03-15T10:00:00Z',
    };
    service.obterDetalheAliquota('3106200', '010101001').subscribe(detalhe => {
      expect(detalhe).toEqual(detalheMock);
    });
    const req = httpTesting.expectOne('/api/v1/municipios/3106200/aliquotas/010101001');
    expect(req.request.method).toBe('GET');
    req.flush(detalheMock);
  });
});
