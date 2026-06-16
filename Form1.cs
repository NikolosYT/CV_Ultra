namespace CV_Ultra
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private Bitmap ConvertToGrayscale(Bitmap original)
        {
            // Создаем копию оригинального изображения, чтобы не портить исходник
            Bitmap output = new Bitmap(original.Width, original.Height);

            // Проходим циклом по каждому пикселю изображения (по ширине и высоте)
            for (int x = 0; x < original.Width; x++)
            {
                for (int y = 0; y < original.Height; y++)
                {
                    // Берем цвет текущего пикселя
                    Color pixelColor = original.GetPixel(x, y);

                    // Стандартная формула для перевода в градации серого (учитывает чувствительность глаза)
                    // К - 30%, З - 59%, С - 11%
                    int grayScale = (int)(pixelColor.R * 0.3 + pixelColor.G * 0.59 + pixelColor.B * 0.11);

                    // Создаем новый серый цвет
                    Color grayColor = Color.FromArgb(grayScale, grayScale, grayScale);

                    // Записываем его в результирующую картинку
                    output.SetPixel(x, y, grayColor);
                }
            }

            return output;
        }

        private Bitmap EqualizeHistogram(Bitmap src)
        {
            int w = src.Width;
            int h = src.Height;
            int total = w * h; // Всего пикселей на картинке

            // 1. Считаем, сколько каких пикселей на картинке
            int[] hist = new int[256];
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    int color = src.GetPixel(x, y).R; // Картинка серая, берем любой канал (R)
                    hist[color]++;
                }
            }

            // 2. Ищем самый первый (самый темный) цвет, который вообще есть на фото
            int minCDF = 0;
            for (int i = 0; i < 256; i++)
            {
                if (hist[i] > 0)
                {
                    minCDF = hist[i];
                    break;
                }
            }

            // 3. Создаем шпаргалку для перекраски: [Старый цвет] -> [Новый цвет]
            int[] lut = new int[256];
            int sum = 0;

            for (int i = 0; i < 256; i++)
            {
                sum += hist[i]; // Копим сумму пикселей

                if (total - minCDF > 0)
                {
                    // Самая простая формула растяжения контраста
                    lut[i] = (int)(((float)(sum - minCDF) / (total - minCDF)) * 255);

                    // Защита, чтобы цвет не вылетел за границы [0, 255]
                    if (lut[i] < 0) lut[i] = 0;
                    if (lut[i] > 255) lut[i] = 255;
                }
                else
                {
                    lut[i] = i;
                }
            }

            // 4. Перекрашиваем пиксели по нашей шпаргалке lut
            Bitmap res = new Bitmap(w, h); // Сюда рисуем результат
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    int oldColor = src.GetPixel(x, y).R;
                    int newColor = lut[oldColor]; // Просто смотрим в шпаргалку

                    res.SetPixel(x, y, Color.FromArgb(newColor, newColor, newColor));
                }
            }

            return res;
        }

        private Bitmap BlurImage(Bitmap src, int kernelSize)
        {
            int w = src.Width;
            int h = src.Height;
            Bitmap res = new Bitmap(w, h);

            // Радиус окошка — это то, на сколько пикселей мы отходим от центра влево/вправо/вверх/вниз
            int rad = kernelSize / 2;

            // Бежим по всей картинке
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    int sumR = 0, sumG = 0, sumB = 0;
                    int count = 0;

                    // Бежим внутри окошка фильтра (ядра) вокруг текущего пикселя (x, y)
                    for (int kx = -rad; kx <= rad; kx++)
                    {
                        for (int ky = -rad; ky <= rad; ky++)
                        {
                            int px = x + kx;
                            int py = y + ky;

                            // Защита от выхода за границы картинки (чтобы на краях код не падал)
                            if (px >= 0 && px < w && py >= 0 && py < h)
                            {
                                Color pixelColor = src.GetPixel(px, py);
                                sumR += pixelColor.R;
                                sumG += pixelColor.G;
                                sumB += pixelColor.B;
                                count++; // Считаем, сколько реально пикселей попало в обработку
                            }
                        }
                    }

                    // Считаем среднее арифметическое для каждого канала
                    int avgR = sumR / count;
                    int avgG = sumG / count;
                    int avgB = sumB / count;

                    // Записываем размытый пиксель в результирующую картинку
                    res.SetPixel(x, y, Color.FromArgb(avgR, avgG, avgB));
                }
            }

            return res;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            pictureBox1.BackgroundImage = ConvertToGrayscale(new Bitmap(pictureBox1.BackgroundImage));
            for (int i = 0; i < 10; i++)
            {
                pictureBox1.BackgroundImage = BlurImage(new Bitmap(pictureBox1.BackgroundImage), 5);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.BackgroundImage = Properties.Resources.светлая;
            button1.BackgroundImage = Properties.Resources.кнопкасветкрасиво;
            button2.BackgroundImage = Properties.Resources.лунасвет;
            button3.BackgroundImage = Properties.Resources.солнцесвет;
            button4.BackgroundImage = Properties.Resources.кнопкасветкрасиво;
            button5.BackgroundImage = Properties.Resources.светбольшая;
            button6.BackgroundImage = Properties.Resources.светбольшая;
            button7.BackgroundImage = Properties.Resources.светбольшая;
            button8.BackgroundImage = Properties.Resources.светбольшая;
            button9.BackgroundImage = Properties.Resources.светбольшая;
            button10.BackgroundImage = Properties.Resources.светбольшая;
            button11.BackgroundImage = Properties.Resources.светбольшая;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.BackgroundImage = Properties.Resources.основа_темной_темы_с_кнопками_вывода_и_загрузки;
            button1.BackgroundImage = Properties.Resources.черное;
            button2.BackgroundImage = Properties.Resources.черниии;
            button3.BackgroundImage = Properties.Resources.кнопка_светли;
            button4.BackgroundImage = Properties.Resources.черное;
            button5.BackgroundImage = Properties.Resources.черное2;
            button6.BackgroundImage = Properties.Resources.черное2;
            button7.BackgroundImage = Properties.Resources.черное2;
            button8.BackgroundImage = Properties.Resources.черное2;
            button9.BackgroundImage = Properties.Resources.черное2;
            button10.BackgroundImage = Properties.Resources.черное2;
            button11.BackgroundImage = Properties.Resources.черное2;
        }
    }
}
