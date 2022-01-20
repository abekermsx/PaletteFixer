
namespace PaletteFixer
{
    public record Color(int R, int G, int B)
    {
        public override string ToString()
        {
            return R.ToString() + G.ToString() + B.ToString();
        }

        public string ToString(string format)
        {
            if ("RGB".Equals(format))
                return ToString();

            return G.ToString() + R.ToString() + B.ToString();
        }
    }

    public class Program
    {
        public const int paletteIndex = 0x7687;

        public static IList<Color> GetPalette(byte[] image)
        {
            var paletteData = image.Skip(paletteIndex).Take(32).ToArray();

            var palette = new List<Color>();

            for (int color = 0; color < 16; color++)
            {
                palette.Add(
                    new Color(
                            paletteData[color * 2] >> 4,
                            paletteData[color * 2 + 1] & 15,
                            paletteData[color * 2] & 15)
                    );
            }

            return palette;
        }


        public static void DisplayPalettes(IList<Tuple<string,byte[]>> files, string format)
        {
            int maxFileNameLength = files.Max(t => t.Item1.Length);

            Console.WriteLine("File:".PadRight(maxFileNameLength + 2) + $"Palette ({format}):");
            Console.WriteLine(new string('-', maxFileNameLength + 2 + 16 * 4));

            var separator = "data".Equals(format) ? ",$" : " ";

            foreach (var file in files)
            {
                Console.Write($"{file.Item1}  ".PadRight(maxFileNameLength + 2));

                var palette = GetPalette(file.Item2).Select(c => c.ToString(format));

                if ("data".Equals(format))
                    Console.Write("dw $");

                Console.WriteLine(string.Join(separator, palette));
            }
        }

        public static void FixPalettes(IList<Tuple<string, byte[]>> files)
        {
            var targetPalette = GetPalette(files[0].Item2);

            foreach (var file in files.Skip(1))
            {
                var palette = GetPalette(file.Item2);
                var colorMap = new List<Tuple<int, int>>();

                for (int color = 0; color < 16; color++)
                {
                    for (int targetColor = 0; targetColor < 16; targetColor++)
                    {
                        if (colorMap.Select(m => m.Item2).Contains(targetColor))
                            continue;

                        if (palette[color] == targetPalette[targetColor])
                        {
                            colorMap.Add(new Tuple<int, int>(color, targetColor));
                            break;
                        }
                    }
                }

                foreach (var item in colorMap)
                {
                    file.Item2[paletteIndex + item.Item2 * 2] = (byte)(targetPalette[item.Item2].R * 16 + targetPalette[item.Item2].B);
                    file.Item2[paletteIndex + item.Item2 * 2 + 1] = (byte)targetPalette[item.Item2].G;
                }

                var unmappedColors = Enumerable.Range(0, 16).Where(v => !colorMap.Select(c => c.Item1).Contains(v));
                var freeColors = Enumerable.Range(0, 16).Where(v => !colorMap.Select(c => c.Item2).Contains(v)).ToList();

                foreach (var color in unmappedColors)
                {
                    var freeColor = freeColors[0];

                    colorMap.Add(new Tuple<int, int>(color, freeColor));

                    file.Item2[paletteIndex + freeColor * 2] = (byte)(palette[color].R * 16 + palette[color].B);
                    file.Item2[paletteIndex + freeColor * 2 + 1] = (byte)palette[color].G;

                    freeColors.RemoveAt(0);
                }


                for (int i = 7; i < 7 + 212 * 128; i++)
                {
                    int color1 = file.Item2[i] >> 4;
                    int color2 = file.Item2[i] & 15;

                    int color = colorMap.First(c => c.Item1 == color1).Item2 * 16 + colorMap.First(c => c.Item1 == color2).Item2;

                    file.Item2[i] = (byte)color;
                }

                var fileNameParts = file.Item1.Split(".");
                var fileName = "";

                if (fileNameParts.Length == 1)
                {
                    fileName = fileNameParts[0] + "-new";
                }
                else
                {
                    fileNameParts[fileNameParts.Length - 2] += "-new";
                    fileName = string.Join('.', fileNameParts);
                }

                File.WriteAllBytes(fileName, file.Item2);
            }
        }

        public static void Main(string[] args)
        {
            bool outputRGB = false;
            bool outputData = false;
            bool fixPalette = false;

            List<Tuple<string, byte[]>> files = new List<Tuple<string, byte[]>>();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-"))
                {
                    if ("-data".Equals(args[i]))
                        outputData = true;

                    if ("-rgb".Equals(args[i]))
                        outputRGB = true;

                    if ("-fix".Equals(args[i]))
                        fixPalette = true;
                }
                else
                {
                    files.Add(new Tuple<string, byte[]>(args[i], File.ReadAllBytes(args[i])));
                }
            }

            if (!files.Any())
            {
                Console.WriteLine("PaletteFixer v0.00");
                Console.WriteLine("Usage: PaletteFixer [-rgb] [-data] [-fix] image1.ge5 image2.ge5 ...");
                Console.WriteLine("   -rgb  : Output palette of all files in RGB format");
                Console.WriteLine("   -data : Output palette of all files as data statement");
                Console.WriteLine("   -fix  : Reorder colors in image2.ge5, image3.ge5, ... to match palette of image1 and save to image<x>-new.ge5");
                Console.WriteLine("If -fix is specified, -rgb and -data output the resulting palettes");
                return;
            }

            if (fixPalette)
                FixPalettes(files);

            if (outputRGB)
                DisplayPalettes(files, "RGB");

            if (outputData)
                DisplayPalettes(files, "data");
        }
    }
}
