# Analise da API Externa - NFS-e ADN

> Analise detalhada da API externa NFS-e ADN (Ambiente de Dados Nacional), utilizada como fonte de dados para consulta de aliquotas municipais, convenios, retencoes e beneficios.

---

## 1. Informacoes Gerais

| Propriedade         | Valor                                      |
| ------------------- | ------------------------------------------ |
| **Base URL**        | `https://adn.nfse.gov.br`                 |
| **Protocolo**       | HTTPS                                      |
| **Autenticacao**    | mTLS (Mutual TLS) com certificado PFX     |
| **Metodos HTTP**    | Todos os endpoints sao GET                 |
| **Body**            | Nenhum (todos os parametros sao via path)  |
| **Headers custom**  | Nenhum (autenticacao via certificado, nao por header) |
| **Content-Type**    | Inferido como `application/json` (respostas) |
| **Encode URL**      | Habilitado (`encodeUrl: true`)             |
| **Timeout padrao**  | 0 (sem timeout nos Bruno files - indefinido) |

---

## 2. Autenticacao: mTLS com Certificado PFX

A API utiliza autenticacao mTLS (Mutual TLS), onde o cliente precisa apresentar um certificado digital durante o handshake TLS. Nao ha headers de autenticacao (Bearer, API Key, etc.).

### Configuracao do certificado (conforme bruno.json)

```json
{
  "clientCertificates": {
    "enabled": true,
    "certs": [
      {
        "domain": "adn.nfse.gov.br",
        "type": "pfx",
        "pfxFilePath": "",
        "passphrase": ""
      }
    ]
  }
}
```

### Implicacoes para implementacao

- O certificado PFX e a passphrase devem ser armazenados de forma segura (vault, secrets manager, variavel de ambiente)
- O certificado deve ser carregado no `HttpClientHandler` do .NET para todas as chamadas
- O certificado tem data de validade e precisa ser renovado periodicamente
- Chamadas sem certificado valido resultarao em erro 403 ou falha de conexao TLS

---

## 3. Endpoints Documentados

### 3.1 Consulta de Aliquota

```
GET /parametrizacao/{codigoMunicipio}/{codigoServico}/{competencia}/aliquota
```

**Parametros de path:**

| Parametro          | Tipo   | Exemplo        | Descricao                                       |
| ------------------ | ------ | -------------- | ----------------------------------------------- |
| `codigoMunicipio`  | string | `3106200`      | Codigo IBGE do municipio (7 digitos)            |
| `codigoServico`    | string | `01.01.01.001` | Codigo do servico conforme LC 116 (com pontos) |
| `competencia`      | string | `2026-01-09`   | Data de referencia da competencia               |

**Finalidade:** Retorna a aliquota de ISS vigente para um municipio, servico e competencia especificos.

**Exemplo de chamada:**
```
GET https://adn.nfse.gov.br/parametrizacao/3106200/01.01.01.001/2026-01-09/aliquota
```

---

### 3.2 Historico de Aliquotas

```
GET /parametrizacao/{codigoMunicipio}/{codigoServico}/historicoaliquotas
```

**Parametros de path:**

| Parametro          | Tipo   | Exemplo        | Descricao                                       |
| ------------------ | ------ | -------------- | ----------------------------------------------- |
| `codigoMunicipio`  | string | `3304557`      | Codigo IBGE do municipio (7 digitos)            |
| `codigoServico`    | string | `17.23.01.001` | Codigo do servico conforme LC 116 (com pontos) |

**Finalidade:** Retorna o historico completo de alteracoes de aliquota para um municipio e servico.

**Exemplo de chamada:**
```
GET https://adn.nfse.gov.br/parametrizacao/3304557/17.23.01.001/historicoaliquotas
```

---

### 3.3 Consulta de Convenio

```
GET /parametrizacao/{codigoMunicipio}/convenio
```

**Parametros de path:**

| Parametro          | Tipo   | Exemplo   | Descricao                                    |
| ------------------ | ------ | --------- | -------------------------------------------- |
| `codigoMunicipio`  | string | `3505906` | Codigo IBGE do municipio (7 digitos)         |

**Finalidade:** Verifica se o municipio possui convenio ativo com o sistema NFS-e nacional. Este endpoint e essencial para **discovery** -- permite identificar quais municipios estao ativos na plataforma.

**Exemplo de chamada:**
```
GET https://adn.nfse.gov.br/parametrizacao/3505906/convenio
```

---

### 3.4 Consulta de Retencoes

```
GET /parametrizacao/{codigoMunicipio}/{competencia}/retencoes
```

**Parametros de path:**

