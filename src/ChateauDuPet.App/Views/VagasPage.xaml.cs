using ChateauDuPet.App.Core.ViewModels;

namespace ChateauDuPet.App.Views;

public partial class VagasPage : PaginaBase
{
    public VagasPage(VagasViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
