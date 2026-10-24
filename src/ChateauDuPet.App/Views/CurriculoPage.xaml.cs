using ChateauDuPet.App.Core.ViewModels;

namespace ChateauDuPet.App.Views;

public partial class CurriculoPage : PaginaBase, IQueryAttributable
{
    private readonly CurriculoViewModel _vm;

    public CurriculoPage(CurriculoViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id))
            _vm.Abrir((int)id);
    }
}
