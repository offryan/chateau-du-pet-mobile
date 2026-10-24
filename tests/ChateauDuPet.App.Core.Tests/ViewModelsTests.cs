using ChateauDuPet.App.Core.Servicos;
using ChateauDuPet.App.Core.ViewModels;
using ChateauDuPet.Contracts;

namespace ChateauDuPet.App.Core.Tests;

public abstract class TesteDeViewModel
{
    protected readonly ApiFalsa Api = new();
    protected readonly NavegacaoFalsa Navegacao = new();
    protected readonly PlataformaFalsa Plataforma = new();
    protected readonly Sessao Sessao = new(new ArmazenamentoEmMemoria(), TimeProvider.System);
    protected readonly AlteracoesDeDados Alteracoes = new();
}

public class VagasViewModelTests : TesteDeViewModel
{
    private VagasViewModel Criar() => new(Api, Navegacao, Alteracoes) { AtrasoDaBusca = TimeSpan.Zero };

    [Fact]
    public async Task Carrega_as_vagas_ao_aparecer_apenas_na_primeira_vez()
    {
        Api.AoListarVagas = _ => Task.FromResult<IReadOnlyList<VagaResumo>>([Dados.Resumo(1), Dados.Resumo(2, "Tosador")]);
        var vm = Criar();

        await vm.AoAparecerAsync();
        await vm.AoAparecerAsync();

        Assert.Equal(2, vm.Vagas.Count);
        Assert.Equal("2 vagas abertas", vm.Resumo);
        Assert.Single(Api.Buscas);
        Assert.True(vm.MostrarConteudo);
        Assert.False(vm.SemResultados);
    }

    [Fact]
    public async Task Erro_da_api_vira_mensagem_e_tentar_de_novo_recarrega()
    {
        Api.AoListarVagas = _ => throw new ApiException("Não foi possível conectar ao servidor.");
        var vm = Criar();

        await vm.CarregarAsync();
        Assert.True(vm.TemErro);
        Assert.False(vm.MostrarConteudo);

        Api.AoListarVagas = _ => Task.FromResult<IReadOnlyList<VagaResumo>>([Dados.Resumo(1)]);
        await vm.TentarDeNovoCommand.ExecuteAsync(null);

        Assert.False(vm.TemErro);
        Assert.Single(vm.Vagas);
    }

    [Fact]
    public async Task Busca_consulta_a_api_com_o_termo_e_informa_quando_nao_ha_resultados()
    {
        var vm = Criar();

        vm.Busca = "girafa";
        await Aguardar(() => !vm.Carregando && Api.Buscas.Count == 1);

        Assert.Equal("girafa", Api.Buscas[0]);
        Assert.True(vm.SemResultados);
        Assert.Equal("0 resultados para \"girafa\"", vm.Resumo);
    }

    [Fact]
    public async Task Resultado_atrasado_de_uma_busca_antiga_e_descartado()
    {
        var lenta = new TaskCompletionSource<IReadOnlyList<VagaResumo>>();
        Api.AoListarVagas = busca => busca == "vet" ? lenta.Task : Task.FromResult<IReadOnlyList<VagaResumo>>([Dados.Resumo(2, "Veterinário")]);
        var vm = Criar();

        vm.Busca = "vet";
        await Aguardar(() => Api.Buscas.Count == 1);
        vm.Busca = "veterinário";
        await Aguardar(() => Api.Buscas.Count == 2 && vm.Vagas.Count == 1);
        lenta.SetResult([Dados.Resumo(9, "Resultado velho")]);
        await Task.Yield();

        Assert.Equal(2, Assert.Single(vm.Vagas).Id);
    }

    [Fact]
    public async Task Puxar_para_atualizar_mantem_a_lista_visivel_e_desliga_o_indicador()
    {
        var vm = Criar();
        vm.Atualizando = true;

        Assert.False(vm.MostrarIndicador);
        await vm.AtualizarCommand.ExecuteAsync(null);

        Assert.False(vm.Atualizando);
    }

    [Fact]
    public async Task Tocar_numa_vaga_abre_o_detalhe()
    {
        await Criar().AbrirVagaCommand.ExecuteAsync(Dados.Resumo(42));
        Assert.Equal(["vaga/42"], Navegacao.Historico);
    }

