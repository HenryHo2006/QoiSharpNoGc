using System.Drawing;
using BenchmarkDotNet.Attributes;
using QoiSharp.Codec;

namespace QoiSharp.Cli.Benchmarking;
/*
Some test results:
AMD Ryzen 5 5600G
Image size: 108*190

| Method                   | dataType           | Mean      | Error    | StdDev   | Gen0    | Code Size | Gen1    | Gen2    | Allocated |
|------------------------- |------------------- |----------:|---------:|---------:|--------:|----------:|--------:|--------:|----------:|
| 'Fast encoding code'     | Index              |  49.93 us | 0.855 us | 0.758 us | 12.1460 |   2,263 B |  2.9907 |       - | 100.57 KB |
| 'Streamed encoding code' | Index              |  48.43 us | 0.439 us | 0.411 us |  0.6714 |   4,779 B |       - |       - |   5.54 KB |
| 'Fast encoding code'     | Luma               |  48.92 us | 0.889 us | 0.831 us | 12.1460 |   2,293 B |  2.9907 |       - | 100.57 KB |
| 'Streamed encoding code' | Luma               |  48.18 us | 0.334 us | 0.312 us |  0.6714 |   4,793 B |       - |       - |   5.54 KB |
| 'Fast encoding code'     | RgbaAlphaRandomRun | 138.84 us | 1.534 us | 1.435 us | 32.2266 |   2,404 B | 32.2266 | 32.2266 | 180.76 KB |
| 'Streamed encoding code' | RgbaAlphaRandomRun |  88.72 us | 1.191 us | 1.114 us |  0.6104 |   5,607 B |       - |       - |   5.68 KB |
| 'Fast encoding code'     | RgbaIndex          |  67.76 us | 1.316 us | 1.231 us | 32.2266 |   2,420 B | 32.2266 | 32.2266 | 120.63 KB |
| 'Streamed encoding code' | RgbaIndex          |  53.31 us | 1.065 us | 0.944 us |  0.6714 |   4,965 B |       - |       - |   5.68 KB |
| 'Fast encoding code'     | RgbaLuma           |  67.27 us | 1.129 us | 1.001 us | 32.2266 |   2,424 B | 32.2266 | 32.2266 | 120.63 KB |
| 'Streamed encoding code' | RgbaLuma           |  48.26 us | 0.419 us | 0.392 us |  0.6714 |   5,763 B |       - |       - |   5.68 KB |
| 'Fast encoding code'     | RgbaRun            |  28.17 us | 0.489 us | 0.502 us | 32.2571 |   2,363 B | 32.2571 | 32.2571 | 100.91 KB |
| 'Streamed encoding code' | RgbaRun            |  19.81 us | 0.389 us | 0.464 us |  0.6714 |   6,035 B |       - |       - |   5.68 KB |
| 'Fast encoding code'     | Run                |  26.07 us | 0.805 us | 2.374 us |  9.7961 |   2,233 B |       - |       - |  80.85 KB |
| 'Streamed encoding code' | Run                |  22.26 us | 0.430 us | 0.478 us |  0.6714 |   5,365 B |       - |       - |   5.54 KB |


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
        var height = 108;
        var width = 190;
        var data = new byte[height * width * 3];
        var channel = Channels.Rgb;
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

    // [Benchmark(Description = "Stream no encoding")]
    // public Stream StreamCreation()
    // {
    //     ImageDataStream.Position = 0;
    //     return new QoiEncoderStream(ImageDataStream, new Size(Image.Width, Image.Height), Image.Channels);
    // }

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
