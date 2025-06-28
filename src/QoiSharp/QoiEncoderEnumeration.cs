using QoiSharp.Codec;
using QoiSharp.Exceptions;

namespace QoiSharp;

/// <summary>
/// QOI encoder.
/// </summary>
public static class QoiEncoderEnumeration
{
    /// <summary>
    /// Encodes raw pixel data into QOI.
    /// </summary>
    /// <param name="image">QOI image.</param>
    /// <returns>Encoded image.</returns>
    /// <exception cref="QoiEncodingException">Thrown when image information is invalid.</exception>
    public static IEnumerable<byte> Encode(QoiImage image)
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


        yield return (byte)(QoiCodec.Magic >> 24);
        yield return (byte)(QoiCodec.Magic >> 16);
        yield return (byte)(QoiCodec.Magic >> 8);
        yield return (byte)QoiCodec.Magic;

        yield return (byte)(width >> 24);
        yield return (byte)(width >> 16);
        yield return (byte)(width >> 8);
        yield return (byte)width;

        yield return (byte)(height >> 24);
        yield return (byte)(height >> 16);
        yield return  (byte)(height >> 8);
        yield return  (byte)height;

        yield return  channels;
        yield return  colorSpace;

        byte[] index = new byte[QoiCodec.HashTableSize * 4];

        byte prevR = 0;
        byte prevG = 0;
        byte prevB = 0;
        byte prevA = 255;

        byte r = 0;
        byte g = 0;
        byte b = 0;
        byte a = 255;

        int run = 0;
        int p = QoiCodec.HeaderSize;
        bool hasAlpha = channels == 4;

        int pixelsLength = width * height * channels;
        int pixelsEnd = pixelsLength - channels;
        int counter = 0;

        for (int pxPos = 0; pxPos < pixelsLength; pxPos += channels)
        {
            r = pixels[pxPos];
            g = pixels[pxPos + 1];
            b = pixels[pxPos + 2];
            if (hasAlpha)
            {
                a = pixels[pxPos + 3];
            }

            if (RgbaEquals(prevR, prevG, prevB, prevA, r, g, b, a))
            {
                run++;
                if (run == 62 || pxPos == pixelsEnd)
                {
                    yield return (byte)(QoiCodec.Run | (run - 1));
                    run = 0;
                }
            }
            else
            {
                if (run > 0)
                {
                   yield return (byte)(QoiCodec.Run | (run - 1));
                    run = 0;
                }

                int indexPos = QoiCodec.CalculateHashTableIndex(r, g, b, a);

                if (RgbaEquals(r, g, b, a, index[indexPos], index[indexPos + 1], index[indexPos + 2], index[indexPos + 3]))
                {
                   yield return (byte)(QoiCodec.Index | (indexPos / 4));
                }
                else
                {
                    index[indexPos] = r;
                    index[indexPos + 1] = g;
                    index[indexPos + 2] = b;
                    index[indexPos + 3] = a;

                    if (a == prevA)
                    {
                        int vr = r - prevR;
                        int vg = g - prevG;
                        int vb = b - prevB;

                        int vgr = vr - vg;
                        int vgb = vb - vg;

                        if (vr is > -3 and < 2 &&
                            vg is > -3 and < 2 &&
                            vb is > -3 and < 2)
                        {
                            counter++;
                           yield return (byte)(QoiCodec.Diff | (vr + 2) << 4 | (vg + 2) << 2 | (vb + 2));
                        }
                        else if (vgr is > -9 and < 8 &&
                                 vg is > -33 and < 32 &&
                                 vgb is > -9 and < 8
                                )
                        {
                           yield return (byte)(QoiCodec.Luma | (vg + 32));
                           yield return (byte)((vgr + 8) << 4 | (vgb + 8));
                        }
                        else
                        {
                           yield return QoiCodec.Rgb;
                           yield return r;
                           yield return g;
                           yield return b;
                        }
                    }
                    else
                    {
                       yield return QoiCodec.Rgba;
                       yield return r;
                       yield return g;
                       yield return b;
                       yield return a;
                    }
                }
            }

            prevR = r;
            prevG = g;
            prevB = b;
            prevA = a;
        }

        for (int padIdx = 0; padIdx < QoiCodec.Padding.Length; padIdx++)
        {
            yield return QoiCodec.Padding[padIdx];
        }

        p += QoiCodec.Padding.Length;

    }

    private static bool RgbaEquals(byte r1, byte g1, byte b1, byte a1, byte r2, byte g2, byte b2, byte a2) =>
        r1 == r2 &&
        g1 == g2 &&
        b1 == b2 &&
        a1 == a2;
}