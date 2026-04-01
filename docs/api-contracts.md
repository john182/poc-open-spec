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
  ],
  "codigo": "VALIDATION_ERROR"
}
```

**Response Body (409):**

```json
{
  "erro": "Email ja cadastrado",
  "codigo": "EMAIL_ALREADY_EXISTS"
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
  "erro": "Credenciais invalidas",
  "codigo": "INVALID_CREDENTIALS"
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
  "expiresIn": 3600
}
```

**Response Body (401):**

```json
{
  "erro": "Refresh token invalido ou expirado",
  "codigo": "INVALID_REFRESH_TOKEN"
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
  "expiresIn": 3600
}
```

---

## Consulta de Dados

Todos os endpoints desta secao requerem autenticacao JWT.

---

### GET /api/v1/estados

Retorna a lista de todos os 27 estados brasileiros.

**Autenticacao:** JWT Bearer (obrigatorio)

**Parametros:** Nenhum

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Lista de estados |
| 401 Unauthorized | Token ausente ou invalido |

**Response Body (200):**

```json
[
  {
    "codigo": 31,
    "nome": "Minas Gerais",
    "sigla": "MG",
    "regiao": "Sudeste"
  },
  {
    "codigo": 35,
    "nome": "Sao Paulo",
    "sigla": "SP",
    "regiao": "Sudeste"
  }
]
```

| Campo | Tipo | Descricao |
|-------|------|-----------|
| codigo | integer | Codigo IBGE do estado |
| nome | string | Nome completo do estado |
| sigla | string | Sigla da UF (2 caracteres) |
| regiao | string | Regiao geografica (Norte, Nordeste, Centro-Oeste, Sudeste, Sul) |

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
  { "codigo": 12, "nome": "Acre", "sigla": "AC", "regiao": "Norte" },
  { "codigo": 27, "nome": "Alagoas", "sigla": "AL", "regiao": "Nordeste" },
  { "codigo": 13, "nome": "Amazonas", "sigla": "AM", "regiao": "Norte" }
]
```

---

### GET /api/v1/estados/:uf/municipios

Retorna a lista de municipios de um estado especifico.

**Autenticacao:** JWT Bearer (obrigatorio)

**Path Parameters:**

| Parametro | Tipo | Descricao |
|-----------|------|-----------|
| uf | string | Sigla da UF (2 caracteres, case-insensitive). Ex: `MG`, `SP`, `RJ` |

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Lista de municipios do estado |
| 401 Unauthorized | Token ausente ou invalido |
| 404 Not Found | UF invalida ou nao encontrada |

**Response Body (200):**

```json
[
  {
    "codigoIbge": 3106200,
    "nome": "Belo Horizonte",
    "siglaEstado": "MG"
  },
  {
    "codigoIbge": 3118601,
    "nome": "Contagem",
    "siglaEstado": "MG"
  }
]
```

| Campo | Tipo | Descricao |
|-------|------|-----------|
| codigoIbge | integer | Codigo IBGE do municipio (7 digitos) |
| nome | string | Nome do municipio |
| siglaEstado | string | Sigla da UF a que pertence |

**Response Body (404):**