    private static async Task Aguardar(Func<bool> condicao)
    {
        for (var i = 0; i < 200 && !condicao(); i++) await Task.Delay(5);
        Assert.True(condicao(), "A condição não foi atingida a tempo");
    }
}

public class VagaDetalheViewModelTests : TesteDeViewModel
{
    private VagaDetalheViewModel Criar(int idVaga = 1)
    {
        var vm = new VagaDetalheViewModel(Api, Sessao, Navegacao, Plataforma);
        vm.Abrir(idVaga);
        return vm;
    }

    [Fact]
    public async Task Visitante_e_convidado_a_entrar()
    {
        var vm = Criar();
        await vm.AoAparecerAsync();

        Assert.True(vm.ConvidarParaEntrar);
        Assert.False(vm.PodeCandidatar);
        Assert.Equal("2 candidatos", vm.Candidatos);

        await vm.EntrarCommand.ExecuteAsync(null);
        Assert.Equal(["login"], Navegacao.Historico);
    }

    [Fact]
    public async Task Empresa_logada_ve_aviso_de_que_so_profissionais_se_candidatam()
    {
        await Sessao.IniciarAsync(Dados.Login(Dados.Empresa));
        var vm = Criar();
        await vm.AoAparecerAsync();

        Assert.True(vm.SomenteProfissionais);
        Assert.False(vm.ConvidarParaEntrar);
        Assert.False(vm.PodeCandidatar);
    }

    [Fact]
    public async Task Profissional_se_candidata_e_a_tela_atualiza_sem_nova_consulta()
    {
        await Sessao.IniciarAsync(Dados.Login(Dados.Profissional));
        Api.AoObterVaga = id => Task.FromResult(Dados.Detalhe(id, jaCandidatado: false, candidatos: 2));
        var candidatadas = new List<int>();
        Api.AoCandidatar = id => { candidatadas.Add(id); return Task.CompletedTask; };
        var vm = Criar();
        await vm.AoAparecerAsync();

        await vm.CandidatarCommand.ExecuteAsync(null);

        Assert.Equal([1], candidatadas);
        Assert.True(vm.JaCandidatado);
        Assert.Equal("3 candidatos", vm.Candidatos);
        Assert.Single(Api.VagasConsultadas);
        Assert.Contains("Candidatura enviada", Assert.Single(Plataforma.Avisos));
    }

    [Fact]
    public async Task Conflito_mostra_a_mensagem_e_sincroniza_com_o_servidor()
    {
        await Sessao.IniciarAsync(Dados.Login(Dados.Profissional));
        var jaCandidatado = false;
        Api.AoObterVaga = id => Task.FromResult(Dados.Detalhe(id, jaCandidatado));
        Api.AoCandidatar = _ =>
        {
            jaCandidatado = true; // candidatou-se em outro aparelho
            throw new ApiException("Você já se candidatou a esta vaga.", 409);
        };
        var vm = Criar();
        await vm.AoAparecerAsync();

        await vm.CandidatarCommand.ExecuteAsync(null);

        Assert.Equal(["Você já se candidatou a esta vaga."], Plataforma.Avisos);
        Assert.True(vm.JaCandidatado);
        Assert.Equal(2, Api.VagasConsultadas.Count);
    }

    [Fact]
    public async Task Cancelar_candidatura_pede_confirmacao()
    {
        await Sessao.IniciarAsync(Dados.Login(Dados.Profissional));
        Api.AoObterVaga = id => Task.FromResult(Dados.Detalhe(id, jaCandidatado: true));
        var canceladas = 0;
        Api.AoCancelar = _ => { canceladas++; return Task.CompletedTask; };
        var vm = Criar();
        await vm.AoAparecerAsync();

        Plataforma.RespostaDaConfirmacao = false;
        await vm.CancelarCandidaturaCommand.ExecuteAsync(null);
        Assert.Equal(0, canceladas);
        Assert.True(vm.JaCandidatado);

        Plataforma.RespostaDaConfirmacao = true;
        await vm.CancelarCandidaturaCommand.ExecuteAsync(null);
        Assert.Equal(1, canceladas);
        Assert.True(vm.PodeCandidatar);
        Assert.Equal("1 candidato", vm.Candidatos);
    }

