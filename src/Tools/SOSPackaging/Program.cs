using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SOSPackaging
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var app = new CommandApp();

            app.Configure(config =>
            {
                config.AddCommand<SignCommand>("sign");
                config.AddCommand<VerifyCommand>("verify");

                config.AddCommand<NewCommand>("new");
                config.AddCommand<PostCommand>("post");
                config.AddCommand<GenerateCommand>("generate");
            });
            return app.Run(args);
        }
    }

    // --- OPTIONS ---
    public class NewPackageOptions : CommandSettings
    {
        [CommandArgument(0, "<Path>")] public string Path { get; set; }

        [CommandArgument(1, "[PrivateKey]")] public string PrivateKey { get; set; }

        [CommandArgument(2, "[Signer]")] public string Signer { get; set; }

        [CommandArgument(3, "[SASKey]")] public string SasKey { get; set; }

        [CommandOption("--config")] public string Configuration { get; set; }
    }

    public class PackageSignOptions : CommandSettings
    {
        [CommandArgument(0, "<Path>")] public string Path { get; set; }

        [CommandArgument(1, "[PrivateKey]")] public string PrivateKey { get; set; }

        [CommandArgument(2, "[Signer]")] public string Signer { get; set; }

        [CommandOption("--config")] public string Configuration { get; set; }
    }

    public class PackageVerifyOptions : CommandSettings
    {
        [CommandArgument(0, "<Path>")] public string Path { get; set; }

        [CommandArgument(1, "[PublicKey]")] public string PublicKey { get; set; }

        [CommandOption("--config")] public string Configuration { get; set; }
    }

    public class PackagePostOptions : CommandSettings
    {
        [CommandArgument(0, "<Path>")] public string Path { get; set; }

        [CommandArgument(1, "[SasKey]")] public string SasKey { get; set; }

        [CommandOption("--config")] public string Configuration { get; set; }
    }

    // --- COMMANDS ---
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

    public class SignCommand : Command<PackageSignOptions>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] PackageSignOptions settings)
        {
            SharedActions.ApplyConfiguration(settings);

            SharedActions.Sign(settings.Path, settings.PrivateKey, settings.Signer);
            return 0;
        }
    }

    public class VerifyCommand : Command<PackageVerifyOptions>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] PackageVerifyOptions settings)
        {
            SharedActions.ApplyConfiguration(settings);

            SharedActions.Verify(settings.Path, settings.PublicKey);
            return 0;
        }
    }

    public class PostCommand : Command<PackageVerifyOptions>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] PackageVerifyOptions settings)
        {
            SharedActions.ApplyConfiguration(settings);

            SharedActions.Post(settings.Path, settings.PublicKey);
            return 0;
        }
    }

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