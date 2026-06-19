namespace CV_Ultra
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            panel1.Visible = false;
            panel2.Visible = false;
            panel3.Visible = false;
            panel4.Visible = false;
            panel5.Visible = false;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Преобразование изображения
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

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Блюр
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
                    int sumGray = 0;
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
                                // Картинка уже серая, поэтому R, G и B равны. Берём только R
                                sumGray += src.GetPixel(px, py).R;
                                count++; // Считаем, сколько реально пикселей попало в обработку
                            }
                        }
                    }

                    // Считаем среднее арифметическое для серого канала
                    int avgGray = sumGray / count;

                    // Записываем размытый пиксель, дублируя серый цвет во все три канала
                    res.SetPixel(x, y, Color.FromArgb(avgGray, avgGray, avgGray));
                }
            }

            return res;
        }

        // Гаусов блюр
        public Bitmap ApplyGaussianBlurToGrayscale(Bitmap sourceBitmap, int radius, double sigma)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            Bitmap resultBitmap = new Bitmap(width, height);

            // 1. Создаем ядро Гаусса
            int kernelSize = radius * 2 + 1;
            double[,] kernel = new double[kernelSize, kernelSize];
            double kernelSum = 0;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    double exponent = -(x * x + y * y) / (2 * sigma * sigma);
                    double weight = (1.0 / (2 * Math.PI * sigma * sigma)) * Math.Exp(exponent);

                    kernel[y + radius, x + radius] = weight;
                    kernelSum += weight;
                }
            }

            // Нормализация ядра
            for (int y = 0; y < kernelSize; y++)
            {
                for (int x = 0; x < kernelSize; x++)
                {
                    kernel[y, x] /= kernelSum;
                }
            }

            // 2. Применяем ядро (считаем только один канал)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double graySum = 0;

                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            // Обработка краев изображения (отзеркаливание)
                            int pixelX = Math.Min(Math.Max(x + kx, 0), width - 1);
                            int pixelY = Math.Min(Math.Max(y + ky, 0), height - 1);

                            // Так как картинка серая, R, G и B одинаковы. Берем только .R
                            int grayValue = sourceBitmap.GetPixel(pixelX, pixelY).R;
                            double weight = kernel[ky + radius, kx + radius];

                            graySum += grayValue * weight;
                        }
                    }

                    // Ограничиваем значение от 0 до 255
                    int finalGray = (int)Math.Min(Math.Max(graySum, 0), 255);

                    // Записываем одно и то же значение во все три канала R, G, B
                    resultBitmap.SetPixel(x, y, Color.FromArgb(finalGray, finalGray, finalGray));
                }
            }

            return resultBitmap;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------

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
            button29.BackgroundImage = Properties.Resources.кнопкасветкрасиво;
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
            button29.BackgroundImage = Properties.Resources.черное;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void panel5_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button33_Click(object sender, EventArgs e)
        {

        }

        private void button29_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            panel1.Visible = true;
            panel2.Visible = false;
            panel3.Visible = false;
            panel4.Visible = false;
            panel5.Visible = false;
        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            panel1.Visible = false;
            panel2.Visible = true;
            panel3.Visible = false;
            panel4.Visible = false;
            panel5.Visible = false;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            panel1.Visible = false;
            panel2.Visible = false;
            panel3.Visible = true;
            panel4.Visible = false;
            panel5.Visible = false;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            panel1.Visible = false;
            panel2.Visible = false;
            panel3.Visible = false;
            panel4.Visible = true;
            panel5.Visible = false;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            panel1.Visible = false;
            panel2.Visible = false;
            panel3.Visible = false;
            panel4.Visible = false;
            panel5.Visible = true;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            panel1.Visible = false;
            panel2.Visible = false;
            panel3.Visible = false;
            panel4.Visible = false;
            panel5.Visible = false;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            panel1.Visible = false;
            panel2.Visible = false;
            panel3.Visible = false;
            panel4.Visible = false;
            panel5.Visible = false;
        }
    }
}
