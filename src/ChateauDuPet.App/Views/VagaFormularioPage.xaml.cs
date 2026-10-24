using ChateauDuPet.App.Core.ViewModels;

namespace ChateauDuPet.App.Views;

public partial class VagaFormularioPage : PaginaBase, IQueryAttributable
{
    private readonly VagaFormularioViewModel _vm;

    public VagaFormularioPage(VagaFormularioViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.Abrir(null); // sem parâmetro na rota: vaga nova
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _vm.Abrir(query.TryGetValue("id", out var id) ? (int)id : null);
    }
}
