using System.Buffers.Binary;
using System.Drawing;
using System.Runtime.CompilerServices;
using QoiSharp.Codec;
using QoiSharp.Exceptions;

namespace QoiSharp;

/// <summary>
/// QOI decoder.
/// </summary>
public class QoiDecoderStream : Stream
{
    public QoiDecoderStream(Stream qoiStream)
    {
        byte[] qoiData = new byte[QoiCodec.HeaderSize];
        if (qoiStream.Read(qoiData, 0, qoiData.Length) != qoiData.Length)
        {
            throw new QoiDecodingException("QOI header too short");
        }

        if (!QoiCodec.IsValidMagic(qoiData[..4]))
        {
            throw new QoiDecodingException("Invalid file magic"); // TODO: add magic value
        }

        int width = BinaryPrimitives.ReadInt32BigEndian(qoiData.AsSpan(4, 4));
        int height = BinaryPrimitives.ReadInt32BigEndian(qoiData.AsSpan(8, 4));
        Channels = (Channels)qoiData[12];
        ColorSpace = (ColorSpace)qoiData[13];

        if (width < 1)
        {
            throw new QoiDecodingException($"Invalid width: {width}");
        }
        if (height < 1)
        {
            throw new QoiDecodingException($"Invalid height: {height}. Maximum for this image is {QoiCodec.MaxPixels / width - 1}");
        }
        if (Channels is not Channels.Rgb and not Channels.RgbWithAlpha)
        {
            throw new QoiDecodingException($"Invalid number of channels: {Channels}");
        }

        qoiDataStream = qoiStream;
        ImageSize = new Size(width, height);

        pixelOutputBuffer = new byte[3600]; //this number must be dividable by 12, because 3*4 = 12
        qoiInputBuffer = new byte[4096];
        pixelsLeftToWrite = (long)width * height * (long)Channels;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    private Stream qoiDataStream = new MemoryStream([]);
    public ColorSpace ColorSpace { get; private set; }
    public Size ImageSize { get; private set; }
    public Channels Channels { get; private set; }
    private byte[] pixelOutputBuffer = [];
    private byte[] qoiInputBuffer = [];
    private int qoiInputBufferPosition = 0;
    private int qoiInputBufferLength = 0;
    private int outputPixelStartPos = 0;
    private int outputPixelLength = 0;
    private long pixelsLeftToWrite
    {
        get;
        set;
    } = 0;
    private int[] pixelHashTable = new int[QoiCodec.HashTableSize];
    private int currentPixel = 255;
    private int runLength = -1;
    private bool reachedEndOfStream = false;

    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesWrittenTotal = 0;
        int remainingBytesToWriteBack = count - offset;
        if (outputPixelLength > 0)
        {
            bytesWrittenTotal = CopyBytesToOutputBuffer(buffer, offset, bytesWrittenTotal, remainingBytesToWriteBack);
            if (bytesWrittenTotal == remainingBytesToWriteBack)
            {
                return bytesWrittenTotal;
            }
            remainingBytesToWriteBack = count - offset - bytesWrittenTotal;
        }
        if (reachedEndOfStream)
        {
            return bytesWrittenTotal;
        }
        int p = qoiInputBufferPosition;

        byte r = 0;
        byte g = 0;
        byte b = 0;
        do
        {
            var bytesToWrite = Math.Min(remainingBytesToWriteBack, Math.Min(pixelsLeftToWrite, pixelOutputBuffer.Length));
            int pxPos = 0;
            for (; runLength >= 0; runLength--)
            {
                SetPixels(Channels, pixelOutputBuffer, currentPixel, pxPos);
                pxPos += (byte)Channels;
            }
            for (; pxPos < bytesToWrite; pxPos += (byte)Channels)
            {
                if (qoiInputBufferPosition + 5 >= qoiInputBufferLength)
                {
                    qoiInputBufferLength = ReadMoreBytesFromInput();
                    if (qoiInputBufferLength < QoiCodec.Padding.Length)
                    {
                        throw new QoiDecodingException($"Input stream added abruptly.");
                    }
                }

                byte b1 = qoiInputBuffer[qoiInputBufferPosition++];
                if (b1 >> 6 == 3)
                {
                    if (b1 == QoiCodec.Rgb)
                    {
                        currentPixel = qoiInputBuffer[qoiInputBufferPosition++] << 24 | qoiInputBuffer[qoiInputBufferPosition++] << 16
                            | qoiInputBuffer[qoiInputBufferPosition++] << 8 | (currentPixel & 0xFF);
                    }
                    else if (b1 == QoiCodec.Rgba)
                    {
                        currentPixel = BinaryPrimitives.ReadInt32BigEndian(qoiInputBuffer.AsSpan(qoiInputBufferPosition, 4));
                        qoiInputBufferPosition += 4;
                    }
                    else
                    {
                        runLength = b1 & 0x3F;
                        for (; runLength > 0 && pxPos < bytesToWrite; runLength--)
                        {
                            SetPixels(Channels, pixelOutputBuffer, currentPixel, pxPos);
                            pxPos += (byte)Channels;
                        }
                        if (pxPos < bytesToWrite)
                        {
                            runLength--;
                            SetPixels(Channels, pixelOutputBuffer, currentPixel, pxPos);
                        }
                        else
                        {
                            break;
                        }
                        continue;
                    }
                }
                else
                {
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
                        int b2 = qoiInputBuffer[qoiInputBufferPosition++];
                        int vg = (b1 & 0x3F) - 32;
                        r = (byte)(currentPixel >> 24);
                        g = (byte)(currentPixel >> 16);
                        b = (byte)(currentPixel >> 8);
                        r += (byte)(vg - 8 + ((b2 >> 4) & 0x0F));
                        g += (byte)vg;
                        b += (byte)(vg - 8 + (b2 & 0x0F));
                        currentPixel = r << 24 | g << 16 | b << 8 | (currentPixel & 0xFF);
                    }
                    else //b1 is an index
                    {
                        currentPixel = pixelHashTable[b1 & ~QoiCodec.Mask2];
                        SetPixels(Channels, pixelOutputBuffer, currentPixel, pxPos);
                        continue;
                    }
                }
                var indexPos3 = QoiCodec.CalculateHashTableRgbaIndex(currentPixel);
                pixelHashTable[indexPos3] = currentPixel;

                SetPixels(Channels, pixelOutputBuffer, currentPixel, pxPos);
            }
            // pxPos -= (byte)Channels;
            outputPixelLength = pxPos;
            bytesWrittenTotal = CopyBytesToOutputBuffer(buffer, offset, bytesWrittenTotal, remainingBytesToWriteBack);
            if (bytesWrittenTotal == remainingBytesToWriteBack)
            {
                return bytesWrittenTotal;
            }
            remainingBytesToWriteBack = (int)Math.Min(pixelsLeftToWrite, count - offset - bytesWrittenTotal);
            bytesToWrite = Math.Min(remainingBytesToWriteBack, Math.Min(pixelsLeftToWrite, pixelOutputBuffer.Length));
        }
        while (remainingBytesToWriteBack > 0 && pixelsLeftToWrite > 0);
        reachedEndOfStream = true;
        if (qoiInputBufferLength - qoiInputBufferPosition < QoiCodec.Padding.Length)
        {
            qoiInputBufferLength = ReadMoreBytesFromInput();
        }
        if (qoiInputBufferLength - qoiInputBufferPosition < QoiCodec.Padding.Length)
        {
            throw new QoiDecodingException($"Input stream ended abruptly.");
        }
        var qoiData = qoiInputBuffer.AsSpan(qoiInputBufferPosition, QoiCodec.Padding.Length);
        for (int padIdx = 0; padIdx < QoiCodec.Padding.Length; padIdx++)
        {
            if (qoiData[padIdx] != QoiCodec.Padding[padIdx])
            {
                throw new QoiDecodingException("Invalid padding");
            }
        }
        return bytesWrittenTotal;
    }

    private int ReadMoreBytesFromInput()
    {
        var remainingBytes = qoiInputBufferLength - qoiInputBufferPosition;
        qoiInputBuffer.AsSpan(qoiInputBufferPosition, remainingBytes)
            .CopyTo(qoiInputBuffer.AsSpan(0, remainingBytes));
        qoiInputBufferLength = qoiDataStream.Read(qoiInputBuffer, remainingBytes, qoiInputBuffer.Length - remainingBytes)
            + remainingBytes;
        return qoiInputBufferLength;
    }

    private int CopyBytesToOutputBuffer(byte[] buffer, int bufferOffset, int bytesWrittenTotal, int remainingBytesToWriteBack)
    {
        var bytesToWriteOut = Math.Min(remainingBytesToWriteBack, outputPixelLength - outputPixelStartPos);
        pixelOutputBuffer.AsMemory(outputPixelStartPos, bytesToWriteOut).CopyTo(buffer.AsMemory(bufferOffset + bytesWrittenTotal, bytesToWriteOut));
        outputPixelStartPos = bytesToWriteOut == outputPixelLength - outputPixelStartPos
            ? 0
            //we have some more bytes to write out, so cache them for the next read
            : bytesToWriteOut + outputPixelStartPos;
        bytesWrittenTotal += bytesToWriteOut;
        if (outputPixelStartPos == 0 || remainingBytesToWriteBack == 0)
        {
            outputPixelLength = 0; // Reset if the end of the output buffer was reached
        }
        pixelsLeftToWrite -= bytesToWriteOut;
        return bytesWrittenTotal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetPixels(Channels channels, byte[] pixels, int currentPixel, int pxPos)
    {
        pixels[pxPos] = (byte)(currentPixel >> 24);
        pixels[pxPos + 1] = (byte)(currentPixel >> 16);
        pixels[pxPos + 2] = (byte)(currentPixel >> 8);
        if (channels == Channels.RgbWithAlpha)
        {
            pixels[pxPos + 3] = (byte)currentPixel;
        }
    }

    public override void Flush()
    {
        //Does nothing, since this is a read based stream
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
