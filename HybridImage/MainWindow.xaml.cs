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
        #region Variables

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
