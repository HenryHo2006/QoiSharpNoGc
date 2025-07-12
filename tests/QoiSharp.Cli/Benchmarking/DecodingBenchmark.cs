using BenchmarkDotNet.Attributes;
using QoiSharp.Codec;

namespace QoiSharp.Cli.Benchmarking;

[Config(typeof(CustomBenchmarkConfig))]
public class DecodingBenchmark
{    
/*
108*190
| Method             | dataType | Mean      | Error    | StdDev   | Gen0   | Allocated |
|------------------- |--------- |----------:|---------:|---------:|-------:|----------:|
| 'QOI Decoding'     | Diff     |  37.94 us | 0.677 us | 0.779 us | 7.3242 |  60.48 KB |
| 'QOI Decoding old' | Diff     |  28.12 us | 0.556 us | 0.570 us | 7.3242 |  60.48 KB |
| 'QOI Decoding'     | Index    |  79.21 us | 1.583 us | 3.198 us | 7.3242 |  60.48 KB |
| 'QOI Decoding old' | Index    | 101.15 us | 2.001 us | 2.224 us | 7.3242 |  60.48 KB |
| 'QOI Decoding'     | Luma     |  77.69 us | 1.358 us | 1.270 us | 7.3242 |  60.48 KB |
| 'QOI Decoding old' | Luma     | 101.30 us | 1.157 us | 1.082 us | 7.3242 |  60.48 KB |
| 'QOI Decoding'     | Rgb      |  80.10 us | 1.200 us | 1.064 us | 7.3242 |  60.48 KB |
| 'QOI Decoding old' | Rgb      |  85.20 us | 1.359 us | 1.271 us | 7.3242 |  60.48 KB |
| 'QOI Decoding'     | Rgba     |  81.28 us | 0.869 us | 0.771 us | 9.7656 |  80.52 KB |
| 'QOI Decoding old' | Rgba     |  86.50 us | 0.929 us | 0.776 us | 9.7656 |  80.52 KB |
| 'QOI Decoding'     | Run      |  41.12 us | 0.814 us | 1.836 us | 7.3242 |  60.48 KB |
| 'QOI Decoding old' | Run      |  31.19 us | 0.604 us | 1.376 us | 7.3242 |  60.48 KB |


| Method             | dataType | Mean     | Error    | StdDev   | Gen0   | Allocated |
|------------------- |--------- |---------:|---------:|---------:|-------:|----------:|
| 'QOI Decoding'     | Diff     | 36.89 us | 0.695 us | 0.713 us | 7.3242 |  60.48 KB |
| 'QOI Decoding old' | Diff     | 26.77 us | 0.385 us | 0.360 us | 7.3242 |  60.48 KB |
| 'QOI Decoding ifs' | Diff     | 26.68 us | 0.522 us | 0.489 us | 7.3242 |  60.48 KB |
| 'QOI Decoding'     | Index    | 73.41 us | 1.388 us | 1.363 us | 7.3242 |  60.48 KB |
| 'QOI Decoding old' | Index    | 98.26 us | 1.914 us | 1.791 us | 7.3242 |  60.48 KB |
| 'QOI Decoding ifs' | Index    | 95.54 us | 1.246 us | 1.104 us | 7.3242 |  60.48 KB |
| 'QOI Decoding'     | Luma     | 78.92 us | 1.530 us | 1.571 us | 7.3242 |  60.48 KB |
| 'QOI Decoding old' | Luma     | 95.62 us | 0.822 us | 0.769 us | 7.3242 |  60.48 KB |
| 'QOI Decoding ifs' | Luma     | 96.08 us | 0.719 us | 0.673 us | 7.3242 |  60.48 KB |
| 'QOI Decoding'     | Rgb      | 76.03 us | 0.814 us | 0.762 us | 7.3242 |  60.48 KB |
| 'QOI Decoding old' | Rgb      | 80.30 us | 0.732 us | 0.649 us | 7.3242 |  60.48 KB |
| 'QOI Decoding ifs' | Rgb      | 85.61 us | 0.508 us | 0.475 us | 7.3242 |  60.48 KB |
| 'QOI Decoding'     | Rgba     | 77.07 us | 1.318 us | 1.233 us | 9.7656 |  80.52 KB |
| 'QOI Decoding old' | Rgba     | 87.18 us | 0.909 us | 0.806 us | 9.7656 |  80.52 KB |
| 'QOI Decoding ifs' | Rgba     | 96.67 us | 1.167 us | 1.091 us | 9.7656 |  80.52 KB |
| 'QOI Decoding'     | Run      | 36.28 us | 0.393 us | 0.349 us | 7.3242 |  60.48 KB |
| 'QOI Decoding old' | Run      | 27.46 us | 0.455 us | 0.425 us | 7.3242 |  60.48 KB |
| 'QOI Decoding ifs' | Run      | 27.41 us | 0.509 us | 0.587 us | 7.3242 |  60.48 KB |


| Method             | dataType | Mean     | Error    | StdDev   | Gen0   | Allocated |
|------------------- |--------- |---------:|---------:|---------:|-------:|----------:|
| 'QOI Decoding'     | Diff     |       NA |       NA |       NA |     NA |        NA |
| 'QOI Decoding old' | Diff     | 27.68 us | 0.338 us | 0.264 us | 7.3242 |  60.48 KB |
| 'QOI Decoding ifs' | Diff     | 26.30 us | 0.505 us | 0.561 us | 7.3242 |  60.48 KB |
| 'QOI Decoding'     | Index    | 72.58 us | 1.230 us | 1.150 us | 7.3242 |  60.48 KB |
| 'QOI Decoding old' | Index    | 90.77 us | 0.690 us | 0.611 us | 7.3242 |  60.48 KB |
| 'QOI Decoding ifs' | Index    | 88.49 us | 0.931 us | 0.826 us | 7.3242 |  60.48 KB |
| 'QOI Decoding'     | Luma     | 76.83 us | 1.458 us | 1.292 us | 7.3242 |  60.48 KB |
| 'QOI Decoding old' | Luma     | 94.05 us | 0.753 us | 0.704 us | 7.3242 |  60.48 KB |
| 'QOI Decoding ifs' | Luma     | 92.75 us | 1.535 us | 1.361 us | 7.3242 |  60.48 KB |
| 'QOI Decoding'     | Rgb      | 75.16 us | 0.522 us | 0.463 us | 7.3242 |  60.48 KB |
| 'QOI Decoding old' | Rgb      | 79.77 us | 0.628 us | 0.557 us | 7.3242 |  60.48 KB |
| 'QOI Decoding ifs' | Rgb      | 84.61 us | 0.366 us | 0.285 us | 7.3242 |  60.48 KB |
| 'QOI Decoding'     | Rgba     | 70.95 us | 0.596 us | 0.558 us | 9.7656 |  80.52 KB |
| 'QOI Decoding old' | Rgba     | 82.23 us | 1.341 us | 1.491 us | 9.7656 |  80.52 KB |
| 'QOI Decoding ifs' | Rgba     | 86.53 us | 0.795 us | 0.744 us | 9.7656 |  80.52 KB |
| 'QOI Decoding'     | Run      |       NA |       NA |       NA |     NA |        NA |
| 'QOI Decoding old' | Run      | 26.14 us | 0.357 us | 0.334 us | 7.3242 |  60.48 KB |
| 'QOI Decoding ifs' | Run      | 24.96 us | 0.378 us | 0.353 us | 7.3242 |  60.48 KB |

*/

    // [Params(nameof(QoiCodec.Run), "RgbaRun", "RgbaAlphaRandomRun", nameof(QoiCodec.Index), "RgbaIndex", nameof(QoiCodec.Luma), "RgbaLuma")]
    // [Params(nameof(QoiCodec.Rgb), "RgbaAlphaRandomRun", nameof(QoiCodec.Rgba), nameof(QoiCodec.Luma), nameof(QoiCodec.Diff), nameof(QoiCodec.Run), nameof(QoiCodec.Index))]
    [Params(nameof(QoiCodec.Run))]
    public string dataType = "";

    [GlobalSetup]
    public void Setup()
    {
        var height = 108;
        var width = 190;
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

    [Benchmark(Description = "QOI Decoding old")]
    public QoiImage QoiDecoding_old()
    {
        return QoiDecoderOld.Decode(_qoiData);
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