| Parametro          | Tipo   | Exemplo      | Descricao                                    |
| ------------------ | ------ | ------------ | -------------------------------------------- |
| `codigoMunicipio`  | string | `3205002`    | Codigo IBGE do municipio (7 digitos)         |
| `competencia`      | string | `2026-01-01` | Data de referencia da competencia            |

**Finalidade:** Retorna informacoes sobre retencoes de ISS aplicaveis no municipio para a competencia informada.

**Exemplo de chamada:**
```
GET https://adn.nfse.gov.br/parametrizacao/3205002/2026-01-01/retencoes
```

---

### 3.5 Consulta de Beneficio

```
GET /parametrizacao/{codigoMunicipio}/{numeroBeneficio}/{competencia}/beneficio
```

**Parametros de path:**

| Parametro          | Tipo   | Exemplo            | Descricao                                    |
| ------------------ | ------ | ------------------ | -------------------------------------------- |
| `codigoMunicipio`  | string | `4106902`          | Codigo IBGE do municipio (7 digitos)         |
| `numeroBeneficio`  | string | `41069020200013`   | Numero identificador do beneficio fiscal     |
| `competencia`      | string | `2026-01-01`       | Data de referencia da competencia            |

**Finalidade:** Retorna detalhes de um beneficio fiscal especifico vigente no municipio.

**Exemplo de chamada:**
```
GET https://adn.nfse.gov.br/parametrizacao/4106902/41069020200013/2026-01-01/beneficio
```

---

### 3.6 Consulta CNC (Cadastro Nacional de Contribuintes)

```
GET /cnc/consulta/cad/{codigoMunicipio}
```

**Parametros de path:**

| Parametro          | Tipo   | Exemplo   | Descricao                                    |
| ------------------ | ------ | --------- | -------------------------------------------- |
| `codigoMunicipio`  | string | `3106200` | Codigo IBGE do municipio (7 digitos)         |

**Finalidade:** Consulta o Cadastro Nacional de Contribuintes para um municipio. Pode ser util para **discovery de servicos** ativos por municipio.

**Exemplo de chamada:**
```
GET https://adn.nfse.gov.br/cnc/consulta/cad/3106200
```

**Observacao:** Nos Bruno files, este endpoint possui variaveis de `codigoServico` (`010301001`, sem pontos) e `competencia` definidas como variaveis pre-request, mas que nao sao usadas na URL. Isso sugere que essas variaveis podem ser usadas em requests encadeados ou que o endpoint pode ter variantes nao documentadas.

---

## 4. Resumo dos Endpoints

| # | Endpoint                                                              | Parametros                                       | Uso principal                    |
| - | --------------------------------------------------------------------- | ------------------------------------------------ | -------------------------------- |
| 1 | `GET /parametrizacao/{mun}/{svc}/{comp}/aliquota`                     | municipio, servico, competencia                  | Aliquota pontual                 |
| 2 | `GET /parametrizacao/{mun}/{svc}/historicoaliquotas`                  | municipio, servico                               | Historico de aliquotas           |
| 3 | `GET /parametrizacao/{mun}/convenio`                                  | municipio                                        | Discovery de municipios ativos   |
| 4 | `GET /parametrizacao/{mun}/{comp}/retencoes`                          | municipio, competencia                           | Retencoes aplicaveis             |
| 5 | `GET /parametrizacao/{mun}/{ben}/{comp}/beneficio`                    | municipio, beneficio, competencia                | Beneficio fiscal especifico      |
| 6 | `GET /cnc/consulta/cad/{mun}`                                        | municipio                                        | Cadastro de contribuintes        |

---

## 5. Divergencias Encontradas

### 5.1 Formato de Competencia

**Observacao:** Os Bruno files utilizam datas completas no formato `YYYY-MM-DD` (ex: `2026-01-09`, `2026-01-01`). Porem, documentacoes externas e o README do repositorio da API indicam que o formato esperado pode ser `YYYYMM` (apenas ano e mes).

**Evidencias:**
- `param aliquotas.bru`: `competencia: 2026-01-09`
- `param retencoes.bru`: `competencia: 2026-01-01`
- `param beneficio.bru`: `competencia: 2026-01-01`

**Premissa adotada:** A API aceita a data completa (`YYYY-MM-DD`). Para fins de normalizacao interna no worker/backend, todas as competencias serao normalizadas para o **primeiro dia do mes** (`YYYY-MM-01`), garantindo consistencia e evitando ambiguidade.

**Acao recomendada:** Validar empiricamente com chamada real qual formato a API aceita e documentar o resultado.

---

### 5.2 Formato de Codigo de Servico

**Observacao:** Existem dois formatos de codigo de servico nos Bruno files:

