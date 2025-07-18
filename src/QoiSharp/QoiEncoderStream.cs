using System.Drawing;
using QoiSharp.Codec;
using QoiSharp.Exceptions;

namespace QoiSharp;

/// <summary>
/// QOI encoder stream.
/// This stream reads raw pixel data and encodes it into QOI format on demand
/// </summary>
public class QoiEncoderStream : Stream
{
    public QoiEncoderStream(Stream pixelStream, int width, int height, Channels channels, ColorSpace colorSpace = ColorSpace.SRgb)
    {
        if (width < 1)
        {
            throw new QoiEncodingException($"Invalid width: {width}");
        }
        if (height < 1)
        {
            throw new QoiEncodingException($"Invalid height: {height}.");
        }

        PixelStream = pixelStream;
        ImageSize = new Size(width, height);
        Channels = channels;
        var readArraySize = bufferSize / 4 * 3;
        if (channels == Channels.RgbWithAlpha)
        {
            readArraySize = bufferSize / 5 * 4;
            previousPixel = 255;
        }

        pixelInputBuffer = new byte[readArraySize];
        //Write the header, ready to be read
        QoiEncoderInternal.WriteHeader(outputBytesBuffer, ImageSize.Width, ImageSize.Height, channels, colorSpace);
        outputPixelLength = QoiCodec.HeaderSize;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    /// <summary>
    /// Size of the internal buffer used internaly
    /// This number needs to be dividable by 60 (dividable by 3, 4 and 5)
    /// </summary>
    private const int bufferSize = 3000;
    private Size ImageSize;
    private Stream PixelStream;
    private Channels Channels;

    //Work variables:
    private int previousPixel = 0;
    private int equalPixelRun = 0;
    private int readPixels = 0;
    private byte[] pixelInputBuffer;
    private byte[] outputBytesBuffer = new byte[bufferSize];
    private int outputPixelStartPos = 0;
    private int outputPixelLength = 0;
    int[] pixelHashTable = new int[QoiCodec.HashTableSize];
    private bool endOfStreamWritteToBuffer = false;

    public override int Read(byte[] buffer, int offset, int count)
    {
        int read;
        int bytesWrittenTotal = 0;
        var bytesToWriteToBuffer = count - offset;
        int remainingBytesToWriteBack = count - offset;
        if (outputPixelLength > 0)
        {
            bytesWrittenTotal = CopyBytesToOutputBuffer(buffer, offset, bytesWrittenTotal, remainingBytesToWriteBack);
            if (bytesWrittenTotal == bytesToWriteToBuffer)
            {
                return bytesWrittenTotal;
            }
            remainingBytesToWriteBack = bytesToWriteToBuffer - bytesWrittenTotal;
        }
        if (endOfStreamWritteToBuffer)
        {
            return bytesWrittenTotal; // If the end of stream was already written to the buffer, return immediately
        }
        do
        {
            //Read from the image byte stream into the pixel buffer
            read = PixelStream.Read(pixelInputBuffer, 0, pixelInputBuffer.Length);
            if (read == 0)
            {
                break; // End of stream
            }
            readPixels += read;
            (previousPixel, equalPixelRun, outputPixelLength) = Channels == Channels.RgbWithAlpha
               ? QoiEncoderInternal.RunRgbaCompression(pixelInputBuffer, outputBytesBuffer, 0, read, equalPixelRun, previousPixel, pixelHashTable)
               : QoiEncoderInternal.RunRgbCompression(pixelInputBuffer, outputBytesBuffer, 0, read, equalPixelRun, previousPixel, pixelHashTable);
            //Write the output bytes from the encoded block to the stream
            bytesWrittenTotal = CopyBytesToOutputBuffer(buffer, offset, bytesWrittenTotal, remainingBytesToWriteBack);
            if (bytesWrittenTotal == bytesToWriteToBuffer)
            {
                return bytesWrittenTotal;
            }
            remainingBytesToWriteBack = bytesToWriteToBuffer - bytesWrittenTotal;
        }
        while (true);
        long expectedPixelLength = (long)ImageSize.Width * ImageSize.Height * (int)Channels;
        if (readPixels != expectedPixelLength)
        {
            throw new QoiEncodingException($"Invalid pixel data length: {readPixels}. Expected: {expectedPixelLength}");
        }
        //If the last block was a run, write it out
        if (equalPixelRun > 0)
        {
            outputBytesBuffer[0] = (byte)(QoiCodec.Run | (equalPixelRun - 1));
            outputPixelLength = 1;
            equalPixelRun = 0;
            bytesWrittenTotal = CopyBytesToOutputBuffer(buffer, offset, bytesWrittenTotal, remainingBytesToWriteBack);
            if (bytesWrittenTotal == bytesToWriteToBuffer)
            {
                return bytesWrittenTotal;
            }
            remainingBytesToWriteBack = bytesToWriteToBuffer - bytesWrittenTotal;
        }

        QoiCodec.Padding.AsMemory().CopyTo(outputBytesBuffer.AsMemory(0, QoiCodec.Padding.Length));
        outputPixelLength = QoiCodec.Padding.Length;
        endOfStreamWritteToBuffer = true;

        bytesWrittenTotal = CopyBytesToOutputBuffer(buffer, offset, bytesWrittenTotal, remainingBytesToWriteBack);
        return bytesWrittenTotal;
    }

    private int CopyBytesToOutputBuffer(byte[] buffer, int bufferOffset, int bytesWrittenTotal, int remainingBytesToWriteBack)
    {
        var bytesToWriteOut = Math.Min(remainingBytesToWriteBack, outputPixelLength - outputPixelStartPos);
        outputBytesBuffer.AsMemory(outputPixelStartPos, bytesToWriteOut)
            .CopyTo(buffer.AsMemory(bufferOffset + bytesWrittenTotal, bytesToWriteOut));
        outputPixelStartPos = bytesToWriteOut == outputPixelLength - outputPixelStartPos
            ? 0
            //we have some more bytes to write out, so cache them for the next read
            : bytesToWriteOut + outputPixelStartPos;
        bytesWrittenTotal += bytesToWriteOut;
        if (outputPixelStartPos == 0)
        {
            outputPixelLength = 0; // Reset if the end of the output buffer was reached
        }
        return bytesWrittenTotal;
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
