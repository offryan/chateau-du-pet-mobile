using ChateauDuPet.App.Core.ViewModels;
using ChateauDuPet.Contracts;

namespace ChateauDuPet.App.Views;

public partial class CandidatosPage : PaginaBase, IQueryAttributable
{
    private readonly CandidatosViewModel _vm;

    public CandidatosPage(CandidatosViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("vaga", out var valor) && valor is VagaResumo vaga)
            _vm.Abrir(vaga.Id, vaga.Titulo);
    }
}
