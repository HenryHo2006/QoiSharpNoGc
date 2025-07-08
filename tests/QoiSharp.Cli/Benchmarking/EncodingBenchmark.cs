using System.Drawing;
using BenchmarkDotNet.Attributes;
using QoiSharp.Codec;

namespace QoiSharp.Cli.Benchmarking;
/*

| Method                   | dataType           | Mean      | Error     | StdDev   | Gen0    | Gen1    | Gen2    | Allocated |
|------------------------- |------------------- |----------:|----------:|---------:|--------:|--------:|--------:|----------:|
| 'Fast encoding code'     | Luma               |  51.25 us | 27.438 us | 1.504 us | 12.1460 |  2.9907 |       - | 100.57 KB |
| 'Streamed encoding code' | Luma               |  50.14 us |  3.695 us | 0.203 us | 12.6343 |  1.0376 |       - | 103.45 KB |
| 'Fast encoding code'     | Rgb                | 134.95 us | 23.567 us | 1.292 us | 19.5313 |       - |       - | 160.66 KB |
| 'Streamed encoding code' | Rgb                | 183.30 us | 37.266 us | 2.043 us | 36.8652 | 36.8652 | 36.8652 | 257.09 KB |
| 'Fast encoding code'     | RgbaAlphaRandomRun | 126.88 us |  2.714 us | 0.149 us | 32.2266 | 32.2266 | 32.2266 | 181.54 KB |
| 'Streamed encoding code' | RgbaAlphaRandomRun | 150.36 us | 30.410 us | 1.667 us | 30.2734 | 30.2734 | 30.2734 | 218.39 KB |
| 'Fast encoding code'     | RgbaLuma           |  67.02 us |  5.719 us | 0.313 us | 32.2266 | 32.2266 | 32.2266 | 120.63 KB |
| 'Streamed encoding code' | RgbaLuma           |  53.46 us |  5.163 us | 0.283 us | 11.5356 |  1.4038 |       - |  94.66 KB |

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
| Method                   | dataType           | Mean      | Error     | StdDev    | Gen0     | Gen1     | Gen2     | Allocated   |
|------------------------- |------------------- |----------:|----------:|----------:|---------:|---------:|---------:|------------:|
| 'Nice encoding code'     | Index              |  9.134 ms | 3.4359 ms | 0.1883 ms | 593.7500 | 593.7500 | 593.7500 | 10020.28 KB |
| 'Fastest encoding code'  | Index              |  4.879 ms | 0.3074 ms | 0.0169 ms | 593.7500 | 593.7500 | 593.7500 | 10020.28 KB |
| 'Streamed encoding code' | Index              |  6.501 ms | 0.6054 ms | 0.0332 ms | 296.8750 | 281.2500 | 281.2500 |  7568.05 KB |
| 'Nice encoding code'     | Luma               |  9.001 ms | 0.9739 ms | 0.0534 ms | 593.7500 | 593.7500 | 593.7500 | 10020.28 KB |
| 'Fastest encoding code'  | Luma               |  5.006 ms | 3.8510 ms | 0.2111 ms | 593.7500 | 593.7500 | 593.7500 | 10020.34 KB |
| 'Streamed encoding code' | Luma               |  6.890 ms | 5.7676 ms | 0.3161 ms | 296.8750 | 281.2500 | 281.2500 |  7564.05 KB |
| 'Nice encoding code'     | RgbaAlphaRandomRun | 21.665 ms | 8.8850 ms | 0.4870 ms | 562.5000 | 562.5000 | 562.5000 | 18036.23 KB |
| 'Fastest encoding code'  | RgbaAlphaRandomRun | 15.946 ms | 5.6135 ms | 0.3077 ms | 625.0000 | 625.0000 | 625.0000 | 18036.85 KB |
| 'Streamed encoding code' | RgbaAlphaRandomRun | 16.550 ms | 4.3578 ms | 0.2389 ms | 625.0000 | 625.0000 | 625.0000 | 24364.31 KB |
| 'Nice encoding code'     | RgbaIndex          |  9.838 ms | 1.0077 ms | 0.0552 ms | 500.0000 | 500.0000 | 500.0000 | 12025.84 KB |
| 'Fastest encoding code'  | RgbaIndex          |  5.269 ms | 2.0080 ms | 0.1101 ms | 468.7500 | 468.7500 | 468.7500 |  12025.8 KB |
| 'Streamed encoding code' | RgbaIndex          |  6.423 ms | 1.3605 ms | 0.0746 ms | 507.8125 | 492.1875 | 492.1875 |  6072.09 KB |
| 'Nice encoding code'     | RgbaLuma           |  9.518 ms | 2.3114 ms | 0.1267 ms | 500.0000 | 500.0000 | 500.0000 | 12025.84 KB |
| 'Fastest encoding code'  | RgbaLuma           |  5.260 ms | 4.6728 ms | 0.2561 ms | 500.0000 | 500.0000 | 500.0000 |    12026 KB |
| 'Streamed encoding code' | RgbaLuma           |  6.415 ms | 0.7561 ms | 0.0414 ms | 507.8125 | 492.1875 | 492.1875 |   6068.1 KB |
| 'Nice encoding code'     | RgbaRun            |  3.761 ms | 0.7101 ms | 0.0389 ms | 402.3438 | 398.4375 | 398.4375 | 10052.48 KB |
| 'Fastest encoding code'  | RgbaRun            |  1.848 ms | 0.6399 ms | 0.0351 ms | 402.3438 | 398.4375 | 398.4375 | 10052.75 KB |
| 'Streamed encoding code' | RgbaRun            |  2.820 ms | 0.3809 ms | 0.0209 ms |  19.5313 |   3.9063 |        - |   181.12 KB |
| 'Nice encoding code'     | Run                |  4.977 ms | 1.9285 ms | 0.1057 ms | 390.6250 | 390.6250 | 390.6250 |  8050.08 KB |
| 'Fastest encoding code'  | Run                |  2.948 ms | 0.7032 ms | 0.0385 ms | 398.4375 | 394.5313 | 394.5313 |  8050.14 KB |
| 'Streamed encoding code' | Run                |  2.189 ms | 2.3206 ms | 0.1272 ms |  19.5313 |   3.9063 |        - |   179.66 KB |

*/

public class EncodingBenchmark
{
    // [Params(nameof(QoiCodec.Run), "RgbaRun", "RgbaAlphaRandomRun", nameof(QoiCodec.Index), "RgbaIndex", nameof(QoiCodec.Luma), "RgbaLuma")]
    [Params(nameof(QoiCodec.Luma), "RgbaLuma", "RgbaAlphaRandomRun", nameof(QoiCodec.Rgb))]
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
    }
    [RunOncePerIteration]
    public void TestSettup()
    {
        ImageDataStream = new MemoryStream(Image.Data);
    }

    public QoiImage Image = new([], 0, 0, Channels.Rgb);
    public MemoryStream ImageDataStream = new MemoryStream();


    [Benchmark(Description = "Fast encoding code")]
    public byte[] FastEncoding()
    {
        return QoiEncoder.Encode(Image);
    }

    [Benchmark(Description = "Streamed encoding code")]
    public Stream StreamEncoding()
    {
        ImageDataStream.Position = 0;
        return new QoiEncoderStream(ImageDataStream, new Size(Image.Width, Image.Height), Image.Channels);
    }
}