    [Fact]
    public async Task Sessao_expirada_ao_candidatar_leva_ao_login()
    {
        await Sessao.IniciarAsync(Dados.Login(Dados.Profissional));
        Api.AoObterVaga = id => Task.FromResult(Dados.Detalhe(id, jaCandidatado: false));
        Api.AoCandidatar = _ => throw new SessaoExpiradaException();
        var vm = Criar();
        await vm.AoAparecerAsync();

        await vm.CandidatarCommand.ExecuteAsync(null);

        Assert.Equal(["login"], Navegacao.Historico);
        Assert.Equal(["Sua sessão expirou. Entre novamente."], Plataforma.Avisos);
    }

    [Fact]
    public async Task Recarrega_ao_voltar_para_a_tela_depois_de_entrar()
    {
        var vm = Criar();
        await vm.AoAparecerAsync();
        await vm.AoAparecerAsync();
        Assert.Single(Api.VagasConsultadas);

        await Sessao.IniciarAsync(Dados.Login(Dados.Profissional));
        await vm.AoAparecerAsync();

        Assert.Equal(2, Api.VagasConsultadas.Count);
    }

    [Fact]
    public async Task Compartilhar_monta_texto_com_vaga_e_empresa()
    {
        var vm = Criar();
        await vm.AoAparecerAsync();

        await vm.CompartilharCommand.ExecuteAsync(null);

        Assert.StartsWith("Vaga de Médico Veterinário na Salvadores do Bem (São Paulo).", Assert.Single(Plataforma.Compartilhados));
    }
}

public class LoginViewModelTests : TesteDeViewModel
{
    private LoginViewModel Criar() => new(Api, Sessao, Navegacao);

    [Fact]
    public void Entrar_so_fica_disponivel_com_email_e_senha()
    {
        var vm = Criar();
        Assert.False(vm.EntrarCommand.CanExecute(null));

        vm.Email = "jucelino@exemplo.com";
        Assert.False(vm.EntrarCommand.CanExecute(null));

        vm.Senha = "demo1234";
        Assert.True(vm.EntrarCommand.CanExecute(null));
    }

    [Fact]
    public async Task Login_com_sucesso_inicia_a_sessao_e_fecha_a_tela()
    {
        Api.AoEntrar = (email, senha) => Task.FromResult(Dados.Login(Dados.Profissional));
        var vm = Criar();
        vm.Email = "  jucelino@exemplo.com ";
        vm.Senha = "demo1234";

        await vm.EntrarCommand.ExecuteAsync(null);

        Assert.Equal(Dados.Profissional, Sessao.Usuario);
        Assert.Equal(["voltar"], Navegacao.Historico);
        Assert.Equal("", vm.Senha);
    }

    [Fact]
    public async Task Senha_errada_mostra_erro_e_continua_na_tela()
    {
        var vm = Criar();
        vm.Email = "jucelino@exemplo.com";
        vm.Senha = "errada";

        await vm.EntrarCommand.ExecuteAsync(null);

        Assert.Equal("E-mail ou senha inválidos.", vm.Erro);
        Assert.False(Sessao.Logado);
        Assert.Empty(Navegacao.Historico);
    }

    [Fact]
    public async Task Conta_de_demonstracao_preenche_e_entra()
    {
        string? emailUsado = null;
        Api.AoEntrar = (email, senha) =>
        {
            emailUsado = email;
            return Task.FromResult(Dados.Login(Dados.Empresa));
        };
        var vm = Criar();

        await vm.UsarContaDemoCommand.ExecuteAsync(vm.ContasDemo.Single(c => c.Perfil == "Empresa"));

        Assert.Equal("felipe@exemplo.com", emailUsado);
        Assert.Equal(TipoUsuario.Empresa, Sessao.Usuario?.Tipo);
    }
}

public class MinhaAreaViewModelTests : TesteDeViewModel
{
    private MinhaAreaViewModel Criar() => new(Api, Sessao, Navegacao, Plataforma, Alteracoes);

    private static readonly PerfilProfissional Perfil = new(1, "Jucelino Silva", "jucelino@exemplo.com", "11911290115",
        "Apaixonado por animais", "Superior Completo", "São Paulo", "SP",
        [new ExperienciaInfo("Auxiliar veterinário", "MedPet", "CLT", "São Paulo", new DateOnly(2020, 2, 1), null)],
        []);