| Arquivo                        | Variavel          | Formato          |
| ------------------------------ | ----------------- | ---------------- |
| `param aliquotas.bru`          | `codigoServico`   | `01.01.01.001` (com pontos) |
| `param aliquotas historico.bru`| `codigoServico`   | `17.23.01.001` (com pontos) |
| `cnc consulta.bru`             | `codigoServico`   | `010301001` (sem pontos)    |

**Premissa adotada:** Ambos os formatos sao validos na API. Internamente, o sistema deve:
1. Armazenar o codigo no formato **com pontos** (`XX.XX.XX.XXX`) como formato canonico
2. Aceitar ambos os formatos como input do usuario
3. Normalizar para o formato com pontos antes de persistir
4. Enviar no formato que cada endpoint espera (a ser validado empiricamente)

**Acao recomendada:** Testar os endpoints de parametrizacao com codigo sem pontos e o endpoint CNC com codigo com pontos para confirmar qual formato cada um aceita.

---

## 6. Cenarios de Erro Esperados

Os cenarios abaixo sao **inferidos** com base no comportamento tipico de APIs REST governamentais e nas caracteristicas dos endpoints. Devem ser validados empiricamente.

### 6.1 HTTP 404 - Nao Encontrado

| Cenario                                    | Endpoint provavel       |
| ------------------------------------------ | ----------------------- |
| Municipio sem convenio ativo               | `/convenio`             |
| Servico nao encontrado para o municipio    | `/aliquota`, `/historicoaliquotas` |
| Beneficio inexistente                      | `/beneficio`            |
| Municipio sem dados no CNC                 | `/cnc/consulta/cad`     |
| Competencia sem dados                      | `/aliquota`, `/retencoes` |

### 6.2 HTTP 403 - Proibido

| Cenario                                    | Causa                              |
| ------------------------------------------ | ---------------------------------- |
| Certificado PFX invalido                   | Arquivo corrompido ou incorreto    |
| Certificado PFX expirado                   | Validade ultrapassada              |
| Certificado nao autorizado para o recurso  | Restricao de acesso                |
| Chamada sem certificado                    | mTLS obrigatorio                   |

### 6.3 HTTP 500 - Erro Interno

| Cenario                                    | Causa                              |
| ------------------------------------------ | ---------------------------------- |
| Erro interno da API NFS-e                  | Bug ou indisponibilidade parcial   |
| Problema de banco de dados no servidor     | Manutencao ou sobrecarga           |

### 6.4 Timeout / Erro de Conexao

| Cenario                                    | Causa                              |
| ------------------------------------------ | ---------------------------------- |
| API lenta ou indisponivel                  | Manutencao programada              |
| DNS nao resolvido                          | Problema de rede                   |
| Conexao TLS recusada                       | Firewall ou restricao de IP        |

### 6.5 HTTP 400 - Bad Request (possivel)

| Cenario                                    | Causa                              |
| ------------------------------------------ | ---------------------------------- |
| Formato de competencia invalido            | Data fora do padrao esperado       |
| Codigo de municipio invalido               | Codigo IBGE inexistente            |
| Codigo de servico malformado               | Formato incorreto                  |

---

## 7. Recomendacoes para Implementacao

### 7.1 HttpClient com Certificado PFX (.NET)

A comunicacao com a API deve ser feita via `HttpClient` configurado com `HttpClientHandler` para enviar o certificado PFX no handshake TLS:

```csharp
// Exemplo conceitual - implementacao real no projeto
var handler = new HttpClientHandler();
var certificate = new X509Certificate2(pfxFilePath, passphrase);
handler.ClientCertificates.Add(certificate);

var client = new HttpClient(handler)
{
    BaseAddress = new Uri("https://adn.nfse.gov.br"),
    Timeout = TimeSpan.FromSeconds(30)
};
```

**Pontos de atencao:**
- Usar `IHttpClientFactory` para gerenciamento de ciclo de vida
- Nao recriar o `HttpClient` a cada chamada (pooling de conexoes)
- Certificado PFX e passphrase devem vir de configuracao segura (`appsettings` + User Secrets / Vault)
- Registrar como named client ou typed client no DI

### 7.2 Timeout

O Bruno files define `timeout: 0` (sem timeout), o que nao e adequado para producao.

**Recomendacao:**
- Timeout padrao por chamada: **30 segundos**
- Timeout de conexao: **10 segundos**
- Para operacoes em lote (worker/crawler): considerar timeout maior (60s) com circuit breaker

### 7.3 Normalizacao de Codigo de Servico

Implementar logica de normalizacao bidirecional:

```
Entrada: "01.01.01.001" ou "010101001"
Armazenamento: "01.01.01.001" (formato canonico)
Exibicao: "01.01.01.001" (com pontos para legibilidade)
```

