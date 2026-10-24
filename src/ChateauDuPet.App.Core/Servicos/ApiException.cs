namespace ChateauDuPet.App.Core.Servicos;

/// <summary>Falha ao falar com a API, com uma mensagem pronta para mostrar ao usuário.</summary>
public class ApiException(string mensagem, int? status = null, Exception? causa = null) : Exception(mensagem, causa)
{
    /// <summary>Código HTTP da resposta, ou null quando nem houve resposta (sem internet, timeout).</summary>
    public int? Status { get; } = status;

    public bool SemConexao => Status is null;
}

/// <summary>O token salvo venceu ou foi recusado; a sessão já foi encerrada.</summary>
public class SessaoExpiradaException() : ApiException("Sua sessão expirou. Entre novamente.", 401);
