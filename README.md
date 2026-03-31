# Tax Rate Crawler with City Selection Frontend

A greenfield project that periodically crawls Brazilian municipal tax rates from the NFS-e API and presents them via an interactive frontend where users can browse tax information by city and service code.

**Team Challenge**: Tech stack is decided by your team. This architecture is language and framework-agnostic.

---

## Project Overview

### What It Does

1. **Crawler**: Automatically fetches current tax rates for predefined municipalities and service codes from the NFS-e API
2. **Backend API**: Serves cached tax rate data with fast query capabilities
3. **Frontend**: Interactive UI where users:
   - Select a city from a list of municipalities
   - View all available service codes for that city
   - Select a specific service code to see detailed tax rate information

### Architecture

```
┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│   Crawler    │────────▶│  Data Store  │◀────────│  Backend API │
│   Worker     │         │ (DB or In-   │         │              │
│  (Scheduled) │         │   Memory)    │         └───────┬──────┘
└──────────────┘         └──────────────┘                 │
                                                           │ HTTP
                                                           │
                                                    ┌──────▼──────┐
                                                    │  Frontend   │
                                                    │     UI      │
                                                    └─────────────┘
```

**3-Tier Architecture**:
- **Crawler Worker**: Scheduled service that fetches data from NFS-e API and populates data store
- **Backend API**: REST API serving cached data to frontend
- **Frontend**: User interface for city and service code selection with tax rate display

---

## User Experience Flow

1. **City Selection**: User opens app and sees a list of available municipalities (display format: team decision)
2. **Service Codes List**: After selecting a city, user sees all service codes available for that municipality
3. **Tax Rate Details**: User selects a specific service code and views detailed tax information (alíquota, competência, etc.)

---

## API Reference

### NFS-e API (External - Source Data)

**Base URL**: (from Bruno collection)

#### Get Current Tax Rate
```
GET /parametrizacao/{municipio}/{servico}/{competencia}/aliquota
```

**Parameters**:
- `municipio`: IBGE municipality code (e.g., 3106200 for Belo Horizonte)
- `servico`: Service code following LC 116/2003 (e.g., 01.01.01.001)
- `competencia`: Date in YYYYMM format (e.g., 202603 for March 2026)

**Response Example**:
```json
{
  "codigoMunicipio": "3106200",
  "codigoServico": "01.01.01.001",
  "aliquota": 3.5,
  "competencia": "202603",
  "descricaoServico": "Análise e desenvolvimento de sistemas"
}
```
