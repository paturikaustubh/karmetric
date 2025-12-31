using System;
using System.IO;
using ImageMagick;

namespace IconConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseDir = "c:/Practice/monitor-er";
            string svgPath = Path.Combine(baseDir, "karmetric-logo.svg");
            string icoPath = Path.Combine(baseDir, "Karmetric.ico");
            
            Console.WriteLine($"Converting {svgPath} to {icoPath}...");

            if (!File.Exists(svgPath))
            {
                Console.WriteLine("Error: SVG not found!");
                return;
            }

            try 
            {
                using (var images = new MagickImageCollection())
                {
                    // Define sizes for standard Windows Icon
                    int[] sizes = new int[] { 16, 32, 48, 64, 128, 256 };
                    
                    foreach(var size in sizes)
                    {
                        var settings = new MagickReadSettings { Width = (uint)size, Height = (uint)size, Format = MagickFormat.Svg };
                        var img = new MagickImage(svgPath, settings);
                        img.Format = MagickFormat.Icon;
                        images.Add(img);
                    }
                    
                    images.Write(icoPath);
                }
                Console.WriteLine("Success: Karmetric.ico created.");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
