using SOSPackaging.Commands;
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
}