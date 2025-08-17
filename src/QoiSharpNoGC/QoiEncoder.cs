using QoiSharpNoGC.Codec;
using QoiSharpNoGC.Exceptions;

namespace QoiSharpNoGC;

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
            ? QoiEncoderInternal.RunRgbaCompression(pixels, bytes, p, pixels.Length, 0, 255, stackalloc int[QoiCodec.HashTableSize])
            : QoiEncoderInternal.RunRgbCompression(pixels, bytes, p, pixels.Length, 0, 0, stackalloc int[QoiCodec.HashTableSize]);
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

    /// <summary>
    /// Encode with external buffer, without new alloc
    /// </summary>
    /// <param name="image"></param>
    /// <param name="stride"></param>
    /// <param name="roi_width"></param>
    /// <param name="roi_height"></param>
    /// <param name="buffer"></param>
    /// <param name="channels"></param>
    /// <param name="color_space"></param>
    /// <returns>return encoder used length</returns>
    public static int Encode(ReadOnlySpan<byte> image, int stride, int roi_width, int roi_height,
        Span<byte> buffer, Channels channels = Channels.Rgb, ColorSpace color_space = ColorSpace.Linear)
    {
        if (roi_width == 0)
            throw new QoiEncodingException($"Invalid width: {roi_width}");

        if (roi_height == 0 || roi_height >= QoiCodec.MaxPixels / roi_width)
            throw new QoiEncodingException($"Invalid height: {roi_height}. Maximum for this image is {QoiCodec.MaxPixels / roi_width - 1}");

        if(stride < roi_width * (byte)channels)
            throw new QoiEncodingException($"Invalid stride: {stride}, minimum is {roi_width * (byte)channels}");

        if (stride != roi_width * (byte)channels)
            throw new NotImplementedException("support different stride in future");   // todo

        if (image.Length < stride * roi_height)
            throw new QoiEncodingException($"Invalid pixel data length: {image.Length}. Expected: {stride * roi_height}");

        QoiEncoderInternal.WriteHeader(buffer, roi_width, roi_height, channels, color_space);
        int p = QoiCodec.HeaderSize;

        (_, int run, p) = channels == Channels.RgbWithAlpha
            ? QoiEncoderInternal.RunRgbaCompression(image, buffer, p, 0, 255, stackalloc int[QoiCodec.HashTableSize])
            : QoiEncoderInternal.RunRgbCompression(image, buffer, p, 0, 0, stackalloc int[QoiCodec.HashTableSize]);
        //Check if the last pixel was a run, if so, write it out
        if (run > 0)
        {
            buffer[p++] = (byte)(QoiCodec.Run | (run - 1));
        }
        //Add Padding
        for (int padIdx = 0; padIdx < QoiCodec.Padding.Length; padIdx++)
        {
            buffer[p + padIdx] = QoiCodec.Padding[padIdx];
        }
        //return relevant bytes
        p += QoiCodec.Padding.Length;
        return p;
    }

}
