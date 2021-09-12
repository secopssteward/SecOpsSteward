using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SecOpsSteward.UI
{
    public class AnsiConsoleOptions : ConsoleFormatterOptions
    {
        public string CustomPrefix { get; set; }
    }

    public static class AnsiConsoleLoggerExtensions
    {
        public static ILoggingBuilder AddAnsiConsoleFormatter(
            this ILoggingBuilder builder,
            Action<AnsiConsoleOptions> configure) =>
            builder.AddConsole(options => options.FormatterName = "ansiConsole")
                .AddConsoleFormatter<AnsiConsoleFormatter, AnsiConsoleOptions>(configure);

        public static ILoggingBuilder AddAnsiConsoleFormatter(
            this ILoggingBuilder builder) =>
            builder.AddConsole(options => options.FormatterName = "ansiConsole")
                .AddConsoleFormatter<AnsiConsoleFormatter, AnsiConsoleOptions>();
    }

    public sealed class AnsiConsoleFormatter : ConsoleFormatter, IDisposable
    {
        private readonly IDisposable _optionsReloadToken;
        private AnsiConsoleOptions _formatterOptions;

        private static List<Type> Types { get; }
        static AnsiConsoleFormatter()
        {
            Types = AppDomain.CurrentDomain.GetAssemblies()
                .Where(asm => asm.FullName.StartsWith("SecOpsSteward."))
                .SelectMany(a => a.GetTypes())
                .ToList();
            Types.AddRange(Assembly.GetAssembly(typeof(Program)).GetTypes());
            Types = Types.Distinct().ToList();
        }

        public AnsiConsoleFormatter(IOptionsMonitor<AnsiConsoleOptions> options)
            // Case insensitive
            : base("ansiConsole") =>
            (_optionsReloadToken, _formatterOptions) =
                (options.OnChange(ReloadLoggerOptions), options.CurrentValue);

        private void ReloadLoggerOptions(AnsiConsoleOptions options) =>
            _formatterOptions = options;

        public override void Write<TState>(
            in LogEntry<TState> logEntry,
            IExternalScopeProvider scopeProvider,
            TextWriter textWriter)
        {
            if (logEntry.Exception != null)
            {
                AnsiConsole.WriteException(logEntry.Exception);
                return;
            }

            string message = string.Empty;
            switch (logEntry.LogLevel)
            {
                case LogLevel.Trace:
                    message += "[black on silver]{TRACE}[/] ";
                    break;
                case LogLevel.Debug:
                    message += "[black on grey]{DEBUG}[/] ";
                    break;
                case LogLevel.Information:
                    message += "[black on aqua]{INFO }[/] ";
                    break;
                case LogLevel.Warning:
                    message += "[black on yellow]{WARN }[/] ";
                    break;
                case LogLevel.Error:
                    message += "[black on red]{ERROR}[/] ";
                    break;
                case LogLevel.Critical:
                    message += "[black on darkred]{CRIT }[/] ";
                    break;
                case LogLevel.None:
                    message += "[black on white]{NONE }[/] ";
                    break;
            }

            var categoryName = logEntry.Category;
            var cat = Types.FirstOrDefault(t => t.FullName == categoryName);
            if (cat == null)
                message += $"([underline]{logEntry.Category}[/])  ";
            else
            {
                var idx = Math.Abs(cat.Name.GetHashCode()) % COLORS.Length;
                message += $"([underline][{COLORS[idx].ToMarkup()}]{cat.Name}[/][/])  ";
            }

            message += Markup.Escape(logEntry.Formatter(logEntry.State, logEntry.Exception));

            AnsiConsole.MarkupLine(message);
        }

        private static readonly Spectre.Console.Color[] COLORS = new Spectre.Console.Color[]
        {
            Spectre.Console.Color.Aqua,
            Spectre.Console.Color.Blue,
            Spectre.Console.Color.Red,
            Spectre.Console.Color.PaleVioletRed1,
            Spectre.Console.Color.Purple,
            Spectre.Console.Color.LightSteelBlue,
            Spectre.Console.Color.Yellow,
            Spectre.Console.Color.Green,
            Spectre.Console.Color.GreenYellow,
            Spectre.Console.Color.Orange1,
            Spectre.Console.Color.Violet
        };

        public void Dispose() => _optionsReloadToken?.Dispose();
    }
}
