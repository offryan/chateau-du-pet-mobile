namespace ChateauDuPet.App.Core.Servicos;

/// <summary>
/// Avisa as telas que já carregaram dados quando a gestão muda vagas ou o cadastro
/// (publicar, editar ou excluir). Cada tela marca os dados como desatualizados e
/// recarrega na próxima vez que aparecer, em vez de recarregar tudo a cada visita.
/// </summary>
public class AlteracoesDeDados
{
    public event EventHandler? VagasAlteradas;

    public void NotificarVagasAlteradas() => VagasAlteradas?.Invoke(this, EventArgs.Empty);
}
