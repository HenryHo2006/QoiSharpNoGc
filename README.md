<h1 align="center">

<img src="https://qoiformat.org/qoi-logo.svg" alt="QoiSharp" width="256"/>
<br/>
QoiSharp 
</h1>

#### ✅ **Project status: active**. [What does it mean?](https://github.com/NUlliiON/QoiSharp/blob/main/docs/project-status.md)
### QoiSharp is an implementation of the [QOI](https://github.com/phoboslab/qoi) format for fast, lossless image compression

Supported functionality:
- [x] Encoding
- [x] Decoding
- [x] Encoder stream 
- [x] Decoder stream
- [x] Unittests with 100% code coverage
- [x] Benchmarks


## Installation

Install stable releases via Nuget

| Package Name                   | Release (NuGet) |
|--------------------------------|-----------------|
| `QoiSharp`         | [![NuGet](https://img.shields.io/nuget/v/QoiSharp.svg)](https://www.nuget.org/packages/QoiSharp/)

## API

### Encoding
Prefere the encoding stream to the byte array encoder for maximal performance
```csharp
int width = 1920;
int height = 1080;
var channels = Channels.RgbWithAlpha;
var pixelStream = GetRawPixelStream();
Stream qoiDataStream = new QoiEncoderStream(pixelStream, new Size(width, height),channels);

byte[] data = GetRawPixels();
byte[] qoiData = QoiEncoder.Encode(new QoiImage(data, width, height, channels));
```
### Decoding
Prefere the byte array decoder to the decoding stream for maximal performance, if you have the memory
```csharp
var qoiImage = QoiDecoder.Decode(qoiData);
Console.WriteLine($"Width: {qoiImage.Width}");
Console.WriteLine($"Height: {qoiImage.Height}");
Console.WriteLine($"Channels: {qoiImage.Channels}");
Console.WriteLine($"Color space: {qoiImage.ColorSpace}");
Console.WriteLine($"Data length: {qoiImage.Pixels.Length}");

Stream decoderStream = new QoiDecoderStream(new MemoryStream(qoiImage));
Console.WriteLine($"Width: {decoderStream.Width}");
Console.WriteLine($"Height: {decoderStream.Height}");
Console.WriteLine($"Channels: {decoderStream.Channels}");
Console.WriteLine($"Color space: {decoderStream.ColorSpace}");
```

## License

QoiSharp is licensed under the [MIT](LICENSE) license.
