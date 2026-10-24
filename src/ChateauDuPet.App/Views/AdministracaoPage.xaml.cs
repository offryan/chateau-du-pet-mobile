using ChateauDuPet.App.Core.ViewModels;

namespace ChateauDuPet.App.Views;

public partial class AdministracaoPage : PaginaBase
{
    public AdministracaoPage(AdministracaoViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
