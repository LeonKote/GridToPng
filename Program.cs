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
        // Константы цветов для значений 0, 1 и 2
        static readonly Color color0 = Color.FromArgb(134, 255, 134); // Светло-зеленый (для 0)
        static readonly Color color1 = Color.FromArgb(255, 94, 94);   // Светло-красный (для 1)
        static readonly Color color2 = Color.FromArgb(134, 134, 255); // Светло-синий (для 2)

        static void Main()
        {
            string jsonFile = "grid.json";
            string pngFile = "map.png";
            string outputJsonFile = "grid_new.json";

            try
            {
                // Исходный/шаблонный JSON нужен в обоих случаях
                if (!File.Exists(jsonFile))
                {
                    Console.WriteLine($"Ошибка: Файл {jsonFile} не найден в папке с программой!");
                    Console.WriteLine("Нажмите любую клавишу для выхода...");
                    Console.ReadKey();
                    return;
                }

                if (!File.Exists(pngFile))
                {
                    // Картинки нет -> конвертируем JSON в PNG
                    Console.WriteLine($"Файл {pngFile} не найден. Режим: конвертация из JSON в PNG.");
                    Console.WriteLine($"Читаем {jsonFile}...");
                    JsonToPng(jsonFile, pngFile);
                    Console.WriteLine($"Успешно создан файл {pngFile}!");
                }
                else
                {
                    // Картинка есть -> конвертируем PNG в новый JSON
                    Console.WriteLine($"Файл {pngFile} обнаружен. Режим: конвертация из PNG в JSON.");
                    Console.WriteLine($"Берем структуру из {jsonFile}, читаем пиксели из {pngFile}...");
                    PngToJson(pngFile, jsonFile, outputJsonFile);
                    Console.WriteLine($"Успешно создан файл {outputJsonFile}!");
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

            // Размеры картинки меняются местами (X становится Y, Y становится X)
            using (Bitmap bmp = new Bitmap(grid.sizeY, grid.sizeX))
            {
                for (int y = 0; y < grid.sizeY; y++)
                {
                    for (int x = 0; x < grid.sizeX; x++)
                    {
                        int index = y * grid.sizeX + x;
                        int val = grid.data[index];

                        Color pixelColor;
                        if (val == 1)
                            pixelColor = color1;
                        else if (val == 2)
                            pixelColor = color2;
                        else
                            pixelColor = color0; // По умолчанию рисуем 0

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

                            int val = 0;

                            // Сравниваем строго байт-в-байт RGB
                            if (c.R == color1.R && c.G == color1.G && c.B == color1.B)
                            {
                                val = 1;
                            }
                            else if (c.R == color2.R && c.G == color2.G && c.B == color2.B)
                            {
                                val = 2;
                            }
                            // Для color0 и любых других сторонних цветов оставляем val = 0

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