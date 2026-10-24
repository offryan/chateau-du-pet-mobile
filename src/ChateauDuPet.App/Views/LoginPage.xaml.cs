using ChateauDuPet.App.Core.ViewModels;

namespace ChateauDuPet.App.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        CampoEmail.Completed += (_, _) => CampoSenha.Focus();
    }
}
