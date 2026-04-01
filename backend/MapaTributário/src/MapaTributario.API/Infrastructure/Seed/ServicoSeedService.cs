using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;

namespace MapaTributario.API.Infrastructure.Seed;

public class ServicoSeedService
{
    private readonly IServicoRepository _servicoRepository;
    private readonly ILogger<ServicoSeedService> _logger;

    public ServicoSeedService(
        IServicoRepository servicoRepository,
        ILogger<ServicoSeedService> logger)
    {
        _servicoRepository = servicoRepository;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        var count = await _servicoRepository.CountAsync();
        if (count > 0)
        {
            _logger.LogInformation("Serviços já existem ({Count} registros). Seed ignorado.", count);
            return;
        }

        var servicos = GerarServicosLc116();
        await _servicoRepository.InsertManyAsync(servicos);
        _logger.LogInformation("Seed de serviços concluído: {Count} códigos de tributação inseridos.", servicos.Count);
    }

    private static List<Servico> GerarServicosLc116()
    {
        var servicos = new List<Servico>();

        foreach (var (item, subitens) in Lc116Data)
        {
            foreach (var (subitem, descricao) in subitens)
            {
                var codigo = $"{item}.{subitem}.00";
                servicos.Add(Servico.Create(codigo, descricao, item, subitem, "00"));
            }
        }

        return servicos;
    }

