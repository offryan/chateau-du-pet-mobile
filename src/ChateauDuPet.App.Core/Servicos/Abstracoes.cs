using ChateauDuPet.Contracts;

namespace ChateauDuPet.App.Core.Servicos;

// Pontos em que a lógica do app precisa da plataforma. O projeto MAUI implementa
// cada um; os testes usam versões falsas.

/// <summary>Navegação entre telas (no app, implementada com o Shell do MAUI).</summary>
public interface INavegacao
{
    Task AbrirVagaAsync(int idVaga);
    Task AbrirCandidatosAsync(VagaResumo vaga);
    Task AbrirLoginAsync();
    Task VoltarAsync();

    /// <summary>Formulário de vaga da empresa: sem id publica uma nova, com id edita.</summary>
    Task AbrirFormularioDeVagaAsync(int? idVaga);
    Task AbrirCurriculoAsync(int idProfissional);
    Task AbrirAdministracaoAsync();
}

/// <summary>Interações com o sistema: avisos rápidos, confirmações, compartilhamento e links.</summary>
public interface IPlataforma
{
    Task MostrarAvisoAsync(string mensagem);
    Task<bool> ConfirmarAsync(string titulo, string mensagem, string aceitar, string cancelar);
    Task CompartilharAsync(string titulo, string texto);
    Task AbrirLinkAsync(string url);
}

/// <summary>Armazenamento criptografado do aparelho (Keystore no Android, Keychain no iOS).</summary>
public interface IArmazenamentoSeguro
{
    Task<string?> LerAsync(string chave);
    Task GravarAsync(string chave, string valor);
    void Remover(string chave);
}

/// <summary>Chamado pelas páginas sempre que ficam visíveis.</summary>
public interface IAoAparecer
{
    Task AoAparecerAsync();
}