```json
{
  "erro": "Estado nao encontrado",
  "codigo": "STATE_NOT_FOUND"
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
| codigoIbge | integer | Codigo IBGE do municipio (7 digitos). Ex: `3106200` |

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
| 401 Unauthorized | Token ausente ou invalido |
| 404 Not Found | Municipio nao encontrado no cadastro |

**Response Body (200):**

```json
{
  "items": [
    {
      "codigoServico": "010101001",
      "codigoServicoFormatado": "01.01.01.001",
      "descricaoServico": "Analise e desenvolvimento de sistemas",
      "aliquota": 2.00,
      "competencia": "2026-03-01"
    },
    {
      "codigoServico": "010102001",
      "codigoServicoFormatado": "01.01.02.001",
      "descricaoServico": "Programacao",
      "aliquota": 2.00,
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
| items[].aliquota | decimal | Aliquota de ISS em percentual (ex: 2.00 = 2%) |
| items[].competencia | string | Competencia no formato YYYY-MM-01 |
| pagina | integer | Pagina atual |
| tamanhoPagina | integer | Tamanho da pagina |
| totalItens | integer | Total de registros que atendem os filtros |
| totalPaginas | integer | Total de paginas calculado |

**Response Body (400):**

```json
{
  "erro": "Parametros invalidos",
  "detalhes": [
    "tamanhoPagina deve ser entre 1 e 100",
    "aliquotaMin deve ser um numero positivo"
  ],
  "codigo": "VALIDATION_ERROR"
}
```

**Exemplo de requisicao com filtros:**

```http
GET /api/v1/municipios/3106200/aliquotas?pagina=1&tamanhoPagina=10&codigoServico=01.01&aliquotaMin=2&aliquotaMax=5 HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

**Exemplo de requisicao sem filtros (defaults):**

```http
GET /api/v1/municipios/3106200/aliquotas HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

---

### GET /api/v1/municipios/:codigoIbge/aliquotas/:codigoServico

Retorna o detalhe de uma aliquota especifica para um servico em um municipio.

**Autenticacao:** JWT Bearer (obrigatorio)

**Path Parameters:**

| Parametro | Tipo | Descricao |
|-----------|------|-----------|
| codigoIbge | integer | Codigo IBGE do municipio (7 digitos) |
| codigoServico | string | Codigo do servico. Aceita formato com pontos (`01.01.01.001`) ou numerico (`010101001`) |

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Detalhe da aliquota encontrado |
| 401 Unauthorized | Token ausente ou invalido |
| 404 Not Found | Municipio ou servico nao encontrado |

**Response Body (200):**

```json
{
  "codigoMunicipio": 3106200,
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
| codigoMunicipio | integer | Codigo IBGE do municipio |
| nomeMunicipio | string | Nome do municipio |
| codigoServico | string | Codigo numerico puro |
| codigoServicoFormatado | string | Codigo formatado com pontos |
| descricaoServico | string | Descricao do servico |
| aliquota | decimal | Aliquota de ISS em percentual |
| competencia | string | Competencia (YYYY-MM-01) |
| coletadoEm | string (ISO 8601) | Data/hora em que o dado foi coletado da API NFS-e |

**Response Body (404):**

```json
{
  "erro": "Aliquota nao encontrada para este municipio e servico",
  "codigo": "TAX_RATE_NOT_FOUND"
}
```

**Exemplo de requisicao:**

```http
GET /api/v1/municipios/3106200/aliquotas/01.01.01.001 HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

---

## Crawler Admin

Endpoints para gerenciamento do worker/crawler. Requerem autenticacao JWT.
No MVP nao ha controle por role; futuramente serao restritos a administradores.

---

### POST /api/v1/crawler/executar

Dispara uma execucao manual do crawler.

**Autenticacao:** JWT Bearer (obrigatorio)

**Request Body (opcional):**

```json
{
  "forcarReprocessamento": false
}
```

| Campo | Tipo | Default | Descricao |
|-------|------|---------|-----------|
| forcarReprocessamento | boolean | false | Se `true`, regenera a fila completa ignorando dados ja coletados na competencia atual |

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 202 Accepted | Execucao iniciada com sucesso |
| 401 Unauthorized | Token ausente ou invalido |
| 409 Conflict | Ja existe uma execucao em andamento |

**Response Body (202):**

```json
{
  "execucaoId": "660f1a2b3c4d5e6f7a8b9c0d"
}
```

**Response Body (409):**

```json
{
  "erro": "Ja existe uma execucao em andamento",
  "codigo": "EXECUTION_IN_PROGRESS"
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
  "execucaoId": "660f1a2b3c4d5e6f7a8b9c0d"
}
```

---

### GET /api/v1/crawler/status

Retorna o status da execucao mais recente do crawler.

**Autenticacao:** JWT Bearer (obrigatorio)

**Parametros:** Nenhum

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Status da ultima execucao |
| 204 No Content | Nenhuma execucao registrada |
| 401 Unauthorized | Token ausente ou invalido |

**Response Body (200):**

```json
{
  "id": "660f1a2b3c4d5e6f7a8b9c0d",
  "inicio": "2026-03-15T02:00:00Z",
  "fim": "2026-03-15T03:45:22Z",
  "status": "concluido",
  "tipo": "agendado",
  "totalMunicipios": 27,
  "totalServicos": 598,
  "processados": 16146,
  "erros": 12,
  "detalhesErro": [
    {
      "codigoMunicipio": 1302603,
      "codigoServico": "010101001",
      "erro": "Timeout apos 30s",
      "tentativas": 3
    }
  ]
}
```

| Campo | Tipo | Descricao |
|-------|------|-----------|
| id | string | Identificador unico da execucao (ObjectId) |
| inicio | string (ISO 8601) | Data/hora de inicio |
| fim | string (ISO 8601) ou null | Data/hora de termino (null se em andamento) |
| status | string | `em_andamento`, `concluido`, `falha_parcial`, `falha` |
| tipo | string | `agendado` ou `manual` |
| totalMunicipios | integer | Total de municipios processados |
| totalServicos | integer | Total de codigos de servico distintos processados |
| processados | integer | Total de itens processados com sucesso |
| erros | integer | Total de itens com erro |
| detalhesErro | array | Lista de erros (limitado aos ultimos 100) |

**Exemplo de requisicao:**

```http
GET /api/v1/crawler/status HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

---

### GET /api/v1/crawler/execucoes

Retorna o historico das ultimas 20 execucoes do crawler.

**Autenticacao:** JWT Bearer (obrigatorio)

**Parametros:** Nenhum

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Lista de execucoes |
| 401 Unauthorized | Token ausente ou invalido |

**Response Body (200):**

```json
[
  {
    "id": "660f1a2b3c4d5e6f7a8b9c0d",
    "inicio": "2026-03-15T02:00:00Z",
    "fim": "2026-03-15T03:45:22Z",
    "status": "concluido",
    "tipo": "agendado",
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
    "status": "falha_parcial",
    "tipo": "agendado",
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

**Autenticacao:** JWT Bearer (obrigatorio)

**Content-Type:** `multipart/form-data`

**Request Body (form-data):**

| Campo | Tipo | Descricao |
|-------|------|-----------|
| file | file (binary) | Arquivo PFX do certificado digital |
| password | string | Senha do certificado PFX |

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Certificado carregado com sucesso |
| 400 Bad Request | Arquivo PFX invalido ou senha incorreta |
| 401 Unauthorized | Token ausente ou invalido |

**Response Body (200):**

```json
{
  "hasCertificate": true,
  "uploadedAt": "2024-01-01T00:00:00Z"
}
```

**Response Body (400):**

```json
{
  "erro": "Arquivo PFX invalido ou senha incorreta",
  "codigo": "INVALID_CERTIFICATE"
}
```

**Exemplo de requisicao:**

```http
POST /api/v1/crawler/certificado HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: multipart/form-data; boundary=----FormBoundary

------FormBoundary
Content-Disposition: form-data; name="file"; filename="nfse.pfx"
Content-Type: application/x-pkcs12

(binary data)
------FormBoundary
Content-Disposition: form-data; name="password"

minhaSenha123
------FormBoundary--
```

---

### GET /api/v1/crawler/certificado

Retorna o status do certificado PFX atualmente carregado.

**Autenticacao:** JWT Bearer (obrigatorio)

**Parametros:** Nenhum

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 200 OK | Status do certificado |
| 401 Unauthorized | Token ausente ou invalido |

**Response Body (200 - com certificado):**

```json
{
  "hasCertificate": true,
  "uploadedAt": "2024-01-01T00:00:00Z"
}
```

**Response Body (200 - sem certificado):**

```json
{
  "hasCertificate": false,
  "uploadedAt": null
}
```

**Exemplo de requisicao:**

```http
GET /api/v1/crawler/certificado HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

---

### DELETE /api/v1/crawler/certificado

Remove o certificado PFX atualmente carregado.

**Autenticacao:** JWT Bearer (obrigatorio)

**Parametros:** Nenhum

**Respostas:**

| Status | Descricao |
|--------|-----------|
| 204 No Content | Certificado removido com sucesso |
| 401 Unauthorized | Token ausente ou invalido |

**Exemplo de requisicao:**

```http
DELETE /api/v1/crawler/certificado HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
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
  "detalhes": ["string (opcional, lista de erros especificos)"],
  "codigo": "string (opcional, codigo de erro programatico)"
}
```

| Campo | Tipo | Obrigatorio | Descricao |
|-------|------|-------------|-----------|
| erro | string | Sim | Mensagem de erro legivel em portugues |
| detalhes | string[] | Nao | Lista de detalhes adicionais (ex: erros de validacao por campo) |
| codigo | string | Nao | Codigo de erro para tratamento programatico no frontend |

### Codigos de erro conhecidos

| Codigo | HTTP Status | Descricao |
|--------|-------------|-----------|
| `VALIDATION_ERROR` | 400 | Erro de validacao nos campos da requisicao |
| `INVALID_CREDENTIALS` | 401 | Email ou senha incorretos |
| `INVALID_REFRESH_TOKEN` | 401 | Refresh token invalido ou expirado |
| `UNAUTHORIZED` | 401 | Token JWT ausente ou invalido |
| `EMAIL_ALREADY_EXISTS` | 409 | Email ja cadastrado |
| `STATE_NOT_FOUND` | 404 | UF nao encontrada |
| `MUNICIPALITY_NOT_FOUND` | 404 | Municipio nao encontrado |
| `TAX_RATE_NOT_FOUND` | 404 | Aliquota nao encontrada |
| `EXECUTION_IN_PROGRESS` | 409 | Ja existe execucao do crawler em andamento |
| `INTERNAL_ERROR` | 500 | Erro interno nao esperado |

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
