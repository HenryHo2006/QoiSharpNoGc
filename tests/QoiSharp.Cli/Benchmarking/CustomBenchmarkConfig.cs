using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace QoiSharp.Cli.Benchmarking;

public class CustomBenchmarkConfig : ManualConfig
{
    public CustomBenchmarkConfig()
    {
        AddJob(Job.Default.WithRuntime(CoreRuntime.Core80)
            .WithPlatform(Platform.X64)
            );
        AddDiagnoser(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default);
    }
}
