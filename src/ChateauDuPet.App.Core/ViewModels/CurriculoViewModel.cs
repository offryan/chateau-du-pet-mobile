using ChateauDuPet.App.Core.Servicos;
using ChateauDuPet.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChateauDuPet.App.Core.ViewModels;

/// <summary>Currículo de um profissional, visto por uma empresa (dos próprios candidatos) ou pelo administrador.</summary>
public partial class CurriculoViewModel(IChateauApi api, IPlataforma plataforma) : ViewModelBase, IAoAparecer
{
    private int _idProfissional;
    private bool _carregou;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Iniciais))]
    public partial PerfilProfissional? Perfil { get; set; }

    public string Iniciais => Formatos.Iniciais(Perfil?.Nome);

    public void Abrir(int idProfissional)
    {
        _idProfissional = idProfissional;
        _carregou = false;
    }

    public async Task AoAparecerAsync()
    {
        if (!_carregou) await CarregarAsync();
    }

    public override async Task CarregarAsync()
    {
        if (await CarregarComIndicadorAsync(async () => Perfil = await api.CurriculoAsync(_idProfissional)))
            _carregou = true;
    }

    [RelayCommand]
    private Task EnviarEmailAsync() => Perfil is null
        ? Task.CompletedTask
        : plataforma.AbrirLinkAsync($"mailto:{Perfil.Email}?subject={Uri.EscapeDataString("Chateau Du Pet: sua candidatura")}");

    [RelayCommand]
    private Task LigarAsync() => Perfil is null || string.IsNullOrWhiteSpace(Perfil.Telefone)
        ? Task.CompletedTask
        : plataforma.AbrirLinkAsync($"tel:{Perfil.Telefone}");
}
