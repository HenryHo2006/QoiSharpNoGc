using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using QoiSharp.Codec;
using QoiSharp.Decoding.Tests;
using QoiSharp.Exceptions;
using Xunit;

namespace QoiSharp.Encoding.Tests;

public class QoiEncoderTests
{

    [Fact]
    public void RgbEncoding_SingleColor_QOI_OP_RUN()
    {
        byte[] imageBytes = [
            12 ,34, 65, 12 ,34, 65, 12 ,34, 65,
            12 ,34, 65, 12 ,34, 65, 12 ,34, 65
        ];

        var qoiImage = new QoiImage(imageBytes, 3, 2, Channels.Rgb);

        byte[] qoiData = QoiEncoder.Encode(qoiImage);
        Assert.Equal(QoiCodec.HeaderSize + 4 + 1 + QoiCodec.Padding.Length, qoiData.Length);
        Assert.Equal(128 + 64 + 4, qoiData[14 + 4]); //Check run byte
        Assert.Equal(QoiEncoderReference.Encode(qoiImage), qoiData);

        var img = QoiDecoderReference.Decode(qoiData);
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

        byte[] qoiData = QoiEncoder.Encode(qoiImage);
        Assert.Equal(QoiCodec.HeaderSize + 4 + 5 + QoiCodec.Padding.Length, qoiData.Length);
        Assert.Equal(127, qoiData[14 + 4]); //Check diff byte
        Assert.Equal(qoiData[14 + 4], qoiData[14 + 4 + 1]);
        Assert.Equal(64 + 16 + 4 + 1, qoiData[14 + 4 + 4]);
        Assert.Equal(QoiEncoderReference.Encode(qoiImage), qoiData);

        var img = QoiDecoderReference.Decode(qoiData);
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
            12 ,14, 15, 22 ,24, 25,
            22 ,24, 25, 13 ,14, 15,
        ];

        var qoiImage = new QoiImage(imageBytes, 2, 2, Channels.Rgb);

        byte[] qoiData = QoiEncoder.Encode(qoiImage);
        Assert.Equal(QoiEncoderReference.Encode(qoiImage), qoiData);
        Assert.Equal(QoiCodec.HeaderSize + 4 + 3 + QoiCodec.Padding.Length, qoiData.Length);
        Assert.Equal(128 + 64, qoiData[14 + 4]); //Check luma byte1
        Assert.Equal(128 + 16 + 4 + 2, qoiData[14 + 5]); //Check luma byte2

