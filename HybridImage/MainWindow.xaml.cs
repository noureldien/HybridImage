using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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
        #region Constants

        #endregion

        #region Variables

        /// <summary>
        /// Names of image-pairs. 5 pairs (10 image in total).
        /// </summary>
        private string[][] imagePairs = new string[][]
        {
            new string[] {"einstein.bmp", "marilyn.bmp" },
            new string[] {"bicycle.bmp", "motorcycle.bmp" },
            new string[] {"cat.bmp", "dog.bmp" },
            new string[] {"bird.bmp", "fish.bmp" },            
            new string[] {"plane.bmp", "submarine.bmp" },
        };

        /// <summary>
        /// Low-pass filter (smooth image that will appear from a far distance).
        /// </summary>
        private System.Drawing.Bitmap bitmap1;
        /// <summary>
        /// High-pass filter (sharp image that will appear from a near distance).
        /// </summary>
        private System.Drawing.Bitmap bitmap2;
        /// <summary>
        /// Hybrid image.
        /// </summary>
        private System.Drawing.Bitmap hybridImage;
        /// <summary>
        /// Guassian matrix/template used to apply template convolution to the images (Guassian smoothing).
        /// </summary>
        private double[,] convolutionTemplate;
        /// <summary>
        /// Which image-pair is selected.
        /// </summary>
        private int selectedPairsIndex;

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

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            Convolution();
        }

        #endregion

        #region Methods

        private void Initialize()
        {
            convolutionTemplate = new double[,]
            {
                { 0.002, 0.013, 0.022, 0.013, 0.002 },
                { 0.013, 0.060, 0.098, 0.060, 0.013 },
                { 0.022, 0.098, 0.162, 0.098, 0.022 },
                { 0.013, 0.060, 0.098, 0.060, 0.013 },
                { 0.002, 0.013, 0.022, 0.013, 0.002 },
            };

            selectedPairsIndex = 0;
            LoadImage();
        }

        private void LoadImage()
        {
            string source1 = String.Format(@"Images/{0}", imagePairs[selectedPairsIndex][0]);
            string source2 = String.Format(@"Images/{0}", imagePairs[selectedPairsIndex][1]);
            bitmap1 = new System.Drawing.Bitmap(source1);
            bitmap2 = new System.Drawing.Bitmap(source2);
        }

        private void Convolution()
        {
            // H = I1 · G1 + I2 ·(1 − G2),
            // H: Hybrid Image
            // I1: image 1
            // I2: image 2
            // G1: gaussian low-pass filter
            // 1-G2: gaussian high-pass filter

            int templateWidth = convolutionTemplate.GetLength(1);
            int templateHeight = convolutionTemplate.GetLength(0);
            int xOffset = templateWidth / 2;
            int yOffset = templateHeight / 2;

            System.Drawing.Bitmap i1 = new System.Drawing.Bitmap(bitmap1.Width, bitmap1.Height, bitmap1.PixelFormat);
            System.Drawing.Bitmap i2 = new System.Drawing.Bitmap(bitmap2.Width, bitmap2.Height, bitmap2.PixelFormat);

            int r, g, b;
            System.Drawing.Color pixel;

            for (int imageY = yOffset; imageY < i1.Height - yOffset; imageY++)
            {
                for (int imageX = xOffset; imageX < i1.Width - xOffset; imageX++)
                {
                    r = 0;
                    g = 0;
                    b = 0;

                    for (int templateY = 0; templateY < templateHeight; templateY++)
                    {
                        for (int templateX = 0; templateX < templateWidth; templateX++)
                        {
                            pixel = bitmap1.GetPixel(imageX + templateX - xOffset, imageY + templateY - yOffset);
                            r += (int)(pixel.R * convolutionTemplate[templateY, templateX]);
                            g += (int)(pixel.G * convolutionTemplate[templateY, templateX]);
                            b += (int)(pixel.B * convolutionTemplate[templateY, templateX]);
                        }
                    }

                    pixel = System.Drawing.Color.FromArgb(r, g, b);
                    i1.SetPixel(imageX, imageY, pixel);
                }
            }

            imageView3.Source = BitmapToBitmapSource(i1);

        }

        private void TryOne()
        {
            using (IplImage image = new IplImage(128, 128, BitDepth.U8, 1))
            {
                image.Zero();
                for (int x = 0; x < image.Width; x++)
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        int offset = y * image.WidthStep + x;
                        byte value = (byte)(x + y);
                        Marshal.WriteByte(image.ImageData, offset, value);
                    }
                }
                using (CvWindow window = new CvWindow(image))
                {
                    CvWindow.WaitKey();

                }
            }
        }

        private void TryTwo()
        {
            using (var img = new IplImage(@"C:\Lenna.png"))
            {
                Cv.SetImageROI(img, new CvRect(200, 200, 180, 200));
                Cv.Not(img, img);
                Cv.ResetImageROI(img);
                using (new CvWindow(img))
                {
                    Cv.WaitKey();
                }
            }
        }

        private BitmapSource BitmapToBitmapSource(System.Drawing.Bitmap source)
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                          source.GetHbitmap(),
                          IntPtr.Zero,
                          Int32Rect.Empty,
                          BitmapSizeOptions.FromEmptyOptions());
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

        #endregion
    }
}
