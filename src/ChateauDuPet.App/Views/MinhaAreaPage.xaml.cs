using ChateauDuPet.App.Core.ViewModels;

namespace ChateauDuPet.App.Views;

public partial class MinhaAreaPage : PaginaBase
{
    public MinhaAreaPage(MinhaAreaViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
