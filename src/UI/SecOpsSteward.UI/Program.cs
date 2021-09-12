using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace SecOpsSteward.UI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // NOTE THIS BLOWS AWAY SQLITE EVERY TIME
            if (File.Exists("sos.db")) File.Delete("sos.db");

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
