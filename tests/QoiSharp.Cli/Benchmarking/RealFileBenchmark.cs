using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using QoiSharp.Codec;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using StbImageSharp;

namespace QoiSharp.Cli.Benchmarking;

/// <summary>
/// Real file benchmark that loads images from ../Images folder structure and measures QOI encoding performance.
/// 
/// Directory structure expected:
/// ../Images/
///   folder1/
///     image1.qoi
///     image2.qoi
///   folder2/
///     image3.qoi
/// </summary>
public class RealFileBenchmark
{
    private class ImageData
    {
        public string Name { get; set; } = string.Empty;
        public string FolderName { get; set; } = string.Empty;
        public QoiImage Image { get; set; } = new QoiImage([], 0, 0, Channels.Rgb);
    }
    private class TestResult
    {
        public string ImageName { get; set; } = string.Empty;
        public string FolderName { get; set; } = string.Empty;
        public double[] TimesInMs { get; set; } = [];
        public int CompressedSize { get; set; }
        public int OriginalSize { get; set; }
    }
    /// <summary>
    /// Examples path to the images directory.
    /// </summary>
    private static string _imagesPath = "../Images";

    public static void RunRealImagesBenchmark(string imagesPath)
    {
        var baseDirectory = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Environment.CurrentDirectory)));
        var imagesDirectory = Path.Combine(baseDirectory!, imagesPath);

        if (!Directory.Exists(imagesDirectory))
        {
            Console.WriteLine($"Images directory not found: {imagesDirectory}");
            return;
        }

        Console.WriteLine($"Loading images from: {imagesDirectory}");

        var folders = Directory.GetDirectories(imagesDirectory);
        foreach (var folder in folders
            .Take(1)
        )
        {
            var imageLoadTime = Stopwatch.StartNew();
            Console.WriteLine($"Processing folder: {folder}");
            List<ImageData> loadedImages = LoadImages(folder);
            imageLoadTime.Stop();
            Console.WriteLine($"Image loading took: {imageLoadTime.ElapsedMilliseconds} ms");

            //Preheat the QOI encoder
            RunFivePassBenchmark(loadedImages.Take(1).ToList(), false);

            // Run benchmark for this folder's images
            RunFivePassBenchmark(loadedImages);
        }
    }

    private static List<ImageData> LoadImages(string folder)
    {
        var loadedImages = new List<ImageData>();
        var folderName = Path.GetFileName(folder);

        var files = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => IsSupportedImageFormat(f))
            // .Take(5)
            .ToArray();

        Console.WriteLine($"Found {files.Length} image files in {folderName}");

        Parallel.ForEach(files, file =>
        {
            try
            {
                var image = LoadImageFile(file);
                if (image != null)
                {
                    loadedImages.Add(new ImageData
                    {
                        Name = Path.GetFileName(file),
                        FolderName = folderName,
                        Image = image
                    });
                    Console.WriteLine($"Loaded: {Path.GetFileName(file)} ({image.Width}x{image.Height}, {image.Channels} channels)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load {file}: {ex.Message}");
            }
        });

        return loadedImages;
    }


    private static List<TestResult> RunFivePassBenchmark(List<ImageData> images, bool printResults = true)
    {
        if (printResults)
        {
            Console.WriteLine($"Running 5-pass encoding benchmark on {images.Count} images...\n");
        }
        var allResults = new List<TestResult>();
        var outputBytes = new byte[2048 * 2048 * 4];
        foreach (var imageData in images)
        {
            var times = new double[5];
            var compressedSize = 0;

            if (printResults)
            {
                Console.WriteLine($"Processing {imageData.FolderName}/{imageData.Name} ({imageData.Image.Width}x{imageData.Image.Height}, {imageData.Image.Channels} channels)...");
            }
            for (int pass = 0; pass < 5; pass++)
            {
                var stopwatch = Stopwatch.StartNew();
                // var encoded = QoiEncoder.Encode(imageData.Image);
                var encoderStream = new QoiEncoderStream(new MemoryStream(imageData.Image.Data),
                    imageData.Image.Width, imageData.Image.Height, imageData.Image.Channels);
                var readBytes = encoderStream.Read(outputBytes, 0, outputBytes.Length);
                stopwatch.Stop();

                times[pass] = stopwatch.Elapsed.TotalMilliseconds;
                if (pass == 0) compressedSize = readBytes;

                if (printResults)
                {
                    Console.WriteLine($"  Pass {pass + 1}: {times[pass]:F2}ms");
                }
            }

            var avgTime = times.Average();
            var minTime = times.Min();
            var maxTime = times.Max();
            var originalSize = imageData.Image.Data.Length;
            var compressionRatio = (double)compressedSize / originalSize;
            var pixelCount = imageData.Image.Width * imageData.Image.Height;
            var mpixelsPerSecond = pixelCount / (avgTime * 1000.0);

            if (printResults)
            {
                Console.WriteLine($"  Results: Avg={avgTime:F2}ms, Min={minTime:F2}ms, Max={maxTime:F2}ms");
                Console.WriteLine($"  Performance: {mpixelsPerSecond:F1} MPix/s");
                Console.WriteLine($"  Compression: {originalSize / 1024:N1} -> {compressedSize / 1024:N1} kilo-bytes ({compressionRatio:P1})\n");
            }
            allResults.Add(new TestResult
            {
                ImageName = imageData.Name,
                FolderName = imageData.FolderName,
                TimesInMs = times,
                CompressedSize = compressedSize,
                OriginalSize = originalSize
            });
        }

        // Print summary
        if (allResults.Count > 0)
        {
            var overallAvgTime = allResults.SelectMany(r => r.TimesInMs).Average();
            var totalOriginalSize = allResults.Sum(r => r.OriginalSize);
            var totalCompressedSize = allResults.Sum(r => r.CompressedSize);
            var overallCompressionRatio = (double)totalCompressedSize / totalOriginalSize;

            if (printResults)
            {
                Console.WriteLine("=== SUMMARY ===");
                Console.WriteLine($"Images processed: {allResults.Count}");
                Console.WriteLine($"Average encoding time: {overallAvgTime:F2}ms per image");
                Console.WriteLine($"Total size: {totalOriginalSize:N0} -> {totalCompressedSize:N0} bytes");
                Console.WriteLine($"Overall compression ratio: {overallCompressionRatio:P1}");
            }
        }
        return allResults;
    }


    private static QoiImage? LoadImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".qoi" => LoadQoiImage(filePath),
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tga" or ".gif" or ".tiff" or ".tif" => LoadGenericImage(filePath),
            _ => null
        };
    }

    private static QoiImage LoadQoiImage(string filePath)
    {
        var qoiData = File.ReadAllBytes(filePath);
        return QoiDecoder.Decode(qoiData);
    }

    /// <summary>
    /// Extension method that converts an Image to a raw RGBA pixel array
    /// </summary>
    /// <param name="imageIn">The Image to convert</param>
    /// <returns>A byte array containing raw RGBA pixel data</returns>
    public static byte[] ToBitmapArray(Image<Rgba32> imageIn)
    {
        var pixelData = new byte[imageIn.Width * imageIn.Height * 4];
        imageIn.CopyPixelDataTo(pixelData);
        return pixelData;
    }

    private static QoiImage? LoadGenericImage(string filePath)
    {
        try
        {
            // Try SixLabors.ImageSharp first (more feature-rich)
            using var image = Image.Load<Rgba32>(filePath);
            var pixelData = ToBitmapArray(image);
            return new QoiImage(pixelData, image.Width, image.Height, Channels.RgbWithAlpha);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SixLabors.ImageSharp failed for {Path.GetFileName(filePath)}: {ex.Message}");

            // Fall back to StbImageSharp
            try
            {
                var imageResult = StbImageSharp.ImageResult.FromMemory(File.ReadAllBytes(filePath));
                if (imageResult != null)
                {
                    var channels = imageResult.Comp switch
                    {
                        StbImageSharp.ColorComponents.RedGreenBlue => Channels.Rgb,
                        StbImageSharp.ColorComponents.RedGreenBlueAlpha => Channels.RgbWithAlpha,
                        StbImageSharp.ColorComponents.Grey => Channels.Rgb,
                        StbImageSharp.ColorComponents.GreyAlpha => Channels.RgbWithAlpha,
                        _ => Channels.Rgb
                    };

                    // Convert to the expected format if needed
                    byte[] pixelData;
                    if (channels == Channels.Rgb && imageResult.Comp == StbImageSharp.ColorComponents.Grey)
                    {
                        // Convert grayscale to RGB
                        pixelData = new byte[imageResult.Width * imageResult.Height * 3];
                        for (int i = 0, j = 0; i < imageResult.Data.Length; i++, j += 3)
                        {
                            pixelData[j] = imageResult.Data[i];     // R
                            pixelData[j + 1] = imageResult.Data[i]; // G
                            pixelData[j + 2] = imageResult.Data[i]; // B
                        }
                    }
                    else if (channels == Channels.RgbWithAlpha && imageResult.Comp == StbImageSharp.ColorComponents.GreyAlpha)
                    {
                        // Convert grayscale+alpha to RGBA
                        pixelData = new byte[imageResult.Width * imageResult.Height * 4];
                        for (int i = 0, j = 0; i < imageResult.Data.Length; i += 2, j += 4)
                        {
                            pixelData[j] = imageResult.Data[i];     // R
                            pixelData[j + 1] = imageResult.Data[i]; // G
                            pixelData[j + 2] = imageResult.Data[i]; // B
                            pixelData[j + 3] = imageResult.Data[i + 1]; // A
                        }
                    }
                    else
                    {
                        // Use data as-is
                        pixelData = imageResult.Data;
                    }

                    return new QoiImage(pixelData, imageResult.Width, imageResult.Height, channels);
                }
            }
            catch (Exception stbEx)
            {
                Console.WriteLine($"StbImageSharp also failed for {Path.GetFileName(filePath)}: {stbEx.Message}");
            }
        }

        Console.WriteLine($"Could not load image: {Path.GetFileName(filePath)}");
        return null;
    }

    /// <summary>
    /// Main benchmark method: encodes each loaded image 5 times and measures performance
    /// </summary>
    private void QoiEncodingDetailedTiming(List<ImageData> _images)
    {
        var totalOriginalSize = 0L;
        var totalCompressedSize = 0L;
        var totalElapsedTicks = 0L;
        var imageCount = 0;

        foreach (var imageData in _images)
        {
            var results = new List<long>();

            // Warmup
            QoiEncoder.Encode(imageData.Image);

            // Measure 5 passes
            for (int pass = 0; pass < 5; pass++)
            {
                var stopwatch = Stopwatch.StartNew();
                var encoded = QoiEncoder.Encode(imageData.Image);
                stopwatch.Stop();

                results.Add(stopwatch.ElapsedTicks);

                if (pass == 0)
                {
                    totalOriginalSize += imageData.Image.Data.Length;
                    totalCompressedSize += encoded.Length;
                }
            }

            var avgTicks = results.Average();
            totalElapsedTicks += (long)avgTicks;
            imageCount++;

            var avgMs = TimeSpan.FromTicks((long)avgTicks).TotalMilliseconds;
            var pixelCount = imageData.Image.Width * imageData.Image.Height;
            var mpixelsPerSecond = pixelCount / (avgMs * 1000.0);

            Console.WriteLine($"{imageData.FolderName}/{imageData.Name}: {avgMs:F2}ms ({mpixelsPerSecond:F1} MPix/s)");
        }

        if (imageCount > 0)
        {
            var avgMs = TimeSpan.FromTicks(totalElapsedTicks / imageCount).TotalMilliseconds;
            var totalCompressionRatio = (double)totalCompressedSize / totalOriginalSize;

            Console.WriteLine($"\nSummary ({imageCount} images):");
            Console.WriteLine($"  Average encoding time: {avgMs:F2}ms per image");
            Console.WriteLine($"  Total size: {totalOriginalSize:N0} -> {totalCompressedSize:N0} bytes");
            Console.WriteLine($"  Overall compression ratio: {totalCompressionRatio:P1}");
        }
    }

    private static bool IsSupportedImageFormat(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".qoi" => true,
            ".png" => true,
            ".jpg" => true,
            ".jpeg" => true,
            ".bmp" => true,
            ".tga" => true,
            ".gif" => true,
            ".tiff" => true,
            ".tif" => true,
            _ => false
        };
    }
}
