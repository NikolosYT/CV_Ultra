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

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
        }
    }
}
