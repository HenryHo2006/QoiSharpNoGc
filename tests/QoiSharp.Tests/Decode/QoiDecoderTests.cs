using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using QoiSharpNoGC;
using QoiSharpNoGC.Codec;
using QoiSharp.Encoding.Tests;
using QoiSharpNoGC.Exceptions;
using Xunit;

namespace QoiSharp.Decoding.Tests;

public class QoiDecoderTests
{
  [Fact]
  [ExcludeFromCodeCoverage(Justification = "Error with Assert.Throws")]
  public void Decode_WrongHeader()
  {
    //Length to short
    Assert.Throws<QoiDecodingException>(() => QoiDecoder.Decode([]));
    //Magic not matching
    Assert.Throws<QoiDecodingException>(() => QoiDecoder.Decode(new byte[50]));
    //With valid magic string
    var header = new byte[50];
    BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), QoiCodec.Magic);
    Assert.Throws<QoiDecodingException>(() => QoiDecoder.Decode(header));
    //With valid width setting
    BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), 200);
    Assert.Throws<QoiDecodingException>(() => QoiDecoder.Decode(header));
    //With huge valid height setting
    BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(8, 4), int.MaxValue / 2);
    Assert.Throws<QoiDecodingException>(() => QoiDecoder.Decode(header));
    //With normal valid height setting, chanels missing
    BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(8, 4), 50);
    Assert.Throws<QoiDecodingException>(() => QoiDecoder.Decode(header));
  }

  [Fact]
  [ExcludeFromCodeCoverage(Justification = "Error with Assert.Throws")]
  public void Decode_InvalidPadding()
  {
    byte[] data = { QoiCodec.Rgb, 12, 13, 14, QoiCodec.Run | 2 };
    byte[] qoiImageData = CreateQoiImageData(data, Channels.Rgb);
    qoiImageData[^1] = 200;
    Assert.Throws<InvalidOperationException>(() => QoiDecoder.Decode(qoiImageData));
  }

  [Fact]
  public void RgbDecode_Run()
  {
    byte[] data = { QoiCodec.Rgb, 12, 13, 14, QoiCodec.Run | 2 };
    byte[] qoiImageData = CreateQoiImageData(data, Channels.Rgb);
    var decoded = QoiDecoder.Decode(qoiImageData);
    Assert.Equal(2, decoded.Width);
    Assert.Equal(2, decoded.Height);
    Assert.Equal(4 * 3, decoded.Data.Length);
    Assert.Equal(new byte[] {
         12, 13, 14, 12, 13, 14,
         12, 13, 14, 12, 13, 14 },
       decoded.Data);

  }

  [Fact]
  public void RgbaDecode_Run()
  {
    byte[] data = { QoiCodec.Rgba, 12, 13, 14, 200, QoiCodec.Run | 2 };
    byte[] qoiImageData = CreateQoiImageData(data, Channels.RgbWithAlpha);
    var decoded = QoiDecoder.Decode(qoiImageData);
    Assert.Equal(2, decoded.Width);
    Assert.Equal(2, decoded.Height);
    Assert.Equal(4 * 4, decoded.Data.Length);
    Assert.Equal(new byte[] {
         12, 13, 14, 200, 12, 13, 14, 200,
         12, 13, 14, 200, 12, 13, 14, 200 },
       decoded.Data);
  }

  [Fact]
  public void RgbDecode_Diff()
  {
    byte[] data = {
      QoiCodec.Rgb, 112, 113, 114,
      90, 90, 127
    };
    byte[] qoiImageData = CreateQoiImageData(data, Channels.Rgb);
    var decoded = QoiDecoder.Decode(qoiImageData);
    Assert.Equal(2, decoded.Width);
    Assert.Equal(2, decoded.Height);
    Assert.Equal(QoiEncoderReference.Encode(decoded).AsSpan(14, 7), data.AsSpan(0, 7));
    Assert.Equal(new byte[] {
         112, 113, 114, 111, 113, 114,
         110, 113, 114, 111, 114, 115  },
       decoded.Data);
  }

  [Fact]
  public void RgbDecode_Luma()
  {
    byte[] data = {
     QoiCodec.Rgb, 112, 113, 114,
      170,136,170,136,151,120
    };
    byte[] qoiImageData = CreateQoiImageData(data, Channels.Rgb);
    var decoded = QoiDecoder.Decode(qoiImageData);
    Assert.Equal(2, decoded.Width);
    Assert.Equal(2, decoded.Height);
    Assert.Equal(QoiEncoderReference.Encode(decoded).AsSpan(14, 10), data.AsSpan(0, 10));
    Assert.Equal(new byte[] {
         112, 113, 114, 122, 123, 124,
         132, 133, 134, 122, 124, 125 },
       decoded.Data);
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
    var decoded = QoiDecoder.Decode(qoiImageData);
    Assert.Equal(2, decoded.Width);
    Assert.Equal(2, decoded.Height);
    Assert.Equal(QoiEncoderReference.Encode(decoded).AsSpan(14, 10), data.AsSpan(0, 10));
    Assert.Equal(new byte[] {
         112, 113, 114, 50, 190, 3,
         112, 113, 114, 50, 190, 3},
       decoded.Data);
  }

  [Fact]
  public void RgbEncodingShouldWork()
  {
    var qoiImage = new QoiImage(_pngData, 8, 4, Channels.Rgb);
    byte[] qoiData = QoiEncoder.Encode(qoiImage);
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
    var decoded = QoiDecoder.Decode(qoiData);
    Assert.Equal(imageBytes, decoded.Data);
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
