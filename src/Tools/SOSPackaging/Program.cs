using Spectre.Console.Cli;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SOSPackaging
{
    class Program
    {
        static int Main(string[] args)
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
        [CommandArgument(0, "<Path>")]
        public string Path { get; set; }

        [CommandArgument(1, "[PrivateKey]")]
        public string PrivateKey { get; set; }

        [CommandArgument(2, "[Signer]")]
        public string Signer { get; set; }

        [CommandArgument(3, "[SASKey]")]
        public string SasKey { get; set; }

        [CommandOption("--config")]
        public string Configuration { get; set; }
    }

    public class PackageSignOptions : CommandSettings
    {
        [CommandArgument(0, "<Path>")]
        public string Path { get; set; }

        [CommandArgument(1, "[PrivateKey]")]
        public string PrivateKey { get; set; }

        [CommandArgument(2, "[Signer]")]
        public string Signer { get; set; }

        [CommandOption("--config")]
        public string Configuration { get; set; }
    }

    public class PackageVerifyOptions : CommandSettings
    {
        [CommandArgument(0, "<Path>")]
        public string Path { get; set; }

        [CommandArgument(1, "[PublicKey]")]
        public string PublicKey { get; set; }

        [CommandOption("--config")]
        public string Configuration { get; set; }
    }

    public class PackagePostOptions : CommandSettings
    {
        [CommandArgument(0, "<Path>")]
        public string Path { get; set; }

        [CommandArgument(1, "[SasKey]")]
        public string SasKey { get; set; }

        [CommandOption("--config")]
        public string Configuration { get; set; }
    }

    // --- COMMANDS ---
    public class NewCommand : Command<NewPackageOptions>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] NewPackageOptions settings)
        {
            SharedActions.ApplyConfiguration(settings);

            Console.WriteLine("Building from " + settings.Path);
            var pkg = SharedActions.Create(settings.Path);

            Console.WriteLine("Signing package with public repository key");
            SharedActions.Sign(pkg, settings.PrivateKey, settings.Signer);

            if (!string.IsNullOrEmpty(settings.SasKey))
            {
                Console.WriteLine("Uploading package to storage");
                SharedActions.Post(pkg, settings.SasKey);
            }
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
                Console.WriteLine(Convert.ToBase64String(Encoding.ASCII.GetBytes(rsa.ToXmlString(false))));
                Console.WriteLine();
                Console.WriteLine("Private Key:");
                Console.WriteLine(Convert.ToBase64String(Encoding.ASCII.GetBytes(rsa.ToXmlString(true))));
                Console.WriteLine();
            }
            return 0;
        }
    }
}
