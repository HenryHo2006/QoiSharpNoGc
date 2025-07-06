using BenchmarkDotNet.Running;
using QoiSharp.Cli.Benchmarking;

namespace QoiSharp.Cli
{
    public class Program
    {
        public static int Main(string[] args)
        {
            if (args.FirstOrDefault() != "encode-to-qoi")
            {
                _ = BenchmarkRunner.Run<DecodingBenchmark>();
            }if (args.FirstOrDefault() != "benchmark-decoding")
            {
            _ = BenchmarkRunner.Run<EncodingBenchmark>();
            }
            return 0;
        }
    }
}
