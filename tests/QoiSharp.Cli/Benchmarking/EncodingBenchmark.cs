using BenchmarkDotNet.Attributes;
using QoiSharp.Cli.Benchmarking.Configs;
using QoiSharp.Codec;

namespace QoiSharp.Cli.Benchmarking;
/*

|                  Method | dataType |      Mean |     Error |   StdDev |   Gen 0 |   Gen 1 | Allocated |
|------------------------ |--------- |----------:|----------:|---------:|--------:|--------:|----------:|
|    'Nice encoding code' |    Index |  93.67 us | 24.932 us | 1.367 us | 12.0850 | 11.9629 |    101 KB |
| 'Fasterx encoding code' |    Index |  61.91 us | 14.015 us | 0.768 us | 12.1460 | 12.0850 |    101 KB |
| 'Fastest encoding code' |    Index |  51.68 us | 25.045 us | 1.373 us |  9.7656 |       - |     81 KB |
|    'Nice encoding code' |     Luma |  91.10 us | 10.170 us | 0.557 us | 12.0850 | 11.9629 |    101 KB |
| 'Fasterx encoding code' |     Luma |  60.24 us |  6.881 us | 0.377 us | 12.1460 | 12.0850 |    101 KB |
| 'Fastest encoding code' |     Luma |  49.78 us |  6.367 us | 0.349 us |  9.7656 |       - |     81 KB |
|    'Nice encoding code' |      Rgb | 174.84 us | 20.321 us | 1.114 us | 19.5313 | 19.2871 |    161 KB |
| 'Fasterx encoding code' |      Rgb | 164.95 us | 33.829 us | 1.854 us | 19.5313 | 19.2871 |    161 KB |
| 'Fastest encoding code' |      Rgb | 135.15 us |  6.960 us | 0.381 us |  9.7656 |       - |     81 KB |
|    'Nice encoding code' |     Rgba | 169.84 us | 27.759 us | 1.522 us | 19.5313 | 19.2871 |    161 KB |
| 'Fasterx encoding code' |     Rgba | 165.65 us | 10.341 us | 0.567 us | 19.5313 | 19.2871 |    161 KB |
| 'Fastest encoding code' |     Rgba | 137.20 us | 26.217 us | 1.437 us |  9.7656 |       - |     81 KB |
|    'Nice encoding code' |      Run |  55.66 us | 40.020 us | 2.194 us |  9.7656 |  0.1831 |     81 KB |
| 'Fasterx encoding code' |      Run |  27.20 us | 23.868 us | 1.308 us |  9.7656 |  0.1831 |     81 KB |
| 'Fastest encoding code' |      Run |  24.39 us | 25.445 us | 1.395 us |  9.7961 |       - |     81 KB |


|                   Method | dataType |         Mean |        Error |      StdDev |   Gen 0 |   Gen 1 |   Gen 2 | Allocated |
|------------------------- |--------- |-------------:|-------------:|------------:|--------:|--------:|--------:|----------:|
|     'Nice encoding code' |    Index |  87,861.4 ns | 21,830.75 ns | 1,196.62 ns | 12.0850 | 11.9629 |       - |    101 KB |
|  'Fastest encoding code' |    Index |  54,582.9 ns | 85,258.09 ns | 4,673.28 ns | 12.1460 | 12.0850 |       - |    101 KB |
| 'Streamed encoding code' |    Index |     174.3 ns |    411.22 ns |    22.54 ns |  0.3049 |  0.0017 |       - |      2 KB |
|     'Nice encoding code' |     Luma |  91,693.9 ns | 23,934.30 ns | 1,311.92 ns | 12.0850 | 11.9629 |       - |    101 KB |
|  'Fastest encoding code' |     Luma |  45,483.8 ns | 21,583.04 ns | 1,183.04 ns | 12.1460 | 12.0850 |       - |    101 KB |
| 'Streamed encoding code' |     Luma |     133.0 ns |     18.19 ns |     1.00 ns |  0.3049 |  0.0017 |       - |      2 KB |
|     'Nice encoding code' |      Rgb | 169,000.6 ns | 33,553.36 ns | 1,839.17 ns | 19.5313 | 19.2871 |       - |    161 KB |
|  'Fastest encoding code' |      Rgb | 149,306.4 ns |  9,731.79 ns |   533.43 ns | 19.5313 | 19.2871 |       - |    161 KB |
| 'Streamed encoding code' |      Rgb |     146.7 ns |     66.14 ns |     3.63 ns |  0.3049 |  0.0017 |       - |      2 KB |
|     'Nice encoding code' |     Rgba | 120,493.5 ns | 14,759.49 ns |   809.02 ns | 62.2559 | 62.2559 | 62.2559 |    201 KB |
|  'Fastest encoding code' |     Rgba |  93,670.5 ns | 33,561.69 ns | 1,839.63 ns | 62.5000 | 62.3779 | 62.3779 |    201 KB |
| 'Streamed encoding code' |     Rgba |     133.7 ns |     35.04 ns |     1.92 ns |  0.3107 |  0.0014 |       - |      3 KB |
|     'Nice encoding code' |      Run |  47,966.3 ns | 40,303.04 ns | 2,209.15 ns |  9.7656 |  0.1831 |       - |     81 KB |
|  'Fastest encoding code' |      Run |  25,590.2 ns |  2,731.75 ns |   149.74 ns |  9.7961 |  0.0916 |       - |     81 KB |
| 'Streamed encoding code' |      Run |     157.7 ns |    497.80 ns |    27.29 ns |  0.3049 |  0.0017 |       - |      2 KB |


|                  Method | dataType |      Mean |      Error |    StdDev |    Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|------------------------ |--------- |----------:|-----------:|----------:|---------:|---------:|---------:|----------:|
|   'Nice encdoding code' |    Index | 11.901 ms | 13.7039 ms | 0.7512 ms | 125.0000 | 125.0000 | 125.0000 |     10 MB |
| 'Faster encdoding code' |    Index |  8.564 ms |  4.9181 ms | 0.2696 ms | 187.5000 | 187.5000 | 187.5000 |     10 MB |
|   'Nice encdoding code' |     Luma |  7.708 ms |  2.8399 ms | 0.1557 ms | 203.1250 | 203.1250 | 203.1250 |     10 MB |
| 'Faster encdoding code' |     Luma |  8.066 ms |  3.4160 ms | 0.1872 ms | 140.6250 | 140.6250 | 140.6250 |     10 MB |
|   'Nice encdoding code' |      Rgb | 21.218 ms |  6.2368 ms | 0.3419 ms | 125.0000 | 125.0000 | 125.0000 |     16 MB |
| 'Faster encdoding code' |      Rgb | 21.882 ms |  4.3142 ms | 0.2365 ms | 125.0000 | 125.0000 | 125.0000 |     16 MB |
|   'Nice encdoding code' |      Run |  3.376 ms |  0.3133 ms | 0.0172 ms | 402.3438 | 398.4375 | 398.4375 |      8 MB |
| 'Faster encdoding code' |      Run |  3.469 ms |  2.5499 ms | 0.1398 ms | 402.3438 | 398.4375 | 398.4375 |      8 MB |

|                   Method | dataType |      Mean |     Error |    StdDev |    Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|------------------------- |--------- |----------:|----------:|----------:|---------:|---------:|---------:|----------:|
|     'Nice encoding code' |     Luma | 10.374 ms | 2.2025 ms | 0.1207 ms | 140.6250 | 140.6250 | 140.6250 |     10 MB |
|  'Fastest encoding code' |     Luma |  5.700 ms | 5.0335 ms | 0.2759 ms | 140.6250 | 140.6250 | 140.6250 |     10 MB |
| 'Streamed encoding code' |     Luma |  6.140 ms | 2.3868 ms | 0.1308 ms | 335.9375 | 320.3125 | 320.3125 |      4 MB |
|     'Nice encoding code' | RgbaLuma | 10.108 ms | 4.7563 ms | 0.2607 ms | 140.6250 | 140.6250 | 140.6250 |     12 MB |
|  'Fastest encoding code' | RgbaLuma |  6.625 ms | 2.2326 ms | 0.1224 ms | 203.1250 | 203.1250 | 203.1250 |     12 MB |
| 'Streamed encoding code' | RgbaLuma |  6.834 ms | 0.3298 ms | 0.0181 ms | 367.1875 | 351.5625 | 351.5625 |      4 MB |

*/
[Config(typeof(ShortRunConfig))]
public class EncodingBenchmark
{
    // [Params(nameof(QoiCodec.Run), nameof(QoiCodec.Rgb), nameof(QoiCodec.Index), nameof(QoiCodec.Luma), "RgbaIndex"))]
    // [Params(nameof(QoiCodec.Run), "RgbaRun", "RgbaAlphaRandomRun", nameof(QoiCodec.Index), "RgbaIndex", nameof(QoiCodec.Luma), "RgbaLuma")]
    // [Params(nameof(QoiCodec.Run))]
    // [Params(nameof(QoiCodec.Rgba))]
    [Params(nameof(QoiCodec.Luma),"RgbaLuma")]
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
        compressedSize = QoiEncoder.Encode(Image).Length;
    }
    [RunOncePerIteration]
    public void TestSettup()
    {
        ImageDataStream = new MemoryStream(Image.Data);
    }

    public QoiImage Image = new QoiImage([], 0, 0, Channels.Rgb);
    public MemoryStream ImageDataStream = new MemoryStream();
    private int compressedSize = 0;
    [Benchmark(Description = "Nice encoding code")]
    public byte[] NiceEncoding()
    {
        return QoiEncoder.Encode(Image);
    }

    [Benchmark(Description = "Fasterx encoding code")]
    public byte[] FasterEncoding()
    {
        return QoiEncoderFaster.Encode(Image);
    }

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