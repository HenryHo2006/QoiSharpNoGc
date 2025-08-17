using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using QoiSharp.Codec;

namespace QoiSharp;

/// <summary>
/// QOI encoder.
/// </summary>
internal static class QoiEncoderInternal
{
    /// <summary>
    /// Encodes raw rgba pixel data into QOI.
    /// </summary>  
    internal static (int previousPixel, int run, int bytesPos) RunRgbaCompression
        (byte[] pixelsToCompress, byte[] outputBytes, int bytesPos, int pixelsLength, int run, int previousPixel, Span<int> pixelHashTable)
    {
        int currentPixel;
        for (int pxPos = 0; pxPos < pixelsLength; pxPos += 4)
        {
            currentPixel = BinaryPrimitives.ReadInt32BigEndian(pixelsToCompress.AsSpan(pxPos, 4));
            if (previousPixel == currentPixel)
            {
                run++;
                if (run == 62)
                {
                    outputBytes[bytesPos++] = (byte)(QoiCodec.Run | (run - 1));
                    run = 0;
                }
            }
            else
            {
                if (run > 0)
                {
                    outputBytes[bytesPos++] = (byte)(QoiCodec.Run | (run - 1));
                    run = 0;
                }
                int indexPos = QoiCodec.CalculateHashTableRgbaIndex(currentPixel);
                if (currentPixel == pixelHashTable[indexPos])
                {
                    outputBytes[bytesPos++] = (byte)(QoiCodec.Index | (indexPos));
                }
                else
                {
                    pixelHashTable[indexPos] = currentPixel;
                    if ((currentPixel & 0xFF) == (previousPixel & 0xFF))
                    {
                        int vr = (currentPixel >> 24) - (previousPixel >> 24) + 2;
                        int vg = ((currentPixel >> 16) & 0xFF) - ((previousPixel >> 16) & 0xFF) + 2;
                        int vb = ((currentPixel >> 8) & 0xFF) - ((previousPixel >> 8) & 0xFF) + 2;
                        if ((uint)vr < 4 &&
                            (uint)vg < 4 &&
                            (uint)vb < 4)
                        {
                            outputBytes[bytesPos++] = (byte)(QoiCodec.Diff | vr << 4 | vg << 2 | vb);
                        }
                        else
                        {
                            int vgr = vr - vg + 8;
                            int vgb = vb - vg + 8;
                            vg += 30; // -2 from the previous calculation and +32 to fit into the range of -32 to 31
                            if ((uint)vgr < 16 &&
                                 (uint)vgb < 16 &&
                                 (uint)vg < 64)
                            {
                                outputBytes[bytesPos++] = (byte)(QoiCodec.Luma | vg);
                                outputBytes[bytesPos++] = (byte)(vgr << 4 | vgb);
                            }
                            else
                            {
                                outputBytes[bytesPos++] = QoiCodec.Rgb;
                                outputBytes[bytesPos++] = (byte)(currentPixel >> 24);
                                outputBytes[bytesPos++] = (byte)(currentPixel >> 16);
                                outputBytes[bytesPos++] = (byte)(currentPixel >> 8);
                            }
                        }
                    }
                    else
                    {
                        outputBytes[bytesPos++] = QoiCodec.Rgba;
                        BinaryPrimitives.WriteInt32BigEndian(outputBytes.AsSpan(bytesPos, 4), currentPixel);
                        bytesPos += 4;
                    }
                }
            }
            previousPixel = currentPixel;
        }
        return (previousPixel, run, bytesPos);
    }

