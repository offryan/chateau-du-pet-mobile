using System.Net;
using System.Net.Http.Json;
using ChateauDuPet.Contracts;

namespace ChateauDuPet.Api.Tests;

// Dados do seed: a empresa felipe@ é dona das vagas 1 e 2; a vaga 3 é de outra empresa.
// Candidaturas: profissionais 1 e 2 na vaga 1, profissional 4 na vaga 3; o 3 não se candidatou.
[Collection(ColecaoDaApi.Nome)]
public class GestaoTests(ApiFixture api) : IAsyncLifetime
{
    private const string Empresa = "felipe@exemplo.com";
    private const string Profissional = "jucelino@exemplo.com";
    private const string Admin = "admin@exemplo.com";

    private static readonly ChateauJsonContext Json = ChateauJsonContext.Default;
    private static CancellationToken CT => TestContext.Current.CancellationToken;

    private static readonly VagaFormulario Tosador =
        new("  Tosador  ", "Até 1 ano", "CLT", "Indiferente", "1 mês", "Banho e tosa em cães e gatos.");

    public async ValueTask InitializeAsync() => await api.RestaurarDadosAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static Task<HttpResponseMessage> Publicar(HttpClient cliente, VagaFormulario vaga) =>
        cliente.PostAsJsonAsync("/api/me/vagas", vaga, Json.VagaFormulario, CT);

    [Fact]
    public async Task Empresa_publica_vaga_que_aparece_para_todos()
    {
        var cliente = await api.ClienteLogadoAsync(Empresa);

        var resposta = await Publicar(cliente, Tosador with { Experiencia = "   " });
        var vaga = await resposta.Content.ReadFromJsonAsync(Json.VagaDetalhe, CT);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal($"/api/vagas/{vaga!.Id}", resposta.Headers.Location!.OriginalString);
        Assert.Equal("Tosador", vaga.Titulo);      // espaços removidos
        Assert.Null(vaga.Experiencia);             // campo em branco vira nulo
        Assert.Equal("Salvadores do Bem", vaga.Empresa);
        Assert.Equal(0, vaga.TotalCandidatos);

        var publicas = await api.CreateClient().GetFromJsonAsync("/api/vagas", Json.ListVagaResumo, CT);
        Assert.Contains(publicas!, v => v.Id == vaga.Id);
        var daEmpresa = await cliente.GetFromJsonAsync("/api/me/vagas", Json.ListVagaResumo, CT);
        Assert.Equal(3, daEmpresa!.Count);
    }

    [Theory]
    [InlineData("", "Descrição", "Informe o título da vaga.")]
    [InlineData("Tosador", "  ", "Descreva a vaga.")]
    public async Task Formulario_invalido_e_recusado_com_mensagem(string titulo, string descricao, string mensagem)
    {
        var cliente = await api.ClienteLogadoAsync(Empresa);

        var resposta = await Publicar(cliente, Tosador with { Titulo = titulo, Descricao = descricao });
        var problema = await resposta.Content.ReadFromJsonAsync(Json.ProblemaApi, CT);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(mensagem, problema!.Detail);
    }

    [Fact]
    public async Task Titulo_maior_que_a_coluna_e_recusado_antes_de_chegar_ao_banco()
    {
        var cliente = await api.ClienteLogadoAsync(Empresa);

        var resposta = await Publicar(cliente, Tosador with { Titulo = new string('a', 101) });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Empresa_edita_a_propria_vaga()
    {
        var cliente = await api.ClienteLogadoAsync(Empresa);

        var resposta = await cliente.PutAsJsonAsync("/api/me/vagas/2", Tosador, Json.VagaFormulario, CT);
        var vaga = await resposta.Content.ReadFromJsonAsync(Json.VagaDetalhe, CT);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(2, vaga!.Id);
        Assert.Equal("Tosador", vaga.Titulo);
        Assert.Equal("Banho e tosa em cães e gatos.", vaga.Descricao);
    }

    [Fact]
    public async Task Empresa_nao_edita_nem_exclui_vaga_de_outra_empresa()
    {
        var cliente = await api.ClienteLogadoAsync(Empresa);
        var antes = await api.CreateClient().GetFromJsonAsync("/api/vagas/3", Json.VagaDetalhe, CT);

        var edicao = await cliente.PutAsJsonAsync("/api/me/vagas/3", Tosador, Json.VagaFormulario, CT);
        var exclusao = await cliente.DeleteAsync("/api/me/vagas/3", CT);

        Assert.Equal(HttpStatusCode.NotFound, edicao.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, exclusao.StatusCode);
        Assert.Equal(antes, await api.CreateClient().GetFromJsonAsync("/api/vagas/3", Json.VagaDetalhe, CT));
    }

    [Fact]
    public async Task Empresa_exclui_a_propria_vaga_com_as_candidaturas()
    {
        var empresa = await api.ClienteLogadoAsync(Empresa);
        var profissional = await api.ClienteLogadoAsync(Profissional);

        Assert.Equal(HttpStatusCode.NoContent, (await empresa.DeleteAsync("/api/me/vagas/1", CT)).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await api.CreateClient().GetAsync("/api/vagas/1", CT)).StatusCode);
        var candidaturas = await profissional.GetFromJsonAsync("/api/me/candidaturas", Json.ListCandidaturaResumo, CT);
        Assert.DoesNotContain(candidaturas!, c => c.IdVaga == 1);
        Assert.Equal(HttpStatusCode.NotFound, (await empresa.DeleteAsync("/api/me/vagas/1", CT)).StatusCode);
    }

