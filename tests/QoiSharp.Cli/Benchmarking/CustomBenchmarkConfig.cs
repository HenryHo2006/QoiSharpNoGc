using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using QoiSharp.Codec;

namespace QoiSharp.Cli.Benchmarking;

public class CustomBenchmarkConfig : ManualConfig
{
    public CustomBenchmarkConfig()
    {
        AddJob(Job.Default.WithRuntime(CoreRuntime.Core80)
            .WithPlatform(Platform.X64)
            .WithJit(Jit.RyuJit)
            .WithWarmupCount(1)
            );
        AddDiagnoser(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default);
    }

    public static byte[] CreateTestData(int height, int width, ref Channels channel, string dataType)
    {
        var data = new byte[height * width * 3];
        if (dataType.StartsWith(nameof(QoiCodec.Rgba)))
        {
            channel = Channels.RgbWithAlpha;
            data = new byte[height * width * 4];
            var random = new Random();
            random.NextBytes(data);
            if (!dataType.Contains("AlphaRandom"))
            {
                for (int i = 3; i < data.Length; i += 4)
                {
                    data[i] = 150; // Set alpha channel to 150
                }
            }
        }
        if (dataType == nameof(QoiCodec.Rgb))
        {
            var random = new Random();
            random.NextBytes(data);
        }
        else
        {
            byte[] indexData = [12, 34, 56, 153, 232, 12, 76, 87, 87];
            byte[] lumaData = [12, 34, 56, 20, 40, 60, 10, 30, 50];
            var index = 0;
            for (var i = 0; i < data.Length; i += 3)
            {
                if (dataType.Contains(nameof(QoiCodec.Run)))
                {
                    data[i] = 12;
                    data[i + 1] = 34;
                    data[i + 2] = 56;
                }
                else if (dataType.Contains(nameof(QoiCodec.Index)))
                {
                    data[i] = indexData[index % 9];
                    data[i + 1] = indexData[index % 9 + 1];
                    data[i + 2] = indexData[index % 9 + 2];
                }
                else if (dataType.Contains(nameof(QoiCodec.Luma)))
                {
                    data[i] = lumaData[index % 9];
                    data[i + 1] = lumaData[index % 9 + 1];
                    data[i + 2] = lumaData[index % 9 + 2];
                }
                index += 3;
                if (dataType.StartsWith(nameof(QoiCodec.Rgba)))
                { i++; }
            }
        }

        return data;
    }
}
