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
            new string[] {"bicycle.bmp", "motorcycle.bmp" },
            new string[] {"cat.bmp", "dog.bmp" },
            new string[] {"bird.bmp", "fish.bmp" },
            new string[] {"einstein.bmp", "marilyn.bmp" },
            new string[] {"plane.bmp", "submarine.bmp" },
        };

        /// <summary>
        /// Low-pass filter (smooth image that will appear from a far distance).
        /// </summary>
        private IplImage image1;
        /// <summary>
        /// High-pass filter (sharp image that will appear from a near distance).
        /// </summary>
        private IplImage image2;
        /// <summary>
        /// Hybrid image.
        /// </summary>
        private IplImage hybridImage;
        /// <summary>
        /// Which image-pair is selected.
        /// </summary>
        private int selectedPairsIndex;

        /// <summary>
        /// Guassian matrix/template used to apply template convolution to the images (Guassian smoothing).
        /// </summary>
        private double[,] convolutionTemplate;

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
            //source1 = System.IO.Path.Combine(folder, source1);
            //source1 = System.IO.Path.Combine(folder, source2);

            image1 = new IplImage(source1);
            image2 = new IplImage(source2);
        }

        private void Convolution()
        {
            // H = I1 · G1 + I2 ·(1 − G2),
            // H: Hybrid Image
            // I1: image 1
            // I2: image 2
            // G1: gaussian low-pass filter
            // 1-G2: gaussian high-pass filter
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

        #endregion
    }
}
