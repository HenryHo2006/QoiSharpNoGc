using QoiSharp.Codec;
using QoiSharp.Exceptions;

namespace QoiSharp;

/// <summary>
/// QOI encoder.
/// </summary>
public static class QoiEncoder
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

        if (image.Height == 0 || image.Height >= QoiCodec.MaxPixels / (long)image.Width)
        {
            throw new QoiEncodingException($"Invalid height: {image.Height}. Maximum for this image is {QoiCodec.MaxPixels / image.Width - 1}");
        }

        long imagePixelCount = image.Width * (long)image.Height;
        byte channels = (byte)image.Channels;
        long pixelsLength = imagePixelCount * channels;
        byte[] pixels = image.Data;

        if (pixels.Length != pixelsLength)
        {
            throw new QoiEncodingException($"Invalid pixel data length: {pixels.Length}. Expected: {pixelsLength}");
        }

        byte[] bytes = new byte[QoiCodec.HeaderSize + QoiCodec.Padding.Length + (imagePixelCount * (channels + 1))];
        QoiEncoderInternal.WriteHeader(bytes, image);
        int p = QoiCodec.HeaderSize;

        (_, int run, p) = channels == 4
            ? QoiEncoderInternal.RunRgbaCompression(pixels, bytes, p, pixels.Length, 0, 255, new int[QoiCodec.HashTableSize])
            : QoiEncoderInternal.RunRgbCompression(pixels, bytes, p, pixels.Length, 0, 0, new int[QoiCodec.HashTableSize]);
        //Check if the last pixel was a run, if so, write it out
        if (run > 0)
        {
            bytes[p++] = (byte)(QoiCodec.Run | (run - 1));
        }
        //Add Padding
        for (int padIdx = 0; padIdx < QoiCodec.Padding.Length; padIdx++)
        {
            bytes[p + padIdx] = QoiCodec.Padding[padIdx];
        }
        //return relevant bytes
        p += QoiCodec.Padding.Length;
        return bytes[..p];
    }
}
