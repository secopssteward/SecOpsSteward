using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;

namespace SOSPackaging.Commands
{
    public class PackageSignOptions : CommandSettings
    {
        [CommandArgument(0, "<Path>")] public string Path { get; set; }

        [CommandArgument(1, "[PrivateKey]")] public string PrivateKey { get; set; }

        [CommandArgument(2, "[Signer]")] public string Signer { get; set; }

        [CommandOption("--config")] public string Configuration { get; set; }
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
}
