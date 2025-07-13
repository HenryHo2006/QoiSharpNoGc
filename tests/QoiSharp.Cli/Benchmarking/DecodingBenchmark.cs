using BenchmarkDotNet.Attributes;
using QoiSharp.Codec;

namespace QoiSharp.Cli.Benchmarking;

[Config(typeof(CustomBenchmarkConfig))]
public class DecodingBenchmark
{    
/*
Some test results:
AMD Ryzen 5 5600G

Image size: 1024*1024
| Method                | dataType           | Mean     | Error     | StdDev    | Gen0     | Gen1     | Gen2     | Allocated  |
|---------------------- |------------------- |---------:|----------:|----------:|---------:|---------:|---------:|-----------:|
| 'QOI Decoding'        | Diff               | 1.390 ms | 0.0276 ms | 0.0445 ms | 330.0781 | 330.0781 | 330.0781 | 3072.58 KB |
| 'QOI Decoding Stream' | Diff               | 1.714 ms | 0.0311 ms | 0.0484 ms |        - |        - |        - |    8.07 KB |
| 'QOI Decoding'        | Index              | 2.705 ms | 0.0314 ms | 0.0262 ms | 152.3438 | 152.3438 | 152.3438 | 3072.47 KB |
| 'QOI Decoding Stream' | Index              | 2.779 ms | 0.0552 ms | 0.0875 ms |        - |        - |        - |    8.07 KB |
| 'QOI Decoding'        | Luma               | 2.702 ms | 0.0520 ms | 0.0657 ms | 152.3438 | 152.3438 | 152.3438 | 3072.47 KB |
| 'QOI Decoding Stream' | Luma               | 2.963 ms | 0.0334 ms | 0.0279 ms |        - |        - |        - |    8.07 KB |
| 'QOI Decoding'        | Rgb                | 3.691 ms | 0.0508 ms | 0.0425 ms | 238.2813 | 238.2813 | 238.2813 | 3072.52 KB |
| 'QOI Decoding Stream' | Rgb                | 5.045 ms | 0.0999 ms | 0.0934 ms |        - |        - |        - |    8.07 KB |
| 'QOI Decoding'        | Rgba               | 4.237 ms | 0.0618 ms | 0.0548 ms | 273.4375 | 273.4375 | 273.4375 | 4096.54 KB |
| 'QOI Decoding Stream' | Rgba               | 5.304 ms | 0.0602 ms | 0.0563 ms |        - |        - |        - |    8.07 KB |
| 'QOI Decoding'        | RgbaAlphaRandomRun | 5.031 ms | 0.0481 ms | 0.0426 ms | 273.4375 | 273.4375 | 273.4375 | 4096.54 KB |
| 'QOI Decoding Stream' | RgbaAlphaRandomRun | 6.313 ms | 0.0388 ms | 0.0363 ms |        - |        - |        - |    8.07 KB |
| 'QOI Decoding'        | Run                | 1.398 ms | 0.0270 ms | 0.0253 ms | 330.0781 | 330.0781 | 330.0781 | 3072.58 KB |
| 'QOI Decoding Stream' | Run                | 1.713 ms | 0.0123 ms | 0.0109 ms |        - |        - |        - |    8.07 KB |

*/

    [Params(nameof(QoiCodec.Rgb), "RgbaAlphaRandomRun", nameof(QoiCodec.Rgba), nameof(QoiCodec.Luma), nameof(QoiCodec.Diff), nameof(QoiCodec.Run), nameof(QoiCodec.Index))]
    // [Params(nameof(QoiCodec.Rgb), "RgbaAlphaRandomRun", nameof(QoiCodec.Rgba), nameof(QoiCodec.Diff))]
    // [Params(nameof(QoiCodec.Run))]
    public string dataType = "";

    [GlobalSetup]
    public void Setup()
    {
        var height = 1024;
        var width = 1024;
        var channel = Channels.Rgb;
        byte[] data = CustomBenchmarkConfig.CreateTestData(height, width, ref channel, dataType);
        _qoiData = QoiEncoder.Encode(new QoiImage(data, width, height, channel));
        QoiDataStream = new MemoryStream(_qoiData);
        _streamCopyTarget = new byte[height * width * 5 + 30];
    }

    [RunOncePerIteration]
    public void TestSetup()
    {
        QoiDataStream = new MemoryStream(_qoiData);
    }

    public QoiImage Image = new([], 0, 0, Channels.Rgb);
    public MemoryStream QoiDataStream = new MemoryStream();
    private byte[] _streamCopyTarget = [];
    private byte[] _qoiData = [];

    [Benchmark(Description = "QOI Decoding")]
    public QoiImage QoiDecoding()
    {
        return QoiDecoder.Decode(_qoiData);
    }
    
    [Benchmark(Description = "QOI Decoding Stream")]
    public Span<byte> QoiDecoding_Span()
    {
        QoiDataStream.Position = 0;
        var stream = new QoiDecoderStream(QoiDataStream);
        var readBytes = stream.Read(_streamCopyTarget, 0, _streamCopyTarget.Length);
        stream.Flush();
        return _streamCopyTarget.AsSpan(0, readBytes);
    }
}
