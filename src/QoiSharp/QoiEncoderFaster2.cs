using System.Runtime.CompilerServices;
using System.Buffers.Binary;
using QoiSharp.Codec;
using QoiSharp.Exceptions;

namespace QoiSharp;

/// <summary>
/// QOI encoder.
/// </summary>
public static class QoiEncoderFaster2
{
    /// <summary>
    /// Encodes raw pixel data into QOI.
    /// </summary>
    /// <param name="image">QOI image.</param>
    /// <returns>Encoded image.</returns>
    /// <exception cref="QoiEncodingException">Thrown when image information is invalid.</exception>
    public static byte[] Encode(QoiImage image)
    {
        if (image.Width == 0)
        {
            throw new QoiEncodingException($"Invalid width: {image.Width}");
        }

        if (image.Height == 0 || image.Height >= QoiCodec.MaxPixels / image.Width)
        {
            throw new QoiEncodingException($"Invalid height: {image.Height}. Maximum for this image is {QoiCodec.MaxPixels / image.Width - 1}");
        }

        int width = image.Width;
        int height = image.Height;
        byte channels = (byte)image.Channels;
        byte[] pixels = image.Data;

        byte[] bytes = new byte[QoiCodec.HeaderSize + QoiCodec.Padding.Length + (width * height * (channels + 1))];

        QoiEncoderInternal.WriteHeader(bytes, width, height, image.Channels, image.ColorSpace);

        int p = QoiCodec.HeaderSize;
        int pixelsLength = width * height * channels;
        int run = 0;
        if (channels == 4)
        {
            (_, run, p) = QoiEncoderInternal.RunRgbaCompression(pixels, bytes, p, pixelsLength, 0, 255, new int[QoiCodec.HashTableSize]);
        }
        else
        {
            (_, run, p) = QoiEncoderInternal.RunRgbCompression(pixels, bytes, p, pixelsLength, 0, 0, new int[QoiCodec.HashTableSize]);
        }
        if (run > 0)
        {
            bytes[p++] = (byte)(QoiCodec.Run | (run - 1));
        }
        for (int padIdx = 0; padIdx < QoiCodec.Padding.Length; padIdx++)
        {
            bytes[p + padIdx] = QoiCodec.Padding[padIdx];
        }
        p += QoiCodec.Padding.Length;
        return bytes[..p];
    }
}
