using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using QoiSharp.Codec;
using QoiSharp.Exceptions;

namespace QoiSharp;

/// <summary>
/// QOI encoder.
/// </summary>
public static class QoiEncoderStream
{
    /// <summary>
    /// Encodes raw pixel data into QOI.
    /// </summary>
    /// <param name="image">QOI image.</param>
    /// <returns>Encoded image.</returns>
    /// <exception cref="QoiEncodingException">Thrown when image information is invalid.</exception>
    public static Stream Encode(QoiImage image, Stream imageByteStream)
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
        byte colorSpace = (byte)image.ColorSpace;
        var bufferSize = 30000; //1020 bytes as a minimal size 
        var readArraySize = channels == 4 ? (bufferSize / 5 * 4) : (bufferSize / 4 * 3);
        byte[] pixels = new byte[readArraySize];

        var outputStream = new MemoryStream();
        var header = new byte[QoiCodec.HeaderSize];
        QoiEncoderInternal.WriteHeader(header, width, height, image.Channels, image.ColorSpace);
        outputStream.Write(header, 0, QoiCodec.HeaderSize);

        byte[] outputBytesBuffer = new byte[bufferSize];
        int read = 0;
        int run = 0;
        int bytesWritten = 0;
        int prevI = channels == 4 ? 255 : 0;
        int[] intIndex = new int[QoiCodec.HashTableSize];
        do
        {
            read = imageByteStream.Read(pixels, 0, pixels.Length);
            if (read == 0)
            {
                break; // End of stream
            }
            if (channels == 4)
            {
                (prevI, run, bytesWritten) = QoiEncoderInternal.RunRgbaCompression(pixels, outputBytesBuffer, 0, read, run, prevI, intIndex);
                outputStream.Write(outputBytesBuffer, 0, bytesWritten);
            }
            else
            {
                (prevI, run, bytesWritten) = QoiEncoderInternal.RunRgbCompression(pixels, outputBytesBuffer, 0, read, run, prevI, intIndex);
                outputStream.Write(outputBytesBuffer, 0, bytesWritten);
            }
        }
        while (read == readArraySize);
        if (run > 0)
        {
            outputStream.WriteByte((byte)(QoiCodec.Run | (run - 1)));
        }
        outputStream.Write(QoiCodec.Padding, 0, QoiCodec.Padding.Length);
        return outputStream;
    }
}
