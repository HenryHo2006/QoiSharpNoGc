using BenchmarkDotNet.Running;
using QoiSharp.Cli.Benchmarking;
using Spectre.Console;
using Spectre.Console.Cli;

namespace QoiSharp.Cli.Commands.Benchmarks
{
    public sealed class DecodingBenchmarkCommand : Command<DecodingBenchmarkCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
        }


        public override int Execute(CommandContext context, Settings settings)
        {
            try
            {
                ExecuteInternal(context, settings);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(
                    ex,
                    ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes |
                    ExceptionFormats.ShortenMethods | ExceptionFormats.ShowLinks);
                return 1;
            }

            return 0;
        }

        private int ExecuteInternal(CommandContext context, Settings settings)
        {
            _ = BenchmarkRunner.Run<DecodingBenchmark>();

            return 0;
        }
    }
}
