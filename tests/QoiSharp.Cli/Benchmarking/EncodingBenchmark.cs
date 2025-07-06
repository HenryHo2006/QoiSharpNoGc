using BenchmarkDotNet.Attributes;
using QoiSharp.Cli.Benchmarking.Configs;
using QoiSharp.Codec;

namespace QoiSharp.Cli.Benchmarking;
/*

108*190
|                   Method |           dataType |      Mean |     Error |   StdDev |   Gen 0 |   Gen 1 |   Gen 2 | Allocated |
|------------------------- |------------------- |----------:|----------:|---------:|--------:|--------:|--------:|----------:|
|  'Fastest encoding code' |              Index |  49.88 us | 50.198 us | 2.752 us | 12.1460 | 12.0850 |       - |    101 KB |
| 'Streamed encoding code' |              Index |  57.20 us | 34.830 us | 1.909 us | 12.6343 | 12.5732 |       - |    103 KB |
|  'Fastest encoding code' |               Luma |  51.62 us | 34.982 us | 1.917 us | 12.1460 | 12.0850 |       - |    101 KB |
| 'Streamed encoding code' |               Luma |  56.07 us | 10.532 us | 0.577 us | 12.6343 | 12.5732 |       - |    103 KB |
|  'Fastest encoding code' | RgbaAlphaRandomRun | 160.66 us | 72.174 us | 3.956 us | 32.2266 | 32.2266 | 32.2266 |    181 KB |
| 'Streamed encoding code' | RgbaAlphaRandomRun | 142.69 us | 55.468 us | 3.040 us | 44.4336 | 31.2500 | 29.2969 |    219 KB |
|  'Fastest encoding code' |          RgbaIndex |  75.26 us | 12.081 us | 0.662 us | 32.2266 | 32.2266 | 32.2266 |    121 KB |
| 'Streamed encoding code' |          RgbaIndex |  66.15 us | 37.029 us | 2.030 us | 11.4746 | 11.3525 |       - |     95 KB |
|  'Fastest encoding code' |           RgbaLuma |  75.12 us |  3.884 us | 0.213 us | 32.2266 | 32.2266 | 32.2266 |    121 KB |
| 'Streamed encoding code' |           RgbaLuma |  62.68 us | 26.188 us | 1.435 us | 11.4746 | 11.3525 |       - |     95 KB |
|  'Fastest encoding code' |            RgbaRun |  33.96 us |  0.513 us | 0.028 us | 32.2266 | 32.2266 | 32.2266 |    101 KB |
| 'Streamed encoding code' |            RgbaRun |  29.67 us | 28.985 us | 1.589 us |  6.5918 |  1.0071 |       - |     54 KB |
|  'Fastest encoding code' |                Run |  20.17 us |  2.218 us | 0.122 us |  9.7961 |  0.0916 |       - |     81 KB |
| 'Streamed encoding code' |                Run |  24.18 us | 17.496 us | 0.959 us |  6.4087 |  0.0916 |       - |     52 KB |


1080*1900
|                   Method |           dataType |      Mean |     Error |    StdDev |    Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|------------------------- |------------------- |----------:|----------:|----------:|---------:|---------:|---------:|----------:|
|  'Fastest encoding code' |              Index |  5.309 ms | 2.5504 ms | 0.1398 ms | 257.8125 | 257.8125 | 257.8125 | 10,020 KB |
| 'Streamed encoding code' |              Index |  6.261 ms | 5.3393 ms | 0.2927 ms | 125.0000 | 109.3750 | 109.3750 |  7,568 KB |
|  'Fastest encoding code' |               Luma |  5.523 ms | 3.4761 ms | 0.1905 ms | 218.7500 | 218.7500 | 218.7500 | 10,020 KB |
| 'Streamed encoding code' |               Luma |  6.145 ms | 1.9319 ms | 0.1059 ms | 125.0000 | 109.3750 | 109.3750 |  7,564 KB |
|  'Fastest encoding code' | RgbaAlphaRandomRun | 13.856 ms | 4.9296 ms | 0.2702 ms | 125.0000 | 125.0000 | 125.0000 | 18,035 KB |
| 'Streamed encoding code' | RgbaAlphaRandomRun | 12.143 ms | 1.8685 ms | 0.1024 ms |  78.1250 |  62.5000 |  62.5000 |  6,062 KB |
|  'Fastest encoding code' |          RgbaIndex |  5.404 ms | 0.9224 ms | 0.0506 ms | 226.5625 | 226.5625 | 226.5625 | 12,024 KB |
| 'Streamed encoding code' |          RgbaIndex |  6.304 ms | 2.8036 ms | 0.1537 ms |  93.7500 |  78.1250 |  78.1250 |  6,068 KB |
|  'Fastest encoding code' |           RgbaLuma |  5.630 ms | 3.2608 ms | 0.1787 ms | 179.6875 | 179.6875 | 179.6875 | 12,024 KB |
| 'Streamed encoding code' |           RgbaLuma |  6.273 ms | 3.6353 ms | 0.1993 ms |  93.7500 |  78.1250 |  78.1250 |  6,064 KB |
|  'Fastest encoding code' |            RgbaRun |  1.865 ms | 0.3722 ms | 0.0204 ms | 402.3438 | 400.3906 | 398.4375 | 10,052 KB |
| 'Streamed encoding code' |            RgbaRun |  2.437 ms | 0.2917 ms | 0.0160 ms |  19.5313 |  15.6250 |        - |    181 KB |
|  'Fastest encoding code' |                Run |  2.589 ms | 1.3562 ms | 0.0743 ms | 402.3438 | 398.4375 | 398.4375 |  8,050 KB |
| 'Streamed encoding code' |                Run |  2.090 ms | 0.2832 ms | 0.0155 ms |  19.5313 |  15.6250 |        - |    180 KB |

*/
[Config(typeof(ShortRunConfig))]
public class EncodingBenchmark
{
    // [Params(nameof(QoiCodec.Run), nameof(QoiCodec.Rgb), nameof(QoiCodec.Index), nameof(QoiCodec.Luma), "RgbaIndex"))]
    [Params(nameof(QoiCodec.Run), "RgbaRun", "RgbaAlphaRandomRun", nameof(QoiCodec.Index), "RgbaIndex", nameof(QoiCodec.Luma), "RgbaLuma")]
    // [Params(nameof(QoiCodec.Run))]
    // [Params(nameof(QoiCodec.Rgba))]
    // [Params(nameof(QoiCodec.Luma),"RgbaLuma")]
    public string dataType = "";

