export interface StatusCrawler {
  id: string;
  inicio: string;
  fim: string | null;
  status: string;
  tipo: string;
  totalMunicipios: number;
  totalServicos: number;
  processados: number;
  erros: number;
  detalhesErro: string[];
  temCertificado: boolean;
  ufAtual: string | null;
  ufsProcessadas: string[];
  progressoUfs: Record<string, ProgressoUf>;
}

export interface ProgressoUf {
  uf: string;
  status: string;
  municipiosEncontrados: number;
  inicio: string;
  fim: string | null;
}

export interface ExecutarCrawlerRequest {
  forcarReprocessamento: boolean;
  ufs?: string[];
}

export interface ExecutarCrawlerResponse {
  execucaoId?: string;
  mensagem: string;
}

export interface CertificadoStatus {
  hasCertificate: boolean;
  uploadedAt: string | null;
}

export interface ConfiguracaoCrawler {
  id: string;
  cronSchedule: string;
  limiteRequisicoesPorSegundo: number;
  orcamentoDiario: number;
  tamanhoLoteCertificado: number;
  pausaLoteSegundos: number;
  tamanhoLoteMongo: number;
  maxTentativas: number;
  limiteParadaAntecipada: number;
  maxDesdobramento: number;
  maxDetalhamento: number;
  maxFalhasConsecutivasDetalhamento: number;
  maxFalhasConsecutivasDesdobramento: number;
  maxItensParalelos: number;
  codigosSondagem: string[];
  validadeDiasProcessamento: number;
  circuitBreakerLimiarErroPercent: number;
  circuitBreakerJanelaAvaliacaoSegundos: number;
  circuitBreakerPausaSegundos: number;
  circuitBreakerAmostraMinima: number;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string;
}

export interface AtualizarConfiguracaoCrawlerRequest {
  cronSchedule: string;
  limiteRequisicoesPorSegundo: number;
  orcamentoDiario: number;
  tamanhoLoteCertificado: number;
  pausaLoteSegundos: number;
  tamanhoLoteMongo: number;
  maxTentativas: number;
  limiteParadaAntecipada: number;
  maxDesdobramento: number;
  maxDetalhamento: number;
  maxFalhasConsecutivasDetalhamento: number;
  maxFalhasConsecutivasDesdobramento: number;
  maxItensParalelos: number;
  codigosSondagem: string[];
  validadeDiasProcessamento: number;
  circuitBreakerLimiarErroPercent: number;
  circuitBreakerJanelaAvaliacaoSegundos: number;
  circuitBreakerPausaSegundos: number;
  circuitBreakerAmostraMinima: number;
  ativo: boolean;
}
