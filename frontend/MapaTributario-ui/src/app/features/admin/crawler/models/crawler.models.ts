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
