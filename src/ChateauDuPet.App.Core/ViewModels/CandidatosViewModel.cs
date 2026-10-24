using ChateauDuPet.App.Core.Servicos;
using ChateauDuPet.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChateauDuPet.App.Core.ViewModels;

/// <summary>Candidatos de uma vaga, visto pela empresa que a publicou.</summary>
public partial class CandidatosViewModel(IChateauApi api, IPlataforma plataforma) : ViewModelBase, IAoAparecer
{
    private int _idVaga;
    private bool _carregou;

    [ObservableProperty]
    public partial string TituloVaga { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Resumo), nameof(SemCandidatos))]
    public partial IReadOnlyList<CandidatoResumo> Candidatos { get; set; } = [];

    public string Resumo => Formatos.Quantidade(Candidatos.Count, "candidato", "candidatos");
    public bool SemCandidatos => _carregou && Candidatos.Count == 0;

    public void Abrir(int idVaga, string tituloVaga)
    {
        _idVaga = idVaga;
        TituloVaga = tituloVaga;
        _carregou = false;
    }

    public async Task AoAparecerAsync()
    {
        if (!_carregou) await CarregarAsync();
    }

    public override async Task CarregarAsync()
    {
        if (await CarregarComIndicadorAsync(async () => Candidatos = await api.CandidatosDaVagaAsync(_idVaga)))
        {
            _carregou = true;
            OnPropertyChanged(nameof(SemCandidatos));
        }
    }

    [RelayCommand]
    private Task EnviarEmailAsync(CandidatoResumo? candidato) => candidato is null
        ? Task.CompletedTask
        : plataforma.AbrirLinkAsync(
            $"mailto:{candidato.Email}?subject={Uri.EscapeDataString($"Sua candidatura: {TituloVaga}")}");
}
