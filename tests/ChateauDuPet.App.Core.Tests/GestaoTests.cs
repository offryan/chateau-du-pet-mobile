using ChateauDuPet.App.Core.Servicos;
using ChateauDuPet.App.Core.ViewModels;
using ChateauDuPet.Contracts;

namespace ChateauDuPet.App.Core.Tests;

public class VagaFormularioTests
{
    [Fact]
    public void Validacao_compartilhada_com_a_api()
    {
        var valida = new VagaFormulario("Tosador", null, null, null, null, "Banho e tosa");

        Assert.Null(valida.Validar());
        Assert.Equal("Informe o título da vaga.", (valida with { Titulo = " " }).Validar());
        Assert.Equal("Descreva a vaga.", (valida with { Descricao = null }).Validar());
        Assert.Equal("O título pode ter até 100 caracteres.", (valida with { Titulo = new string('a', 101) }).Validar());
    }

    [Fact]
    public void Normalizado_tira_espacos_e_troca_vazios_por_nulo()
    {
        var formulario = new VagaFormulario("  Tosador ", " ", "CLT ", null, "", "  Banho  ");

        Assert.Equal(new VagaFormulario("Tosador", null, "CLT", null, null, "Banho"), formulario.Normalizado());
    }
}

public class VagaFormularioViewModelTests : TesteDeViewModel
{
    private VagaFormularioViewModel Criar(int? idVaga = null)
    {
        var vm = new VagaFormularioViewModel(Api, Navegacao, Plataforma, Alteracoes);
        vm.Abrir(idVaga);
        return vm;
    }

    [Fact]
    public async Task Vaga_nova_comeca_com_os_padroes_do_portal_e_sem_consultar_a_api()
    {
        var vm = Criar();
        await vm.AoAparecerAsync();

        Assert.False(vm.EhEdicao);
        Assert.Equal("Publicar vaga", vm.TituloDaTela);
        Assert.Equal("Indiferente", vm.Sexo);
        Assert.Equal("1 mês", vm.Validade);
        Assert.Empty(Api.VagasConsultadas);
        Assert.False(vm.ExcluirCommand.CanExecute(null));
    }

    [Fact]
    public void Salvar_so_fica_disponivel_com_titulo_e_descricao()
    {
        var vm = Criar();
        Assert.False(vm.SalvarCommand.CanExecute(null));

        vm.Titulo = "Tosador";
        Assert.False(vm.SalvarCommand.CanExecute(null));

        vm.Descricao = "Banho e tosa";
        Assert.True(vm.SalvarCommand.CanExecute(null));
    }

    [Fact]
    public async Task Publicar_envia_o_formulario_normalizado_avisa_as_listas_e_volta()
    {
        var avisos = 0;
        Alteracoes.VagasAlteradas += (_, _) => avisos++;
        var vm = Criar();
        vm.Titulo = "  Tosador ";
        vm.Descricao = "Banho e tosa";
        vm.TipoRegistro = "CLT";

        await vm.SalvarCommand.ExecuteAsync(null);

        var enviada = Assert.Single(Api.Publicadas);
        Assert.Equal(new VagaFormulario("Tosador", null, "CLT", "Indiferente", "1 mês", "Banho e tosa"), enviada);
        Assert.Equal(1, avisos);
        Assert.Equal(["Vaga publicada!"], Plataforma.Avisos);
        Assert.Equal(["voltar"], Navegacao.Historico);
    }

    [Fact]
    public async Task Titulo_grande_demais_mostra_o_erro_sem_chamar_a_api()
    {
        var vm = Criar();
        vm.Titulo = new string('a', 101);
        vm.Descricao = "Banho e tosa";

        await vm.SalvarCommand.ExecuteAsync(null);

        Assert.Equal("O título pode ter até 100 caracteres.", vm.ErroDoFormulario);
        Assert.Empty(Api.Publicadas);
        Assert.Empty(Navegacao.Historico);

        vm.Titulo = "Tosador";
        Assert.Null(vm.ErroDoFormulario); // some ao corrigir
    }

    [Fact]
    public async Task Falha_da_api_ao_publicar_avisa_e_continua_no_formulario()
    {
        Api.AoPublicar = () => throw new ApiException("O servidor teve um problema. Tente de novo em instantes.", 500);
        var vm = Criar();
        vm.Titulo = "Tosador";
        vm.Descricao = "Banho e tosa";

        await vm.SalvarCommand.ExecuteAsync(null);

        Assert.Equal(["O servidor teve um problema. Tente de novo em instantes."], Plataforma.Avisos);
        Assert.Empty(Navegacao.Historico);
        Assert.False(vm.Salvando);
        Assert.Equal("Tosador", vm.Titulo); // nada do que foi digitado se perde
    }

    [Fact]
    public async Task Sessao_expirada_ao_publicar_leva_ao_login()
    {
        Api.AoPublicar = () => throw new SessaoExpiradaException();
        var vm = Criar();
        vm.Titulo = "Tosador";
        vm.Descricao = "Banho e tosa";

        await vm.SalvarCommand.ExecuteAsync(null);

        Assert.Equal(["login"], Navegacao.Historico);
    }