    [Fact]
    public async Task Visitante_ve_o_convite_para_entrar_sem_chamar_a_api()
    {
        var vm = Criar();
        await vm.AoAparecerAsync();

        Assert.True(vm.EhVisitante);
        Assert.Null(vm.Perfil);
        Assert.False(vm.Carregando);
    }

    [Fact]
    public async Task Profissional_ve_curriculo_e_candidaturas()
    {
        Api.Perfil = Perfil;
        Api.Candidaturas = [new CandidaturaResumo(1, "Médico Veterinário", "Salvadores do Bem", new DateTime(2026, 10, 20))];
        await Sessao.IniciarAsync(Dados.Login(Dados.Profissional));
        var vm = Criar();

        await vm.AoAparecerAsync();

        Assert.True(vm.EhProfissional);
        Assert.Equal("JS", vm.Iniciais);
        Assert.Equal(Perfil, vm.Perfil);
        Assert.Equal("1 candidatura", vm.ResumoCandidaturas);

        await vm.AbrirCandidaturaCommand.ExecuteAsync(vm.Candidaturas[0]);
        Assert.Equal(["vaga/1"], Navegacao.Historico);
    }

    [Fact]
    public async Task Empresa_ve_as_vagas_publicadas_e_abre_os_candidatos()
    {
        Api.MinhasVagas = [Dados.Resumo(1), Dados.Resumo(2, "Zootecnista")];
        await Sessao.IniciarAsync(Dados.Login(Dados.Empresa));
        var vm = Criar();

        await vm.AoAparecerAsync();
        await vm.AbrirCandidatosCommand.ExecuteAsync(vm.MinhasVagas[1]);

        Assert.True(vm.EhEmpresa);
        Assert.Equal("2 vagas publicadas", vm.ResumoVagas);
        Assert.Equal(["candidatos/2"], Navegacao.Historico);
    }

    [Fact]
    public async Task Login_feito_em_outra_tela_atualiza_a_area()
    {
        Api.Perfil = Perfil;
        var vm = Criar();
        await vm.AoAparecerAsync();
        var mudancas = new List<string?>();
        vm.PropertyChanged += (_, e) => mudancas.Add(e.PropertyName);

        await Sessao.IniciarAsync(Dados.Login(Dados.Profissional));
        await vm.AoAparecerAsync();

        Assert.Contains(string.Empty, mudancas); // avisa a tela que tudo mudou
        Assert.True(vm.EhProfissional);
        Assert.Equal(Perfil, vm.Perfil);
    }

    [Fact]
    public async Task Sair_pede_confirmacao_e_volta_a_ser_visitante()
    {
        Api.MinhasVagas = [Dados.Resumo(1)];
        await Sessao.IniciarAsync(Dados.Login(Dados.Empresa));
        var vm = Criar();
        await vm.AoAparecerAsync();

        Plataforma.RespostaDaConfirmacao = false;
        await vm.SairCommand.ExecuteAsync(null);
        Assert.True(Sessao.Logado);

        Plataforma.RespostaDaConfirmacao = true;
        await vm.SairCommand.ExecuteAsync(null);
        Assert.False(Sessao.Logado);
        Assert.True(vm.EhVisitante);
        Assert.Empty(vm.MinhasVagas);
    }
}

public class CandidatosViewModelTests : TesteDeViewModel
{
    [Fact]
    public async Task Lista_candidatos_e_abre_email_com_assunto_da_vaga()
    {
        Api.Candidatos = [new CandidatoResumo(1, "Jucelino Silva", "jucelino@exemplo.com", "Superior Completo", new DateTime(2026, 10, 20))];
        var vm = new CandidatosViewModel(Api, Navegacao, Plataforma);
        vm.Abrir(1, "Médico Veterinário");

        await vm.AoAparecerAsync();
        await vm.EnviarEmailCommand.ExecuteAsync(vm.Candidatos[0]);

        Assert.Equal("1 candidato", vm.Resumo);
        Assert.False(vm.SemCandidatos);
        Assert.Equal(["mailto:jucelino@exemplo.com?subject=Sua%20candidatura%3A%20M%C3%A9dico%20Veterin%C3%A1rio"], Plataforma.Links);
    }
}
