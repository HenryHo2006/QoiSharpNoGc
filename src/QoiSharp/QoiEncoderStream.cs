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
        var bufferSize = 300; //1020 bytes as a minimal size 
        var readArraySize = channels == 4 ? (bufferSize / 5 * 4) : (bufferSize / 4 * 3);
        byte[] pixels = new byte[readArraySize];

        var outputStream = new MemoryStream();
        WriteHeader(outputStream, width, height, channels, colorSpace);

        byte[] bytes = new byte[bufferSize];
        int read = 0;
        int run = 0;
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
                (prevI, run) = RunRgbaCompression(pixels, read, bytes, outputStream, intIndex, prevI, run);
            }
            else
            {
                (prevI, run) = RunRgbCompression(pixels, read, bytes, outputStream, intIndex, prevI, run);
            }
        }
        while (read == readArraySize);

        if (run > 0)
        {
            outputStream.WriteByte((byte)(QoiCodec.Run | (run - 1)));
        }
        outputStream.Write(QoiCodec.Padding, 0, QoiCodec.Padding.Length);
        outputStream.Position = 0;
        return outputStream;
    }

    private static (int, int) RunRgbaCompression(byte[] pixels, int pixelsLength, byte[] outputBytes, MemoryStream outputStream,
        int[] intIndex, int prevI, int run)
    {
        int p = 0;
        int i = 0;
        for (int pxPos = 0; pxPos < pixelsLength; pxPos += 4)
        {
            i = pixels[pxPos] << 24 | pixels[pxPos + 1] << 16 | pixels[pxPos + 2] << 8 | pixels[pxPos + 3];
            if (prevI == i)
            {
                run++;
                if (run == 62)
                {
                    outputBytes[p++] = (byte)(QoiCodec.Run | (run - 1));
                    run = 0;
                }
            }
            else
            {
                if (run > 0)
                {
                    outputBytes[p++] = (byte)(QoiCodec.Run | (run - 1));
                    run = 0;
                }
                int indexPos = QoiCodec.CalculateHashTableIndex(i);
                if (i == intIndex[indexPos])
                {
                    outputBytes[p++] = (byte)(QoiCodec.Index | (indexPos));
                }
                else
                {
                    intIndex[indexPos] = i;
                    if ((i & 0xFF) == (prevI & 0xFF))
                    {
                        int vr = (i >> 24) - (prevI >> 24);
                        int vg = ((i >> 16) & 0xFF) - ((prevI >> 16) & 0xFF);
                        int vb = ((i >> 8) & 0xFF) - ((prevI >> 8) & 0xFF);
                        if (vr is > -3 and < 2 &&
                            vg is > -3 and < 2 &&
                            vb is > -3 and < 2)
                        {
                            outputBytes[p++] = (byte)(QoiCodec.Diff | (vr + 2) << 4 | (vg + 2) << 2 | (vb + 2));
                        }
                        else
                        {
                            int vgr = vr - vg;
                            int vgb = vb - vg;
                            if (vgr is > -9 and < 8 &&
                                 vg is > -33 and < 32 &&
                                 vgb is > -9 and < 8)
                            {
                                outputBytes[p++] = (byte)(QoiCodec.Luma | (vg + 32));
                                outputBytes[p++] = (byte)((vgr + 8) << 4 | (vgb + 8));
                            }
                            else
                            {
                                outputBytes[p++] = QoiCodec.Rgb;
                                outputBytes[p++] = (byte)(i >> 24);
                                outputBytes[p++] = (byte)(i >> 16);
                                outputBytes[p++] = (byte)(i >> 8);
                            }
                        }
                    }
                    else
                    {
                        outputBytes[p++] = QoiCodec.Rgba;
                        outputBytes[p++] = (byte)(i >> 24);
                        outputBytes[p++] = (byte)(i >> 16);
                        outputBytes[p++] = (byte)(i >> 8);
                        outputBytes[p++] = (byte)i;
                    }
                }
            }
            prevI = i;
        }
        outputStream.Write(outputBytes, 0, p);
        return (prevI, run);
    }

    private static (int, int) RunRgbCompression(byte[] pixels, int pixelsLength, byte[] outputBytes, MemoryStream outputStream,
        int[] intIndex, int prevI, int run)
    {
        int p = 0;
        int i = 0;
        for (int pxPos = 0; pxPos < pixelsLength; pxPos += 3)
        {
            i = pixels[pxPos] << 16 | pixels[pxPos + 1] << 8 | pixels[pxPos + 2];
            if (prevI == i)
            {
                run++;
                if (run == 62)
                {
                    outputBytes[p++] = (byte)(QoiCodec.Run | (run - 1));
                    run = 0;
                }
            }
            else
            {
                if (run > 0)
                {
                    outputBytes[p++] = (byte)(QoiCodec.Run | (run - 1));
                    run = 0;
                }
                int indexPos = QoiCodec.CalculateHashTableRgbIndex(i);
                if (i == intIndex[indexPos])
                {
                    outputBytes[p++] = (byte)(QoiCodec.Index | (indexPos));
                }
                else
                {
                    intIndex[indexPos] = i;
                    int vr = (i >> 16) - (prevI >> 16);
                    int vg = ((i >> 8) & 0xFF) - ((prevI >> 8) & 0xFF);
                    int vb = (i & 0xFF) - (prevI & 0xFF);
                    if (vr is > -3 and < 2 &&
                        vg is > -3 and < 2 &&
                        vb is > -3 and < 2)
                    {
                        outputBytes[p++] = (byte)(QoiCodec.Diff | (vr + 2) << 4 | (vg + 2) << 2 | (vb + 2));
                    }
                    else
                    {
                        int vgr = vr - vg;
                        int vgb = vb - vg;
                        if (vgr is > -9 and < 8 &&
                             vg is > -33 and < 32 &&
                             vgb is > -9 and < 8)
                        {
                            outputBytes[p++] = (byte)(QoiCodec.Luma | (vg + 32));
                            outputBytes[p++] = (byte)((vgr + 8) << 4 | (vgb + 8));
                        }
                        else
                        {
                            outputBytes[p++] = QoiCodec.Rgb;
                            outputBytes[p++] = (byte)(i >> 16);
                            outputBytes[p++] = (byte)(i >> 8);
                            outputBytes[p++] = (byte)i;
                        }
                    }
                }
            }
            prevI = i;
        }
        outputStream.Write(outputBytes, 0, p);
        return (prevI, run);
    }

    private static void WriteHeader(MemoryStream outputStream, int width, int height, byte channels, byte colorSpace)
    {
        var bytes = new byte[QoiCodec.HeaderSize];
        bytes[0] = (byte)(QoiCodec.Magic >> 24);
        bytes[1] = (byte)(QoiCodec.Magic >> 16);
        bytes[2] = (byte)(QoiCodec.Magic >> 8);
        bytes[3] = (byte)QoiCodec.Magic;

        bytes[4] = (byte)(width >> 24);
        bytes[5] = (byte)(width >> 16);
        bytes[6] = (byte)(width >> 8);
        bytes[7] = (byte)width;

        bytes[8] = (byte)(height >> 24);
        bytes[9] = (byte)(height >> 16);
        bytes[10] = (byte)(height >> 8);
        bytes[11] = (byte)height;

        bytes[12] = channels;
        bytes[13] = colorSpace;

        outputStream.Write(bytes, 0, bytes.Length);
    }
}
