using System.Runtime.CompilerServices;
using QoiSharp.Codec;
using QoiSharp.Exceptions;

namespace QoiSharp;

/// <summary>
/// QOI encoder.
/// </summary>
public static class QoiEncoderFaster
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
        byte colorSpace = (byte)image.ColorSpace;
        byte[] pixels = image.Data;

        byte[] bytes = new byte[QoiCodec.HeaderSize + QoiCodec.Padding.Length + (width * height * (channels + 1))];

        WriteHeader(bytes, width, height, channels, colorSpace);
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

        int[] intIndex = new int[QoiCodec.HashTableSize];

        int prevI = 255;
        int i = 255;

        int run = 0;
        int p = QoiCodec.HeaderSize;
        bool hasAlpha = channels == 4;

        int pixelsLength = width * height * channels;

        for (int pxPos = 0; pxPos < pixelsLength; pxPos += channels)
        {
             i = pixels[pxPos] << 24 | pixels[pxPos + 1] << 16 | pixels[pxPos + 2] << 8;
            if (hasAlpha)
            {
                i |= pixels[pxPos + 3];
            }
            else
            {
                i |= 255; // Default alpha
            }

            if (prevI == i)
            {
                run++;
                if (run == 62)
                {
                    bytes[p++] = (byte)(QoiCodec.Run | (run - 1));
                    run = 0;
                }
            }
            else
            {
                if (run > 0)
                {
                    bytes[p++] = (byte)(QoiCodec.Run | (run - 1));
                    run = 0;
                }

                int indexPos = QoiCodec.CalculateHashTableRgbaIndex(i);

                if (i == intIndex[indexPos])
                {
                    bytes[p++] = (byte)(QoiCodec.Index | (indexPos));
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
                            bytes[p++] = (byte)(QoiCodec.Diff | (vr + 2) << 4 | (vg + 2) << 2 | (vb + 2));
                        }
                        else
                        {
                            int vgr = vr - vg;
                            int vgb = vb - vg;
                            if (vgr is > -9 and < 8 &&
                                 vg is > -33 and < 32 &&
                                 vgb is > -9 and < 8)
                            {
                                bytes[p++] = (byte)(QoiCodec.Luma | (vg + 32));
                                bytes[p++] = (byte)((vgr + 8) << 4 | (vgb + 8));
                            }
                            else
                            {
                                bytes[p++] = QoiCodec.Rgb;
                                bytes[p++] = (byte)(i >> 24);
                                bytes[p++] = (byte)(i >> 16);
                                bytes[p++] = (byte)(i >> 8);
                            }
                        }
                    }
                    else
                    {
                        bytes[p++] = QoiCodec.Rgba;
                        bytes[p++] = (byte)(i >> 24);
                        bytes[p++] = (byte)(i >> 16);
                        bytes[p++] = (byte)(i >> 8);
                        bytes[p++] = (byte)i;
                    }
                }
            }
            prevI = i;
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteHeader(byte[] bytes, int width, int height, byte channels, byte colorSpace)
    {
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
    }
}