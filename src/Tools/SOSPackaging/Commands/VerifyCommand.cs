using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;

namespace SOSPackaging.Commands
{
    public class PackageVerifyOptions : CommandSettings
    {
        [CommandArgument(0, "<Path>")] public string Path { get; set; }

        [CommandArgument(1, "[PublicKey]")] public string PublicKey { get; set; }

        [CommandOption("--config")] public string Configuration { get; set; }
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
}