    [Fact]
    public async Task Edicao_carrega_a_vaga_e_mantem_valores_antigos_fora_das_opcoes()
    {
        // Vaga antiga com experiência "1 ano", que não está na lista atual de opções
        var vm = Criar(idVaga: 7);
        await vm.AoAparecerAsync();

        Assert.True(vm.EhEdicao);
        Assert.Equal("Editar vaga", vm.TituloDaTela);
        Assert.Equal("Médico Veterinário", vm.Titulo);
        Assert.Equal("1 ano", vm.Experiencia);
        Assert.Equal("1 ano", vm.Experiencias[0]);
        Assert.Equal(OpcoesDeVaga.Experiencias.Count + 1, vm.Experiencias.Count);
        Assert.Same(OpcoesDeVaga.TiposDeRegistro, vm.TiposDeRegistro); // "CLT" já é uma opção
    }

    [Fact]
    public async Task Salvar_na_edicao_atualiza_a_vaga_certa()
    {
        var vm = Criar(idVaga: 7);
        await vm.AoAparecerAsync();
        vm.Titulo = "Médico Veterinário Sênior";

        await vm.SalvarCommand.ExecuteAsync(null);

        var (id, vaga) = Assert.Single(Api.Editadas);
        Assert.Equal(7, id);
        Assert.Equal("Médico Veterinário Sênior", vaga.Titulo);
        Assert.Equal(["Vaga atualizada."], Plataforma.Avisos);
        Assert.Empty(Api.Publicadas);
    }

    [Fact]
    public async Task Excluir_pede_confirmacao()
    {
        var vm = Criar(idVaga: 7);
        await vm.AoAparecerAsync();

        Plataforma.RespostaDaConfirmacao = false;
        await vm.ExcluirCommand.ExecuteAsync(null);
        Assert.Empty(Api.Exclusoes);

        Plataforma.RespostaDaConfirmacao = true;
        await vm.ExcluirCommand.ExecuteAsync(null);
        Assert.Equal(["minha-vaga/7"], Api.Exclusoes);
        Assert.Equal(["voltar"], Navegacao.Historico);
    }
}

public class ListasReagemAGestaoTests : TesteDeViewModel
{
    [Fact]
    public async Task Lista_de_vagas_recarrega_depois_de_uma_alteracao()
    {
        var vm = new VagasViewModel(Api, Navegacao, Alteracoes) { AtrasoDaBusca = TimeSpan.Zero };
        await vm.AoAparecerAsync();
        await vm.AoAparecerAsync();
        Assert.Single(Api.Buscas);

        Alteracoes.NotificarVagasAlteradas();
        await vm.AoAparecerAsync();

        Assert.Equal(2, Api.Buscas.Count);
    }

    [Fact]
    public async Task Area_da_empresa_publica_edita_e_recarrega_depois_de_uma_alteracao()
    {
        var carregamentos = 0;
        Api.MinhasVagas = [Dados.Resumo(1)];
        await Sessao.IniciarAsync(Dados.Login(Dados.Empresa));
        var vm = new MinhaAreaViewModel(Api, Sessao, Navegacao, Plataforma, Alteracoes);
        vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.MinhasVagas)) carregamentos++; };
        await vm.AoAparecerAsync();
        var antes = carregamentos;

        await vm.PublicarVagaCommand.ExecuteAsync(null);
        await vm.EditarVagaCommand.ExecuteAsync(vm.MinhasVagas[0]);
        Alteracoes.NotificarVagasAlteradas();
        await vm.AoAparecerAsync();

        Assert.Equal(["vaga/nova", "vaga/1/editar"], Navegacao.Historico);
        Assert.True(carregamentos > antes);
    }

    [Fact]
    public async Task Administrador_abre_o_painel_pela_minha_area()
    {
        await Sessao.IniciarAsync(Dados.Login(new UsuarioInfo(1, "Administrador Demo", "admin@demo.com", TipoUsuario.Administrativo)));
        var vm = new MinhaAreaViewModel(Api, Sessao, Navegacao, Plataforma, Alteracoes);

        await vm.AoAparecerAsync();
        await vm.AbrirAdministracaoCommand.ExecuteAsync(null);

        Assert.True(vm.EhAdministrador);
        Assert.Equal(["administracao"], Navegacao.Historico);
    }

    [Fact]
    public async Task Candidato_abre_o_curriculo()
    {
        var vm = new CandidatosViewModel(Api, Navegacao, Plataforma);

        await vm.AbrirCurriculoCommand.ExecuteAsync(
            new CandidatoResumo(4, "Caroline Cordeiro", "caroline@exemplo.com", null, new DateTime(2026, 10, 20)));

        Assert.Equal(["curriculo/4"], Navegacao.Historico);
    }
}

public class CurriculoViewModelTests : TesteDeViewModel
{
    private static readonly PerfilProfissional Caroline = new(4, "Caroline Cordeiro", "caroline@exemplo.com", "11955869445",
        null, "Superior Completo", "São Paulo", "SP", [], []);