    /// <summary>
    /// Tabela completa da LC 116/2003 — 40 itens com subitens.
    /// Formato: (item, [(subitem, descricao), ...])
    /// </summary>
    private static readonly (string Item, (string Sub, string Desc)[] Subitens)[] Lc116Data =
    [
        ("01", [
            ("01", "Análise e desenvolvimento de sistemas"),
            ("02", "Programação"),
            ("03", "Processamento, armazenamento ou hospedagem de dados"),
            ("04", "Elaboração de programas de computadores"),
            ("05", "Licenciamento ou cessão de uso de programas de computação"),
            ("06", "Assessoria e consultoria em informática"),
            ("07", "Suporte técnico em informática"),
            ("08", "Planejamento, confecção, manutenção de páginas eletrônicas"),
            ("09", "Disponibilização de conteúdos de áudio, vídeo, imagem e texto"),
        ]),
        ("02", [("01", "Serviços de pesquisas e desenvolvimento de qualquer natureza")]),
        ("03", [
            ("02", "Cessão de direito de uso de marcas e sinais de propaganda"),
            ("03", "Exploração de salões de festas e outros espaços"),
            ("04", "Locação, sublocação, arrendamento de bens"),
            ("05", "Cessão de andaimes, palcos e estruturas de uso temporário"),
        ]),
        ("04", [
            ("01", "Medicina e biomedicina"),
            ("02", "Análises clínicas, patologia e eletricidade médica"),
            ("03", "Hospitais, clínicas, laboratórios, sanatórios e congêneres"),
            ("04", "Instrumentação cirúrgica"),
            ("05", "Acupuntura"),
            ("06", "Enfermagem e terapia ocupacional"),
            ("07", "Serviços farmacêuticos"),
            ("08", "Terapia de qualquer espécie"),
            ("09", "Nutrição"),
            ("10", "Obstetrícia"),
            ("11", "Odontologia"),
            ("12", "Ortóptica"),
            ("13", "Próteses sob encomenda"),
            ("14", "Psicanálise"),
            ("15", "Psicologia"),
            ("16", "Casas de repouso e de recuperação"),
            ("17", "Bancos de sangue, leite, pele, olhos, óvulos"),
            ("18", "Coleta de sangue, leite, tecidos e congêneres"),
            ("19", "Unidade de atendimento, assistência ou tratamento móvel"),
            ("20", "Planos de medicina de grupo ou individual"),
            ("21", "Outros planos de saúde"),
            ("22", "Outros serviços de saúde"),
            ("23", "Serviços de saúde prestados em regime de home care"),
        ]),
        ("05", [
            ("01", "Medicina veterinária e zootecnia"),
            ("02", "Hospitais, clínicas e ambulatórios veterinários"),
            ("03", "Laboratórios de análise na área veterinária"),
            ("04", "Inseminação artificial, fertilização e congêneres"),
            ("05", "Bancos de sangue e de órgãos veterinários"),
            ("06", "Tosa, banho e embelezamento de animais"),
            ("07", "Guarda, tratamento, amestramento e adestramento"),
            ("08", "Ensino e treinamento de animais"),
            ("09", "Planos de atendimento e assistência veterinária"),
        ]),
        ("06", [
            ("01", "Barbearia, cabeleireiros e congêneres"),
            ("02", "Esteticistas, tratamento de pele e congêneres"),
            ("03", "Banhos, duchas, sauna e massagens"),
            ("04", "Ginástica, dança, esportes e congêneres"),
            ("05", "Centros de emagrecimento, spa e congêneres"),
            ("06", "Aplicação de tatuagens, piercings e congêneres"),
        ]),
        ("07", [
            ("01", "Engenharia, agronomia, agrimensura e congêneres"),
            ("02", "Execução de obras de construção civil e hidráulica"),
            ("03", "Elaboração de planos diretores e projetos"),
            ("04", "Demolição"),
            ("05", "Reparação, conservação e reforma de edifícios"),
            ("06", "Colocação e instalação de tapetes e cortinas"),
            ("07", "Recuperação, raspagem, polimento e lustração de pisos"),
            ("08", "Calafetação"),
            ("09", "Varrição, coleta, remoção e incineração de lixo"),
            ("10", "Limpeza, manutenção e conservação de imóveis"),
            ("11", "Decoração e jardinagem"),
            ("12", "Controle e tratamento de efluentes"),
            ("13", "Dedetização, desinfecção e desinsetização"),
            ("14", "Florestamento, reflorestamento e semeadura"),
            ("15", "Escoramento, contenção de encostas e congêneres"),
            ("16", "Limpeza e dragagem"),
            ("17", "Acompanhamento e fiscalização de obras"),
            ("18", "Aerofotogrametria e cartografia"),
            ("19", "Pesquisa, perfuração, cimentação e perfilagem de poços"),
            ("20", "Nucleação e bombardeamento de nuvens"),
            ("21", "Serviços de topografia"),
            ("22", "Serviços de geologia e geotécnica"),
        ]),
        ("08", [
            ("01", "Ensino regular pré-escolar, fundamental, médio e superior"),
            ("02", "Instrução, treinamento, orientação pedagógica, avaliação"),
        ]),
        ("09", [
            ("01", "Hospedagem em hotéis, apart-hotéis e congêneres"),
            ("02", "Agenciamento, organização e promoção de turismo"),
            ("03", "Guias de turismo"),
        ]),
        ("10", [
            ("01", "Agenciamento, corretagem de câmbio, seguros e congêneres"),
            ("02", "Agenciamento, corretagem de títulos em geral"),
            ("03", "Agenciamento, corretagem de contratos de arrendamento mercantil"),
            ("04", "Agenciamento, corretagem de contratos de franquia"),
            ("05", "Agenciamento, corretagem de imóveis"),
            ("06", "Agenciamento marítimo"),
            ("07", "Agenciamento de notícias"),
            ("08", "Agenciamento de publicidade e propaganda"),
            ("09", "Representação de qualquer natureza"),
            ("10", "Distribuição de bens de terceiros"),
        ]),
        ("11", [
            ("01", "Guarda e estacionamento de veículos"),
            ("02", "Vigilância, segurança ou monitoramento"),
            ("03", "Escolta, inclusive de veículos e cargas"),
            ("04", "Armazenamento, depósito, carga e descarga"),
        ]),
        ("12", [
            ("01", "Espetáculos teatrais"),
            ("02", "Exibições cinematográficas"),
            ("03", "Espetáculos circenses"),
            ("04", "Programas de auditório"),
            ("05", "Parques de diversões, centros de lazer"),
            ("06", "Boates, taxi-dancing e congêneres"),
            ("07", "Shows, ballet, danças, desfiles e congêneres"),
            ("08", "Feiras, exposições, congressos e congêneres"),
            ("09", "Bilhares, boliches e diversões eletrônicas"),
            ("10", "Corridas e competições de animais"),
            ("11", "Competições esportivas ou de destreza"),
            ("12", "Execução de música"),
            ("13", "Produção de eventos e shows"),
            ("14", "Fornecimento de música para ambientes"),
            ("15", "Desfiles de blocos carnavalescos"),
            ("16", "Exibição de filmes e espetáculos"),
            ("17", "Recreação e animação"),
        ]),
        ("13", [
            ("01", "Fonografia"),
            ("02", "Fotografia e cinematografia"),
            ("03", "Reprografia, microfilmagem e digitalização"),
            ("04", "Composição gráfica e fotocomposição"),
            ("05", "Composição gráfica, inclusive confecção de impressos gráficos"),
        ]),
        ("14", [
            ("01", "Lubrificação, limpeza, lustração, revisão de máquinas"),
            ("02", "Assistência técnica"),
            ("03", "Recondicionamento de motores"),
            ("04", "Recauchutagem ou regeneração de pneus"),
            ("05", "Restauração, recondicionamento de quaisquer objetos"),
            ("06", "Instalação e montagem de aparelhos e equipamentos"),
            ("07", "Colocação de molduras e afins"),
            ("08", "Encadernação, gravação e douração"),
            ("09", "Alfaiataria e costura"),
            ("10", "Tinturaria e lavanderia"),
            ("11", "Tapeçaria e reforma de estofamentos"),
            ("12", "Funilaria e lanternagem"),
            ("13", "Carpintaria e serralheria"),
            ("14", "Guincho intramunicipal, guindaste e içamento"),
        ]),
        ("15", [
            ("01", "Administração de fundos, consórcio e cartão de crédito"),
            ("02", "Abertura de contas"),
            ("03", "Locação e manutenção de cofres"),
            ("04", "Fornecimento ou emissão de atestados"),
            ("05", "Cadastro, elaboração de ficha cadastral"),
            ("06", "Emissão, reemissão e fornecimento de avisos"),
            ("07", "Acesso, movimentação e consulta a contas"),
            ("08", "Emissão, reemissão, alteração de contratos de câmbio"),
            ("09", "Arrendamento mercantil (leasing)"),
            ("10", "Serviços de cobranças, recebimentos ou pagamentos"),
            ("11", "Devolução de títulos"),
            ("12", "Sustação de protesto"),
            ("13", "Fornecimento de segunda via de documentos"),
            ("14", "Emissão de certificados"),
            ("15", "Compensação de cheques e títulos"),
            ("16", "Emissão, fornecimento de documentos bancários"),
            ("17", "Concessão, revisão, retificação de empréstimos"),
            ("18", "Emissão, concessão, alteração de cartão de crédito"),
        ]),
        ("16", [
            ("01", "Serviços de transporte coletivo municipal rodoviário"),
            ("02", "Outros serviços de transporte de natureza municipal"),
        ]),
        ("17", [
            ("01", "Assessoria ou consultoria de qualquer natureza"),
            ("02", "Datilografia, digitação, estenografia e congêneres"),
            ("03", "Planejamento, coordenação, programação e organização técnica"),
            ("04", "Recrutamento, seleção e colocação de mão-de-obra"),
            ("05", "Fornecimento de mão-de-obra, mesmo em caráter temporário"),
            ("06", "Propaganda e publicidade"),
            ("07", "Franquia (franchising)"),
            ("08", "Perícias, laudos, exames técnicos e análises técnicas"),
            ("09", "Planejamento, organização e administração de feiras"),
            ("10", "Organização de festas e recepções"),
            ("11", "Administração em geral, inclusive de bens e negócios"),
            ("12", "Leilão e congêneres"),
            ("13", "Advocacia"),
            ("14", "Arbitragem de qualquer espécie"),
            ("15", "Auditoria"),
            ("16", "Análise de Organização e Métodos"),
            ("17", "Atuária e cálculos técnicos"),
            ("18", "Contabilidade e auditoria"),
            ("19", "Consultoria e assessoria econômica ou financeira"),
            ("20", "Estatística"),
            ("21", "Cobrança em geral"),
            ("22", "Assessoria, análise, avaliação e informações de risco"),
            ("23", "Apresentação de palestras, conferências e seminários"),
            ("24", "Inserção de textos, desenhos e outros materiais de propaganda"),
            ("25", "Serviços de investigações particulares, detetives e congêneres"),
        ]),
        ("18", [("01", "Serviços de regulação de sinistros")]),
        ("19", [("01", "Serviços de distribuição e venda de bilhetes de loteria")]),
        ("20", [
            ("01", "Serviços portuários e ferroportuários"),
            ("02", "Serviços aeroportuários"),
            ("03", "Serviços de terminais rodoviários, ferroviários e metroviários"),
        ]),
        ("21", [("01", "Serviços de registros públicos, cartorários e notariais")]),
        ("22", [("01", "Serviços de exploração de rodovia mediante cobrança de pedágio")]),
        ("23", [("01", "Serviços de programação e comunicação visual, desenho industrial")]),
        ("24", [("01", "Serviços de chaveiros, confecção de carimbos, placas, banners")]),
        ("25", [
            ("01", "Funerais, inclusive fornecimento de caixão e urna"),
            ("02", "Cremação de corpos e partes de corpos cadavéricos"),
            ("03", "Planos ou convênio funerários"),
            ("04", "Manutenção e conservação de jazigos e cemitérios"),
            ("05", "Cessão de uso de espaços em cemitérios para sepultamento"),
        ]),
        ("26", [("01", "Serviços de coleta, remessa ou entrega de correspondências e objetos")]),
        ("27", [("01", "Serviços de assistência social")]),
        ("28", [("01", "Serviços de avaliação de bens e serviços de qualquer natureza")]),
        ("29", [("01", "Serviços de biblioteconomia")]),
        ("30", [("01", "Serviços de biologia, biotecnologia e química")]),
        ("31", [("01", "Serviços técnicos em edificações, eletrônica, eletrotécnica, mecânica e telecomunicações")]),
        ("32", [("01", "Serviços de desenhos técnicos")]),
        ("33", [("01", "Serviços de desembaraço aduaneiro, comissários e despachantes")]),
        ("34", [("01", "Serviços de investigações particulares, detetives e congêneres")]),
        ("35", [("01", "Serviços de reportagem, assessoria de imprensa, jornalismo e relações públicas")]),
        ("36", [("01", "Serviços de meteorologia")]),
        ("37", [("01", "Serviços de artistas, atletas, modelos e manequins")]),
        ("38", [("01", "Serviços de museologia")]),
        ("39", [("01", "Serviços de ourivesaria e lapidação de metais preciosos, pedras e afins")]),
        ("40", [("01", "Obras de arte sob encomenda")]),
    ];
}
