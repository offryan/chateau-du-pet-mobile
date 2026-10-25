// Iniciador do app no Windows: fica na raiz do zip, ao lado da pasta "app" com os
// arquivos do .NET MAUI, para que o usuário encontre de cara o que abrir.
//
// É compilado no CI pelo csc do .NET Framework 4.8, que já vem no Windows 10 e 11,
// por isso usa só C# 5 e ocupa poucos KB. Veja .github/workflows/ci.yml.
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

static class Iniciar
{
    [STAThread]
    static void Main()
    {
        string pasta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app");
        string exe = Path.Combine(pasta, "ChateauDuPet.App.exe");

        if (!File.Exists(exe))
        {
            Avisar("Não encontrei a pasta \"app\" ao lado deste arquivo.\n\n" +
                   "Extraia o zip inteiro (botão direito → Extrair tudo) e abra o Chateau Du Pet de dentro da pasta extraída.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = pasta, UseShellExecute = false });
        }
        catch (Win32Exception erro)
        {
            Avisar("Não foi possível abrir o app: " + erro.Message);
        }
    }

    static void Avisar(string mensagem)
    {
        MessageBox.Show(mensagem, "Chateau Du Pet", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
