## Why

O crawler hoje processa UFs sequencialmente num `foreach` (Fase 1: convênio, Fase 2: probe). Com 27 UFs e latência média de ~60ms por request, as fases de descoberta (convênio + CNC) são serializadas por UF, desperdiçando a capacidade comprovada da API de suportar 50+ req/s sem rate limiting. Um crawl completo das 27 UFs leva tempo proporcional a `soma_municipios × latencia × fases`, quando poderia ser `max_municipios_por_uf × latencia × fases / grau_paralelismo`. O benchmark provou que a API não diferencia requisições por UF — SE, RR e AP simultâneos tiveram latência idêntica a uma UF isolada.

## What Changes

- Refatorar `CrawlerService.FaseConvenioAsync` para processar múltiplas UFs em paralelo (N UFs simultâneas, configurável)
- Refatorar `CrawlerService.FaseProbeAsync` para processar múltiplas UFs em paralelo
- Adicionar campo `MaxUfsParalelas` em `ConfiguracaoCrawler` (default: 5) para controlar o grau de paralelismo por UF
- Tornar os métodos de tracking de UF em `ExecucaoCrawler` thread-safe (hoje usam acesso sequencial sem lock)
- Ajustar `StatusCrawlerResponse` para refletir múltiplas UFs em andamento simultaneamente (campo `UfAtual` → `UfsEmAndamento`)
- Manter compatibilidade: a Fase 3 (ProcessarFilaAsync) já é paralela por item e não muda

## Capabilities

### New Capabilities
- `crawler-multi-uf`: Capacidade do crawler processar múltiplas UFs simultaneamente nas fases de descoberta (convênio e probe), com grau de paralelismo configurável

### Modified Capabilities
- `data-crawler`: O requisito "Concurrency control" muda para incluir paralelismo por UF além do paralelismo por item na fila. O requisito "Execution tracking" muda para suportar múltiplas UFs em andamento simultâneo (campo `UfAtual` vira lista `UfsEmAndamento`)

## Impact

- **Backend**: `CrawlerService.cs` (refatoração das fases 1 e 2), `ExecucaoCrawler.cs` (thread-safety), `ConfiguracaoCrawler.cs` (novo campo), DTOs de resposta (status)
- **API**: Response de `GET /api/v1/crawler/status` muda — `ufAtual: string` → `ufsEmAndamento: string[]`. **BREAKING** para consumidores que dependem de `ufAtual`
- **Frontend**: Dashboard do crawler precisa mostrar múltiplas UFs em andamento (se existir componente que mostra UF atual)
- **MongoDB**: Campo `ufAtual` no documento de execução muda para `ufsEmAndamento` (array). Documentos antigos mantêm compatibilidade via leitura tolerante
- **Configuração**: Novo campo `maxUfsParalelas` em `configuracoesCrawler` (MongoDB) e no PATCH API
- **Resiliência**: RateLimiter e CertificateProtection são compartilhados entre todas as UFs — o paralelismo por UF aumenta a pressão no rate limiter global, mas os limites já foram validados no benchmark (50 req/s suportados)