    [Fact]
    public async Task Carrega_o_curriculo_e_oferece_contato()
    {
        Api.Curriculos[4] = Caroline;
        var vm = new CurriculoViewModel(Api, Plataforma);
        vm.Abrir(4);

        await vm.AoAparecerAsync();
        await vm.EnviarEmailCommand.ExecuteAsync(null);
        await vm.LigarCommand.ExecuteAsync(null);

        Assert.Equal("CC", vm.Iniciais);
        Assert.StartsWith("mailto:caroline@exemplo.com?subject=", Plataforma.Links[0]);
        Assert.Equal("tel:11955869445", Plataforma.Links[1]);
    }

    [Fact]
    public async Task Curriculo_inacessivel_mostra_o_erro()
    {
        var vm = new CurriculoViewModel(Api, Plataforma);
        vm.Abrir(3);

        await vm.AoAparecerAsync();

        Assert.True(vm.TemErro);
        Assert.Null(vm.Perfil);
    }
}

public class AdministracaoViewModelTests : TesteDeViewModel
{
    private AdministracaoViewModel Criar()
    {
        Api.ProfissionaisAdmin =
        [
            new(1, "Jucelino Silva", "jucelino", "candidato@demo.com", "11911290115", "São Paulo"),
            new(3, "Jessica Ferreira", "jessica", "jessica@exemplo.com", "11911290112", "Campinas")
        ];
        Api.EmpresasAdmin = [new(1, "Salvadores do Bem", "Felipe", "empresa@demo.com", null, 2)];
        Api.AoListarVagas = _ => Task.FromResult<IReadOnlyList<VagaResumo>>([Dados.Resumo(1), Dados.Resumo(2, "Zootecnista")]);
        return new AdministracaoViewModel(Api, Navegacao, Plataforma, Alteracoes);
    }

    [Fact]
    public async Task Carrega_as_tres_listas_com_os_totais()
    {
        var vm = Criar();
        await vm.AoAparecerAsync();

        Assert.True(vm.EmProfissionais);
        Assert.Equal("Profissionais (2)", vm.TituloProfissionais);
        Assert.Equal("Empresas (1)", vm.TituloEmpresas);
        Assert.Equal("Vagas (2)", vm.TituloVagas);
    }

    [Fact]
    public async Task Busca_ignora_maiusculas_e_acentos_e_limpa_ao_trocar_de_secao()
    {
        var vm = Criar();
        await vm.AoAparecerAsync();

        vm.Filtro = "SAO PAULO";
        Assert.Equal("Jucelino Silva", Assert.Single(vm.Profissionais).Nome);

        vm.Filtro = "girafa";
        Assert.True(vm.SemResultados);

        vm.MostrarSecaoCommand.Execute(SecaoDaAdministracao.Vagas);
        Assert.True(vm.EmVagas);
        Assert.Equal("", vm.Filtro);
        Assert.Equal(2, vm.Vagas.Count);
    }

    [Fact]
    public async Task Excluir_pede_confirmacao_avisa_as_listas_e_recarrega()
    {
        var avisos = 0;
        Alteracoes.VagasAlteradas += (_, _) => avisos++;
        var vm = Criar();
        await vm.AoAparecerAsync();

        Plataforma.RespostaDaConfirmacao = false;
        await vm.ExcluirEmpresaCommand.ExecuteAsync(vm.Empresas[0]);
        Assert.Empty(Api.Exclusoes);

        Plataforma.RespostaDaConfirmacao = true;
        Api.EmpresasAdmin = [];
        await vm.ExcluirEmpresaCommand.ExecuteAsync(vm.Empresas[0]);

        Assert.Equal(["empresa/1"], Api.Exclusoes);
        Assert.Equal(1, avisos);
        Assert.Equal(["Empresa excluída."], Plataforma.Avisos);
        Assert.Equal("Empresas (0)", vm.TituloEmpresas);
    }

    [Fact]
    public async Task Item_ja_excluido_por_outra_pessoa_avisa_e_atualiza_a_lista()
    {
        var vm = Criar();
        await vm.AoAparecerAsync();
        Api.AoExcluir = _ => throw new ApiException("Não encontramos o que você procurou. Talvez tenha sido removido.", 404);
        Api.ProfissionaisAdmin = [Api.ProfissionaisAdmin[1]];

        await vm.ExcluirProfissionalCommand.ExecuteAsync(vm.Profissionais[0]);

        Assert.Equal(["Não encontramos o que você procurou. Talvez tenha sido removido."], Plataforma.Avisos);
        Assert.Equal("Profissionais (1)", vm.TituloProfissionais);
        Assert.False(vm.Excluindo);
    }

    [Fact]
    public async Task Abre_curriculo_e_vaga()
    {
        var vm = Criar();
        await vm.AoAparecerAsync();

        await vm.VerCurriculoCommand.ExecuteAsync(vm.Profissionais[1]);
        await vm.AbrirVagaCommand.ExecuteAsync(vm.Vagas[1]);

        Assert.Equal(["curriculo/3", "vaga/2"], Navegacao.Historico);
    }
}
