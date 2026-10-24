using ChateauDuPet.App.Core.Servicos;
using CommunityToolkit.Mvvm.Input;

namespace ChateauDuPet.App.Core.ViewModels;

public partial class SobreViewModel(IPlataforma plataforma) : ViewModelBase
{
    public const string SiteOficial = "https://chateaudupet.com.br";
    public const string PortalWeb = "https://chateau-du-pet-demo.onrender.com";

    [RelayCommand]
    private Task AbrirLinkAsync(string url) => plataforma.AbrirLinkAsync(url);
}
