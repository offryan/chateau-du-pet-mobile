using ChateauDuPet.App.Core.Servicos;
using ChateauDuPet.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChateauDuPet.App.Core.ViewModels;

/// <summary>
/// Aba "Minha área": convite para entrar (visitante), currículo e candidaturas
/// (profissional) ou vagas publicadas (empresa).
/// </summary>
public partial class MinhaAreaViewModel : ViewModelBase, IAoAparecer
{
    private readonly IChateauApi _api;
    private readonly Sessao _sessao;
    private readonly INavegacao _navegacao;
    private readonly IPlataforma _plataforma;
    private (bool Carregada, int? IdUsuario) _carregadaPara;

    public MinhaAreaViewModel(IChateauApi api, Sessao sessao, INavegacao navegacao, IPlataforma plataforma)
    {
        _api = api;
        _sessao = sessao;
        _navegacao = navegacao;
        _plataforma = plataforma;
        _sessao.Alterada += (_, _) => AtualizarTipo();
    }

    public UsuarioInfo? Usuario => _sessao.Usuario;
    public bool EhVisitante => _sessao.Usuario is null;
    public bool EhProfissional => _sessao.Usuario?.Tipo == TipoUsuario.Profissional;
    public bool EhEmpresa => _sessao.Usuario?.Tipo == TipoUsuario.Empresa;
    public bool EhAdministrador => _sessao.Usuario?.Tipo == TipoUsuario.Administrativo;
    public string Iniciais => Formatos.Iniciais(_sessao.Usuario?.Nome);

    [ObservableProperty]
    public partial PerfilProfissional? Perfil { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResumoCandidaturas))]
    public partial IReadOnlyList<CandidaturaResumo> Candidaturas { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResumoVagas))]
    public partial IReadOnlyList<VagaResumo> MinhasVagas { get; set; } = [];

    public string ResumoCandidaturas => Formatos.Quantidade(Candidaturas.Count, "candidatura", "candidaturas");
    public string ResumoVagas => Formatos.Quantidade(MinhasVagas.Count, "vaga publicada", "vagas publicadas");

    public async Task AoAparecerAsync()
    {
        if (_carregadaPara != (true, _sessao.Usuario?.Id)) await CarregarAsync();
    }

    public override async Task CarregarAsync()
    {
        var usuario = _sessao.Usuario;
        // Marca antes de carregar para o AoAparecer não disparar uma segunda carga em paralelo
        _carregadaPara = (true, usuario?.Id);
        Perfil = null;
        Candidaturas = [];
        MinhasVagas = [];
        if (usuario is null || EhAdministrador)
        {
            Erro = null;
            return;
        }

        var ok = await CarregarComIndicadorAsync(async () =>
        {
            if (usuario.Tipo == TipoUsuario.Profissional)
            {
                var perfil = _api.MeuPerfilAsync();
                var candidaturas = _api.MinhasCandidaturasAsync();
                Perfil = await perfil;
                Candidaturas = await candidaturas;
            }
            else
            {
                MinhasVagas = await _api.MinhasVagasAsync();
            }
        });
        if (!ok) _carregadaPara = default;
    }

    [RelayCommand]
    private Task EntrarAsync() => _navegacao.AbrirLoginAsync();

    [RelayCommand]
    private async Task SairAsync()
    {
        if (await _plataforma.ConfirmarAsync("Sair", "Deseja sair da sua conta neste aparelho?", "Sair", "Cancelar"))
            await _sessao.EncerrarAsync();
    }

    [RelayCommand]
    private Task AbrirCandidaturaAsync(CandidaturaResumo? candidatura) =>
        candidatura is null ? Task.CompletedTask : _navegacao.AbrirVagaAsync(candidatura.IdVaga);

    [RelayCommand]
    private Task AbrirCandidatosAsync(VagaResumo? vaga) =>
        vaga is null ? Task.CompletedTask : _navegacao.AbrirCandidatosAsync(vaga);

    private void AtualizarTipo()
    {
        // Login/logout: todas as propriedades derivadas do usuário mudam
        OnPropertyChanged(string.Empty);
        _ = CarregarAsync();
    }
}
