using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;

namespace SOSPackaging.Commands
{
    public class PackagePostOptions : CommandSettings
    {
        [CommandArgument(0, "<Path>")] public string Path { get; set; }

        [CommandArgument(1, "[SasKey]")] public string SasKey { get; set; }

        [CommandOption("--config")] public string Configuration { get; set; }
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
}
