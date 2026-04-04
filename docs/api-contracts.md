# Contratos de API - Mapa Tributario

> Documento de referencia para todos os endpoints REST internos do backend.
> Versao da API: `v1` | Base URL: `/api/v1`

---

## Indice

- [Convencoes Gerais](#convencoes-gerais)
- [Autenticacao (Auth)](#autenticacao-auth)
  - [POST /api/v1/auth/register](#post-apiv1authregister)
  - [POST /api/v1/auth/login](#post-apiv1authlogin)
  - [POST /api/v1/auth/refresh](#post-apiv1authrefresh)
- [Consulta de Dados](#consulta-de-dados)
  - [GET /api/v1/estados](#get-apiv1estados)
  - [GET /api/v1/estados/:uf/municipios](#get-apiv1estadosufmunicipios)
  - [GET /api/v1/municipios/:codigoIbge/aliquotas](#get-apiv1municipioscodigoibgealiquotas)
  - [GET /api/v1/municipios/:codigoIbge/aliquotas/:codigoServico](#get-apiv1municipioscodigoibgealiquotascodigoservico)
- [Crawler Admin](#crawler-admin)
  - [POST /api/v1/crawler/executar](#post-apiv1crawlerexecutar)
  - [GET /api/v1/crawler/status](#get-apiv1crawlerstatus)
  - [GET /api/v1/crawler/execucoes](#get-apiv1crawlerexecucoes)
  - [POST /api/v1/crawler/certificado](#post-apiv1crawlercertificado)
  - [GET /api/v1/crawler/certificado](#get-apiv1crawlercertificado)
  - [DELETE /api/v1/crawler/certificado](#delete-apiv1crawlercertificado)
  - [GET /api/v1/crawler/configuracao](#get-apiv1crawlerconfiguracao)
  - [PUT /api/v1/crawler/configuracao](#put-apiv1crawlerconfiguracao)
  - [PATCH /api/v1/crawler/configuracao](#patch-apiv1crawlerconfiguracao)
- [Health Check](#health-check)
  - [GET /health](#get-health)
- [Formato Padrao de Erro](#formato-padrao-de-erro)
- [Referencia: API Externa NFS-e](#referencia-api-externa-nfs-e)

---

## Convencoes Gerais

| Aspecto | Convencao |
|---------|-----------|
| Content-Type | `application/json` em todas as requisicoes e respostas |
| Autenticacao | JWT Bearer token no header `Authorization: Bearer {accessToken}` |
| Versionamento | Prefixo `/api/v1/` em todos os endpoints (exceto health) |
| Paginacao | Body com `{ items, pagina, tamanhoPagina, totalItens, totalPaginas }` |
| Erros | Body com `{ erro, detalhes?, codigo? }` |
| Codigo de servico | Aceita `01.01.01.001` (pontos) ou `010101001` (numerico); resposta sempre formatada com pontos |
| Competencia | Formato `YYYYMM` nos query params; armazenado internamente como `YYYY-MM-01` |
| Ordenacao | Alfabetica por nome (estados, municipios); por codigo de servico (aliquotas) |

---

## Autenticacao (Auth)

Endpoints publicos (nao requerem JWT).

---

### POST /api/v1/auth/register

Cadastra um novo usuario no sistema.

**Autenticacao:** Nenhuma

**Request Body:**

```json
{
  "email": "string (required, email valido)",
  "nome": "string (required, min 2 caracteres)",
  "senha": "string (required, min 8 caracteres)"
}
```

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 201 Created | Cadastro realizado com sucesso |
| 400 Bad Request | Validacao falhou |
| 409 Conflict | Email ja cadastrado |

**Response Body (201):**

```json
{
  "accessToken": "string (JWT)",
  "refreshToken": "string (opaque token)",
  "expiresIn": 3600
}
```

**Response Body (400):**

```json
{
  "erro": "Validacao falhou",
  "detalhes": [
    "O campo email e obrigatorio",
    "A senha deve ter no minimo 8 caracteres"
  ]
}
```

**Response Body (409):**

```json
{
  "erro": "Email ja cadastrado"
}
```

**Exemplo de requisicao:**

```http
POST /api/v1/auth/register HTTP/1.1
Content-Type: application/json

{
  "email": "maria@exemplo.com",
  "nome": "Maria Silva",
  "senha": "MinhaSenh@123"
}
```

**Exemplo de resposta (201):**

```http
HTTP/1.1 201 Created
Content-Type: application/json

{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g...",
  "expiresIn": 3600
}
```

---

### POST /api/v1/auth/login

Autentica um usuario existente.

**Autenticacao:** Nenhuma

**Request Body:**

```json
{
  "email": "string (required)",
  "senha": "string (required)"
}
```

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Login realizado com sucesso |
| 400 Bad Request | Campos obrigatorios ausentes |
| 401 Unauthorized | Credenciais invalidas |

**Response Body (200):**

```json
{
  "accessToken": "string (JWT)",
  "refreshToken": "string (opaque token)",
  "expiresIn": 3600
}
```

**Response Body (401):**

```json
{
  "erro": "Credenciais invalidas"
}
```

**Exemplo de requisicao:**

```http
POST /api/v1/auth/login HTTP/1.1
Content-Type: application/json

{
  "email": "maria@exemplo.com",
  "senha": "MinhaSenh@123"
}
```

**Exemplo de resposta (200):**

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g...",
  "expiresIn": 3600
}
```

---

### POST /api/v1/auth/refresh

Renova o access token usando um refresh token valido.

**Autenticacao:** Nenhuma (o refresh token esta no body)

**Request Body:**

```json
{
  "refreshToken": "string (required)"
}
```

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Token renovado com sucesso |
| 400 Bad Request | Refresh token ausente |
| 401 Unauthorized | Refresh token invalido ou expirado |

**Response Body (200):**

```json
{
  "accessToken": "string (JWT)",
  "refreshToken": "string (novo refresh token)",
  "expiresIn": 3600
}
```

> **Nota:** O endpoint de refresh retorna um novo `refreshToken` alem do `accessToken`, implementando rotacao de refresh tokens. O frontend deve substituir ambos os tokens armazenados.

**Response Body (401):**

```json
{
  "erro": "Token invalido"
}
```

**Exemplo de requisicao:**

```http
POST /api/v1/auth/refresh HTTP/1.1
Content-Type: application/json

{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g..."
}
```

**Exemplo de resposta (200):**

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...(novo token)",
  "refreshToken": "dGhpcyBpcyBhIG5vdm8gcmVm...(novo refresh)",
  "expiresIn": 3600
}
```

---

## Consulta de Dados

Endpoints protegidos — requerem autenticacao JWT via `[Authorize]`. Qualquer usuario autenticado pode consultar dados.

---

### GET /api/v1/estados

Retorna a lista de todos os 27 estados brasileiros.

**Autenticacao:** JWT Bearer (obrigatorio)

**Parametros:** Nenhum

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Lista de estados |

**Response Body (200):**

```json
[
  {
    "sigla": "MG",
    "nome": "Minas Gerais",
    "regiao": "Sudeste"
  },
  {
    "sigla": "SP",
    "nome": "Sao Paulo",
    "regiao": "Sudeste"
  }
]
```

| Campo | Tipo | Descricao |
|-------|------|-----------|
| sigla | string | Sigla da UF (2 caracteres) |
| nome | string | Nome completo do estado |
| regiao | string | Sigla da regiao geografica: `N` (Norte), `NE` (Nordeste), `CO` (Centro-Oeste), `SE` (Sudeste), `S` (Sul) |

> **Nota:** O campo `codigo` (codigo IBGE do estado) nao e exposto no response. A sigla da UF e usada como identificador nos endpoints de consulta.

**Exemplo de requisicao:**

```http
GET /api/v1/estados HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

**Exemplo de resposta (200):**

```http
HTTP/1.1 200 OK
Content-Type: application/json

[
  { "sigla": "AC", "nome": "Acre", "regiao": "N" },
  { "sigla": "AL", "nome": "Alagoas", "regiao": "NE" },
  { "sigla": "AM", "nome": "Amazonas", "regiao": "N" }
]
```

---

### GET /api/v1/estados/:uf/municipios

Retorna a lista de municipios de um estado especifico, com informacoes sobre o status de processamento do crawler.

**Autenticacao:** JWT Bearer (obrigatorio)

**Path Parameters:**

| Parametro | Tipo | Descricao |
|-----------|------|-----------|
| uf | string | Sigla da UF (2 caracteres, case-insensitive). Ex: `MG`, `SP`, `RJ` |

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Lista de municipios do estado com status de processamento |
| 404 Not Found | UF invalida ou nao encontrada |

**Response Body (200):**

```json
{
  "statusProcessamento": "processando",
  "ultimoProcessamento": "2026-03-15T02:00:00Z",
  "semCertificado": false,
  "municipios": [
    {
      "codigoIbge": "3106200",
      "nome": "Belo Horizonte",
      "siglaEstado": "MG",
      "possuiAliquotas": true
    },
    {
      "codigoIbge": "3118601",
      "nome": "Contagem",
      "siglaEstado": "MG",
      "possuiAliquotas": false
    }
  ]
}
```

| Campo | Tipo | Descricao |
|-------|------|-----------|
| statusProcessamento | string | Status do processamento do crawler para esta UF: `aguardandoProcessamento`, `processando`, `concluido` |
| ultimoProcessamento | string (ISO 8601) ou null | Data/hora do ultimo processamento concluido |
| semCertificado | boolean | Se `true`, nao ha certificado PFX disponivel para o crawler |
| municipios | array | Lista de municipios encontrados pelo crawler |
| municipios[].codigoIbge | string | Codigo IBGE do municipio (7 digitos) |
| municipios[].nome | string | Nome do municipio |
| municipios[].siglaEstado | string | Sigla da UF a que pertence |
| municipios[].possuiAliquotas | boolean | Se `true`, o municipio ja possui aliquotas coletadas |

> **Nota:** A lista de municipios retornada depende do processamento do crawler. Se o crawler ainda nao processou a UF, a lista pode estar vazia mesmo que existam municipios cadastrados no seed IBGE. O campo `semCertificado` indica se o upload de certificado PFX e necessario antes de iniciar o crawler.

**Response Body (404):**

```json
{
  "erro": "Estado nao encontrado"
}
```

**Exemplo de requisicao:**

```http
GET /api/v1/estados/MG/municipios HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

---

### GET /api/v1/municipios/:codigoIbge/aliquotas

Retorna lista paginada de aliquotas de servicos para um municipio.

**Autenticacao:** JWT Bearer (obrigatorio)

**Path Parameters:**

| Parametro | Tipo | Descricao |
|-----------|------|-----------|
| codigoIbge | string | Codigo IBGE do municipio (7 digitos). Ex: `3106200` |

**Query Parameters:**

| Parametro | Tipo | Default | Descricao |
|-----------|------|---------|-----------|
| pagina | integer | 1 | Numero da pagina (min: 1) |
| tamanhoPagina | integer | 20 | Itens por pagina (min: 1, max: 100) |
| codigoServico | string | - | Filtro por prefixo do codigo de servico. Ex: `01.01` retorna todos que comecam com `01.01` |
| descricao | string | - | Filtro por texto na descricao do servico (case-insensitive, busca parcial) |
| aliquotaMin | decimal | - | Filtro por aliquota minima (inclusivo) |
| aliquotaMax | decimal | - | Filtro por aliquota maxima (inclusivo) |
| competencia | string | - | Filtro por competencia no formato `YYYYMM`. Ex: `202603` |

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Lista paginada de aliquotas |
| 400 Bad Request | Parametros de query invalidos |
| 404 Not Found | Municipio nao encontrado no cadastro |

**Response Body (200):**

```json
{
  "items": [
    {
      "codigoServico": "010101001",
      "codigoServicoFormatado": "01.01.01.001",
      "descricaoServico": "Analise e desenvolvimento de sistemas",
      "valorAliquota": 2.00,
      "competencia": "2026-03-01"
    },
    {
      "codigoServico": "010102001",
      "codigoServicoFormatado": "01.01.02.001",
      "descricaoServico": "Programacao",
      "valorAliquota": 2.00,
      "competencia": "2026-03-01"
    }
  ],
  "pagina": 1,
  "tamanhoPagina": 20,
  "totalItens": 342,
  "totalPaginas": 18
}
```

| Campo | Tipo | Descricao |
|-------|------|-----------|
| items | array | Lista de aliquotas da pagina corrente |
| items[].codigoServico | string | Codigo numerico puro (sem pontos) |
| items[].codigoServicoFormatado | string | Codigo formatado com pontos (XX.XX.XX.XXX) |
| items[].descricaoServico | string | Descricao do servico conforme LC 116/2003 |
| items[].valorAliquota | decimal | Aliquota de ISS em percentual (ex: 2.00 = 2%) |
| items[].competencia | string | Competencia no formato YYYY-MM-01 |
| pagina | integer | Pagina atual |
| tamanhoPagina | integer | Tamanho da pagina |
| totalItens | integer | Total de registros que atendem os filtros |
| totalPaginas | integer | Total de paginas calculado |

**Response Body (400):**

```json
{
  "erro": "Validacao falhou",
  "detalhes": [
    "tamanhoPagina deve ser entre 1 e 100",
    "aliquotaMin deve ser um numero positivo"
  ]
}
```

**Exemplo de requisicao com filtros:**

```http
GET /api/v1/municipios/3106200/aliquotas?pagina=1&tamanhoPagina=10&codigoServico=01.01&aliquotaMin=2&aliquotaMax=5 HTTP/1.1
```

**Exemplo de requisicao sem filtros (defaults):**

```http
GET /api/v1/municipios/3106200/aliquotas HTTP/1.1
```

---

### GET /api/v1/municipios/:codigoIbge/aliquotas/:codigoServico

Retorna o detalhe de uma aliquota especifica para um servico em um municipio.

**Autenticacao:** JWT Bearer (obrigatorio)

**Path Parameters:**

| Parametro | Tipo | Descricao |
|-----------|------|-----------|
| codigoIbge | string | Codigo IBGE do municipio (7 digitos) |
| codigoServico | string | Codigo do servico. Aceita formato com pontos (`01.01.01.001`) ou numerico (`010101001`) |

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Detalhe da aliquota encontrado |
| 404 Not Found | Municipio ou servico nao encontrado |

**Response Body (200):**

```json
{
  "codigoMunicipio": "3106200",
  "nomeMunicipio": "Belo Horizonte",
  "codigoServico": "010101001",
  "codigoServicoFormatado": "01.01.01.001",
  "descricaoServico": "Analise e desenvolvimento de sistemas",
  "aliquota": 2.00,
  "competencia": "2026-03-01",
  "coletadoEm": "2026-03-15T02:34:12Z"
}
```

| Campo | Tipo | Descricao |
|-------|------|-----------|
| codigoMunicipio | string | Codigo IBGE do municipio |
| nomeMunicipio | string | Nome do municipio |
| codigoServico | string | Codigo numerico puro |
| codigoServicoFormatado | string | Codigo formatado com pontos |
| descricaoServico | string | Descricao do servico |
| aliquota | decimal | Aliquota de ISS em percentual (ex: 2.00 = 2%) |
| competencia | string | Competencia (YYYY-MM-01) |
| coletadoEm | string (ISO 8601) | Data/hora em que o dado foi coletado da API NFS-e |

**Response Body (404):**

```json
{
  "erro": "Aliquota nao encontrada para este municipio e servico"
}
```

**Exemplo de requisicao:**

```http
GET /api/v1/municipios/3106200/aliquotas/01.01.01.001 HTTP/1.1
```

---

## Crawler Admin

Endpoints para gerenciamento do worker/crawler. Requerem autenticacao JWT com role `Admin` via `[Authorize(Roles = "Admin")]`.

> **Nota:** Somente usuarios com a role `Admin` podem acessar estes endpoints. O usuario admin seed (`admin@admin.com`) ja possui esta role.

---

### POST /api/v1/crawler/executar

Dispara uma execucao manual do crawler.

**Autenticacao:** JWT Bearer + role Admin

**Request Body (opcional):**

```json
{
  "forcarReprocessamento": false,
  "ufs": ["SP", "RJ"],
  "capitaisPrimeiro": true
}
```

| Campo | Tipo | Default | Descricao |
|-------|------|---------|-----------|
| forcarReprocessamento | boolean | false | Se `true`, regenera a fila completa ignorando dados ja coletados na competencia atual |
| ufs | string[] | null | Lista de UFs para filtrar a execucao. Se null ou vazio, processa todas as 27 UFs |
| capitaisPrimeiro | boolean | false | Se `true`, processa primeiro as 27 capitais estaduais e depois os demais municipios |

> **Nota:** Quando `capitaisPrimeiro` e `true`, o crawler processa primeiro as 27 capitais estaduais e em seguida automaticamente os demais municipios. O campo `ufs` e ignorado quando `capitaisPrimeiro` e `true`.

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 202 Accepted | Execucao iniciada com sucesso |
| 400 Bad Request | Nenhum certificado disponivel |
| 401 Unauthorized | Token ausente ou invalido |
| 403 Forbidden | Usuario nao possui role Admin |
| 409 Conflict | Ja existe uma execucao em andamento |

**Response Body (202):**

```json
{
  "execucaoId": null,
  "mensagem": "Execucao iniciada com sucesso"
}
```

> **Nota:** O campo `execucaoId` e sempre `null` na resposta atual porque a execucao e disparada de forma assincrona (fire-and-forget). O ID da execucao sera criado pelo worker ao iniciar o processamento. Para consultar o status da execucao, use `GET /api/v1/crawler/status`.

**Response Body (409):**

```json
{
  "erro": "Uma execucao ja esta em andamento"
}
```

**Exemplo de requisicao:**

```http
POST /api/v1/crawler/executar HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "forcarReprocessamento": true
}
```

**Exemplo de resposta (202):**

```http
HTTP/1.1 202 Accepted
Content-Type: application/json

{
  "execucaoId": null,
  "mensagem": "Execucao iniciada com sucesso"
}
```

---

### GET /api/v1/crawler/status

Retorna o status da execucao mais recente do crawler.

**Autenticacao:** JWT Bearer + role Admin

**Parametros:** Nenhum

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Status da ultima execucao (ou status `NenhumaExecucao` se nenhuma execucao registrada) |
| 401 Unauthorized | Token ausente ou invalido |
| 403 Forbidden | Usuario nao possui role Admin |

**Response Body (200 - com execucao):**

```json
{
  "id": "660f1a2b3c4d5e6f7a8b9c0d",
  "inicio": "2026-03-15T02:00:00Z",
  "fim": "2026-03-15T03:45:22Z",
  "status": "Concluido",
  "tipo": "Agendado",
  "faseAtual": "ProcessamentoFila",
  "totalMunicipios": 609,
  "totalServicos": 203,
  "processados": 16146,
  "erros": 12,
  "detalhesErro": [
    "Timeout apos 30s para municipio 1302603 servico 010101001"
  ],
  "temCertificado": true,
  "ufAtual": "SP",
  "ufsEmAndamento": ["SP"],
  "ufsProcessadas": ["SP", "RJ"],
  "progressoUfs": {
    "SP": {
      "uf": "SP",
      "status": "EmAndamento",
      "municipiosEncontrados": 609,
      "municipiosAtivos": 609,
      "inicio": "2026-03-15T02:00:00Z",
      "fim": null
    }
  }
}
```

| Campo | Tipo | Descricao |
|-------|------|-----------|
| id | string | Identificador unico da execucao (ObjectId) |
| inicio | string (ISO 8601) | Data/hora de inicio |
| fim | string (ISO 8601) ou null | Data/hora de termino (null se em andamento) |
| status | string | `NenhumaExecucao`, `EmAndamento`, `Concluido`, `FalhaParcial`, `Falha` |
| tipo | string | `Agendado` ou `Manual` |
| faseAtual | string ou null | Fase atual do crawler: `DescobertaConvenios`, `GeraDaFila`, `ProcessamentoFila`, ou null se finalizado |
| totalMunicipios | integer | Total de municipios a processar |
| totalServicos | integer | Total de codigos de servico distintos |
| processados | integer | Total de itens processados com sucesso |
| erros | integer | Total de itens com erro |
| detalhesErro | string[] | Lista de mensagens de erro em texto livre |
| temCertificado | boolean | Se ha certificado PFX disponivel |
| ufAtual | string ou null | UF sendo processada no momento |
| ufsEmAndamento | string[] | Lista de UFs em processamento |
| ufsProcessadas | string[] | Lista de UFs ja processadas |
| progressoUfs | object | Mapa de progresso por UF, com status individual |

**Response Body (200 - sem execucao):**

```json
{
  "id": "",
  "inicio": "0001-01-01T00:00:00",
  "fim": null,
  "status": "NenhumaExecucao",
  "tipo": "",
  "faseAtual": null,
  "totalMunicipios": 0,
  "totalServicos": 0,
  "processados": 0,
  "erros": 0,
  "detalhesErro": [],
  "temCertificado": false,
  "ufAtual": null,
  "ufsEmAndamento": [],
  "ufsProcessadas": [],
  "progressoUfs": {}
}
```

> **Nota:** Quando nao ha execucoes registradas, o endpoint retorna 200 com status `NenhumaExecucao` (nao 404).

**Exemplo de requisicao:**

```http
GET /api/v1/crawler/status HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

---

### GET /api/v1/crawler/execucoes

Retorna o historico das ultimas 20 execucoes do crawler.

**Autenticacao:** JWT Bearer + role Admin

**Parametros:** Nenhum

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Lista de execucoes |
| 401 Unauthorized | Token ausente ou invalido |
| 403 Forbidden | Usuario nao possui role Admin |

**Response Body (200):**

```json
[
  {
    "id": "660f1a2b3c4d5e6f7a8b9c0d",
    "inicio": "2026-03-15T02:00:00Z",
    "fim": "2026-03-15T03:45:22Z",
    "status": "Concluido",
    "tipo": "Agendado",
    "totalMunicipios": 27,
    "totalServicos": 598,
    "processados": 16146,
    "erros": 12,
    "detalhesErro": []
  },
  {
    "id": "660e0b1a2c3d4e5f6a7b8c9d",
    "inicio": "2026-03-14T02:00:00Z",
    "fim": "2026-03-14T03:30:10Z",
    "status": "FalhaParcial",
    "tipo": "Agendado",
    "totalMunicipios": 27,
    "totalServicos": 598,
    "processados": 15980,
    "erros": 178,
    "detalhesErro": []
  }
]
```

**Nota:** O campo `detalhesErro` e retornado vazio na listagem para reduzir o tamanho da resposta. Para ver os detalhes de erro de uma execucao especifica, consultar o endpoint de status ou implementar um endpoint de detalhe por ID (futuro).

**Exemplo de requisicao:**

```http
GET /api/v1/crawler/execucoes HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

---

### Certificado PFX

Endpoints para gerenciamento do certificado digital PFX usado pelo worker para autenticacao mTLS com a API NFS-e.

---

### POST /api/v1/crawler/certificado

Faz upload de um certificado PFX com senha. Substitui o certificado anterior, se existir.

**Autenticacao:** JWT Bearer + role Admin

**Content-Type:** `multipart/form-data`

**Request Body (form-data):**

| Campo | Tipo | Descricao |
|-------|------|-----------|
| arquivo | file (binary) | Arquivo PFX do certificado digital (max 10 MB) |
| senha | string | Senha do certificado PFX |

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Certificado carregado com sucesso |
| 400 Bad Request | Arquivo PFX invalido ou senha incorreta |
| 401 Unauthorized | Token ausente ou invalido |
| 403 Forbidden | Usuario nao possui role Admin |

**Response Body (200):**

```json
{
  "mensagem": "Certificado armazenado com sucesso"
}
```

**Response Body (400):**

```json
{
  "erro": "Arquivo PFX invalido ou senha incorreta"
}
```

**Exemplo de requisicao:**

```http
POST /api/v1/crawler/certificado HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: multipart/form-data; boundary=----FormBoundary

------FormBoundary
Content-Disposition: form-data; name="arquivo"; filename="nfse.pfx"
Content-Type: application/x-pkcs12

(binary data)
------FormBoundary
Content-Disposition: form-data; name="senha"

minhaSenha123
------FormBoundary--
```

---

### GET /api/v1/crawler/certificado

Retorna o status do certificado PFX atualmente carregado.

**Autenticacao:** JWT Bearer + role Admin

**Parametros:** Nenhum

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Status do certificado |
| 401 Unauthorized | Token ausente ou invalido |
| 403 Forbidden | Usuario nao possui role Admin |

**Response Body (200 - com certificado):**

```json
{
  "hasCertificate": true,
  "uploadedAt": "2026-01-01T00:00:00Z",
  "thumbprint": "B046004B8CB474EF2A2265B9B5255AD75461AB67",
  "subject": "CN=EMPRESA LTDA:12345678000199, OU=Certificado PJ A1, ...",
  "validoAte": "2026-09-29T14:15:00+00:00"
}
```

**Response Body (200 - sem certificado):**

```json
{
  "hasCertificate": false,
  "uploadedAt": null,
  "thumbprint": null,
  "subject": null,
  "validoAte": null
}
```

| Campo | Tipo | Descricao |
|-------|------|-----------|
| hasCertificate | boolean | Se ha certificado carregado |
| uploadedAt | string (ISO 8601) ou null | Data/hora do upload |
| thumbprint | string ou null | Thumbprint SHA1 do certificado |
| subject | string ou null | Subject (CN) do certificado |
| validoAte | string (ISO 8601) ou null | Data de validade do certificado |

**Exemplo de requisicao:**

```http
GET /api/v1/crawler/certificado HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

---

### DELETE /api/v1/crawler/certificado

Remove o certificado PFX atualmente carregado.

**Autenticacao:** JWT Bearer + role Admin

**Parametros:** Nenhum

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Certificado removido com sucesso |
| 401 Unauthorized | Token ausente ou invalido |
| 403 Forbidden | Usuario nao possui role Admin |

**Response Body (200):**

```json
{
  "mensagem": "Certificado removido com sucesso"
}
```

**Exemplo de requisicao:**

```http
DELETE /api/v1/crawler/certificado HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

---

### Configuracao do Crawler

Endpoints para gerenciamento da configuracao do crawler. Requerem autenticacao JWT e role Admin.

---

### GET /api/v1/crawler/configuracao

Retorna a configuracao atual do crawler.

**Autenticacao:** JWT Bearer + role Admin

**Parametros:** Nenhum

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Configuracao atual do crawler |
| 401 Unauthorized | Token ausente ou invalido |
| 403 Forbidden | Usuario nao possui role Admin |

**Response Body (200):**

```json
{
  "id": "string",
  "ativo": true,
  "cronSchedule": "0 2 * * 0",
  "timeoutSegundos": 30,
  "tentativasMaximas": 3,
  "tamanhoPaginacao": 50,
  "tamanhoLote": 100,
  "pausaLoteSegundos": 2,
  "limiteParadaAntecipada": 10,
  "paralelismo": 5,
  "criadoEm": "2026-01-01T00:00:00Z",
  "atualizadoEm": "2026-01-01T00:00:00Z"
}
```

| Campo | Tipo | Descricao |
|-------|------|-----------|
| id | string | Identificador unico da configuracao |
| ativo | boolean | Se o agendamento automatico esta ativo |
| cronSchedule | string | Expressao CRON do agendamento |
| timeoutSegundos | integer | Timeout em segundos para cada requisicao |
| tentativasMaximas | integer | Numero maximo de tentativas por requisicao |
| tamanhoPaginacao | integer | Tamanho da paginacao para consultas |
| tamanhoLote | integer | Tamanho do lote de processamento |
| pausaLoteSegundos | integer | Pausa em segundos entre lotes |
| limiteParadaAntecipada | integer | Limite de erros consecutivos para parada antecipada |
| paralelismo | integer | Numero de requisicoes paralelas |
| criadoEm | string (ISO 8601) | Data/hora de criacao |
| atualizadoEm | string (ISO 8601) | Data/hora da ultima atualizacao |

**Exemplo de requisicao:**

```http
GET /api/v1/crawler/configuracao HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

---

### PUT /api/v1/crawler/configuracao

Atualiza a configuracao completa do crawler. Todos os campos sao obrigatorios.

**Autenticacao:** JWT Bearer + role Admin

**Request Body:**

```json
{
  "ativo": true,
  "cronSchedule": "0 2 * * 0",
  "timeoutSegundos": 30,
  "tentativasMaximas": 3,
  "tamanhoPaginacao": 50,
  "tamanhoLote": 100,
  "pausaLoteSegundos": 2,
  "limiteParadaAntecipada": 10,
  "paralelismo": 5
}
```

| Campo | Tipo | Obrigatorio | Validacao |
|-------|------|-------------|-----------|
| ativo | boolean | Sim | - |
| cronSchedule | string | Sim | Deve ser uma expressao CRON valida |
| timeoutSegundos | integer | Sim | Min: 1, Max: 300 |
| tentativasMaximas | integer | Sim | Min: 1, Max: 10 |
| tamanhoPaginacao | integer | Sim | Min: 1, Max: 200 |
| tamanhoLote | integer | Sim | Min: 1, Max: 5000 |
| pausaLoteSegundos | integer | Sim | Min: 0, Max: 60 |
| limiteParadaAntecipada | integer | Sim | Min: 1, Max: 100 |
| paralelismo | integer | Sim | Min: 1, Max: 20 |

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Configuracao atualizada com sucesso |
| 400 Bad Request | Validacao falhou |
| 401 Unauthorized | Token ausente ou invalido |
| 403 Forbidden | Usuario nao possui role Admin |

**Response Body (200):**

Mesma estrutura do `GET /api/v1/crawler/configuracao`.

**Response Body (400):**

```json
{
  "erro": "Validacao falhou",
  "detalhes": [
    "cronSchedule deve ser uma expressao CRON valida",
    "timeoutSegundos deve ser entre 1 e 300"
  ]
}
```

**Exemplo de requisicao:**

```http
PUT /api/v1/crawler/configuracao HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "ativo": true,
  "cronSchedule": "0 3 * * 1",
  "timeoutSegundos": 60,
  "tentativasMaximas": 5,
  "tamanhoPaginacao": 100,
  "tamanhoLote": 200,
  "pausaLoteSegundos": 5,
  "limiteParadaAntecipada": 20,
  "paralelismo": 10
}
```

---

### PATCH /api/v1/crawler/configuracao

Atualiza parcialmente a configuracao do crawler. Todos os campos sao opcionais, mas pelo menos um deve ser informado.

**Autenticacao:** JWT Bearer + role Admin

**Request Body:**

```json
{
  "timeoutSegundos": 60,
  "paralelismo": 10
}
```

| Campo | Tipo | Obrigatorio | Validacao |
|-------|------|-------------|-----------|
| ativo | boolean | Nao | - |
| cronSchedule | string | Nao | Deve ser uma expressao CRON valida |
| timeoutSegundos | integer | Nao | Min: 1, Max: 300 |
| tentativasMaximas | integer | Nao | Min: 1, Max: 10 |
| tamanhoPaginacao | integer | Nao | Min: 1, Max: 200 |
| tamanhoLote | integer | Nao | Min: 1, Max: 5000 |
| pausaLoteSegundos | integer | Nao | Min: 0, Max: 60 |
| limiteParadaAntecipada | integer | Nao | Min: 1, Max: 100 |
| paralelismo | integer | Nao | Min: 1, Max: 20 |

> **Nota:** Pelo menos um campo deve ser informado no body. Campos nao enviados permanecem inalterados.

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Configuracao atualizada com sucesso |
| 400 Bad Request | Nenhum campo informado ou validacao falhou |
| 401 Unauthorized | Token ausente ou invalido |
| 403 Forbidden | Usuario nao possui role Admin |

**Response Body (200):**

Mesma estrutura do `GET /api/v1/crawler/configuracao`.

**Response Body (400 - nenhum campo):**

```json
{
  "erro": "Pelo menos um campo deve ser informado"
}
```

**Response Body (400 - validacao):**

```json
{
  "erro": "Validacao falhou",
  "detalhes": [
    "paralelismo deve ser entre 1 e 20"
  ]
}
```

**Exemplo de requisicao:**

```http
PATCH /api/v1/crawler/configuracao HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "timeoutSegundos": 60,
  "paralelismo": 10
}
```

---

## Health Check

Endpoint publico para verificacao de saude do servico.

---

### GET /health

Verifica a saude do servico e suas dependencias.

**Autenticacao:** Nenhuma

**Parametros:** Nenhum

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Servico saudavel |
| 503 Service Unavailable | Servico com problemas |

**Response Body (200):**

```json
{
  "status": "healthy",
  "mongodb": "connected"
}
```

**Response Body (503):**

```json
{
  "status": "unhealthy",
  "mongodb": "disconnected"
}
```

**Exemplo de requisicao:**

```http
GET /health HTTP/1.1
```

**Exemplo de resposta (200):**

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "status": "healthy",
  "mongodb": "connected"
}
```

---

## Formato Padrao de Erro

Todas as respostas de erro seguem o formato abaixo:

```json
{
  "erro": "string (descricao legivel do erro)",
  "detalhes": ["string (opcional, lista de erros especificos)"]
}
```

| Campo | Tipo | Obrigatorio | Descricao |
|-------|------|-------------|-----------|
| erro | string | Sim | Mensagem de erro legivel em portugues |
| detalhes | string[] | Nao | Lista de detalhes adicionais (ex: erros de validacao por campo) |

> **Nota:** Os controllers nao incluem um campo `codigo` nas respostas de erro. Apenas o middleware global de erro (`ErrorHandlingMiddleware`) retorna um campo `codigo` com o valor `"ERRO_INTERNO"` para erros 500 nao tratados.

### Formato do erro global (500)

```json
{
  "erro": "Erro interno do servidor",
  "codigo": "ERRO_INTERNO"
}
```

### Codigos de erro conhecidos

| Codigo | HTTP Status | Descricao |
|--------|-------------|-----------|
| `ERRO_INTERNO` | 500 | Erro interno nao esperado (unico codigo retornado pelo sistema) |

---

## Referencia: API Externa NFS-e

Endpoints da API externa consumidos pelo worker/crawler. Documentados aqui para referencia cruzada.

**Base URL:** `https://adn.nfse.gov.br`
**Autenticacao:** Certificado cliente PFX (mTLS). Sem header de autorizacao. Sem body.
**Metodo:** GET (todos os endpoints)

| Endpoint | Descricao |
|----------|-----------|
| `GET /parametrizacao/{municipio}/{servico}/{competencia}/aliquota` | Aliquota de ISS para municipio/servico/competencia |
| `GET /parametrizacao/{municipio}/{servico}/historicoaliquotas` | Historico de aliquotas para municipio/servico |
| `GET /parametrizacao/{municipio}/convenio` | Verifica se municipio tem convenio ativo |
| `GET /parametrizacao/{municipio}/{competencia}/retencoes` | Retencoes do municipio na competencia |
| `GET /parametrizacao/{municipio}/{numeroBeneficio}/{competencia}/beneficio` | Beneficio fiscal |
| `GET /cnc/consulta/cad/{municipio}` | Consulta cadastro CNC do municipio |

**Parametros da API externa:**

| Parametro | Formato | Exemplo |
|-----------|---------|---------|
| municipio | Codigo IBGE (7 digitos) | `3106200` |
| servico | Codigo numerico (9 digitos, sem pontos) | `010101001` |
| competencia | Data completa (YYYY-MM-DD) | `2026-03-01` |
| numeroBeneficio | Numero do beneficio fiscal | `123456` |

**Nota:** O worker normaliza o codigo de servico para formato sem pontos antes de chamar a API externa e normaliza a competencia para o primeiro dia do mes (`YYYY-MM-01`).
