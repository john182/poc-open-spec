# Crawler de Alíquotas com Frontend de Seleção de Cidades

Um projeto greenfield que periodicamente coleta alíquotas de impostos municipais brasileiros da API NFS-e e apresenta através de um frontend interativo onde usuários podem navegar pelas informações tributárias (somente ISS retornado pela API do ADN) por cidade e código de serviço.

**Desafio em Equipe**: A stack tecnológica é decidida pela sua equipe. Esta arquitetura é agnóstica a linguagens e frameworks. O projeto deve estar disponível em uma imagem (dockerizado).

---

## Visão Geral do Projeto

### O Que Faz

1. **Crawler**: Busca automaticamente as alíquotas atuais para municípios e códigos de serviço predefinidos da API NFS-e
2. **API Backend**: Serve dados de alíquotas em cache com capacidades de consulta rápida
3. **Frontend**: UI interativa onde os usuários:
   - Selecionam uma cidade de uma lista de municípios
   - Visualizam todos os códigos de serviço disponíveis para aquela cidade
   - Selecionam um código de serviço específico para ver informações detalhadas sobre a alíquota

### Arquitetura

```
┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│   Crawler    │────────▶│  Data Store  │◀────────│  Backend API │
│   Worker     │         │ (BD ou Em    │         │              │
│  (Agendado)  │         │   Memória)   │         └───────┬──────┘
└──────────────┘         └──────────────┘                 │
                                                           │ HTTP
                                                           │
                                                    ┌──────▼──────┐
                                                    │  Frontend   │
                                                    │     UI      │
                                                    └─────────────┘
```

**Arquitetura 3 Camadas**:
- **Crawler Worker**: Serviço agendado que busca dados da API NFS-e e popula o armazenamento de dados
- **Backend API**: API REST servindo dados em cache para o frontend
- **Frontend**: Interface do usuário para seleção de cidade e código de serviço com exibição de alíquotas

---

## Fluxo de Experiência do Usuário

1. **Seleção de Cidade**: Usuário abre o app e vê uma lista de municípios disponíveis (formato de exibição: decisão da equipe)
2. **Lista de Códigos de Serviço**: Após selecionar uma cidade, usuário vê todos os códigos de serviço disponíveis para aquele município
3. **Detalhes da Alíquota**: Usuário seleciona um código de serviço específico e visualiza informações tributárias detalhadas (alíquota, competência, etc.)

---

## Referência da API

### API NFS-e (Externa - Fonte de Dados)

**URL Base**: (da coleção Bruno)

#### Obter Alíquota Atual
```
GET /parametrizacao/{municipio}/{servico}/{competencia}/aliquota
```

**Parâmetros**:
- `municipio`: Código IBGE do município (ex: 3106200 para Belo Horizonte)
- `servico`: Código de serviço seguindo LC 116/2003 (ex: 01.01.01.001). Esse código de serviço pode estar definido de duas formas. No modelo de 6 dígitos ou no modelo de 9 dígitos.
- `competencia`: Data no formato YYYYMM (ex: 202603 para Março de 2026)

**Exemplo de Resposta**:
```json
{
  "codigoMunicipio": "3106200",
  "codigoServico": "01.01.01.001",
  "aliquota": 3.5,
  "competencia": "202603",
  "descricaoServico": "Análise e desenvolvimento de sistemas"
}
```