        var img = QoiDecoderReference.Decode(qoiData);
        Assert.True(img.Data.SequenceEqual(imageBytes));
        Assert.Equal(img.Width, qoiImage.Width);
        Assert.Equal(img.Height, qoiImage.Height);
        Assert.Equal(img.Channels, qoiImage.Channels);
        Assert.Equal(img.ColorSpace, qoiImage.ColorSpace);
    }

    [Fact]
    public void RgbaEncoding_QOI_OP_LUMA()
    {
        byte[] imageBytes = [
            12 ,14, 15, 255, 22 ,24, 25, 255,
            22 ,24, 25, 255, 13 ,14, 15, 255
        ];

        var qoiImage = new QoiImage(imageBytes, 2, 2, Channels.RgbWithAlpha);

        byte[] qoiData = QoiEncoder.Encode(qoiImage);
        Assert.Equal(QoiEncoderReference.Encode(qoiImage), qoiData);
        Assert.Equal(QoiCodec.HeaderSize + 4 + 3 + QoiCodec.Padding.Length, qoiData.Length);

        var img = QoiDecoderReference.Decode(qoiData);
        Assert.True(img.Data.SequenceEqual(imageBytes));
    }

    [Fact]
    public void RgbEncoding_Index()
    {
        byte[] imageBytes = [
            12 ,34, 65, 222 ,144, 75, 12 ,34, 65,
            222,144,75, 12 ,34, 65, 222 ,144, 75,
        ];

        var qoiImage = new QoiImage(imageBytes, 3, 2, Channels.Rgb);

        byte[] qoiData = QoiEncoder.Encode(qoiImage);
        Assert.Equal(QoiCodec.HeaderSize + 8 + 4 + QoiCodec.Padding.Length, qoiData.Length);
        Assert.Equal(8 + 2, qoiData[14 + 8]); //Check index byte
        Assert.Equal(QoiEncoderReference.Encode(qoiImage), qoiData);

        var img = QoiDecoderReference.Decode(qoiData);
        Assert.True(img.Data.SequenceEqual(imageBytes));
        Assert.True(img.Data.SequenceEqual(imageBytes));
        Assert.Equal(img.Width, qoiImage.Width);
        Assert.Equal(img.Height, qoiImage.Height);
        Assert.Equal(img.Channels, qoiImage.Channels);
        Assert.Equal(img.ColorSpace, qoiImage.ColorSpace);
    }

    [Fact]
    public void RgbEncodingShouldWork()
    {
        var qoiImage = new QoiImage(_pixelData, 8, 4, Channels.Rgb);

        byte[] qoiData = QoiEncoder.Encode(qoiImage);
        Assert.Equal(QoiEncoderReference.Encode(qoiImage), qoiData);

        // Assert
        var img = QoiDecoderReference.Decode(qoiData);
        Assert.True(img.Data.SequenceEqual(_pixelData));
        Assert.Equal(_qoiData, _qoiData);
        Assert.Equal(img.Width, qoiImage.Width);
        Assert.Equal(img.Height, qoiImage.Height);
        Assert.Equal(img.Channels, qoiImage.Channels);
        Assert.Equal(img.ColorSpace, qoiImage.ColorSpace);
    }

    [Fact]
    public void RgbEncoding_Index99Length()
    {
        byte[] imageBytes = new byte[300];
        for (int i = 0; i < imageBytes.Length; i += 3)
        {
            imageBytes[i] = 12;
            imageBytes[i + 1] = 34;
            imageBytes[i + 2] = 65;
        }
        var qoiImage = new QoiImage(imageBytes, 10, 10, Channels.Rgb);

        byte[] qoiData = QoiEncoder.Encode(qoiImage);
        Assert.Equal(QoiEncoderReference.Encode(qoiImage), qoiData);
        Assert.Equal(QoiCodec.HeaderSize + 4 + 2 + QoiCodec.Padding.Length, qoiData.Length);

        var img = QoiDecoderReference.Decode(qoiData);
        Assert.True(img.Data.SequenceEqual(imageBytes));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    [InlineData(2,2)]
    [InlineData(int.MaxValue / 2, int.MaxValue / 2)]
    [ExcludeFromCodeCoverage(Justification = "Error with Assert.Throws")]
    public void RgbEncoding_Exceptions(int width, int length)
    {
        byte[] imageBytes = new byte[3];
        var qoiImage = new QoiImage(imageBytes, width, length, Channels.Rgb);

        Assert.Throws<QoiEncodingException>(() => QoiEncoder.Encode(qoiImage));
    }

    [Fact]
    public void RgbEncoding_IndexThenDiff()
    {
        byte[] imageBytes = [
            12 ,34, 65, 12 ,34, 65,
            12 ,34, 65, 13 ,34, 65
        ];
        var qoiImage = new QoiImage(imageBytes, 2,2, Channels.Rgb);

        byte[] qoiData = QoiEncoder.Encode(qoiImage);
        Assert.Equal(QoiCodec.HeaderSize + 4 + 2 + QoiCodec.Padding.Length, qoiData.Length);
        Assert.Equal(QoiEncoderReference.Encode(qoiImage), qoiData);
        var img = QoiDecoderReference.Decode(qoiData);
        Assert.True(img.Data.SequenceEqual(imageBytes));
    }

    [Fact]
    public void RgbaEncoding_IndexThenDiff()
    {
        byte[] imageBytes = [
            12 ,34, 65, 255, 12 ,34, 65, 255,
            12 ,34, 65, 255, 13 ,34, 65, 255
        ];
        var qoiImage = new QoiImage(imageBytes, 2,2, Channels.RgbWithAlpha);

        byte[] qoiData = QoiEncoder.Encode(qoiImage);
        Assert.Equal(QoiCodec.HeaderSize + 4 + 2 + QoiCodec.Padding.Length, qoiData.Length);
        Assert.Equal(QoiEncoderReference.Encode(qoiImage), qoiData);
        var img = QoiDecoderReference.Decode(qoiData);
        Assert.True(img.Data.SequenceEqual(imageBytes));
    }

    [Fact]
    public void RgbaEncoding_Index99Length()
    {
        byte[] imageBytes = new byte[400];
        for (int i = 0; i < imageBytes.Length; i += 4)
        {
            imageBytes[i] = 12;
            imageBytes[i + 1] = 34;
            imageBytes[i + 2] = 65;
            imageBytes[i + 3] = 255;
        }
        var qoiImage = new QoiImage(imageBytes, 10, 10, Channels.RgbWithAlpha);

        byte[] qoiData = QoiEncoder.Encode(qoiImage);
        Assert.Equal(QoiEncoderReference.Encode(qoiImage), qoiData);
        Assert.Equal(QoiCodec.HeaderSize + 4 + 2 + QoiCodec.Padding.Length, qoiData.Length);

        var img = QoiDecoderReference.Decode(qoiData);
        Assert.True(img.Data.SequenceEqual(imageBytes));
    }

    private static byte[] _pixelData = [
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