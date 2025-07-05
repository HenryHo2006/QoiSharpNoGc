using BenchmarkDotNet.Running;
using QoiSharp.Cli.Benchmarking;
using Spectre.Console.Cli;

namespace QoiSharp.Cli.Commands;

public class EncodeImageToQoiCommand : AsyncCommand<EncodeImageToQoiCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        _ = BenchmarkRunner.Run<EncodingBenchmark>();

        return 0;
    }
}