O backend deve aceitar ambos os formatos nos endpoints proprios e normalizar antes de persistir ou consultar a API externa.

### 7.4 Discovery de Municipios Ativos

O endpoint de **convenio** (`/parametrizacao/{mun}/convenio`) e a principal ferramenta de discovery para identificar quais municipios possuem dados disponiveis na plataforma NFS-e.

**Estrategia recomendada para o worker:**
1. Manter lista de codigos IBGE dos municipios brasileiros (fonte: IBGE)
2. Consultar o endpoint de convenio para cada municipio
3. Persistir localmente quais municipios possuem convenio ativo
4. Usar essa lista como base para consultas subsequentes de aliquotas
5. Atualizar periodicamente (ex: semanal) a lista de convenios

### 7.5 CNC para Discovery de Servicos

O endpoint CNC (`/cnc/consulta/cad/{mun}`) **pode** ser util para identificar quais servicos estao cadastrados em cada municipio. Isso reduziria o numero de chamadas necessarias ao endpoint de aliquotas.

**Acao necessaria:** Testar o endpoint com municipios conhecidos para validar:
- Quais dados sao retornados
- Se inclui lista de codigos de servico
- Se os codigos de servico retornados sao validos para consulta de aliquota
- Qual o formato do codigo de servico retornado

### 7.6 Retry e Resiliencia

**Politica de retry recomendada:**
- Maximo de **3 tentativas** por chamada
- Backoff exponencial: 1s, 2s, 4s
- Retry apenas em erros transientes: 500, 502, 503, 504, timeout
- **Nao** fazer retry em: 400, 401, 403, 404
- Usar Polly (.NET) para implementacao

**Circuit breaker:**
- Abrir circuito apos 5 falhas consecutivas
- Tempo de half-open: 30 segundos
- Logar abertura e fechamento do circuito

### 7.7 Rate Limiting / Controle de Concorrencia

A API e governamental e provavelmente tem limites de taxa nao documentados.

**Recomendacoes:**
- Limitar concorrencia maxima a **5 chamadas simultaneas** (ajustar com base em testes)
- Intervalo minimo entre chamadas ao mesmo endpoint: **200ms**
- Para operacoes em lote do worker: usar `SemaphoreSlim` para controle de concorrencia
- Monitorar HTTP 429 (Too Many Requests) e ajustar dinamicamente

### 7.8 Persistencia Local

Para reduzir acoplamento com a API externa e melhorar a experiencia do frontend:

- **Materializar localmente** os dados de aliquotas consultados
- Definir estrategia de cache com TTL (ex: aliquotas validas por 24h, convenios por 7 dias)
- O frontend consulta apenas o backend local, nunca a API externa diretamente
- O worker e responsavel por manter os dados locais atualizados

---

## 8. Mapa de Dependencias

```
Frontend (Angular)
    |
    v
Backend API (.NET)  <-- fonte unica de dados para o frontend
    |
    v
Banco de Dados Local  <-- dados materializados pelo worker
    ^
    |
Worker/Crawler (.NET)  <-- coleta dados da API externa
    |
    v
API NFS-e ADN (https://adn.nfse.gov.br)  <-- fonte externa, mTLS
```

O frontend **nunca** acessa a API externa diretamente. O worker coleta e materializa os dados, o backend serve os dados locais, e o frontend consome o backend.

---

## 9. Riscos Identificados

| Risco                                      | Impacto | Mitigacao                                    |
| ------------------------------------------ | ------- | -------------------------------------------- |
| API fora do ar                             | Alto    | Cache local, retry, circuit breaker          |
| Certificado PFX expirado                   | Alto    | Alerta de expiracao, rotacao automatizada    |
| Rate limiting nao documentado              | Medio   | Controle de concorrencia, backoff            |
| Formato de competencia incorreto           | Medio   | Validacao empirica, normalizacao             |
| Mudanca de contrato da API sem aviso       | Medio   | Monitoramento de health, testes de integracao|
| Volume alto de municipios (5570+)          | Medio   | Batching, priorizacao, atualizacao incremental|
| Dados inconsistentes entre endpoints       | Baixo   | Validacao cruzada, logs de divergencia       |

---

## 10. Proximos Passos

1. **Validar empiricamente** os formatos de competencia e codigo de servico com chamadas reais
2. **Testar o endpoint CNC** para avaliar utilidade no discovery de servicos
3. **Documentar os schemas de resposta** de cada endpoint apos primeiras chamadas reais
4. **Definir estrategia de atualizacao do worker** com base nos tempos de resposta reais da API
5. **Implementar health check** para monitorar disponibilidade da API externa
6. **Mapear codigos IBGE** dos municipios prioritarios para testes iniciais
