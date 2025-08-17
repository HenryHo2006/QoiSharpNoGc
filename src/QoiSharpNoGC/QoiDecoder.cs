using System.Buffers.Binary;
using System.Runtime.CompilerServices;

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
            throw new QoiDecodingException("Invalid file magic");
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

        int currentPixel = 255;

        for (int pxPos = 0; pxPos < pixels.Length; pxPos += channels)
        {
            byte b1 = qoiData[p++];
            if (b1 >> 6 == 3)
            {
                if (b1 == QoiCodec.Rgb)
                {
                    currentPixel = qoiData[p++] << 24 | qoiData[p++] << 16 | qoiData[p++] << 8 | (currentPixel & 0xFF);
                }
                else if (b1 == QoiCodec.Rgba)
                {
                    currentPixel = BinaryPrimitives.ReadInt32BigEndian(qoiData.AsSpan(p, 4));
                    p += 4;
                }
                else
                {
                    var runLength = b1 & 0x3F;
                    for (int i = runLength; i > 0; i--)
                    {
                        SetPixels(channels, pixels, currentPixel, pxPos);
                        pxPos += channels;
                    }
                    SetPixels(channels, pixels, currentPixel, pxPos);
                    continue;
                }
            }
            else
            {
                byte r;
                byte g;
                byte b;
                if ((b1 & QoiCodec.Mask2) == QoiCodec.Diff)
                {
                    r = (byte)(currentPixel >> 24);
                    g = (byte)(currentPixel >> 16);
                    b = (byte)(currentPixel >> 8);
                    r += (byte)(((b1 >> 4) & 0x03) - 2);
                    g += (byte)(((b1 >> 2) & 0x03) - 2);
                    b += (byte)((b1 & 0x03) - 2);
                    currentPixel = r << 24 | g << 16 | b << 8 | (currentPixel & 0xFF);
                }
                else if ((b1 & QoiCodec.Mask2) == QoiCodec.Luma)
                {
                    int b2 = qoiData[p++];
                    int vg = (b1 & 0x3F) - 32;
                    r = (byte)(currentPixel >> 24);
                    g = (byte)(currentPixel >> 16);
                    b = (byte)(currentPixel >> 8);
                    r += (byte)(vg - 8 + ((b2 >> 4) & 0x0F));
                    g += (byte)vg;
                    b += (byte)(vg - 8 + (b2 & 0x0F));
                    currentPixel = r << 24 | g << 16 | b << 8 | (currentPixel & 0xFF);
                }
                else //b1 is an QoiCodec.Index
                {
                    currentPixel = intIndex[b1 & ~QoiCodec.Mask2];
                    SetPixels(channels, pixels, currentPixel, pxPos);
                    continue;
                }
            }
            var indexPos3 = QoiCodec.CalculateHashTableRgbaIndex(currentPixel);
            intIndex[indexPos3] = currentPixel;

            SetPixels(channels, pixels, currentPixel, pxPos);
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

    /// <summary>
    /// Decodes QOI data into raw pixel data.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="image"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="channels"></param>
    /// <param name="color_space"></param>
    /// <exception cref="QoiDecodingException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static void Decode(ReadOnlySpan<byte> buffer, Span<byte> image, out int width, out int height,
        out Channels channels, out ColorSpace color_space)
    {
        if (buffer.Length < QoiCodec.HeaderSize + QoiCodec.Padding.Length)
            throw new QoiDecodingException("File too short");

        if (!QoiCodec.IsValidMagic(buffer.Slice(0, 4)))
            throw new QoiDecodingException("Invalid file magic");

        width = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(4, 4));
        height = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(8, 4));
        channels = (Channels)buffer[12];
        color_space = (ColorSpace)buffer[13];

        if (width == 0)
            throw new QoiDecodingException($"Invalid width: {width}");
        if (height == 0 || height >= QoiCodec.MaxPixels / width)
            throw new QoiDecodingException($"Invalid height: {height}. Maximum for this image is {QoiCodec.MaxPixels / width - 1}");
        if (channels is not Channels.Rgb and not Channels.RgbWithAlpha)
            throw new QoiDecodingException($"Invalid number of channels: {channels}");

        int[] intIndex = new int[QoiCodec.HashTableSize];
        if (channels == Channels.Rgb)
        {
            for (int indexPos = 0; indexPos < intIndex.Length; indexPos++)
            {
                intIndex[indexPos] = 255;
            }
        }

        int p = QoiCodec.HeaderSize;

        int currentPixel = 255;

        for (int pxPos = 0; pxPos < image.Length; pxPos += (byte)channels)
        {
            byte b1 = buffer[p++];
            if (b1 >> 6 == 3)
            {
                if (b1 == QoiCodec.Rgb)
                {
                    currentPixel = buffer[p++] << 24 | buffer[p++] << 16 | buffer[p++] << 8 | (currentPixel & 0xFF);
                }
                else if (b1 == QoiCodec.Rgba)
                {
                    currentPixel = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(p, 4));
                    p += 4;
                }
                else
                {
                    var runLength = b1 & 0x3F;
                    for (int i = runLength; i > 0; i--)
                    {
                        SetPixels(channels, image.Slice(pxPos, (byte)channels), currentPixel);
                        pxPos += (byte)channels;
                    }
                    SetPixels(channels, image.Slice(pxPos, (byte)channels), currentPixel);
                    continue;
                }
            }
            else
            {
                byte r, g, b;
                if ((b1 & QoiCodec.Mask2) == QoiCodec.Diff)
                {
                    r = (byte)(currentPixel >> 24);
                    g = (byte)(currentPixel >> 16);
                    b = (byte)(currentPixel >> 8);
                    r += (byte)(((b1 >> 4) & 0x03) - 2);
                    g += (byte)(((b1 >> 2) & 0x03) - 2);
                    b += (byte)((b1 & 0x03) - 2);
                    currentPixel = r << 24 | g << 16 | b << 8 | (currentPixel & 0xFF);
                }
                else if ((b1 & QoiCodec.Mask2) == QoiCodec.Luma)
                {
                    int b2 = buffer[p++];
                    int vg = (b1 & 0x3F) - 32;
                    r = (byte)(currentPixel >> 24);
                    g = (byte)(currentPixel >> 16);
                    b = (byte)(currentPixel >> 8);
                    r += (byte)(vg - 8 + ((b2 >> 4) & 0x0F));
                    g += (byte)vg;
                    b += (byte)(vg - 8 + (b2 & 0x0F));
                    currentPixel = r << 24 | g << 16 | b << 8 | (currentPixel & 0xFF);
                }
                else //b1 is an QoiCodec.Index
                {
                    currentPixel = intIndex[b1 & ~QoiCodec.Mask2];
                    SetPixels(channels, image.Slice(pxPos, (byte)channels), currentPixel);
                    continue;
                }
            }
            var indexPos3 = QoiCodec.CalculateHashTableRgbaIndex(currentPixel);
            intIndex[indexPos3] = currentPixel;

            SetPixels(channels, image.Slice(pxPos, (byte)channels), currentPixel);
        }

        int pixelsEnd = buffer.Length - QoiCodec.Padding.Length;
        for (int padIdx = 0; padIdx < QoiCodec.Padding.Length; padIdx++)
        {
            if (buffer[pixelsEnd + padIdx] != QoiCodec.Padding[padIdx])
            {
                throw new InvalidOperationException("Invalid padding");
            }
        }
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetPixels(Channels channels, Span<byte> pixels, int rgba)
    {
        pixels[0] = (byte)(rgba >> 24);
        pixels[1] = (byte)(rgba >> 16);
        pixels[2] = (byte)(rgba >> 8);
        if ((byte)channels == 4)
        {
            pixels[3] = (byte)rgba;
        }
    }
}
