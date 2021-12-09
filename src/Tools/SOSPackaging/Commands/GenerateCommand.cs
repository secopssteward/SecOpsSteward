using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace SOSPackaging.Commands
{
    public class GenerateCommand : Command<EmptyCommandSettings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] EmptyCommandSettings settings)
        {
            SharedActions.ApplyConfiguration(settings);

            using (var rsa = RSA.Create())
            {
                rsa.KeySize = 4096;
                var parameters = rsa.ExportParameters(false);

                Console.WriteLine("Public Key:");
                AnsiConsole.MarkupLine($"[green]{Convert.ToBase64String(Encoding.ASCII.GetBytes(rsa.ToXmlString(false)))}[/green]");
                Console.WriteLine();
                Console.WriteLine("Private Key:");
                AnsiConsole.MarkupLine($"[red]{Convert.ToBase64String(Encoding.ASCII.GetBytes(rsa.ToXmlString(true)))}[/red]");
                Console.WriteLine();
            }

            return 0;
        }
    }
}
