using ChateauDuPet.App.Core.ViewModels;

namespace ChateauDuPet.App.Views;

public partial class SobrePage : ContentPage
{
    public SobrePage(SobreViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        Versao.Text = $"Versão {AppInfo.Current.VersionString} ({AppInfo.Current.BuildString}) · API: {Configuracao.ApiUrl.Host}";
    }
}
