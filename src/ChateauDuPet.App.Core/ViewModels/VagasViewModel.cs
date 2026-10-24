using ChateauDuPet.App.Core.Servicos;
using ChateauDuPet.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChateauDuPet.App.Core.ViewModels;

public partial class VagasViewModel : ViewModelBase, IAoAparecer
{
    private readonly IChateauApi _api;
    private readonly INavegacao _navegacao;

    private CancellationTokenSource? _buscaPendente;
    private bool _carregouUmaVez;
    private int _ultimaConsulta;
    private string _buscaDosResultados = "";

    public VagasViewModel(IChateauApi api, INavegacao navegacao, AlteracoesDeDados alteracoes)
    {
        _api = api;
        _navegacao = navegacao;
        // Vaga publicada, editada ou excluída na gestão: a lista recarrega quando a aba voltar a aparecer
        alteracoes.VagasAlteradas += (_, _) => _carregouUmaVez = false;
    }

    /// <summary>Espera após a última tecla antes de buscar, para não chamar a API a cada letra.</summary>
    public TimeSpan AtrasoDaBusca { get; set; } = TimeSpan.FromMilliseconds(400);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Resumo), nameof(SemResultados))]
    public partial IReadOnlyList<VagaResumo> Vagas { get; set; } = [];

    [ObservableProperty]
    public partial string Busca { get; set; } = "";

    public string Resumo => string.IsNullOrWhiteSpace(_buscaDosResultados)
        ? Formatos.Quantidade(Vagas.Count, "vaga aberta", "vagas abertas")
        : $"{Formatos.Quantidade(Vagas.Count, "resultado", "resultados")} para \"{_buscaDosResultados.Trim()}\"";

    public bool SemResultados => _carregouUmaVez && Vagas.Count == 0;

    public async Task AoAparecerAsync()
    {
        if (!_carregouUmaVez) await CarregarAsync();
    }

    public override async Task CarregarAsync()
    {
        var busca = Busca;
        var consulta = ++_ultimaConsulta;
        await CarregarComIndicadorAsync(async () =>
        {
            var vagas = await _api.ListarVagasAsync(busca);
            // Uma busca mais nova já foi disparada: descarta este resultado atrasado
            if (consulta != _ultimaConsulta) return;
            _buscaDosResultados = busca;
            _carregouUmaVez = true;
            Vagas = vagas;
        });
    }

    [RelayCommand]
    private Task AbrirVagaAsync(VagaResumo? vaga) =>
        vaga is null ? Task.CompletedTask : _navegacao.AbrirVagaAsync(vaga.Id);

    partial void OnBuscaChanged(string value)
    {
        _buscaPendente?.Cancel();
        _buscaPendente = new CancellationTokenSource();
        _ = BuscarComAtrasoAsync(_buscaPendente.Token);
    }

    private async Task BuscarComAtrasoAsync(CancellationToken ct)
    {
        try
        {
            if (AtrasoDaBusca > TimeSpan.Zero) await Task.Delay(AtrasoDaBusca, ct);
            ct.ThrowIfCancellationRequested();
            await CarregarAsync();
        }
        catch (OperationCanceledException)
        {
            // O usuário continuou digitando; a próxima busca substitui esta
        }
    }
}
