using System.Drawing;
using BenchmarkDotNet.Attributes;
using QoiSharp.Codec;

namespace QoiSharp.Cli.Benchmarking;
/*
Some test results:
AMD Ryzen 5 5600G

Image size: 1024*1024
| Method                   | dataType           | Mean     | Error     | StdDev    | Gen0     | Gen1     | Gen2     | Allocated  |
|------------------------- |------------------- |---------:|----------:|----------:|---------:|---------:|---------:|-----------:|
| 'Fast encoding code'     | Index              | 3.111 ms | 0.0376 ms | 0.0333 ms | 152.3438 | 152.3438 | 152.3438 |  5120.5 KB |
| 'Streamed encoding code' | Index              | 2.411 ms | 0.0269 ms | 0.0251 ms |        - |        - |        - |    5.54 KB |
| 'Fast encoding code'     | Luma               | 3.161 ms | 0.0629 ms | 0.0796 ms | 152.3438 | 152.3438 | 152.3438 |  5120.5 KB |
| 'Streamed encoding code' | Luma               | 2.458 ms | 0.0210 ms | 0.0164 ms |        - |        - |        - |    5.54 KB |
| 'Fast encoding code'     | RgbaAlphaRandomRun | 8.216 ms | 0.1590 ms | 0.1410 ms | 390.6250 | 390.6250 | 390.6250 | 9219.59 KB |
| 'Streamed encoding code' | RgbaAlphaRandomRun | 5.544 ms | 0.1086 ms | 0.1016 ms |        - |        - |        - |    5.68 KB |
| 'Fast encoding code'     | RgbaIndex          | 3.422 ms | 0.0627 ms | 0.0878 ms | 179.6875 | 179.6875 | 179.6875 | 6144.49 KB |
| 'Streamed encoding code' | RgbaIndex          | 2.447 ms | 0.0465 ms | 0.0667 ms |        - |        - |        - |    5.68 KB |
| 'Fast encoding code'     | RgbaLuma           | 3.274 ms | 0.0637 ms | 0.0758 ms | 148.4375 | 148.4375 | 148.4375 | 6144.47 KB |
| 'Streamed encoding code' | RgbaLuma           | 2.485 ms | 0.0378 ms | 0.0353 ms |        - |        - |        - |    5.68 KB |
| 'Fast encoding code'     | RgbaRun            | 1.307 ms | 0.0255 ms | 0.0340 ms | 181.6406 | 179.6875 | 179.6875 |  5137.7 KB |
| 'Streamed encoding code' | RgbaRun            | 1.005 ms | 0.0176 ms | 0.0264 ms |        - |        - |        - |    5.68 KB |
| 'Fast encoding code'     | Run                | 1.312 ms | 0.0261 ms | 0.0311 ms | 154.2969 | 152.3438 | 152.3438 | 4113.58 KB |
| 'Streamed encoding code' | Run                | 1.270 ms | 0.0242 ms | 0.0288 ms |        - |        - |        - |    5.54 KB |

*/

[Config(typeof(CustomBenchmarkConfig))]
public class EncodingBenchmark
{
    [Params(nameof(QoiCodec.Run), "RgbaRun", "RgbaAlphaRandomRun", nameof(QoiCodec.Index), "RgbaIndex", nameof(QoiCodec.Luma), "RgbaLuma")]
    // [Params(nameof(QoiCodec.Luma), "RgbaLuma", "RgbaAlphaRandomRun", nameof(QoiCodec.Rgb))]
    public string dataType = "";

    [GlobalSetup]
    public void Setup()
    {
        var height = 1024;
        var width = 1024;
        var channel = Channels.Rgb;
        byte[] data = CustomBenchmarkConfig.CreateTestData(height, width, ref channel, dataType);
        Image = new QoiImage(data, width, height, channel);
        ImageDataStream = new MemoryStream(data);
        _streamCopyTarget = new byte[height * width * 5 + 30];
    }

    [RunOncePerIteration]
    public void TestSettup()
    {
        ImageDataStream = new MemoryStream(Image.Data);
    }

    public QoiImage Image = new([], 0, 0, Channels.Rgb);
    public MemoryStream ImageDataStream = new MemoryStream();

    private byte[] _streamCopyTarget = [];

    [Benchmark(Description = "Fast encoding code")]
    public byte[] FastEncoding()
    {
        return QoiEncoder.Encode(Image);
    }

    [Benchmark(Description = "Streamed encoding code")]
    public Span<byte> StreamEncoding()
    {
        ImageDataStream.Position = 0;
        var stream = new QoiEncoderStream(ImageDataStream, new Size(Image.Width, Image.Height), Image.Channels);
        var readBytes = stream.Read(_streamCopyTarget, 0, _streamCopyTarget.Length);
        stream.Flush();
        return _streamCopyTarget.AsSpan(0, readBytes);
    }
}
