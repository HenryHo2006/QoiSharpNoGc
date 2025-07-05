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

*/
[Config(typeof(ShortRunConfig))]
public class EncodingBenchmark
{
    [Params(nameof(QoiCodec.Run), nameof(QoiCodec.Rgb), nameof(QoiCodec.Index), nameof(QoiCodec.Luma), nameof(QoiCodec.Rgba))]
    // [Params(nameof(QoiCodec.Run), nameof(QoiCodec.Rgba), nameof(QoiCodec.Luma), "Alpha150")]
    // [Params(nameof(QoiCodec.Run))]
    // [Params(nameof(QoiCodec.Rgb))]
    // [Params(nameof(QoiCodec.Luma))]
    public string? dataType;

    [GlobalSetup]
    public void Setup()
    {
        var height = 108;
        var width = 190;
        var data = new byte[height * width * 3];
        if (dataType == nameof(QoiCodec.Rgba) || dataType == "Alpha150")
        {
            data = new byte[height * width * 4];
            var random = new Random();
            random.NextBytes(data);
            if (dataType == "Alpha150")
            {
                for (int i = 3; i < data.Length; i += 4)
                {
                    data[i] = 150; // Set alpha channel to 150
                }
            }
        }
        else if (dataType == nameof(QoiCodec.Rgb))
        {
            var random = new Random();
            random.NextBytes(data);
        }
        else
        {
            byte[] indexData = [12, 34, 56, 153, 232, 12, 76, 87, 87];
            byte[] lumaData = [12, 34, 56, 20, 40, 60, 10, 30, 50];
            for (var i = 0; i < data.Length; i += 3)
            {
                switch (dataType)
                {
                    case nameof(QoiCodec.Run):
                        data[i] = 12;
                        data[i + 1] = 34;
                        data[i + 2] = 56;
                        break;
                    case nameof(QoiCodec.Index):
                        data[i] = indexData[i % 9];
                        data[i + 1] = indexData[i % 9 + 1];
                        data[i + 2] = indexData[i % 9 + 2];
                        break;
                    case nameof(QoiCodec.Luma):
                        data[i] = lumaData[i % 9];
                        data[i + 1] = lumaData[i % 9 + 1];
                        data[i + 2] = lumaData[i % 9 + 2];
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(dataType), "Invalid data type");
                }
            }
        }
        Image = new QoiImage(data, width, height, Channels.Rgb);
    }

    public QoiImage Image = new QoiImage([], 0, 0, Channels.Rgb);

    [Benchmark(Description = "Nice encoding code")]
    public byte[] NiceEncoding()
    {
        return QoiEncoder.Encode(Image);
    }

    [Benchmark(Description = "Fasterx encoding code")]
    public byte[] EnumerationEncoding2()
    {
        return QoiEncoderFaster.Encode(Image);
    }

    [Benchmark(Description = "Fastest encoding code")]
    public byte[] EnumerationEncoding()
    {
        return QoiEncoderFaster2.Encode(Image);
    }

    /*
    |                      Method | dataType |      Mean |     Error |    StdDev |    Median | Allocated |
    |---------------------------- |--------- |----------:|----------:|----------:|----------:|----------:|
    |           'RgbaEquals quad' |      Run | 0.0032 ns | 0.0750 ns | 0.0041 ns | 0.0015 ns |         - |
    | 'RgbaEquals quad firstFail' |      Run | 0.0000 ns | 0.0000 ns | 0.0000 ns | 0.0000 ns |         - |
    |     'RgbaEquals int inline' |      Run | 0.0066 ns | 0.1092 ns | 0.0060 ns | 0.0070 ns |         - |
    */

    // [Benchmark(Description = "RgbaEquals quad")]
    // public bool RgbaEquals1Test()
    // {
    //     var data = RgbaEqualsInline(1, 2, 3, 4, 1, 2, 3, 4);
    //     return data;
    // }

    // [Benchmark(Description = "RgbaEquals quad firstFail")]
    // public bool RgbaEquals1Test2()
    // {
    //     var data = RgbaEqualsInline(1, 2, 3, 4, 2, 2, 3, 4);
    //     return data;
    // }

    // [Benchmark(Description = "RgbaEquals int inline")]
    // public bool RgbaEqualsInline22Test()
    // {
    //     var data = RgbaEqualsInline2(1, 2, 3, 4, 1, 2, 3, 4);
    //     return data;
    // }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // private static bool RgbaEqualsInline(byte r1, byte g1, byte b1, byte a1, byte r2, byte g2, byte b2, byte a2) =>
    //         r1 == r2 &&
    //         g1 == g2 &&
    //         b1 == b2 &&
    //         a1 == a2;

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // private static bool RgbaEqualsInline2(byte r1, byte g1, byte b1, byte a1, byte r2, byte g2, byte b2, byte a2)
    // {
    //     var i1 = r1 << 24 | g1 << 16 | b1 << 8 | a1;
    //     var i2 = r2 << 24 | g2 << 16 | b2 << 8 | a2;
    //     return i1 == i2;
    // }
}