using ChateauDuPet.App.Core.ViewModels;

namespace ChateauDuPet.App.Views;

public partial class VagaDetalhePage : PaginaBase, IQueryAttributable
{
    private readonly VagaDetalheViewModel _vm;

    public VagaDetalhePage(VagaDetalheViewModel vm)
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
