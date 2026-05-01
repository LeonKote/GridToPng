using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;

namespace GridConverter
{
    public class GridData
    {
        public double startX { get; set; }
        public double startY { get; set; }
        public double startZ { get; set; }
        public int sizeX { get; set; }
        public int sizeY { get; set; }
        public double nodeSize { get; set; }
        public int[] data { get; set; } = Array.Empty<int>();
    }

    class Program
    {
        static void Main()
        {
            bool convertToPng = true;

            string originalJsonFile = "grid.json";
            string pngFile = "map2.png";
            string outputJsonFile = "grid_new.json";

            try
            {
                if (convertToPng)
                {
                    Console.WriteLine($"Читаем {originalJsonFile} и рисуем {pngFile}...");
                    JsonToPng(originalJsonFile, pngFile);
                    Console.WriteLine("Успешно конвертировано в PNG!");
                }
                else
                {
                    Console.WriteLine($"Берем структуру из {originalJsonFile}, читаем пиксели из {pngFile}...");
                    PngToJson(pngFile, originalJsonFile, outputJsonFile);
                    Console.WriteLine($"Успешно конвертировано в {outputJsonFile}!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }

            Console.WriteLine("Нажмите любую клавишу для завершения...");
            Console.ReadKey();
        }

        static void JsonToPng(string jsonFile, string pngFile)
        {
            string jsonString = File.ReadAllText(jsonFile);
            GridData? grid = JsonSerializer.Deserialize<GridData>(jsonString);

            if (grid == null || grid.data == null)
                throw new Exception("Не удалось распарсить JSON.");

            // Цвета
            Color color0 = Color.FromArgb(134, 255, 134);
            Color color1 = Color.FromArgb(255, 94, 94);

            // ВАЖНО: Размеры картинки меняются местами (X становится Y, Y становится X)
            using (Bitmap bmp = new Bitmap(grid.sizeY, grid.sizeX))
            {
                for (int y = 0; y < grid.sizeY; y++)
                {
                    for (int x = 0; x < grid.sizeX; x++)
                    {
                        int index = y * grid.sizeX + x;
                        int val = grid.data[index];

                        Color pixelColor = (val == 1) ? color1 : color0;

                        // Поворот на 90 градусов по часовой + отражение по горизонтали
                        // математически равны простой замене координат (y, x)
                        int imgX = y;
                        int imgY = x;

                        bmp.SetPixel(imgX, imgY, pixelColor);
                    }
                }
                bmp.Save(pngFile, ImageFormat.Png);
            }
        }

        static void PngToJson(string pngFile, string templateJsonFile, string outputFile)
        {
            string jsonString = File.ReadAllText(templateJsonFile);
            GridData? grid = JsonSerializer.Deserialize<GridData>(jsonString);

            if (grid == null || grid.data == null)
                throw new Exception("Не удалось распарсить шаблонный JSON.");

            using (Bitmap bmp = new Bitmap(pngFile))
            {
                // Проверяем "перевернутые" размеры
                if (bmp.Width != grid.sizeY || bmp.Height != grid.sizeX)
                {
                    Console.WriteLine("ВНИМАНИЕ: Размеры картинки не совпадают с ожидаемыми (повернутыми) размерами из JSON!");
                }

                for (int y = 0; y < grid.sizeY; y++)
                {
                    for (int x = 0; x < grid.sizeX; x++)
                    {
                        // При чтении также меняем координаты местами
                        int imgX = y;
                        int imgY = x;

                        if (imgX >= 0 && imgX < bmp.Width && imgY >= 0 && imgY < bmp.Height)
                        {
                            Color c = bmp.GetPixel(imgX, imgY);

                            int distTo0 = Math.Abs(c.R - 134) + Math.Abs(c.G - 255) + Math.Abs(c.B - 134);
                            int distTo1 = Math.Abs(c.R - 255) + Math.Abs(c.G - 94) + Math.Abs(c.B - 94);

                            int val = (distTo1 < distTo0) ? 1 : 0;

                            int index = y * grid.sizeX + x;
                            grid.data[index] = val;
                        }
                    }
                }
            }

            string outJson = JsonSerializer.Serialize(grid);
            File.WriteAllText(outputFile, outJson);
        }
    }
}
