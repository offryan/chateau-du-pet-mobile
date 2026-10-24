using ChateauDuPet.App.Core.Servicos;
using ChateauDuPet.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChateauDuPet.App.Core.ViewModels;

/// <summary>Formulário da empresa para publicar uma vaga nova ou editar/excluir uma existente.</summary>
public partial class VagaFormularioViewModel(
    IChateauApi api, INavegacao navegacao, IPlataforma plataforma, AlteracoesDeDados alteracoes)
    : ViewModelBase, IAoAparecer
{
    private int? _idVaga;
    private bool _carregou;

    public bool EhEdicao => _idVaga is not null;
    public string TituloDaTela => EhEdicao ? "Editar vaga" : "Publicar vaga";
    public string TextoDoBotao => EhEdicao ? "Salvar alterações" : "Publicar vaga";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SalvarCommand))]
    public partial string Titulo { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SalvarCommand))]
    [NotifyPropertyChangedFor(nameof(ContadorDaDescricao))]
    public partial string Descricao { get; set; } = "";

    [ObservableProperty]
    public partial string? Experiencia { get; set; }

    [ObservableProperty]
    public partial string? TipoRegistro { get; set; }

    // Mesmos valores iniciais do portal web
    [ObservableProperty]
    public partial string? Sexo { get; set; } = "Indiferente";

    [ObservableProperty]
    public partial string? Validade { get; set; } = "1 mês";

    [ObservableProperty]
    public partial IReadOnlyList<string> Experiencias { get; set; } = OpcoesDeVaga.Experiencias;

    [ObservableProperty]
    public partial IReadOnlyList<string> TiposDeRegistro { get; set; } = OpcoesDeVaga.TiposDeRegistro;

    [ObservableProperty]
    public partial IReadOnlyList<string> Sexos { get; set; } = OpcoesDeVaga.Sexos;

    [ObservableProperty]
    public partial IReadOnlyList<string> Validades { get; set; } = OpcoesDeVaga.Validades;

    /// <summary>Problema do formulário mostrado embaixo dos campos.</summary>
    [ObservableProperty]
    public partial string? ErroDoFormulario { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SalvarCommand), nameof(ExcluirCommand))]
    public partial bool Salvando { get; set; }

    public string ContadorDaDescricao => $"{Descricao.Length}/{OpcoesDeVaga.TamanhoDescricao}";

    public void Abrir(int? idVaga)
    {
        _idVaga = idVaga;
        _carregou = idVaga is null;
        OnPropertyChanged(string.Empty);
    }

    public async Task AoAparecerAsync()
    {
        if (!_carregou) await CarregarAsync();
    }

    public override async Task CarregarAsync()
    {
        if (_idVaga is not { } id) return;
        if (await CarregarComIndicadorAsync(async () => Preencher(await api.ObterVagaAsync(id))))
            _carregou = true;
    }

    private void Preencher(VagaDetalhe vaga)
    {
        Titulo = vaga.Titulo;
        Descricao = vaga.Descricao ?? "";
        // Vagas antigas podem ter valores fora das opções atuais: entram na lista para não se perderem
        Experiencias = ComValorAtual(OpcoesDeVaga.Experiencias, vaga.Experiencia);
        TiposDeRegistro = ComValorAtual(OpcoesDeVaga.TiposDeRegistro, vaga.TipoRegistro);
        Sexos = ComValorAtual(OpcoesDeVaga.Sexos, vaga.Sexo);
        Validades = ComValorAtual(OpcoesDeVaga.Validades, vaga.Validade);
        Experiencia = vaga.Experiencia;
        TipoRegistro = vaga.TipoRegistro;
        Sexo = vaga.Sexo;
        Validade = vaga.Validade;
    }

    private static IReadOnlyList<string> ComValorAtual(IReadOnlyList<string> opcoes, string? atual) =>
        string.IsNullOrWhiteSpace(atual) || opcoes.Contains(atual) ? opcoes : [atual, .. opcoes];

    private bool PodeSalvar() => !Salvando && !string.IsNullOrWhiteSpace(Titulo) && !string.IsNullOrWhiteSpace(Descricao);

    [RelayCommand(CanExecute = nameof(PodeSalvar))]
    private async Task SalvarAsync()
    {
        var formulario = new VagaFormulario(Titulo, Experiencia, TipoRegistro, Sexo, Validade, Descricao);
        ErroDoFormulario = formulario.Validar();
        if (ErroDoFormulario is not null) return;

        Salvando = true;
        try
        {
            var idVaga = _idVaga;
            var salvou = await ExecutarAcaoAsync(async () =>
            {
                if (idVaga is { } id) await api.EditarVagaAsync(id, formulario.Normalizado());
                else await api.PublicarVagaAsync(formulario.Normalizado());
            }, plataforma, navegacao);
            if (!salvou) return;

            alteracoes.NotificarVagasAlteradas();
            await plataforma.MostrarAvisoAsync(EhEdicao ? "Vaga atualizada." : "Vaga publicada!");
            await navegacao.VoltarAsync();
        }
        finally
        {
            Salvando = false;
        }
    }

    private bool PodeExcluir() => EhEdicao && !Salvando;

    [RelayCommand(CanExecute = nameof(PodeExcluir))]
    private async Task ExcluirAsync()
    {
        if (_idVaga is not { } id) return;
        if (!await plataforma.ConfirmarAsync("Excluir vaga",
                $"A vaga \"{Titulo}\" e as candidaturas dela serão apagadas. Não dá para desfazer.", "Excluir", "Cancelar"))
            return;

        Salvando = true;
        try
        {
            if (!await ExecutarAcaoAsync(() => api.ExcluirMinhaVagaAsync(id), plataforma, navegacao)) return;
            alteracoes.NotificarVagasAlteradas();
            await plataforma.MostrarAvisoAsync("Vaga excluída.");
            await navegacao.VoltarAsync();
        }
        finally
        {
            Salvando = false;
        }
    }

    [RelayCommand]
    private Task CancelarAsync() => navegacao.VoltarAsync();

    partial void OnTituloChanged(string value) => ErroDoFormulario = null;

    partial void OnDescricaoChanged(string value) => ErroDoFormulario = null;
}
