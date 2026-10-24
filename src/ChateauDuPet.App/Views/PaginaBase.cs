using ChateauDuPet.App.Core.Servicos;

namespace ChateauDuPet.App.Views;

/// <summary>Página que avisa o ViewModel sempre que fica visível (para carregar ou atualizar dados).</summary>
public abstract class PaginaBase : ContentPage
{
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is IAoAparecer vm)
            await vm.AoAparecerAsync();
    }
}