    /// <summary>
    /// Encodes raw rgba pixel data into QOI.
    /// </summary>  
    internal static (int previousPixel, int run, int bytesPos) RunRgbaCompression
        (ReadOnlySpan<byte> pixelsToCompress, Span<byte> outputBytes, int bytesPos, int run, int previousPixel, Span<int> pixelHashTable)
    {
        for (int pxPos = 0; pxPos < pixelsToCompress.Length ; pxPos += 4)
        {
            var currentPixel = BinaryPrimitives.ReadInt32BigEndian(pixelsToCompress.Slice(pxPos, 4));
            if (previousPixel == currentPixel)
            {
                run++;
                if (run == 62)
                {
                    outputBytes[bytesPos++] = (byte)(QoiCodec.Run | (run - 1));
                    run = 0;
                }
            }
            else
            {
                if (run > 0)
                {
                    outputBytes[bytesPos++] = (byte)(QoiCodec.Run | (run - 1));
                    run = 0;
                }
                int indexPos = QoiCodec.CalculateHashTableRgbaIndex(currentPixel);
                if (currentPixel == pixelHashTable[indexPos])
                {
                    outputBytes[bytesPos++] = (byte)(QoiCodec.Index | (indexPos));
                }
                else
                {
                    pixelHashTable[indexPos] = currentPixel;
                    if ((currentPixel & 0xFF) == (previousPixel & 0xFF))
                    {
                        int vr = (currentPixel >> 24) - (previousPixel >> 24) + 2;
                        int vg = ((currentPixel >> 16) & 0xFF) - ((previousPixel >> 16) & 0xFF) + 2;
                        int vb = ((currentPixel >> 8) & 0xFF) - ((previousPixel >> 8) & 0xFF) + 2;
                        if ((uint)vr < 4 &&
                            (uint)vg < 4 &&
                            (uint)vb < 4)
                        {
                            outputBytes[bytesPos++] = (byte)(QoiCodec.Diff | vr << 4 | vg << 2 | vb);
                        }
                        else
                        {
                            int vgr = vr - vg + 8;
                            int vgb = vb - vg + 8;
                            vg += 30; // -2 from the previous calculation and +32 to fit into the range of -32 to 31
                            if ((uint)vgr < 16 &&
                                 (uint)vgb < 16 &&
                                 (uint)vg < 64)
                            {
                                outputBytes[bytesPos++] = (byte)(QoiCodec.Luma | vg);
                                outputBytes[bytesPos++] = (byte)(vgr << 4 | vgb);
                            }
                            else
                            {
                                outputBytes[bytesPos++] = QoiCodec.Rgb;
                                outputBytes[bytesPos++] = (byte)(currentPixel >> 24);
                                outputBytes[bytesPos++] = (byte)(currentPixel >> 16);
                                outputBytes[bytesPos++] = (byte)(currentPixel >> 8);
                            }
                        }
                    }
                    else
                    {
                        outputBytes[bytesPos++] = QoiCodec.Rgba;
                        BinaryPrimitives.WriteInt32BigEndian(outputBytes.Slice(bytesPos, 4), currentPixel);
                        bytesPos += 4;
                    }
                }
            }
            previousPixel = currentPixel;
        }
        return (previousPixel, run, bytesPos);
    }

    /// <summary>
    /// Encodes raw rgb pixel data into QOI.
    /// </summary>  
    internal static (int previousPixel, int run, int bytesPos) RunRgbCompression
        (byte[] pixelsToCompress, byte[] outputBytes, int bytesPos, int pixelsLength, int run, int previousPixel, Span<int> pixelHashTable)
    {
        int currentPixel;
        for (int pxPos = 0; pxPos < pixelsLength; pxPos += 3)
        {
            currentPixel = pixelsToCompress[pxPos] << 16 | pixelsToCompress[pxPos + 1] << 8 | pixelsToCompress[pxPos + 2];
            if (previousPixel == currentPixel)
            {
                run++;
                if (run == 62)
                {
                    outputBytes[bytesPos++] = (byte)(QoiCodec.Run | (run - 1));
                    run = 0;
                }
            }
            else
            {
                if (run > 0)
                {
                    outputBytes[bytesPos++] = (byte)(QoiCodec.Run | (run - 1));
                    run = 0;
                }

                int indexPos = CalculateHashTableRgbIndex(currentPixel);
                if (currentPixel == pixelHashTable[indexPos])
                {
                    outputBytes[bytesPos++] = (byte)(QoiCodec.Index | (indexPos));
                }
                else
                {
                    pixelHashTable[indexPos] = currentPixel;
                    int vr = (currentPixel >> 16) - (previousPixel >> 16) + 2;
                    int vg = ((currentPixel >> 8) & 0xFF) - ((previousPixel >> 8) & 0xFF) + 2;
                    int vb = (currentPixel & 0xFF) - (previousPixel & 0xFF) + 2;
                    if ((uint)vr < 4 &&
                        (uint)vg < 4 &&
                        (uint)vb < 4)
                    {
                        outputBytes[bytesPos++] = (byte)(QoiCodec.Diff | vr << 4 | vg << 2 | vb);
                    }
                    else
                    {
                        int vgr = vr - vg + 8;
                        int vgb = vb - vg + 8;
                        vg += 30; // -2 from the previous calculation and +32 to fit into the range of -32 to 31
                        if ((uint)vgr < 16 &&
                             (uint)vgb < 16 &&
                             (uint)vg < 64)
                        {
                            outputBytes[bytesPos++] = (byte)(QoiCodec.Luma | vg);
                            outputBytes[bytesPos++] = (byte)(vgr << 4 | vgb);
                        }
                        else
                        {
                            BinaryPrimitives.WriteInt32BigEndian(outputBytes.AsSpan(bytesPos, 4), currentPixel | (QoiCodec.Rgb << 24));
                            bytesPos += 4;
                        }
                    }
                }
            }
            previousPixel = currentPixel;
        }
        return (previousPixel, run, bytesPos);
    }

