using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using QoiSharp.Codec;
using QoiSharp.Exceptions;

namespace QoiSharp;

/// <summary>
/// QOI decoder.
/// </summary>
public static class QoiDecoder
{
    /// <summary>
    /// Decodes QOI data into raw pixel data.
    /// </summary>
    /// <param name="qoiData">QOI data</param>
    /// <returns>Decoding result.</returns>
    /// <exception cref="QoiDecodingException">Thrown when data is invalid.</exception>
    public static QoiImage Decode(byte[] qoiData)
    {
        if (qoiData.Length < QoiCodec.HeaderSize + QoiCodec.Padding.Length)
        {
            throw new QoiDecodingException("File too short");
        }

        if (!QoiCodec.IsValidMagic(qoiData[..4]))
        {
            throw new QoiDecodingException("Invalid file magic"); // TODO: add magic value
        }

        int width = BinaryPrimitives.ReadInt32BigEndian(qoiData.AsSpan(4, 4));
        int height = BinaryPrimitives.ReadInt32BigEndian(qoiData.AsSpan(8, 4));
        byte channels = qoiData[12];
        var colorSpace = (ColorSpace)qoiData[13];

        if (width == 0)
        {
            throw new QoiDecodingException($"Invalid width: {width}");
        }
        if (height == 0 || height >= QoiCodec.MaxPixels / width)
        {
            throw new QoiDecodingException($"Invalid height: {height}. Maximum for this image is {QoiCodec.MaxPixels / width - 1}");
        }
        if (channels is not 3 and not 4)
        {
            throw new QoiDecodingException($"Invalid number of channels: {channels}");
        }

        int[] intIndex = new int[QoiCodec.HashTableSize];
        if (channels == 3) 
        {
            for (int indexPos = 0; indexPos < intIndex.Length; indexPos++)
            {
                intIndex[indexPos] = 255;
            }
        }

        byte[] pixels = new byte[width * height * channels];
        int p = QoiCodec.HeaderSize;

        int rgba = 255;
        int runLength;

        byte r = 0;
        byte g = 0;
        byte b = 0;
        for (int pxPos = 0; pxPos < pixels.Length; pxPos += channels)
        {
            byte b1 = qoiData[p++];
            if (b1 >> 6 == 3)
            {
                if (b1 == QoiCodec.Rgb)
                {
                    rgba = qoiData[p++] << 24 | qoiData[p++] << 16 | qoiData[p++] << 8 | (rgba & 0xFF);
                }
                else if (b1 == QoiCodec.Rgba)
                {
                    rgba = BinaryPrimitives.ReadInt32BigEndian(qoiData.AsSpan(p, 4));
                    p += 4;
                }
                else
                {
                    runLength = b1 & 0x3F;
                    for (int i = runLength; i > 0; i--)
                    {
                        SetPixels(channels, pixels, rgba, pxPos);
                        pxPos += channels;
                    }
                    SetPixels(channels, pixels, rgba, pxPos);
                    continue;
                }
            }
            else
            {
                if ((b1 & QoiCodec.Mask2) == QoiCodec.Diff)
                {
                    r = (byte)(rgba >> 24);
                    g = (byte)(rgba >> 16);
                    b = (byte)(rgba >> 8);
                    r += (byte)(((b1 >> 4) & 0x03) - 2);
                    g += (byte)(((b1 >> 2) & 0x03) - 2);
                    b += (byte)((b1 & 0x03) - 2);
                    rgba = r << 24 | g << 16 | b << 8 | (rgba & 0xFF);
                }
                else if ((b1 & QoiCodec.Mask2) == QoiCodec.Luma)
                {
                    int b2 = qoiData[p++];
                    int vg = (b1 & 0x3F) - 32;
                    r = (byte)(rgba >> 24);
                    g = (byte)(rgba >> 16);
                    b = (byte)(rgba >> 8);
                    r += (byte)(vg - 8 + ((b2 >> 4) & 0x0F));
                    g += (byte)vg;
                    b += (byte)(vg - 8 + (b2 & 0x0F));
                    rgba = r << 24 | g << 16 | b << 8 | (rgba & 0xFF);
                }
                else //b1 is an index
                {
                    rgba = intIndex[b1 & ~QoiCodec.Mask2];
                    SetPixels(channels, pixels, rgba, pxPos);
                    continue;
                }
            }
            var indexPos3 = QoiCodec.CalculateHashTableRgbaIndex(rgba);
            intIndex[indexPos3] = rgba;

            SetPixels(channels, pixels, rgba, pxPos);
        }

        int pixelsEnd = qoiData.Length - QoiCodec.Padding.Length;
        for (int padIdx = 0; padIdx < QoiCodec.Padding.Length; padIdx++)
        {
            if (qoiData[pixelsEnd + padIdx] != QoiCodec.Padding[padIdx])
            {
                throw new InvalidOperationException("Invalid padding");
            }
        }

        return new QoiImage(pixels, width, height, (Channels)channels, colorSpace);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetPixels(byte channels, byte[] pixels, int rgba, int pxPos)
    {
        pixels[pxPos] = (byte)(rgba >> 24);
        pixels[pxPos + 1] = (byte)(rgba >> 16);
        pixels[pxPos + 2] = (byte)(rgba >> 8);
        if (channels == 4)
        {
            pixels[pxPos + 3] = (byte)rgba;
        }
    }
}
