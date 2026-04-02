export interface Estado {
  codigo: number;
  nome: string;
  sigla: string;
  regiao: string;
}

export interface Municipio {
  codigoIbge: string;
  nome: string;
  siglaEstado: string;
  possuiAliquotas: boolean;
}

export type StatusProcessamentoUf =
  | 'naoProcessado'
  | 'processando'
  | 'concluido'
  | 'vencido'
  | 'processamentoIniciado'
  | 'atualizando'
  | 'aguardandoProcessamento';

export interface MunicipiosUfResponse {
  statusProcessamento: StatusProcessamentoUf;
  ultimoProcessamento: string | null;
  municipios: Municipio[];
  semCertificado: boolean;
}

export interface Aliquota {
  codigoServico: string;
  codigoServicoFormatado: string;
  descricaoServico: string;
  aliquota: number;
  competencia: string;
}

export interface AliquotaDetalhe extends Aliquota {
  codigoMunicipio: string;
  nomeMunicipio: string;
  coletadoEm: string;
}

export interface RespostaPaginada<T> {
  items: T[];
  pagina: number;
  tamanhoPagina: number;
  totalItens: number;
  totalPaginas: number;
}

export interface FiltroAliquota {
  pagina?: number;
  tamanhoPagina?: number;
  codigoServico?: string;
  descricao?: string;
  aliquotaMin?: number;
  aliquotaMax?: number;
  competencia?: string;
}
