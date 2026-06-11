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

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
        }
    }
}