    [Fact]
    public async Task Somente_empresas_publicam_vagas()
    {
        var profissional = await api.ClienteLogadoAsync(Profissional);
        var admin = await api.ClienteLogadoAsync(Admin);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Publicar(api.CreateClient(), Tosador)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Publicar(profissional, Tosador)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Publicar(admin, Tosador)).StatusCode);
    }

    [Theory]
    [InlineData(1, HttpStatusCode.OK)]        // candidato à vaga 1, da empresa
    [InlineData(3, HttpStatusCode.NotFound)]  // não se candidatou a nada
    [InlineData(4, HttpStatusCode.NotFound)]  // candidato só de outra empresa
    public async Task Empresa_ve_curriculo_apenas_dos_proprios_candidatos(int idProfissional, HttpStatusCode esperado)
    {
        var cliente = await api.ClienteLogadoAsync(Empresa);

        var resposta = await cliente.GetAsync($"/api/profissionais/{idProfissional}/curriculo", CT);

        Assert.Equal(esperado, resposta.StatusCode);
    }

    [Fact]
    public async Task Administrador_ve_qualquer_curriculo_e_profissional_nao_ve_curriculos()
    {
        var admin = await api.ClienteLogadoAsync(Admin);
        var profissional = await api.ClienteLogadoAsync(Profissional);

        var perfil = await admin.GetFromJsonAsync("/api/profissionais/3/curriculo", Json.PerfilProfissional, CT);

        Assert.Equal("Jessica Ferreira", perfil!.Nome);
        Assert.Equal(HttpStatusCode.Forbidden, (await profissional.GetAsync("/api/profissionais/1/curriculo", CT)).StatusCode);
    }

    [Fact]
    public async Task Administrador_lista_profissionais_e_empresas()
    {
        var admin = await api.ClienteLogadoAsync(Admin);

        var profissionais = await admin.GetFromJsonAsync("/api/admin/profissionais", Json.ListProfissionalAdmin, CT);
        var empresas = await admin.GetFromJsonAsync("/api/admin/empresas", Json.ListEmpresaAdmin, CT);

        Assert.Equal(4, profissionais!.Count);
        Assert.Contains(profissionais, p => p is { Nome: "Jucelino Silva", Usuario: "jucelino", Cidade: "São Paulo" });
        Assert.Equal(4, empresas!.Count);
        var salvadores = Assert.Single(empresas, e => e.Nome == "Salvadores do Bem");
        Assert.Equal("Felipe", salvadores.Recrutador);
        Assert.Equal(2, salvadores.TotalVagas);
    }

    [Fact]
    public async Task Administrador_exclui_profissional_com_as_candidaturas()
    {
        var admin = await api.ClienteLogadoAsync(Admin);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync("/api/admin/profissionais/1", CT)).StatusCode);

        var vaga = await api.CreateClient().GetFromJsonAsync("/api/vagas/1", Json.VagaDetalhe, CT);
        Assert.Equal(1, vaga!.TotalCandidatos);
        var login = await api.CreateClient().PostAsJsonAsync("/api/auth/login",
            new LoginRequest(Profissional, "demo1234"), Json.LoginRequest, CT);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync("/api/admin/profissionais/1", CT)).StatusCode);
    }

    [Fact]
    public async Task Administrador_exclui_empresa_com_as_vagas()
    {
        var admin = await api.ClienteLogadoAsync(Admin);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync("/api/admin/empresas/1", CT)).StatusCode);

        var vagas = await api.CreateClient().GetFromJsonAsync("/api/vagas", Json.ListVagaResumo, CT);
        Assert.DoesNotContain(vagas!, v => v.Empresa == "Salvadores do Bem");
        Assert.Equal(2, vagas!.Count);
    }

    [Fact]
    public async Task Administrador_exclui_qualquer_vaga()
    {
        var admin = await api.ClienteLogadoAsync(Admin);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync("/api/admin/vagas/3", CT)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync("/api/admin/vagas/3", CT)).StatusCode);
    }

    [Theory]
    [InlineData(Empresa)]
    [InlineData(Profissional)]
    public async Task Rotas_de_administracao_sao_so_do_administrador(string email)
    {
        var cliente = await api.ClienteLogadoAsync(email);

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/admin/empresas", CT)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.DeleteAsync("/api/admin/vagas/3", CT)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync("/api/admin/profissionais", CT)).StatusCode);
    }
}
