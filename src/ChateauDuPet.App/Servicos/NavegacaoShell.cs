using ChateauDuPet.App.Core.Servicos;
using ChateauDuPet.App.Views;
using ChateauDuPet.Contracts;

namespace ChateauDuPet.App.Servicos;

public static class Rotas
{
    public const string Vaga = "vaga";
    public const string Candidatos = "candidatos";
    public const string Login = "login";
    public const string FormularioDeVaga = "vaga-formulario";
    public const string Curriculo = "curriculo";
    public const string Administracao = "administracao";
}

public class NavegacaoShell : INavegacao
{
    public Task AbrirVagaAsync(int idVaga) =>
        Shell.Current.GoToAsync(Rotas.Vaga, new ShellNavigationQueryParameters { ["id"] = idVaga });

    public Task AbrirCandidatosAsync(VagaResumo vaga) =>
        Shell.Current.GoToAsync(Rotas.Candidatos, new ShellNavigationQueryParameters { ["vaga"] = vaga });

    public Task AbrirLoginAsync() =>
        Shell.Current.CurrentPage is LoginPage ? Task.CompletedTask : Shell.Current.GoToAsync(Rotas.Login);

    public Task VoltarAsync() => Shell.Current.GoToAsync("..");

    /// <summary>Sem id o formulário publica uma vaga nova; com id, edita.</summary>
    public Task AbrirFormularioDeVagaAsync(int? idVaga) => idVaga is { } id
        ? Shell.Current.GoToAsync(Rotas.FormularioDeVaga, new ShellNavigationQueryParameters { ["id"] = id })
        : Shell.Current.GoToAsync(Rotas.FormularioDeVaga);

    public Task AbrirCurriculoAsync(int idProfissional) =>
        Shell.Current.GoToAsync(Rotas.Curriculo, new ShellNavigationQueryParameters { ["id"] = idProfissional });

    public Task AbrirAdministracaoAsync() => Shell.Current.GoToAsync(Rotas.Administracao);
}
