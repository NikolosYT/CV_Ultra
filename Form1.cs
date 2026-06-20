namespace CV_Ultra
{
    public partial class Form1 : Form
    {
        private Bitmap originalBitmap; //вот это добавила
        public Form1()
        {
            InitializeComponent();
            pictureBox2.Visible = false;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //1. Преобразование изображения
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

        // Повышение резкости
        public Bitmap ApplySharpenToGrayscale(Bitmap sourceBitmap, int radius, double sigma, float strength)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            Bitmap resultBitmap = new Bitmap(width, height);

            // 1. Получаем размытую Гауссом копию, используя твою готовую функцию
            Bitmap blurredBitmap = ApplyGaussianBlurToGrayscale(sourceBitmap, radius, sigma);

            // 2. Смешиваем оригинал и размытие по формуле резкости
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Так как картинка серая, читаем только канал R
                    int originalGray = sourceBitmap.GetPixel(x, y).R;
                    int blurredGray = blurredBitmap.GetPixel(x, y).R;

                    // Формула: Оригинал + Сила * (Оригинал - Размытие)
                    int sharpenedGray = (int)(originalGray + strength * (originalGray - blurredGray));

                    // Жёстко ограничиваем диапазон [0, 255], чтобы не было цветовых артефактов
                    sharpenedGray = Math.Min(Math.Max(sharpenedGray, 0), 255);

                    // Записываем результат
                    resultBitmap.SetPixel(x, y, Color.FromArgb(sharpenedGray, sharpenedGray, sharpenedGray));
                }
            }

            return resultBitmap;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //2.  Блюр
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

        // Среднеарифметическая
        public Bitmap ApplyArithmeticMeanFilter(Bitmap sourceBitmap, int radius)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            Bitmap resultBitmap = new Bitmap(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double sum = 0;
                    int count = 0;

                    // Пробегаем по квадратному окну вокруг пикселя
                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int pixelX = Math.Min(Math.Max(x + kx, 0), width - 1);
                            int pixelY = Math.Min(Math.Max(y + ky, 0), height - 1);

                            sum += sourceBitmap.GetPixel(pixelX, pixelY).R;
                            count++;
                        }
                    }

                    // Среднее арифметическое
                    int finalGray = (int)(sum / count);
                    resultBitmap.SetPixel(x, y, Color.FromArgb(finalGray, finalGray, finalGray));
                }
            }

            return resultBitmap;
        }

        // Среднегеометрическая фильтрация
        public Bitmap ApplyGeometricMeanFilter(Bitmap sourceBitmap, int radius)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            Bitmap resultBitmap = new Bitmap(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double logSum = 0;
                    int count = 0;

                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int pixelX = Math.Min(Math.Max(x + kx, 0), width - 1);
                            int pixelY = Math.Min(Math.Max(y + ky, 0), height - 1);

                            int grayVal = sourceBitmap.GetPixel(pixelX, pixelY).R;

                            // Избегаем логарифма нуля (если пиксель абсолютно черный, берем микро-значение)
                            logSum += Math.Log(grayVal == 0 ? 0.001 : grayVal);
                            count++;
                        }
                    }

                    // Считаем среднегеометрическое через экспоненту от среднего логарифмов
                    int finalGray = (int)Math.Exp(logSum / count);

                    // Ограничиваем на всякий случай
                    finalGray = Math.Min(Math.Max(finalGray, 0), 255);

                    resultBitmap.SetPixel(x, y, Color.FromArgb(finalGray, finalGray, finalGray));
                }
            }

            return resultBitmap;
        }

        // Медианная фильтрация
        public Bitmap ApplyMedianFilter(Bitmap sourceBitmap, int radius)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            Bitmap resultBitmap = new Bitmap(width, height);

            // Вычисляем размер окна (например, для радиуса 1 размер будет 3*3 = 9)
            int kernelSize = radius * 2 + 1;
            int totalPixels = kernelSize * kernelSize;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Массив, куда мы будем собирать яркости пикселей из окна
                    int[] windowPixels = new int[totalPixels];
                    int index = 0;

                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int pixelX = Math.Min(Math.Max(x + kx, 0), width - 1);
                            int pixelY = Math.Min(Math.Max(y + ky, 0), height - 1);

                            windowPixels[index] = sourceBitmap.GetPixel(pixelX, pixelY).R;
                            index++;
                        }
                    }

                    // Сортируем массив по возрастанию
                    Array.Sort(windowPixels);

                    // Берём значение, которое оказалось ровно по центру
                    int medianGray = windowPixels[totalPixels / 2];

                    resultBitmap.SetPixel(x, y, Color.FromArgb(medianGray, medianGray, medianGray));
                }
            }

            return resultBitmap;
        }

        // Адаптивная медианная фильтрация
        public Bitmap ApplyAdaptiveMedianFilter(Bitmap sourceBitmap, int startRadius, int maxRadius)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            Bitmap resultBitmap = new Bitmap(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int currentRadius = startRadius;
                    int finalGray = sourceBitmap.GetPixel(x, y).R;
                    bool pixelProcessed = false;

                    // Цикл адаптивного расширения окна
                    while (currentRadius <= maxRadius && !pixelProcessed)
                    {
                        int kernelSize = currentRadius * 2 + 1;
                        int totalPixels = kernelSize * kernelSize;
                        int[] windowPixels = new int[totalPixels];
                        int index = 0;

                        // Собираем пиксели из текущего окна
                        for (int ky = -currentRadius; ky <= currentRadius; ky++)
                        {
                            for (int kx = -currentRadius; kx <= currentRadius; kx++)
                            {
                                int pixelX = Math.Min(Math.Max(x + kx, 0), width - 1);
                                int pixelY = Math.Min(Math.Max(y + ky, 0), height - 1);

                                windowPixels[index] = sourceBitmap.GetPixel(pixelX, pixelY).R;
                                index++;
                            }
                        }

                        // Сортируем для поиска минимума, максимума и медианы
                        Array.Sort(windowPixels);

                        int zMin = windowPixels[0];
                        int zMax = windowPixels[totalPixels - 1];
                        int zMed = windowPixels[totalPixels / 2];
                        int zXY = sourceBitmap.GetPixel(x, y).R;

                        // УРОВЕНЬ А: Проверяем, пригодна ли медиана
                        if (zMin < zMed && zMed < zMax)
                        {
                            // УРОВЕНЬ Б: Проверяем сам пиксель
                            if (zMin < zXY && zXY < zMax)
                            {
                                finalGray = zXY; // Пиксель хороший, оставляем оригинал
                            }
                            else
                            {
                                finalGray = zMed; // Пиксель битый (соль/перец), заменяем на медиану
                            }
                            pixelProcessed = true; // Выходим из while для этого пикселя
                        }
                        else
                        {
                            // Медиана сама является шумом (0 или 255) -> увеличиваем окно
                            currentRadius++;
                        }
                    }

                    // Если дошли до maxRadius и не нашли чистую медиану, 
                    // вынужденно возвращаем последнее посчитанное значение медианы
                    if (!pixelProcessed)
                    {
                        // На всякий случай перестраховываемся
                        pixelProcessed = true;
                    }

                    resultBitmap.SetPixel(x, y, Color.FromArgb(finalGray, finalGray, finalGray));
                }
            }

            return resultBitmap;
        }

        // Инверсная фильтрация

        public Bitmap ApplyInverseFilter(Bitmap sourceBitmap, double threshold = 0.05)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            Bitmap resultBitmap = new Bitmap(width, height);

            // Зашиваем модель искажения (PSF) прямо внутрь функции
            // Сумма всех элементов равна 1, центрированное размытие
            double[,] psfKernel = new double[3, 3] {
        { 0.05, 0.1, 0.05 },
        { 0.1,  0.4, 0.1 },
        { 0.05, 0.1, 0.05 }
    };

            int radius = 1; // Так как размер матрицы 3х3, радиус равен 1

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double currentPixel = sourceBitmap.GetPixel(x, y).R;
                    double blurContribution = 0;

                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int px = Math.Min(Math.Max(x + kx, 0), width - 1);
                            int py = Math.Min(Math.Max(y + ky, 0), height - 1);

                            // Считаем вклад соседних пикселей
                            if (kx != 0 || ky != 0)
                            {
                                blurContribution += sourceBitmap.GetPixel(px, py).R * psfKernel[ky + radius, kx + radius];
                            }
                        }
                    }

                    // Центральный коэффициент (у нас он равен 0.4)
                    double centerWeight = psfKernel[radius, radius];

                    // Защита от деления на слишком маленькие числа (порог отсечения шума)
                    if (Math.Abs(centerWeight) < threshold) centerWeight = threshold;

                    // Обратная операция (деконволюция в пространственной области)
                    int finalGray = (int)((currentPixel - blurContribution) / centerWeight);

                    // Ограничиваем диапазон, чтобы не вылететь за [0, 255]
                    finalGray = Math.Min(Math.Max(finalGray, 0), 255);

                    resultBitmap.SetPixel(x, y, Color.FromArgb(finalGray, finalGray, finalGray));
                }
            }

            return resultBitmap;
        }

        // Реконструкция Люси-Ричардсона

        public Bitmap ApplyRichardsonLucyFilter(Bitmap sourceBitmap, int iterations = 5)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;

            // Шаблон ядра размытия (PSF)
            double[,] psf = new double[3, 3] {
        { 0.05, 0.1, 0.05 },
        { 0.1,  0.4, 0.1 },
        { 0.05, 0.1, 0.05 }
    };
            int radius = 1;

            // Переводим исходный Bitmap в массив double для удобства вычислений
            double[,] g = new double[width, height];
            double[,] f = new double[width, height]; // Текущее приближение (f_k)

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    g[x, y] = sourceBitmap.GetPixel(x, y).R;
                    f[x, y] = g[x, y]; // Изначально f_0 совпадает с искаженной картинкой
                }
            }

            // Главный цикл итераций восстановления
            for (int iter = 0; iter < iterations; iter++)
            {
                // 1. Шаг: Размываем текущее приближение (f * H)
                double[,] blurredF = new double[width, height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        double sum = 0;
                        for (int ky = -radius; ky <= radius; ky++)
                        {
                            for (int kx = -radius; kx <= radius; kx++)
                            {
                                int px = Math.Min(Math.Max(x + kx, 0), width - 1);
                                int py = Math.Min(Math.Max(y + ky, 0), height - 1);
                                sum += f[px, py] * psf[ky + radius, kx + radius];
                            }
                        }
                        blurredF[x, y] = sum;
                    }
                }

                // 2. Шаг: Считаем относительную ошибку (g / blurredF) и делаем обратную свертку
                double[,] correction = new double[width, height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Избегаем деления на ноль
                        double ratio = blurredF[x, y] < 0.001 ? 0 : g[x, y] / blurredF[x, y];

                        double sum = 0;
                        // Свертка с сопряженным ядром (для симметричного ядра код идентичен первому шагу)
                        for (int ky = -radius; ky <= radius; ky++)
                        {
                            for (int kx = -radius; kx <= radius; kx++)
                            {
                                int px = Math.Min(Math.Max(x + kx, 0), width - 1);
                                int py = Math.Min(Math.Max(y + ky, 0), height - 1);
                                sum += ratio * psf[ky + radius, kx + radius];
                            }
                        }
                        correction[x, y] = sum;
                    }
                }

                // 3. Шаг: Обновляем изображение f_{k+1} = f_k * correction
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        f[x, y] = f[x, y] * correction[x, y];
                    }
                }
            }

            // Собираем итоговый Bitmap
            Bitmap resultBitmap = new Bitmap(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int finalGray = (int)f[x, y];
                    finalGray = Math.Min(Math.Max(finalGray, 0), 255);
                    resultBitmap.SetPixel(x, y, Color.FromArgb(finalGray, finalGray, finalGray));
                }
            }

            return resultBitmap;
        }
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Шум "Соль и перец"
        public Bitmap ApplySaltAndPepperNoise(Bitmap sourceBitmap, double percentage)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;

            // ГАРАНТИЯ РАБОТЫ: Создаем холст в стандартном 32-битном формате, где SetPixel разрешен
            Bitmap resultBitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // Честно копируем графику оригинала на новый холст
            using (Graphics g = Graphics.FromImage(resultBitmap))
            {
                g.DrawImage(sourceBitmap, 0, 0, width, height);
            }

            Random rand = new Random();
            int totalPixels = width * height;

            // Внимание: percentage передавать как целое число (например, 10 для 10%)
            int noisePixelsCount = (int)(totalPixels * (percentage / 100.0));

            for (int i = 0; i < noisePixelsCount; i++)
            {
                int x = rand.Next(0, width);
                int y = rand.Next(0, height);

                // 0 - черный (перец), 255 - белый (соль)
                int noiseColor = rand.Next(0, 2) == 0 ? 0 : 255;

                // Явно передаем Alpha = 255, чтобы пиксель не стал прозрачным
                resultBitmap.SetPixel(x, y, Color.FromArgb(255, noiseColor, noiseColor, noiseColor));
            }

            return resultBitmap;
        }

        // Гауссов шум
        public Bitmap ApplyGaussianNoise(Bitmap sourceBitmap, double standardDeviation)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;

            // Точно так же создаем независимый открытый холст
            Bitmap resultBitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Random rand = new Random();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Извлекаем яркость из оригинальной картинки
                    int originalGray = sourceBitmap.GetPixel(x, y).R;

                    // Преобразование Бокса-Мюллера для генерации нормального распределения
                    double u1 = 1.0 - rand.NextDouble();
                    double u2 = 1.0 - rand.NextDouble();

                    // Защита от микроскопического нуля для логарифма
                    if (u1 <= 0) u1 = 0.000001;

                    double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
                    double noise = standardDeviation * randStdNormal;

                    // Смешиваем и жестко зажимаем в рамки байта
                    int finalGray = (int)(originalGray + noise);
                    if (finalGray < 0) finalGray = 0;
                    if (finalGray > 255) finalGray = 255;

                    resultBitmap.SetPixel(x, y, Color.FromArgb(255, finalGray, finalGray, finalGray));
                }
            }

            return resultBitmap;
        }
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // PSNR 
        // Показывает насколько сильно обработанное изображение отличается от исходного.
        private double CalculatePSNR(Bitmap original, Bitmap processed)
        {
            // Проверяем совпадение размеров изображений
            if (original.Width != processed.Width ||
                original.Height != processed.Height)
                throw new Exception("Размеры изображений не совпадают");

            // Среднеквадратичная ошибка (MSE)
            double mse = 0;

            // Проходим по всем пикселям изображения
            for (int x = 0; x < original.Width; x++)
            {
                for (int y = 0; y < original.Height; y++)
                {
                    // Получаем яркости пикселей
                    int p1 = original.GetPixel(x, y).R;
                    int p2 = processed.GetPixel(x, y).R;

                    // Добавляем квадрат разности яркостей
                    mse += Math.Pow(p1 - p2, 2);
                }
            }

            // Находим среднее значение ошибки
            mse /= (original.Width * original.Height);

            // Если изображения полностью совпадают
            if (mse == 0)
                return double.PositiveInfinity;

            // Вычисляем PSNR по стандартной формуле
            double psnr = 10 * Math.Log10((255.0 * 255.0) / mse);

            return psnr;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // SSIM 
        private double CalculateSSIM(Bitmap img1, Bitmap img2)
        {
            // Проверяем размеры изображений
            if (img1.Width != img2.Width ||
                img1.Height != img2.Height)
                throw new Exception("Размеры изображений не совпадают");

            int width = img1.Width;
            int height = img1.Height;

            // Общее количество пикселей
            int N = width * height;

            double meanX = 0;
            double meanY = 0;

            // Вычисляем средние яркости изображений

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    meanX += img1.GetPixel(x, y).R;
                    meanY += img2.GetPixel(x, y).R;
                }
            }

            meanX /= N;
            meanY /= N;

            double varianceX = 0;
            double varianceY = 0;
            double covariance = 0;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    double px = img1.GetPixel(x, y).R;
                    double py = img2.GetPixel(x, y).R;

                    varianceX += Math.Pow(px - meanX, 2);
                    varianceY += Math.Pow(py - meanY, 2);

                    covariance += (px - meanX) * (py - meanY);
                }
            }

            varianceX /= (N - 1);
            varianceY /= (N - 1);
            covariance /= (N - 1);

            double C1 = Math.Pow(0.01 * 255, 2);
            double C2 = Math.Pow(0.03 * 255, 2);

            double numerator =
                (2 * meanX * meanY + C1) *
                (2 * covariance + C2);

            double denominator =
                (meanX * meanX + meanY * meanY + C1) *
                (varianceX + varianceY + C2);

            return numerator / denominator;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Смаз (Motion Blur)
        private Bitmap ApplyMotionBlur(Bitmap src, int length)
        {
            int width = src.Width;
            int height = src.Height;

            Bitmap result = new Bitmap(width, height);

            // Радиус размытия относительно центрального пикселя
            int radius = length / 2;

            // Проходим по всему изображению
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sum = 0;
                    int count = 0;

                    // Берем соседние пиксели по горизонтали
                    for (int k = -radius; k <= radius; k++)
                    {
                        int px = x + k;

                        // Проверяем выход за границы изображения
                        if (px >= 0 && px < width)
                        {
                            sum += src.GetPixel(px, y).R;
                            count++;
                        }
                    }

                    // Вычисляем среднюю яркость
                    int gray = sum / count;

                    // Записываем результат
                    result.SetPixel(
                        x,
                        y,
                        Color.FromArgb(gray, gray, gray)
                    );
                }
            }

            return result;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------

        private void button1_Click(object sender, EventArgs e)
        {

        }
        private void button4_Click(object sender, EventArgs e) //вставить изображение в picktureBox1
        {

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

        private void button29_Click(object sender, EventArgs e) // исходное изображение вывод
        {
            panel1.Visible = false;
            panel2.Visible = false;
            panel3.Visible = false;
            panel4.Visible = false;
            panel5.Visible = false;
            //pictureBox2.Visible = true;
            pictureBox1.BackgroundImage = pictureBox2.BackgroundImage;

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

            pictureBox1.BackgroundImage = ApplyMotionBlur(new Bitmap(pictureBox1.BackgroundImage), 15);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            pictureBox1.BackgroundImage = BlurImage(new Bitmap(pictureBox1.BackgroundImage), 3);
        }
        private void button23_Click(object sender, EventArgs e)
        {
            pictureBox1.BackgroundImage = ApplySaltAndPepperNoise(new Bitmap(pictureBox1.BackgroundImage), 50);
        }
        private void button24_Click(object sender, EventArgs e)
        {
            pictureBox1.BackgroundImage = ApplyGaussianNoise(new Bitmap(pictureBox1.BackgroundImage), 50);
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            // Создаем стандартное диалоговое окно Windows для выбора файла
            OpenFileDialog openDialog = new OpenFileDialog();

            // Показываем только картинки
            openDialog.Filter = "Изображения (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Все файлы (*.*)|*.*";

            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                // Загружаем выбранный файл в фоновое изображение
                pictureBox1.BackgroundImage = new Bitmap(openDialog.FileName);
                pictureBox2.BackgroundImage = new Bitmap(openDialog.FileName);
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            pictureBox1.BackgroundImage = ConvertToGrayscale(new Bitmap(pictureBox1.BackgroundImage));
        }

        private void button13_Click(object sender, EventArgs e)
        {
            pictureBox1.BackgroundImage = EqualizeHistogram(new Bitmap(pictureBox1.BackgroundImage));
        }

        private void button14_Click(object sender, EventArgs e)
        {
            pictureBox1.BackgroundImage = ApplySharpenToGrayscale(new Bitmap(pictureBox1.BackgroundImage), 1, 1.0, 1.5f);
        }

        private void button16_Click(object sender, EventArgs e)
        {
            pictureBox1.BackgroundImage = ApplyGaussianBlurToGrayscale(new Bitmap(pictureBox1.BackgroundImage), 3, 45);
        }

        private void button18_Click(object sender, EventArgs e)
        {
            pictureBox1.BackgroundImage = ApplyMedianFilter(new Bitmap(pictureBox1.BackgroundImage), 3);
        }

        private void button17_Click(object sender, EventArgs e)
        {
            pictureBox1.BackgroundImage = ApplyArithmeticMeanFilter(new Bitmap(pictureBox1.BackgroundImage), 3);
        }

        private void button22_Click(object sender, EventArgs e)
        {
            pictureBox1.BackgroundImage = ApplyGeometricMeanFilter(new Bitmap(pictureBox1.BackgroundImage), 3);
        }

        private void button21_Click(object sender, EventArgs e)
        {
            pictureBox1.BackgroundImage = ApplyAdaptiveMedianFilter(new Bitmap(pictureBox1.BackgroundImage), 1, 3);
        }

        private void button20_Click(object sender, EventArgs e)
        {
            pictureBox1.BackgroundImage = ApplyInverseFilter(new Bitmap(pictureBox1.BackgroundImage));
        }

        private void button19_Click(object sender, EventArgs e)
        {
            pictureBox1.BackgroundImage = ApplyRichardsonLucyFilter(new Bitmap(pictureBox1.BackgroundImage), 5);
        }
    }
}
