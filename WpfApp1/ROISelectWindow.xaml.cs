using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
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
    /// ROISelect.xaml 的互動邏輯
    /// </summary>
    public partial class ROISelectWindow : Window
    {
        private Mat source;
        public ROISelectWindow(Mat source)
        {
            InitializeComponent();
            this.source = source;
            Mat temp = source.Clone();
            System.Drawing.Size size = new System.Drawing.Size(300, 300);
            CvInvoke.ResizeForFrame(temp, temp, size);
            cvIb.Image = source;
            cvIb.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            tb_currentArea.Text = MainWindow.getCfgValue<string>("ROI");
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FontSize = e.NewSize.Height / 20;
        }
        private void btn_ROI_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn.Name.Equals(btn_calculateArea.Name))
            {
                findContour();
            }
            else if (btn.Name.Equals(btn_setArea.Name) && tb_calculate.Text != "")
            {
                tb_setValue.Text = tb_calculate.Text;
            }
            else if (btn.Name.Equals(btn_save.Name) && tb_setValue.Text != "")
            {
                double saveThreshold = double.Parse(tb_setValue.Text);
                //Save saveThreshold * 0.9, Avoid detectionArea not enougth the value
                MainWindow.saveCfgValue("ROI", saveThreshold * 0.9);
                DialogResult = true;
            }
        }

        private void findContour()
        {
            #region filter
            Mat matFilter = new Mat();
            //灰質化
            CvInvoke.CvtColor(source, matFilter, 
                Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
            //閥值化
            CvInvoke.Threshold(matFilter, matFilter, 150, 255, 
                Emgu.CV.CvEnum.ThresholdType.BinaryInv);
            //CvInvoke.Imwrite("Thres-1.tif",matFilter);//輸出圖片

            //指定大小和形狀的結構元素，以進行形態學操作。
            Mat dilateElement = CvInvoke.GetStructuringElement(
                ElementShape.Ellipse,//元素形狀:橢圓
                new System.Drawing.Size(3, 3),//結構元素的尺寸
                new System.Drawing.Point(-1, -1)//在元素內的錨點位置。值（-1，-1）表示錨點位於中心。注意，只有十字形元件的形狀取決於錨位置。在其他情況下，錨點僅調節形態運算結果偏移了多少。
                );
            //使用指定的結構化元素對源圖像進行膨脹，該結構化元素確定在其上獲取最大值的像素鄰域的形狀。該功能支持就地模式。可以多次（重複）應用膨脹。如果是彩色圖像，則每個通道均獨立處理
            CvInvoke.Dilate(matFilter,
                matFilter,
                dilateElement, //用於侵蝕的結構化元素。如果是null，則使用3x3矩形結構元素
                new System.Drawing.Point(-1, -1), //錨點在元素內的位置；默認值（-1，-1）表示錨點位於元素中心。
                2, //施加腐蝕的次數
                Emgu.CV.CvEnum.BorderType.Default, //像素外推方法
                new MCvScalar(0, 0, 0) //邊界不變時的MCvScalar邊界值
                );

            //指定大小和形狀的結構元素，以進行形態學操作。
            Mat erodeElement = CvInvoke.GetStructuringElement(
                ElementShape.Ellipse,//元素形狀:橢圓
                new System.Drawing.Size(3, 3),//結構元素的尺寸
                new System.Drawing.Point(-1, -1)//在元素內的錨點位置。值（-1，-1）表示錨點位於中心。注意，只有十字形元件的形狀取決於錨位置。在其他情況下，錨點僅調節形態運算結果偏移了多少。
                );
            //數學形態學:腐蝕 引述定義參考膨脹↑
            CvInvoke.Erode(matFilter,
                matFilter,
                erodeElement,
                new System.Drawing.Point(-1, -1),
                2,
                Emgu.CV.CvEnum.BorderType.Default,
                new MCvScalar(0, 0, 0)
                );

            #endregion

            #region edge detection
            Mat edges = new Mat();
            //邊緣檢測
            CvInvoke.Canny(matFilter, edges, 200, 255);
            //CvInvoke.Imwrite("thres-2.tif", edges);
            //數學形態學:膨脹
            CvInvoke.Dilate(edges, edges, CvInvoke.GetStructuringElement(ElementShape.Ellipse, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1)), new System.Drawing.Point(-1, -1), 2, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(0, 0, 0));
            //數學形態學:腐蝕
            CvInvoke.Erode(edges, edges, CvInvoke.GetStructuringElement(ElementShape.Ellipse, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1)), new System.Drawing.Point(-1, -1), 2, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(0, 0, 0));
            //存放檢測輪廓
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            //檢測輪廓
            CvInvoke.FindContours(edges, contours, null, Emgu.CV.CvEnum.RetrType.Tree, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);

            #endregion

            #region calculateContours ???
            int ksize = contours.Size;
            double[] m00 = new double[ksize];//存放所有輪廓M00
            double[] m10 = new double[ksize];//存放所有輪廓M10
            double[] m01 = new double[ksize];//存放所有輪廓M01
            Moments[] _hu = new Moments[contours.Size];
            System.Drawing.Point[] _Gravity = new System.Drawing.Point[contours.Size];
            double[] areas = new double[contours.Size];
            Mat sourceWithData = source.Clone();
            for (int i = 0; i < contours.Size; i++)
            {
                //計算輪廓的面積
                areas[i] = CvInvoke.ContourArea(contours[i]);
                VectorOfPoint _tmoment = contours[i];
                //計算直到三階的空間和中心矩，並將它們寫入矩。然後可以使用這些矩來計算形狀的重心，形狀的面積，主軸以及包括7 Hu不變量在內的各種形狀特徵。
                _hu[i] = CvInvoke.Moments(
                    _tmoment, //圖像（設置了COI的1通道或3通道）或多邊形（點的CvSeq或點的向量）
                    false //（僅適用於圖像）如果該標誌為true，則將所有零像素值都視為零，將所有其他像素值均視為1。
                    );
                m00[i] = _hu[i].M00;
                m10[i] = _hu[i].M10;
                m01[i] = _hu[i].M01;
                double X = m10[i] / m00[i]; //get center X
                double Y = m01[i] / m00[i]; //get center y
                _Gravity[i] = new System.Drawing.Point((int)X, (int)Y);
                //在圖像輸出面積值
                CvInvoke.PutText(sourceWithData,
                    areas[i].ToString(),
                    _Gravity[i], //第一個字符左下角的座標
                    FontFace.HersheyPlain, //字體
                    3, //縮放因子
                    new MCvScalar(255, 0, 0), //顏色
                    5, //線段的寬度，以像素爲單位
                    LineType.EightConnected //輪廓線段的類型
                    );
            }

            #endregion

            #region drawing contours ???
            //在圖像繪製輪廓線
            CvInvoke.DrawContours(sourceWithData, //繪製輪廓的圖像。像其他任何繪圖功能一樣，用ROI裁剪輪廓
                contours, //所有輸入輪廓。每個輪廓都存儲為點向量。
                -1, //指示要繪製輪廓的參數。如果為負，則繪製所有輪廓。
                new MCvScalar(0, 0, 255), //輪廓顏色
                5, //繪製輪廓的線的粗細。如果為負，則繪製輪廓內部
                LineType.Filled //輪廓線段的類型
                                //有關層次結構的可選信息。僅當您只想繪製一些輪廓時才需要
                                //繪製輪廓的最大水平。如果為0，則僅繪製輪廓。如果為1，則將繪製輪廓以及其後在同一水平上的所有輪廓。如果為2，則繪製輪廓之後的所有輪廓以及輪廓之下的所有輪廓，等等。如果值為負，則該函數不繪製輪廓之後的輪廓，而是繪製輪廓的子輪廓直至abs（maxLevel）-1級別。
                                //所有點坐標移動指定值。如果在某些圖像ROI中檢索到輪廓，然後在渲染過程中需要考慮ROI偏移，則很有用。
                );
            tb_calculate.Text = areas.Max<double>().ToString();
            cvIb.Image = sourceWithData;
            cvIb.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;



            #endregion
        }
    }
}
