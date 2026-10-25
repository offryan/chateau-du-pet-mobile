using System.Globalization;
using ChateauDuPet.App.Core.Servicos;
using ChateauDuPet.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChateauDuPet.App.Core.ViewModels;

public enum SecaoDaAdministracao { Profissionais, Empresas, Vagas }

/// <summary>Painel do administrador: profissionais, empresas e vagas, com busca e exclusão.</summary>
public partial class AdministracaoViewModel(
    IChateauApi api, INavegacao navegacao, IPlataforma plataforma, AlteracoesDeDados alteracoes)
    : ViewModelBase, IAoAparecer
{
    private bool _carregou;
    private IReadOnlyList<ProfissionalAdmin> _profissionais = [];
    private IReadOnlyList<EmpresaAdmin> _empresas = [];
    private IReadOnlyList<VagaResumo> _vagas = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EmProfissionais), nameof(EmEmpresas), nameof(EmVagas))]
    public partial SecaoDaAdministracao Secao { get; set; }

    public bool EmProfissionais => Secao == SecaoDaAdministracao.Profissionais;
    public bool EmEmpresas => Secao == SecaoDaAdministracao.Empresas;
    public bool EmVagas => Secao == SecaoDaAdministracao.Vagas;

    /// <summary>Busca por nome, e-mail, usuário, empresa ou cidade, dentro da seção aberta.</summary>
    [ObservableProperty]
    public partial string Filtro { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExcluirProfissionalCommand), nameof(ExcluirEmpresaCommand), nameof(ExcluirVagaCommand))]
    public partial bool Excluindo { get; set; }

    public IReadOnlyList<ProfissionalAdmin> Profissionais =>
        Filtrar(_profissionais, p => [p.Nome, p.Email, p.Usuario, p.Cidade]);

    public IReadOnlyList<EmpresaAdmin> Empresas =>
        Filtrar(_empresas, e => [e.Nome, e.Recrutador, e.Email, e.Cnpj]);

    public IReadOnlyList<VagaResumo> Vagas =>
        Filtrar(_vagas, v => [v.Titulo, v.Empresa, v.Cidade]);

    public string TituloProfissionais => $"Candidatos ({_profissionais.Count})";
    public string TituloEmpresas => $"Empresas ({_empresas.Count})";
    public string TituloVagas => $"Vagas ({_vagas.Count})";

    public bool SemResultados => _carregou && Secao switch
    {
        SecaoDaAdministracao.Profissionais => Profissionais.Count == 0,
        SecaoDaAdministracao.Empresas => Empresas.Count == 0,
        _ => Vagas.Count == 0
    };

    public async Task AoAparecerAsync()
    {
        if (!_carregou) await CarregarAsync();
    }

    public override async Task CarregarAsync()
    {
        if (await CarregarComIndicadorAsync(BuscarTudoAsync)) _carregou = true;
        AtualizarListas();
    }

    private async Task BuscarTudoAsync()
    {
        var profissionais = api.ListarProfissionaisAsync();
        var empresas = api.ListarEmpresasAsync();
        var vagas = api.ListarVagasAsync(null);
        _profissionais = await profissionais;
        _empresas = await empresas;
        _vagas = await vagas;
    }

    [RelayCommand]
    private void MostrarSecao(SecaoDaAdministracao secao) => Secao = secao;

    [RelayCommand]
    private Task VerCurriculoAsync(ProfissionalAdmin? profissional) =>
        profissional is null ? Task.CompletedTask : navegacao.AbrirCurriculoAsync(profissional.Id);

    [RelayCommand]
    private Task AbrirVagaAsync(VagaResumo? vaga) =>
        vaga is null ? Task.CompletedTask : navegacao.AbrirVagaAsync(vaga.Id);

    private bool PodeExcluir() => !Excluindo;

    [RelayCommand(CanExecute = nameof(PodeExcluir))]
    private Task ExcluirProfissionalAsync(ProfissionalAdmin? p) => p is null ? Task.CompletedTask : ExcluirAsync(
        "Excluir candidato",
        $"{p.Nome} será excluído junto com o currículo e as candidaturas. Não dá para desfazer.",
        () => api.ExcluirProfissionalAsync(p.Id), "Candidato excluído.");

    [RelayCommand(CanExecute = nameof(PodeExcluir))]
    private Task ExcluirEmpresaAsync(EmpresaAdmin? e) => e is null ? Task.CompletedTask : ExcluirAsync(
        "Excluir empresa",
        $"{e.Nome} será excluída junto com {Formatos.Quantidade(e.TotalVagas, "vaga", "vagas")} e as candidaturas. Não dá para desfazer.",
        () => api.ExcluirEmpresaAsync(e.Id), "Empresa excluída.");

    [RelayCommand(CanExecute = nameof(PodeExcluir))]
    private Task ExcluirVagaAsync(VagaResumo? v) => v is null ? Task.CompletedTask : ExcluirAsync(
        "Excluir vaga",
        $"A vaga \"{v.Titulo}\" ({v.Empresa}) e as candidaturas dela serão apagadas. Não dá para desfazer.",
        () => api.ExcluirVagaComoAdminAsync(v.Id), "Vaga excluída.");

    private async Task ExcluirAsync(string titulo, string mensagem, Func<Task> excluir, string confirmacao)
    {
        if (!await plataforma.ConfirmarAsync(titulo, mensagem, "Excluir", "Cancelar")) return;

        Excluindo = true;
        try
        {
            var excluiu = await ExecutarAcaoAsync(excluir, plataforma, navegacao);
            if (excluiu)
            {
                alteracoes.NotificarVagasAlteradas();
                await plataforma.MostrarAvisoAsync(confirmacao);
            }
            // Exclusões apagam dados em cascata (vagas de uma empresa, candidaturas...), e um 404
            // significa que outra pessoa já excluiu: em qualquer caso, busca as três listas de novo
            await ExecutarAcaoAsync(BuscarTudoAsync, plataforma, navegacao);
            AtualizarListas();
        }
        finally
        {
            Excluindo = false;
        }
    }

    partial void OnFiltroChanged(string value) => AtualizarListas();

    partial void OnSecaoChanged(SecaoDaAdministracao value)
    {
        Filtro = "";
        OnPropertyChanged(nameof(SemResultados));
    }

    private void AtualizarListas()
    {
        OnPropertyChanged(nameof(Profissionais));
        OnPropertyChanged(nameof(Empresas));
        OnPropertyChanged(nameof(Vagas));
        OnPropertyChanged(nameof(TituloProfissionais));
        OnPropertyChanged(nameof(TituloEmpresas));
        OnPropertyChanged(nameof(TituloVagas));
        OnPropertyChanged(nameof(SemResultados));
    }

    private IReadOnlyList<T> Filtrar<T>(IReadOnlyList<T> itens, Func<T, string?[]> campos)
    {
        var termo = Filtro.Trim();
        if (termo.Length == 0) return itens;
        // Ignora maiúsculas e acentos: "sao paulo" encontra "São Paulo"
        var comparacao = CultureInfo.InvariantCulture.CompareInfo;
        const CompareOptions opcoes = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
        return itens.Where(item => campos(item).Any(c => c is not null && comparacao.IndexOf(c, termo, opcoes) >= 0)).ToList();
    }
}
