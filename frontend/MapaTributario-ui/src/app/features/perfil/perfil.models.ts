export interface PerfilResponse {
  id: string;
  nome: string;
  email: string;
}

export interface AtualizarPerfilRequest {
  nome: string;
  senhaAtual?: string;
  novaSenha?: string;
}

export interface AtualizarPerfilResponse {
  id: string;
  nome: string;
  email: string;
  accessToken: string;
}
