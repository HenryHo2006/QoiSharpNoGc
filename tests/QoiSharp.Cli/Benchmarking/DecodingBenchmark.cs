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

*/

    // [Params(nameof(QoiCodec.Run), "RgbaRun", "RgbaAlphaRandomRun", nameof(QoiCodec.Index), "RgbaIndex", nameof(QoiCodec.Luma), "RgbaLuma")]
    // [Params(nameof(QoiCodec.Luma), "RgbaLuma", "RgbaAlphaRandomRun", nameof(QoiCodec.Rgb))]
    [Params(nameof(QoiCodec.Rgb), nameof(QoiCodec.Rgba), nameof(QoiCodec.Luma), nameof(QoiCodec.Diff), nameof(QoiCodec.Run), nameof(QoiCodec.Index))]
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
}
