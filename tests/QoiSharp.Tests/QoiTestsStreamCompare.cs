using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QoiSharp.Codec;
using Xunit;

namespace QoiSharp.Tests;

public class QoiTestsStreamCompare
{

    [Fact]
    public void RgbEncoding_SingleColor_QOI_OP_RUN()
    {
        byte[] imageBytes = [
            12 ,34, 65, 12 ,34, 65, 12 ,34, 65,
            12 ,34, 65, 12 ,34, 65, 12 ,34, 65
        ];

        var qoiImage = new QoiImage(imageBytes, 3, 2, Channels.Rgb);

        byte[] qoiData = ((MemoryStream)QoiEncoderStream.Encode(qoiImage, new MemoryStream(imageBytes))).ToArray();
        Assert.Equal(QoiCodec.HeaderSize + 4 + 1 + QoiCodec.Padding.Length, qoiData.Length);
        Assert.Equal(128 + 64 + 4, qoiData[14 + 4]); //Check run byte
        Assert.Equal(QoiEncoderReference.Encode(qoiImage), qoiData);

        var img = QoiDecoder.Decode(qoiData);
        Assert.True(img.Data.SequenceEqual(imageBytes));
        Assert.Equal(img.Width, qoiImage.Width);
        Assert.Equal(img.Height, qoiImage.Height);
        Assert.Equal(img.Channels, qoiImage.Channels);
        Assert.Equal(img.ColorSpace, qoiImage.ColorSpace);
    }

    [Fact]
    public void RgbEncoding_QOI_OP_DIFF()
    {
        byte[] imageBytes = [
            12 ,34, 65, 13 ,35, 66, 14 ,36, 67,
            13 ,35, 66, 12 ,34, 65, 11 ,33, 64
        ];

        var qoiImage = new QoiImage(imageBytes, 3, 2, Channels.Rgb);

        byte[] qoiData = ((MemoryStream)QoiEncoderStream.Encode(qoiImage, new MemoryStream(imageBytes))).ToArray();
        Assert.Equal(QoiCodec.HeaderSize + 4 + 5 + QoiCodec.Padding.Length, qoiData.Length);
        Assert.Equal(127, qoiData[14 + 4]); //Check diff byte
        Assert.Equal(qoiData[14 + 4], qoiData[14 + 4 + 1]);
        Assert.Equal(64 + 16 + 4 + 1, qoiData[14 + 4 + 4]);
        Assert.Equal(QoiEncoderReference.Encode(qoiImage), qoiData);

        var img = QoiDecoder.Decode(qoiData);
        Assert.True(img.Data.SequenceEqual(imageBytes));
        Assert.True(img.Data.SequenceEqual(imageBytes));
        Assert.Equal(img.Width, qoiImage.Width);
        Assert.Equal(img.Height, qoiImage.Height);
        Assert.Equal(img.Channels, qoiImage.Channels);
        Assert.Equal(img.ColorSpace, qoiImage.ColorSpace);
    }

    [Fact]
    public void RgbEncoding_QOI_OP_LUMA()
    {
        byte[] imageBytes = [
            12 ,34, 65, 22 ,44, 75,
            13 ,33, 66, 23 ,44, 75,
        ];

        var qoiImage = new QoiImage(imageBytes, 2, 2, Channels.Rgb);

        byte[] qoiData = ((MemoryStream)QoiEncoderStream.Encode(qoiImage, new MemoryStream(imageBytes))).ToArray();
        Assert.Equal(QoiCodec.HeaderSize + 4 + 6 + QoiCodec.Padding.Length, qoiData.Length);
        Assert.Equal(128 + 32 + 8 + 2, qoiData[14 + 4]); //Check luma byte1
        Assert.Equal(128 + 8, qoiData[14 + 5]); //Check luma byte2
        Assert.Equal(QoiEncoderReference.Encode(qoiImage), qoiData);

        var img = QoiDecoder.Decode(qoiData);
        Assert.True(img.Data.SequenceEqual(imageBytes));
        Assert.True(img.Data.SequenceEqual(imageBytes));
        Assert.Equal(img.Width, qoiImage.Width);
        Assert.Equal(img.Height, qoiImage.Height);
        Assert.Equal(img.Channels, qoiImage.Channels);
        Assert.Equal(img.ColorSpace, qoiImage.ColorSpace);
    }

