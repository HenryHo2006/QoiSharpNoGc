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
| 'QOI Decoding'        | Diff               | 1.446 ms | 0.0288 ms | 0.0440 ms | 330.0781 | 330.0781 | 330.0781 | 3072.58 KB |
| 'QOI Decoding Stream' | Diff               | 1.823 ms | 0.0178 ms | 0.0149 ms |        - |        - |        - |    8.07 KB |
| 'QOI Decoding'        | Index              | 2.847 ms | 0.0448 ms | 0.0374 ms | 152.3438 | 152.3438 | 152.3438 | 3072.47 KB |
| 'QOI Decoding Stream' | Index              | 2.949 ms | 0.0145 ms | 0.0136 ms |        - |        - |        - |    8.07 KB |
| 'QOI Decoding'        | Luma               | 2.842 ms | 0.0275 ms | 0.0258 ms | 152.3438 | 152.3438 | 152.3438 | 3072.47 KB |
| 'QOI Decoding Stream' | Luma               | 3.000 ms | 0.0331 ms | 0.0309 ms |        - |        - |        - |    8.07 KB |
| 'QOI Decoding'        | Rgb                | 3.779 ms | 0.0637 ms | 0.0565 ms | 238.2813 | 238.2813 | 238.2813 | 3072.52 KB |
| 'QOI Decoding Stream' | Rgb                | 5.030 ms | 0.0526 ms | 0.0492 ms |        - |        - |        - |    8.07 KB |
| 'QOI Decoding'        | Rgba               | 4.145 ms | 0.0420 ms | 0.0393 ms | 273.4375 | 273.4375 | 273.4375 | 4096.54 KB |
| 'QOI Decoding Stream' | Rgba               | 5.058 ms | 0.0303 ms | 0.0253 ms |        - |        - |        - |    8.07 KB |
| 'QOI Decoding'        | RgbaAlphaRandomRun | 5.074 ms | 0.0158 ms | 0.0140 ms | 273.4375 | 273.4375 | 273.4375 | 4096.54 KB |
| 'QOI Decoding Stream' | RgbaAlphaRandomRun | 6.341 ms | 0.0450 ms | 0.0421 ms |        - |        - |        - |    8.07 KB |
| 'QOI Decoding'        | Run                | 1.438 ms | 0.0284 ms | 0.0316 ms | 330.0781 | 330.0781 | 330.0781 | 3072.58 KB |
| 'QOI Decoding Stream' | Run                | 1.744 ms | 0.0146 ms | 0.0122 ms |        - |        - |        - |    8.07 KB |


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
