using System;
using System.Diagnostics.CodeAnalysis;
using QoiSharp.Codec;

namespace QoiSharp.Tests;

public static class Helper
{
    [ExcludeFromCodeCoverage(Justification = "Only to create example data for tests")]
    public static QoiImage CreateExampleData(string dataType, int width, int height)
    {
        var data = new byte[height * width * 3];
        var channel = Channels.Rgb;
        if (dataType == nameof(QoiCodec.Rgba) || dataType == "Alpha150")
        {
            channel = Channels.RgbWithAlpha;
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
        var qoiImage = new QoiImage(data, width, height, channel);
        return qoiImage;
    }
}