    [Fact]
    public void RgbEncoding_Index()
    {
        byte[] imageBytes = [
            12 ,34, 65, 222 ,144, 75, 12 ,34, 65,
            222,144,75, 12 ,34, 65, 222 ,144, 75,
        ];

        var qoiImage = new QoiImage(imageBytes, 3, 2, Channels.Rgb);

        byte[] qoiData = ((MemoryStream)QoiEncoderStream.Encode(qoiImage, new MemoryStream(imageBytes))).ToArray();
        Assert.Equal(QoiCodec.HeaderSize + 8 + 4 + QoiCodec.Padding.Length, qoiData.Length);
        Assert.Equal(8 + 2, qoiData[14 + 8]); //Check index byte
        Assert.Equal(QoiEncoderReference.Encode(qoiImage), qoiData);

        var img = QoiDecoder.Decode(qoiData);
        Assert.True(img.Data.SequenceEqual(imageBytes));
        Assert.True(img.Data.SequenceEqual(imageBytes));
        Assert.Equal(img.Width, qoiImage.Width);
        Assert.Equal(img.Height, qoiImage.Height);
        Assert.Equal(img.Channels, qoiImage.Channels);
        Assert.Equal(img.ColorSpace, qoiImage.ColorSpace);
    }

    [Theory]
    [InlineData(nameof(QoiCodec.Rgba))]
    [InlineData(nameof(QoiCodec.Rgb))]
    [InlineData(nameof(QoiCodec.Luma))]
    [InlineData(nameof(QoiCodec.Index))]
    public void RgbEncoding_Big(string dataType)
    {
        QoiImage qoiImage = CreateExampleData(dataType, 108, 190);

        byte[] qoiData = ((MemoryStream)QoiEncoderStream.Encode(qoiImage, new MemoryStream(qoiImage.Data))).ToArray();
        Assert.Equal(QoiEncoderReference.Encode(qoiImage), qoiData);

        var img = QoiDecoder.Decode(qoiData);
        Assert.True(img.Data.SequenceEqual(qoiImage.Data));
        Assert.Equal(img.Width, qoiImage.Width);
        Assert.Equal(img.Height, qoiImage.Height);
        Assert.Equal(img.Channels, qoiImage.Channels);
        Assert.Equal(img.ColorSpace, qoiImage.ColorSpace);
    }

    [Fact]
    public void RgbEncodingShouldWork()
    {
        var qoiImage = new QoiImage(_pngData, 8, 4, Channels.Rgb);

        byte[] qoiData = ((MemoryStream)QoiEncoderStream.Encode(qoiImage, new MemoryStream(qoiImage.Data))).ToArray();
        Assert.Equal(QoiEncoderReference.Encode(qoiImage), qoiData);

        // Assert
        var img = QoiDecoder.Decode(qoiData);
        Assert.True(img.Data.SequenceEqual(_pngData));
        Assert.Equal(_qoiData, _qoiData);
        Assert.Equal(img.Width, qoiImage.Width);
        Assert.Equal(img.Height, qoiImage.Height);
        Assert.Equal(img.Channels, qoiImage.Channels);
        Assert.Equal(img.ColorSpace, qoiImage.ColorSpace);
    }

    private static QoiImage CreateExampleData(string dataType, int height, int width)
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

    private static byte[] _pngData = [
        130,   0, 212, 124, 204,  88,  79,  26, 210, 104, 117,   4,
        137, 191,  80, 204,  65, 175,  38, 160, 207, 182, 174,  59,
         83,  18, 227,   4, 234, 150,  97, 131,  62,  95, 167, 236,
        132, 143,  78, 175,  86, 172, 237, 113, 195,  87, 227, 242,
         13, 189, 125,  33,  16,  79, 165, 247, 216, 193, 192, 113,
        254, 176, 172, 227,  94, 105, 146, 232, 150,  39, 148, 238,
        105,  65,  23,   4,  33, 252, 243, 111, 120,  32, 150, 144,
         96,  66,   9, 102, 226, 245, 145, 153, 240, 183,  60, 132
       ];
    private static byte[] _qoiData = [
       113, 111, 105, 102,   0,   0,   0,   8,   0,   0,   0,   4,   3,   0, 254, 130,
      0, 212, 254, 124, 204,  88, 254,  79,  26, 210, 254, 104, 117,   4, 254, 137,
    191,  80, 254, 204,  65, 175, 254,  38, 160, 207, 254, 182, 174,  59, 254,  83,
     18, 227, 254,   4, 234, 150, 254,  97, 131,  62, 254,  95, 167, 236, 254, 132,
    143,  78, 254, 175,  86, 172, 254, 237, 113, 195, 254,  87, 227, 242, 254,  13,
    189, 125, 254,  33,  16,  79, 254, 165, 247, 216, 254, 193, 192, 113, 254, 254,
    176, 172, 254, 227,  94, 105, 254, 146, 232, 150, 254,  39, 148, 238, 254, 105,
     65,  23, 254,   4,  33, 252, 254, 243, 111, 120, 254,  32, 150, 144, 254,  96,
     66,   9, 254, 102, 226, 245, 254, 145, 153, 240, 254, 183,  60, 132,   0,   0,
      0,   0,   0,   0,   0,   1
    ];
}