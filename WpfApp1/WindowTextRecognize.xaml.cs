using Emgu.CV;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// WindowTextRecognize.xaml 的互動邏輯
    /// </summary>
    public partial class WindowTextRecognize : Window
    {
        private Image<Bgr, Byte> imgImage;
        private string path;
        public WindowTextRecognize()
        {
            InitializeComponent();
        }

        private void btn_getImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Directory.GetCurrentDirectory() + "\\Img";
            string filePath = "";
            if (openFileDialog.ShowDialog() == true)
            {
                filePath = openFileDialog.FileName;
            }
            else return;
            Image<Bgr, byte> image = new Image<Bgr, byte>(filePath);
        }

        private void btn_recognize_Click(object sender, RoutedEventArgs e)
        {
            Tesseract tesseract = new Tesseract("tessdata", "eng", OcrEngineMode.Default);

            imgImage = new Image<Bgr, byte>(path);

            System.Windows.Forms.Application.DoEvents();
            int i = tesseract.Recognize();
            tb_msg.Text = tesseract.GetUTF8Text();

        }
    }
}
