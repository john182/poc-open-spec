using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;

namespace MapaTributario.API.Infrastructure.Seed;

[ExcludeFromCodeCoverage]
public class IbgeSeedService
{
    private readonly IEstadoRepository _estadoRepository;
    private readonly IMunicipioRepository _municipioRepository;
    private readonly ILogger<IbgeSeedService> _logger;

    private static readonly Dictionary<string, string> _nomeEstado = new()
    {
        ["AC"] = "Acre",
        ["AL"] = "Alagoas",
        ["AM"] = "Amazonas",
        ["AP"] = "Amapá",
        ["BA"] = "Bahia",
        ["CE"] = "Ceará",
        ["DF"] = "Distrito Federal",
        ["ES"] = "Espírito Santo",
        ["GO"] = "Goiás",
        ["MA"] = "Maranhão",
        ["MG"] = "Minas Gerais",
        ["MS"] = "Mato Grosso do Sul",
        ["MT"] = "Mato Grosso",
        ["PA"] = "Pará",
        ["PB"] = "Paraíba",
        ["PE"] = "Pernambuco",
        ["PI"] = "Piauí",
        ["PR"] = "Paraná",
        ["RJ"] = "Rio de Janeiro",
        ["RN"] = "Rio Grande do Norte",
        ["RO"] = "Rondônia",
        ["RR"] = "Roraima",
        ["RS"] = "Rio Grande do Sul",
        ["SC"] = "Santa Catarina",
        ["SE"] = "Sergipe",
        ["SP"] = "São Paulo",
        ["TO"] = "Tocantins"
    };

    private static readonly Dictionary<string, string> _regiaoByUf = new()
    {
        ["AC"] = "N", ["AM"] = "N", ["AP"] = "N", ["PA"] = "N", ["RO"] = "N", ["RR"] = "N", ["TO"] = "N",
        ["AL"] = "NE", ["BA"] = "NE", ["CE"] = "NE", ["MA"] = "NE", ["PB"] = "NE",
        ["PE"] = "NE", ["PI"] = "NE", ["RN"] = "NE", ["SE"] = "NE",
        ["DF"] = "CO", ["GO"] = "CO", ["MS"] = "CO", ["MT"] = "CO",
        ["ES"] = "SE", ["MG"] = "SE", ["RJ"] = "SE", ["SP"] = "SE",
        ["PR"] = "S", ["RS"] = "S", ["SC"] = "S"
    };

    /// <summary>
    /// Códigos IBGE das 27 capitais estaduais brasileiras.
    /// Usado no seed para marcar municípios como capital, permitindo
    /// priorização no processamento do crawler.
    /// </summary>
    private static readonly HashSet<int> _codigosCapitais = new()
    {
        1200401, // Rio Branco (AC)
        2704302, // Maceió (AL)
        1302603, // Manaus (AM)
        1600303, // Macapá (AP)
        2927408, // Salvador (BA)
        2304400, // Fortaleza (CE)
        5300108, // Brasília (DF)
        3205309, // Vitória (ES)
        5208707, // Goiânia (GO)
        2111300, // São Luís (MA)
        3106200, // Belo Horizonte (MG)
        5002704, // Campo Grande (MS)
        5103403, // Cuiabá (MT)
        1501402, // Belém (PA)
        2507507, // João Pessoa (PB)
        2611606, // Recife (PE)
        2211001, // Teresina (PI)
        4106902, // Curitiba (PR)
        3304557, // Rio de Janeiro (RJ)
        2408102, // Natal (RN)
        1100205, // Porto Velho (RO)
        1400100, // Boa Vista (RR)
        4314902, // Porto Alegre (RS)
        4205407, // Florianópolis (SC)
        2800308, // Aracaju (SE)
        3550308, // São Paulo (SP)
        1721000, // Palmas (TO)
    };

    public IbgeSeedService(
        IEstadoRepository estadoRepository,
        IMunicipioRepository municipioRepository,
        ILogger<IbgeSeedService> logger)
    {
        _estadoRepository = estadoRepository;
        _municipioRepository = municipioRepository;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedEstadosAsync();
        await SeedMunicipiosAsync();
    }

    private async Task SeedEstadosAsync()
    {
        var count = await _estadoRepository.CountAsync();
        if (count > 0)
        {
            _logger.LogInformation("Estados já existem ({Count} registros). Seed ignorado.", count);
            return;
        }

        var estados = _nomeEstado.Select(kv =>
            Estado.Create(kv.Key, kv.Value, _regiaoByUf[kv.Key])).ToList();

        await _estadoRepository.InsertManyAsync(estados);
        _logger.LogInformation("Seed de estados concluído: {Count} estados inseridos.", estados.Count);
    }

    private async Task SeedMunicipiosAsync()
    {
        var count = await _municipioRepository.CountAsync();
        if (count > 0)
        {
            _logger.LogInformation("Municípios já existem ({Count} registros). Seed ignorado.", count);
            return;
        }

        var jsonPath = Path.Combine(AppContext.BaseDirectory, "context", "municipios.json");
        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning("Arquivo municipios.json não encontrado em {Path}. Seed de municípios ignorado.", jsonPath);
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath);
        var data = JsonSerializer.Deserialize<MunicipioJsonRoot>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (data?.Data is null || data.Data.Count == 0)
        {
            _logger.LogWarning("Arquivo municipios.json está vazio ou sem dados.");
            return;
        }

        var municipios = data.Data.Select(m =>
            Municipio.Create(m.Codigo.ToString(), m.Nome, m.Uf, _codigosCapitais.Contains(m.Codigo))).ToList();

        await _municipioRepository.InsertManyAsync(municipios);
        _logger.LogInformation("Seed de municípios concluído: {Count} municípios inseridos.", municipios.Count);
    }

    private sealed class MunicipioJsonRoot
    {
        public List<MunicipioJsonItem> Data { get; set; } = new();
    }

    private sealed class MunicipioJsonItem
    {
        public int Id { get; set; }
        public int Codigo { get; set; }
        public string Nome { get; set; } = null!;
        public string Uf { get; set; } = null!;
    }
}