    /// <summary>
    /// Encodes raw rgb pixel data into QOI.
    /// </summary>  
    internal static (int previousPixel, int run, int bytesPos) RunRgbCompression
        (ReadOnlySpan<byte> pixelsToCompress, Span<byte> outputBytes, int bytesPos, int run, int previousPixel, Span<int> pixelHashTable)
    {
        for (int pxPos = 0; pxPos < pixelsToCompress.Length; pxPos += 3)
        {
            var currentPixel = pixelsToCompress[pxPos] << 16 | pixelsToCompress[pxPos + 1] << 8 | pixelsToCompress[pxPos + 2];
            if (previousPixel == currentPixel)
            {
                run++;
                if (run == 62)
                {
                    outputBytes[bytesPos++] = (byte)(QoiCodec.Run | (run - 1));
                    run = 0;
                }
            }
            else
            {
                if (run > 0)
                {
                    outputBytes[bytesPos++] = (byte)(QoiCodec.Run | (run - 1));
                    run = 0;
                }

                int indexPos = CalculateHashTableRgbIndex(currentPixel);
                if (currentPixel == pixelHashTable[indexPos])
                {
                    outputBytes[bytesPos++] = (byte)(QoiCodec.Index | (indexPos));
                }
                else
                {
                    pixelHashTable[indexPos] = currentPixel;
                    int vr = (currentPixel >> 16) - (previousPixel >> 16) + 2;
                    int vg = ((currentPixel >> 8) & 0xFF) - ((previousPixel >> 8) & 0xFF) + 2;
                    int vb = (currentPixel & 0xFF) - (previousPixel & 0xFF) + 2;
                    if ((uint)vr < 4 &&
                        (uint)vg < 4 &&
                        (uint)vb < 4)
                    {
                        outputBytes[bytesPos++] = (byte)(QoiCodec.Diff | vr << 4 | vg << 2 | vb);
                    }
                    else
                    {
                        int vgr = vr - vg + 8;
                        int vgb = vb - vg + 8;
                        vg += 30; // -2 from the previous calculation and +32 to fit into the range of -32 to 31
                        if ((uint)vgr < 16 &&
                             (uint)vgb < 16 &&
                             (uint)vg < 64)
                        {
                            outputBytes[bytesPos++] = (byte)(QoiCodec.Luma | vg);
                            outputBytes[bytesPos++] = (byte)(vgr << 4 | vgb);
                        }
                        else
                        {
                            BinaryPrimitives.WriteInt32BigEndian(outputBytes.Slice(bytesPos, 4), currentPixel | (QoiCodec.Rgb << 24));
                            bytesPos += 4;
                        }
                    }
                }
            }
            previousPixel = currentPixel;
        }
        return (previousPixel, run, bytesPos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateHashTableRgbIndex(int packedPixel)
    {
        // Extract components and calculate hash in one expression
        return (((packedPixel >> 16) * 3) +
                (((packedPixel >> 8) & 0xFF) * 5) +
                ((packedPixel & 0xFF) * 7) + 2805/*result of Alpha 255 * 11*/) & 63;
    }

    /// <summary>
    /// Writes the QOI header to the output byte array.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteHeader(byte[] outputBytes, QoiImage image)
    {
        WriteHeader(outputBytes, image.Width, image.Height, image.Channels, image.ColorSpace);
    }

    /// <summary>
    /// Writes the QOI header to the output byte array.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteHeader(byte[] outputBytes, int width, int height, Channels channels, ColorSpace colorSpace)
    {
        outputBytes[0] = (byte)(QoiCodec.Magic >> 24);
        outputBytes[1] = (byte)(QoiCodec.Magic >> 16);
        outputBytes[2] = (byte)(QoiCodec.Magic >> 8);
        outputBytes[3] = (byte)QoiCodec.Magic;

        outputBytes[4] = (byte)(width >> 24);
        outputBytes[5] = (byte)(width >> 16);
        outputBytes[6] = (byte)(width >> 8);
        outputBytes[7] = (byte)width;

        outputBytes[8] = (byte)(height >> 24);
        outputBytes[9] = (byte)(height >> 16);
        outputBytes[10] = (byte)(height >> 8);
        outputBytes[11] = (byte)height;

        outputBytes[12] = (byte)channels;
        outputBytes[13] = (byte)colorSpace;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteHeader(Span<byte> outputBytes, int width, int height, Channels channels, ColorSpace colorSpace)
    {
        outputBytes[0] = (byte)(QoiCodec.Magic >> 24);
        outputBytes[1] = (byte)(QoiCodec.Magic >> 16);
        outputBytes[2] = (byte)(QoiCodec.Magic >> 8);
        outputBytes[3] = (byte)QoiCodec.Magic;

        outputBytes[4] = (byte)(width >> 24);
        outputBytes[5] = (byte)(width >> 16);
        outputBytes[6] = (byte)(width >> 8);
        outputBytes[7] = (byte)width;

        outputBytes[8] = (byte)(height >> 24);
        outputBytes[9] = (byte)(height >> 16);
        outputBytes[10] = (byte)(height >> 8);
        outputBytes[11] = (byte)height;

        outputBytes[12] = (byte)channels;
        outputBytes[13] = (byte)colorSpace;
    }

}
