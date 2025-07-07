using QoiSharp;
using QoiSharp.Codec;
using StbImageSharp;

public static class Program
{
    public static async Task Main()
    {
        string imagePath = "Resources\\SomeImage.png";

        var img = ImageResult.FromMemory(await File.ReadAllBytesAsync(imagePath), ColorComponents.RedGreenBlueAlpha);
        var qoiImage = new QoiImage(img.Data, img.Width, img.Height, (Channels)img.Comp);
        byte[] qoiData = QoiEncoderReference.Encode(qoiImage);
        
        // saving image
        await File.WriteAllBytesAsync("MyImage.qoi", qoiData);
    }
}