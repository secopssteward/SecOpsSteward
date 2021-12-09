using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace SOSPackaging.Commands
{
    public class NewPackageOptions : CommandSettings
    {
        [CommandArgument(0, "<Path>")] public string Path { get; set; }

        [CommandArgument(1, "[PrivateKey]")] public string PrivateKey { get; set; }

        [CommandArgument(2, "[Signer]")] public string Signer { get; set; }

        [CommandArgument(3, "[SASKey]")] public string SasKey { get; set; }

        [CommandOption("--config")] public string Configuration { get; set; }
    }

    public class NewCommand : Command<NewPackageOptions>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] NewPackageOptions settings)
        {
            SharedActions.ApplyConfiguration(settings);

            AnsiConsole.Markup($"Building package from [blue]{settings.Path}[/] ... ");
            string pkg;
            try
            {
                pkg = SharedActions.Create(settings.Path);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Not a valid package![/]");
                return -1;
            }

            AnsiConsole.Markup("Signing package with public repository key ... ");
            SharedActions.Sign(pkg, settings.PrivateKey, settings.Signer);
            AnsiConsole.MarkupLine("[green]OK![/]");

            if (!string.IsNullOrEmpty(settings.SasKey))
            {
                AnsiConsole.Markup($"Uploading package to public storage ... ");
                SharedActions.Post(pkg, settings.SasKey);
                AnsiConsole.MarkupLine("[green]OK![/]");
            }

            Console.WriteLine();

            File.Delete(pkg);
            return 0;
        }
    }
}
