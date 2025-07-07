using System.Buffers.Binary;
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
        (byte[] pixelsToCompress, byte[] outputBytes, int bytesPos, int pixelsLength, int run, int previousPixel, int[] pixelHashTable)
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
                        int vr = (currentPixel >> 24) - (previousPixel >> 24);
                        int vg = ((currentPixel >> 16) & 0xFF) - ((previousPixel >> 16) & 0xFF);
                        int vb = ((currentPixel >> 8) & 0xFF) - ((previousPixel >> 8) & 0xFF);
                        if (vr is > -3 and < 2 &&
                            vg is > -3 and < 2 &&
                            vb is > -3 and < 2)
                        {
                            outputBytes[bytesPos++] = (byte)(QoiCodec.Diff | (vr + 2) << 4 | (vg + 2) << 2 | (vb + 2));
                        }
                        else
                        {
                            int vgr = vr - vg;
                            int vgb = vb - vg;
                            if (vgr is > -9 and < 8 &&
                                 vg is > -33 and < 32 &&
                                 vgb is > -9 and < 8)
                            {
                                outputBytes[bytesPos++] = (byte)(QoiCodec.Luma | (vg + 32));
                                outputBytes[bytesPos++] = (byte)((vgr + 8) << 4 | (vgb + 8));
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
    /// Encodes raw rgb pixel data into QOI.
    /// </summary>  
    internal static (int previousPixel, int run, int bytesPos) RunRgbCompression
        (byte[] pixelsToCompress, byte[] outputBytes, int bytesPos, int pixelsLength, int run, int previousPixel, int[] pixelHashTable)
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

                int indexPos = QoiCodec.CalculateHashTableRgbIndex(currentPixel);
                if (currentPixel == pixelHashTable[indexPos])
                {
                    outputBytes[bytesPos++] = (byte)(QoiCodec.Index | (indexPos));
                }
                else
                {
                    pixelHashTable[indexPos] = currentPixel;
                    int vr = (currentPixel >> 16) - (previousPixel >> 16);
                    int vg = ((currentPixel >> 8) & 0xFF) - ((previousPixel >> 8) & 0xFF);
                    int vb = (currentPixel & 0xFF) - (previousPixel & 0xFF);

                    if (vr is > -3 and < 2 &&
                        vg is > -3 and < 2 &&
                        vb is > -3 and < 2)
                    {
                        outputBytes[bytesPos++] = (byte)(QoiCodec.Diff | (vr + 2) << 4 | (vg + 2) << 2 | (vb + 2));
                    }
                    else
                    {
                        int vgr = vr - vg;
                        int vgb = vb - vg;
                        if (vgr is > -9 and < 8 &&
                             vg is > -33 and < 32 &&
                             vgb is > -9 and < 8)
                        {
                            outputBytes[bytesPos++] = (byte)(QoiCodec.Luma | (vg + 32));
                            outputBytes[bytesPos++] = (byte)((vgr + 8) << 4 | (vgb + 8));
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
    /// Writes the QOI header to the output byte array.
    /// </summary>
    internal static void WriteHeader(byte[] outputBytes, QoiImage image)
    {
        outputBytes[0] = (byte)(QoiCodec.Magic >> 24);
        outputBytes[1] = (byte)(QoiCodec.Magic >> 16);
        outputBytes[2] = (byte)(QoiCodec.Magic >> 8);
        outputBytes[3] = (byte)QoiCodec.Magic;

        var width = image.Width;
        outputBytes[4] = (byte)(width >> 24);
        outputBytes[5] = (byte)(width >> 16);
        outputBytes[6] = (byte)(width >> 8);
        outputBytes[7] = (byte)width;

        var height = image.Height;
        outputBytes[8] = (byte)(height >> 24);
        outputBytes[9] = (byte)(height >> 16);
        outputBytes[10] = (byte)(height >> 8);
        outputBytes[11] = (byte)height;

        outputBytes[12] = (byte)image.Channels;
        outputBytes[13] = (byte)image.ColorSpace;
    }
}
