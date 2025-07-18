using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Parsers.FrameworkEventSource;
using QoiSharp.Cli.Benchmarking;

namespace QoiSharp.Cli
{
    public class Program
    {
        public static int Main(string[] args)
        {
            if (args.FirstOrDefault() == "images")
            {
                Console.WriteLine(System.AppDomain.CurrentDomain.BaseDirectory);
                RealFileBenchmark.RunRealImagesBenchmark(Path.Combine(
                    System.AppDomain.CurrentDomain.BaseDirectory,
                    "../../../Images"
                ));
                return 0;
            }
            if (args.FirstOrDefault() != "encode-to-qoi")
            {
                _ = BenchmarkRunner.Run<DecodingBenchmark>();
            }
            if (args.FirstOrDefault() != "benchmark-decoding")
            {
                _ = BenchmarkRunner.Run<EncodingBenchmark>();
            }
            return 0;
        }
    }
}