    [GlobalSetup]
    public void Setup()
    {
        var height = 1080;
        var width = 1900;
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
    }
    [RunOncePerIteration]
    public void TestSettup()
    {
        ImageDataStream = new MemoryStream(Image.Data);
    }

    public QoiImage Image = new QoiImage([], 0, 0, Channels.Rgb);
    public MemoryStream ImageDataStream = new MemoryStream();
    // [Benchmark(Description = "Nice encoding code")]
    // public byte[] NiceEncoding()
    // {
    //     return QoiEncoder.Encode(Image);
    // }

    // [Benchmark(Description = "Fasterx encoding code")]
    // public byte[] FasterEncoding()
    // {
    //     return QoiEncoderFaster.Encode(Image);
    // }

    [Benchmark(Description = "Fastest encoding code")]
    public byte[] FastestEncoding()
    {
        return QoiEncoderFaster2.Encode(Image);
    }

    [Benchmark(Description = "Streamed encoding code")]
    public Stream StreamEncoding()
    {
        ImageDataStream.Position = 0;
        return QoiEncoderStream.Encode(Image, ImageDataStream);
        // if (bytes.Length != compressedSize)
        // {
        //     throw new Exception($"Compressed size mismatch: expected {compressedSize}, got {bytes.Length}");
        // }
        // return bytes;
    }
}