using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HybridImage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables

        /// <summary>
        /// Low-pass filter (smooth image that will appear from a far distance).
        /// </summary>
        private byte[, ,] image1;
        /// <summary>
        /// High-pass filter (sharp image that will appear from a near distance).
        /// </summary>
        private byte[, ,] image2;
        /// <summary>
        /// Which image-pair is selected.
        /// </summary>
        private int selectedPairs;
        /// <summary>
        /// Thread to do template convolution under it to release pressure from UI thread.
        /// </summary>
        private Thread thread;
        private System.Drawing.Bitmap bitmap1;
        private System.Drawing.Bitmap bitmap2;
        private int lpfIterations;
        private int hpfIterations;
        private int lpfDimension;
        private int hpfDimension;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Handlers

        private void WindowMain_Loaded(object sender, RoutedEventArgs e)
        {
            Initialize();
        }

        private void WindowMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DisposeObjects();
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            buttonStart.IsEnabled = false;
            buttonStart.Content = "Working!";
            progressBar.Visibility = System.Windows.Visibility.Visible;
            progressBar.Value = 0;

            imageView3.Source = null;
            imageView4.Source = null;
            imageView5.Source = null;

            lpfIterations = (int)byteUpDownLpfIterations.Value;
            hpfIterations = (int)byteUpDownHpfIterations.Value;
            lpfDimension = (int)byteUpDownLpfDimension.Value;
            hpfDimension = (int)byteUpDownHpfDimension.Value;

            // search thread
            ThreadStart threadStart = new ThreadStart(Convolution);
            thread = new Thread(threadStart);
            thread.Start();
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            string tag = ((RadioButton)sender).Tag.ToString();
            selectedPairs = int.Parse(tag);
            LoadImage();
        }

        #endregion

        #region Methods

        private void Initialize()
        {
            progressBar.Visibility = System.Windows.Visibility.Hidden;
            selectedPairs = 1;
            LoadImage();
        }

        private void LoadImage()
        {
            switch (selectedPairs)
            {
                case 1:
                    bitmap1 = HybridImage.Properties.Resources.marilyn;
                    bitmap2 = HybridImage.Properties.Resources.einstein;
                    break;

                case 2:
                    bitmap1 = HybridImage.Properties.Resources.einstein;
                    bitmap2 = HybridImage.Properties.Resources.marilyn;
                    break;

                case 3:
                    bitmap1 = HybridImage.Properties.Resources.motorcycle;
                    bitmap2 = HybridImage.Properties.Resources.bicycle;
                    break;

                case 4:
                    bitmap1 = HybridImage.Properties.Resources.bicycle;
                    bitmap2 = HybridImage.Properties.Resources.motorcycle;
                    break;

                case 5:
                    bitmap1 = HybridImage.Properties.Resources.dog;
                    bitmap2 = HybridImage.Properties.Resources.cat;
                    break;

                case 6:
                    bitmap1 = HybridImage.Properties.Resources.cat;
                    bitmap2 = HybridImage.Properties.Resources.dog;
                    break;

                case 7:
                    bitmap1 = HybridImage.Properties.Resources.bird;
                    bitmap2 = HybridImage.Properties.Resources.plane;
                    break;

                case 8:
                    bitmap1 = HybridImage.Properties.Resources.plane;
                    bitmap2 = HybridImage.Properties.Resources.bird;
                    break;

                case 9:
                    bitmap1 = HybridImage.Properties.Resources.fish;
                    bitmap2 = HybridImage.Properties.Resources.submarine;
                    break;

                case 10:
                    bitmap1 = HybridImage.Properties.Resources.submarine;
                    bitmap2 = HybridImage.Properties.Resources.fish;
                    break;

                default:
                    break;
            }

            imageView1.Source = BitmapToBitmapSource(bitmap1);
            imageView2.Source = BitmapToBitmapSource(bitmap2);
        }

        private void Convolution()
        {
            // H = I1 · G1 + I2 ·(1 − G2),
            // H: Hybrid Image
            // I1: image 1
            // I2: image 2
            // G1: gaussian low-pass filter
            // 1-G2: gaussian high-pass filter

            double progressValue = 0.0;
            double progressStep = 100 / (double)(lpfIterations + hpfIterations + 1);
            double[,] g1, g2;
            //g1 = g2 = new double[,]
            //{
            //    { 0.002, 0.013, 0.022, 0.013, 0.002 },
            //    { 0.013, 0.060, 0.098, 0.060, 0.013 },
            //    { 0.022, 0.098, 0.162, 0.098, 0.022 },
            //    { 0.013, 0.060, 0.098, 0.060, 0.013 },
            //    { 0.002, 0.013, 0.022, 0.013, 0.002 },
            //};
            g1 = GaussianKernel(5, 20);
            g2 = GaussianKernel(13, 10);

            image1 = BitmapToByteArray(bitmap1);
            image2 = BitmapToByteArray(bitmap2);
            progressValue += progressStep;
            UpdateProgressBar(progressValue);

            byte[, ,] i1 = image1;
            for (int i = 0; i < lpfIterations; i++)
            {
                i1 = GaussianFilter(i1, g1);
                progressValue += progressStep;
                UpdateProgressBar(progressValue);
            }

            byte[, ,] i2 = image2;
            for (int i = 0; i < hpfIterations; i++)
            {
                i2 = GaussianFilter(i2, g2);
                progressValue += progressStep;
                UpdateProgressBar(progressValue);
            }
            i2 = Subtract(image2, i2);
            progressValue += progressStep;
            UpdateProgressBar(progressValue);

            // adding h = i1 + i2
            byte[, ,] h = Add(i1, i2);

            Finish(i1, i2, h);
        }

        private double[,] GaussianKernel(int length, double weight)
        {
            double[,] kernel = new double[length, length];
            double sumTotal = 0;

            int kernelRadius = length / 2;
            double distance = 0;

            double calculatedEuler = 1.0 / (2.0 * Math.PI * Math.Pow(weight, 2));

            for (int filterY = -kernelRadius; filterY <= kernelRadius; filterY++)
            {
                for (int filterX = -kernelRadius; filterX <= kernelRadius; filterX++)
                {
                    distance = ((filterX * filterX) + (filterY * filterY)) / (2 * (weight * weight));
                    kernel[filterY + kernelRadius, filterX + kernelRadius] = calculatedEuler * Math.Exp(-distance);
                    sumTotal += kernel[filterY + kernelRadius, filterX + kernelRadius];
                }
            }

            for (int y = 0; y < length; y++)
            {
                for (int x = 0; x < length; x++)
                {
                    kernel[y, x] = kernel[y, x] * (1.0 / sumTotal);
                }
            }

            return kernel;
        }

        private byte[, ,] GaussianFilter(byte[, ,] source, double[,] kernel)
        {
            int sourceHeight = source.GetLength(0);
            int sourceWidth = source.GetLength(1);
            byte[, ,] result = new byte[sourceHeight, sourceWidth, 3];
            int templateWidth = kernel.GetLength(1);
            int templateHeight = kernel.GetLength(0);
            int templateLength = kernel.Length;
            int xOffset = templateWidth / 2;
            int yOffset = templateHeight / 2;

            double r, g, b;

            for (int imageY = yOffset; imageY < sourceHeight - yOffset; imageY++)
            {
                for (int imageX = xOffset; imageX < sourceWidth - xOffset; imageX++)
                {
                    r = 0;
                    g = 0;
                    b = 0;

                    for (int templateY = 0; templateY < templateHeight; templateY++)
                    {
                        for (int templateX = 0; templateX < templateWidth; templateX++)
                        {
                            // result  = source * kernel
                            int x, y;
                            x = imageX + templateX - xOffset;
                            y = imageY + templateY - yOffset;
                            r += source[y, x, 0] * kernel[templateY, templateX];
                            g += source[y, x, 1] * kernel[templateY, templateX];
                            b += source[y, x, 2] * kernel[templateY, templateX];
                        }
                    }

                    result[imageY, imageX, 0] = (byte)r;
                    result[imageY, imageX, 1] = (byte)g;
                    result[imageY, imageX, 2] = (byte)b;
                }
            }

            return result;
        }

        private byte[, ,] Subtract(byte[, ,] data1, byte[, ,] data2)
        {
            int height = data1.GetLength(0);
            int width = data1.GetLength(1);
            byte[, ,] result = new byte[height, width, 3];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[y, x, 0] = (byte)(125 + data1[y, x, 0] - data2[y, x, 0]);
                    result[y, x, 1] = (byte)(125 + data1[y, x, 1] - data2[y, x, 1]);
                    result[y, x, 2] = (byte)(125 + data1[y, x, 2] - data2[y, x, 2]);
                }
            }

            return result;
        }

        private byte[, ,] Brightness(byte[, ,] data, decimal factor)
        {
            int height = data.GetLength(0);
            int width = data.GetLength(1);
            int r, g, b;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    r = (int)(data[y, x, 0] * factor);
                    g = (int)(data[y, x, 1] * factor);
                    b = (int)(data[y, x, 2] * factor);

                    if (r > 255)
                    {
                        r = 255;
                    }
                    if (g > 255)
                    {
                        g = 255;
                    }
                    if (b > 255)
                    {
                        b = 255;
                    }

                    data[y, x, 0] = (byte)r;
                    data[y, x, 1] = (byte)g;
                    data[y, x, 2] = (byte)b;
                }
            }

            return data;
        }

        private byte[, ,] Increment(byte[, ,] data, int factor)
        {
            int height = data.GetLength(0);
            int width = data.GetLength(1);
            int r, g, b;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    r = data[y, x, 0] + factor;
                    g = data[y, x, 1] + factor;
                    b = data[y, x, 2] + factor;

                    if (r > 255)
                    {
                        r = 255;
                    }
                    if (g > 255)
                    {
                        g = 255;
                    }
                    if (b > 255)
                    {
                        b = 255;
                    }

                    data[y, x, 0] = (byte)r;
                    data[y, x, 1] = (byte)g;
                    data[y, x, 2] = (byte)b;
                }
            }

            return data;
        }

        private byte[, ,] Add(byte[, ,] data1, byte[, ,] data2)
        {
            int height = image1.GetLength(0);
            int width = image1.GetLength(1);
            byte[, ,] result = new byte[height, width, 3];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[y, x, 0] = (byte)((data1[y, x, 0] + data2[y, x, 0]) / 2);
                    result[y, x, 1] = (byte)((data1[y, x, 1] + data2[y, x, 1]) / 2);
                    result[y, x, 2] = (byte)((data1[y, x, 2] + data2[y, x, 2]) / 2);
                }
            }

            return result;
        }

        private byte[, ,] BitmapToByteArray(System.Drawing.Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            byte[, ,] data = new byte[height, width, 3];
            System.Drawing.Color pixel;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixel = bitmap.GetPixel(x, y);
                    data[y, x, 0] = pixel.R;
                    data[y, x, 1] = pixel.G;
                    data[y, x, 2] = pixel.B;
                }
            }

            return data;
        }

        private System.Drawing.Bitmap ByteArrayToBitmap(byte[, ,] data)
        {
            int height = data.GetLength(0);
            int width = data.GetLength(1);
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(data[y, x, 0], data[y, x, 1], data[y, x, 2]));
                }
            }

            return bitmap;
        }

        private BitmapSource ByteArrayToBitmapSource(byte[, ,] data)
        {
            return BitmapToBitmapSource(ByteArrayToBitmap(data));
        }

        private BitmapSource BitmapToBitmapSource(System.Drawing.Bitmap source)
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(source.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        private System.Drawing.Bitmap BitmapSourceToBitmap(BitmapSource bitmapsource)
        {
            System.Drawing.Bitmap bitmap;
            using (var outStream = new System.IO.MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new System.Drawing.Bitmap(outStream);
            }
            return bitmap;
        }

        private void Finish(byte[, ,] i1, byte[, ,] i2, byte[, ,] h)
        {
            this.Dispatcher.Invoke(() =>
            {
                buttonStart.Content = "Start";
                buttonStart.IsEnabled = true;
                imageView3.Source = ByteArrayToBitmapSource(i1);
                imageView4.Source = ByteArrayToBitmapSource(i2);
                imageView5.Source = ByteArrayToBitmapSource(h);
                progressBar.Value = 0;
                progressBar.Visibility = System.Windows.Visibility.Hidden;
            });
        }

        private void UpdateProgressBar(double value)
        {
            this.Dispatcher.Invoke(() =>
            {
                progressBar.Value = value;
            });
        }

        private void DisposeObjects()
        {
            if (thread != null && thread.IsAlive)
            {
                thread.Abort();
            }

            if (image1 != null)
            {
                image1 = null;
            }

            if (image2 != null)
            {
                image2 = null;
            }
        }

        #endregion
    }
}
