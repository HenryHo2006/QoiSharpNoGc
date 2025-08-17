using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using QoiSharp.Codec;
using QoiSharp.Encoding.Tests;
using QoiSharp.Exceptions;
using QoiSharp.Tests;
using Xunit;

namespace QoiSharp.Decoding.Tests;

public class QoiDecoderStreamTests
{
  [Fact]
  [ExcludeFromCodeCoverage(Justification = "Error with Assert.Throws")]
  public void Decode_WrongHeader()
  {
    //Length to short
    Assert.Throws<QoiDecodingException>(() => new QoiDecoderStream(new MemoryStream()));
    //Magic not matching
    var header = new byte[50];
    Assert.Throws<QoiDecodingException>(() => new QoiDecoderStream(new MemoryStream(header)));
    //With valid magic string
    BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), QoiCodec.Magic);
    Assert.Throws<QoiDecodingException>(() => new QoiDecoderStream(new MemoryStream(header)));
    //With valid width setting
    BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), 200);
    Assert.Throws<QoiDecodingException>(() => new QoiDecoderStream(new MemoryStream(header)));
    //With huge valid height setting
    BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(8, 4), int.MaxValue / 2);
    Assert.Throws<QoiDecodingException>(() => new QoiDecoderStream(new MemoryStream(header)));
    //With normal valid height setting, chanels missing
    BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(8, 4), 50);
    Assert.Throws<QoiDecodingException>(() => new QoiDecoderStream(new MemoryStream(header)));
    var data = _qoiData.AsSpan(0, 50).ToArray();
    Assert.Throws<QoiDecodingException>(() => new QoiDecoderStream(new MemoryStream(data)).ToByteArray());

  }

  [Fact]
  [ExcludeFromCodeCoverage(Justification = "Error with Assert.Throws")]
  public void Decode_InvalidPadding()
  {
    byte[] data = { QoiCodec.Rgb, 12, 13, 14, QoiCodec.Run | 2 };
    byte[] qoiImageData = CreateQoiImageData(data, Channels.Rgb);
    qoiImageData[^1] = 200;
    Assert.Throws<QoiDecodingException>(() => new QoiDecoderStream(new MemoryStream(qoiImageData)).ToByteArray());
    qoiImageData = [.. qoiImageData.Take(qoiImageData.Length - 1)];
    Assert.Throws<QoiDecodingException>(() => new QoiDecoderStream(new MemoryStream(qoiImageData)).ToByteArray());
  }

  [Fact]
  public void RgbDecode_Run()
  {
    byte[] data = { QoiCodec.Rgb, 12, 13, 14, QoiCodec.Run | 2 };
    byte[] qoiImageData = CreateQoiImageData(data, Channels.Rgb);
    var decoded = new QoiDecoderStream(new MemoryStream(qoiImageData));
    Assert.Equal(2, decoded.Width);
    Assert.Equal(2, decoded.Height);
    var result = decoded.ToByteArray();
    Assert.Equal(4 * 3, result.Length);
    Assert.Equal(new byte[] {
         12, 13, 14, 12, 13, 14,
         12, 13, 14, 12, 13, 14 },
       result);
  }

  [Fact]
  public void RgbaDecode_Run()
  {
    byte[] data = { QoiCodec.Rgba, 12, 13, 14, 200, QoiCodec.Run | 2 };
    byte[] qoiImageData = CreateQoiImageData(data, Channels.RgbWithAlpha);
    var decoded = new QoiDecoderStream(new MemoryStream(qoiImageData));
    Assert.Equal(2, decoded.Width);
    Assert.Equal(2, decoded.Height);
    var result = decoded.ToByteArray();
    Assert.Equal(4 * 4, result.Length);
    Assert.Equal(new byte[] {
         12, 13, 14, 200, 12, 13, 14, 200,
         12, 13, 14, 200, 12, 13, 14, 200 },
       result);
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
    byte[] qoiData = QoiEncoderReference.Encode(qoiImage);
    var decoded = new QoiDecoderStream(new MemoryStream(qoiData));
    Assert.Equal(imageBytes, decoded.ToByteArray());
    decoded = new QoiDecoderStream(new MemoryStream(qoiData));
    Assert.Equal(imageBytes, decoded.ReturnByteByByte().ToArray());
    decoded = new QoiDecoderStream(new MemoryStream(qoiData));
    Assert.Equal(imageBytes, decoded.ReadFiveBytesAtATime().ToArray());
  }

  [Fact]
  public void RgbDecode_Diff()
  {
    byte[] data = {
      QoiCodec.Rgb, 112, 113, 114,
      90, 90, 127
    };
    byte[] qoiImageData = CreateQoiImageData(data, Channels.Rgb);
    var decoded = new QoiDecoderStream(new MemoryStream(qoiImageData));
    Assert.Equal(2, decoded.Width);
    Assert.Equal(2, decoded.Height);
    var result = decoded.ToByteArray();
    Assert.Equal(QoiEncoderReference.Encode(new QoiImage(result, decoded.Width, decoded.Height, decoded.Channels))
      .AsSpan(14, 7), data.AsSpan(0, 7));
    Assert.Equal(new byte[] {
         112, 113, 114, 111, 113, 114,
         110, 113, 114, 111, 114, 115  },
       result);
  }

  [Fact]
  public void RgbDecode_Luma()
  {
    byte[] data = {
     QoiCodec.Rgb, 112, 113, 114,
      170,136,170,136,151,120
    };
    byte[] qoiImageData = CreateQoiImageData(data, Channels.Rgb);
    var decoded = new QoiDecoderStream(new MemoryStream(qoiImageData));
    Assert.Equal(2, decoded.Width);
    Assert.Equal(2, decoded.Height);
    var result = decoded.ToByteArray();
    Assert.Equal(QoiEncoderReference.Encode(new QoiImage(result, decoded.Width, decoded.Height, decoded.Channels))
      .AsSpan(14, 7), data.AsSpan(0, 7));
    Assert.Equal(new byte[] {
         112, 113, 114, 122, 123, 124,
         132, 133, 134, 122, 124, 125 },
       result);
  }

  [Fact]
  public void RgbDecode_Index()
  {
    byte[] data = {
     QoiCodec.Rgb, 112, 113, 114,
     QoiCodec.Rgb, 50, 190, 3,
      24,22
    };
    byte[] qoiImageData = CreateQoiImageData(data, Channels.Rgb);
    var decoded = new QoiDecoderStream(new MemoryStream(qoiImageData));
    Assert.Equal(2, decoded.Width);
    Assert.Equal(2, decoded.Height);
    var result = decoded.ToByteArray();
    Assert.Equal(QoiEncoderReference.Encode(new QoiImage(result, decoded.Width, decoded.Height, decoded.Channels))
      .AsSpan(14, 10), data.AsSpan(0, 10));
    Assert.Equal(new byte[] {
         112, 113, 114, 50, 190, 3,
         112, 113, 114, 50, 190, 3},
       result);
  }

  [Fact]
  public void RgbEncodingShouldWork()
  {
    var qoiImage = new QoiImage(_pngData, 8, 4, Channels.Rgb);
    byte[] qoiData = QoiEncoder.Encode(qoiImage);
    Assert.Equal(QoiEncoderReference.Encode(qoiImage), qoiData);

    // Assert
    var img = new QoiDecoderStream(new MemoryStream(qoiData));
    Assert.True(img.ToByteArray().SequenceEqual(_pngData));
    Assert.Equal(_qoiData, _qoiData);
    Assert.Equal(img.Width, qoiImage.Width);
    Assert.Equal(img.Height, qoiImage.Height);
    Assert.Equal(img.Channels, qoiImage.Channels);
    Assert.Equal(img.ColorSpace, qoiImage.ColorSpace);
  }

  [Theory]
  [InlineData(nameof(QoiCodec.Rgb))]
  [InlineData(nameof(QoiCodec.Luma))]
  [InlineData(nameof(QoiCodec.Index))]
  [InlineData(nameof(QoiCodec.Rgba))]
  [InlineData(nameof(QoiCodec.Run))]
  public void RgbEncoding_Big(string dataType)
  {
    QoiImage qoiImage = Helper.CreateExampleData(dataType, 108, 90);
    var qoiStream = new QoiEncoderStream(new MemoryStream(qoiImage.Data), 108, 90, qoiImage.Channels);
    var stream = new QoiDecoderStream(qoiStream);
    var decoded = stream.ToByteArray();
    Assert.Equal(qoiImage.Data, decoded);

    Assert.Equal(stream.Width, qoiImage.Width);
    Assert.Equal(stream.Height, qoiImage.Height);
    Assert.Equal(stream.Channels, qoiImage.Channels);
    Assert.Equal(stream.ColorSpace, qoiImage.ColorSpace);
  }


  [Fact]
  public void CanSeek()
  {
    var stream = new QoiDecoderStream(new MemoryStream(_qoiData));
    stream.Flush();
    Assert.False(stream.CanSeek);
    Assert.False(stream.CanWrite);
  }

  [Fact]
  [ExcludeFromCodeCoverage(Justification = "Error with Assert.Throws")]
  public void NonAvailableStreamFunctions()
  {
    var stream = new QoiDecoderStream(new MemoryStream(_qoiData));
    Assert.Throws<NotSupportedException>(() => stream.Length);
    Assert.Throws<NotSupportedException>(() => stream.Position);
    Assert.Throws<NotSupportedException>(() => stream.Position = 34);
    Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
    Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
    Assert.Throws<NotSupportedException>(() => stream.Write(new byte[0], 0, 1));
  }

  private static byte[] CreateQoiImageData(byte[] playload, Channels channels)
  {
    var qoiImageData = new byte[QoiCodec.HeaderSize + QoiCodec.Padding.Length + playload.Length];
    BinaryPrimitives.WriteInt32BigEndian(qoiImageData.AsSpan(0, 4), QoiCodec.Magic);
    BinaryPrimitives.WriteInt32BigEndian(qoiImageData.AsSpan(4, 4), 2);
    BinaryPrimitives.WriteInt32BigEndian(qoiImageData.AsSpan(8, 4), 2);
    qoiImageData[12] = (byte)channels; //Channels

    playload.AsSpan().CopyTo(qoiImageData.AsSpan(QoiCodec.HeaderSize, playload.Length));
    QoiCodec.Padding.AsSpan().CopyTo(qoiImageData.AsSpan(qoiImageData.Length - QoiCodec.Padding.Length, QoiCodec.Padding.Length));

    return qoiImageData;
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
