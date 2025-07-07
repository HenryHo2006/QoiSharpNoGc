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
        if (image.Width < 1)
        {
            throw new QoiEncodingException($"Invalid width: {image.Width}");
        }
        if (image.Height < 1)
        {
            throw new QoiEncodingException($"Invalid height: {image.Height}.");
        }

        byte channels = (byte)image.Channels;
        var bufferSize = 30000; //This number needs to be dividable by 12
        var readArraySize = channels == 4 ? (bufferSize / 5 * 4) : (bufferSize / 4 * 3);
        byte[] pixels = new byte[readArraySize];

        var outputStream = new MemoryStream();
        var header = new byte[QoiCodec.HeaderSize];
        QoiEncoderInternal.WriteHeader(header, image);
        outputStream.Write(header, 0, QoiCodec.HeaderSize);

        byte[] outputBytesBuffer = new byte[bufferSize];
        int read;
        int run = 0;
        int bytesWritten;
        int prevI = channels == 4 ? 255 : 0;
        long readPixels = 0;
        int[] intIndex = new int[QoiCodec.HashTableSize];
        do
        {
            //Read from the image byte stream into the pixel buffer
            read = imageByteStream.Read(pixels, 0, pixels.Length);
            if (read == 0)
            {
                break; // End of stream
            }
            readPixels += read;
            (prevI, run, bytesWritten) = channels == 4
               ? QoiEncoderInternal.RunRgbaCompression(pixels, outputBytesBuffer, 0, read, run, prevI, intIndex)
               : QoiEncoderInternal.RunRgbCompression(pixels, outputBytesBuffer, 0, read, run, prevI, intIndex);
            //Write the output bytes from the encoded block to the stream
            outputStream.Write(outputBytesBuffer, 0, bytesWritten);
        }
        while (read == readArraySize);

        long expectedPixelLength = (long)image.Width * image.Height * channels;
        if (readPixels != expectedPixelLength)
        {
            throw new QoiEncodingException($"Invalid pixel data length: {readPixels}. Expected: {expectedPixelLength}");
        }
        //If the last block was a run, write it out
        if (run > 0)
        {
            outputStream.WriteByte((byte)(QoiCodec.Run | (run - 1)));
        }
        //Write the padding bytes and return the stream
        outputStream.Write(QoiCodec.Padding, 0, QoiCodec.Padding.Length);
        return outputStream;
    }
}
