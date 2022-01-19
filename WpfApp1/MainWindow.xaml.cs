using Newtonsoft.Json.Linq;
using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string pathProject = System.IO.Directory.GetCurrentDirectory();
        private static string pathImg = pathProject + "\\Img\\process\\";
        private Mat SetterImg = new Mat();
        private Mat PlottingImg = new Mat();

        private bool enableSelect = false;
        private System.Drawing.Point startPoint;
        private System.Drawing.Point endPoint;
        private bool isStartingDraw = false;
        private int selectCircleNums = 0;
        private CircleF drawingCircle = new CircleF();
        private int thickness => 5;//(int)cvIbImageRect.Width / 180;
        private List<CircleF> circleList = new List<CircleF>();
        private int catchIntent = 0;
        private System.Drawing.Rectangle ROIrect;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tb_id.Text = getCfgValue<string>("idr");
            tb_od.Text = getCfgValue<string>("odr");
            tb_ROI.Text = getCfgValue<string>("ROI");
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FontSize = e.NewSize.Height / 20;
        }
        private void setCvIbImg(Mat source)
        {
            cvIb.Image = source;
            cvIb.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        }
        private void btn_catchPhoto_Click(object sender, RoutedEventArgs e)
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
            CvInvoke.BitwiseAnd(image, image, SetterImg);
            setCvIbImg(SetterImg);
        }
        private void btn_centerLine_Click(object sender, RoutedEventArgs e)
        {
            PlottingImg = SetterImg.Clone();

            System.Drawing.Point cpx1 = new System.Drawing.Point(
                0, PlottingImg.Height / 2);
            System.Drawing.Point cpx2 = new System.Drawing.Point(
                PlottingImg.Width, PlottingImg.Height / 2);
            System.Drawing.Point cpy1 = new System.Drawing.Point(
                PlottingImg.Width / 2, 0);
            System.Drawing.Point cpy2 = new System.Drawing.Point(
                PlottingImg.Width / 2, PlottingImg.Height);
            MCvScalar mCvScalar = new MCvScalar(0, 0, 255);

            CvInvoke.Line(PlottingImg, cpx1, cpx2, mCvScalar, 
                thickness, Emgu.CV.CvEnum.LineType.EightConnected);
            CvInvoke.Line(PlottingImg, cpy1, cpy2, mCvScalar,
                thickness, Emgu.CV.CvEnum.LineType.EightConnected);

            setCvIbImg(PlottingImg);

        }
        private void btn_circle_Click(object sender, RoutedEventArgs e)
        {
            PlottingImg = SetterImg.Clone();
            setCvIbImg(PlottingImg);
            enableSelect = true;
            selectCircleNums = 2;
            Button btn = sender as Button;
            if (btn.Name.Equals(btn_center.Name))
            {
                catchIntent = 0;
            }
            else if (btn.Name.Equals(btn_id.Name))
            {
                catchIntent = 1;
            }
            else if (btn.Name.Equals(btn_od.Name))
            {
                catchIntent = 2;
            }
            else if (btn.Name.Equals(btn_ROI.Name))
            {
                catchIntent = 3;
                selectCircleNums = 0;
            }
        }
        private void cvIb_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (enableSelect && catchIntent != 3)
            {
                if (isStartingDraw)
                {
                    endPoint = e.Location;
                    if (drawingCircle.Radius > 0 && drawingCircle.Center != null)
                    {
                        if(selectCircleNums > 0)
                        {
                            circleList.Add(drawingCircle);

                            //draw circles
                            float x = (drawingCircle.Center.X - cvIbImageOffestPoint.X) * scaleX;
                            float y = (drawingCircle.Center.Y - cvIbImageOffestPoint.Y) * scaleY;
                            System.Drawing.Point point = new System.Drawing.Point(
                                (int)x, (int)y);
                            float radius = drawingCircle.Radius*scaleX;
                            MCvScalar mCvScalar = new MCvScalar(255, 0, 0);
                            CvInvoke.Circle(PlottingImg, point,
                                (int)radius, 
                                mCvScalar, thickness);
                            setCvIbImg(PlottingImg);
                        }

                        if (selectCircleNums == 1)
                        {
                            isStartingDraw = false;
                            enableSelect = false;

                            updateCenterShift();
                        }
                        else selectCircleNums--;
                    }
                }
                else
                {
                    isStartingDraw = true;
                    startPoint = e.Location;
                }
            }

            else if (enableSelect && catchIntent == 3)
            {
                isStartingDraw = true;
                startPoint = e.Location;
            }
        }
        private void updateCenterShift()
        {
            List<double> radiuses = new List<double>();
            foreach(CircleF c in circleList)
            {
                radiuses.Add(c.Radius);
            }
            circleList.Clear();
            radiuses.Sort();
            double min = Math.Round(radiuses[0], 2) * scaleX;
            double max = Math.Round(radiuses[1], 2) * scaleX;
            Mat temp = SetterImg.Clone();
            temp = filterImageToGrabCircle(SetterImg);
            CircleF[] cfs = grabCircle(temp, min, max);
            temp = drawingCircles(SetterImg, cfs);
            if (cfs.Length != 1) return;
            CircleF cf = cfs[0];
            double radius = cf.Radius;
            if (temp != null) setCvIbImg(temp);
            switch (catchIntent)
            {
                case 0:
                    tb_center.Text = string.Format("X: {0}\nY: {1}",
                        cf.Center.X, cf.Center.Y);
                    break;
                case 1:
                    tb_id.Text = string.Format("{0}", Math.Round(radius, 2));
                    saveCfgValue("idr", Math.Round(radius, 2));
                    break;
                case 2:
                    tb_od.Text = string.Format("{0}", Math.Round(radius, 2));
                    saveCfgValue("odr", Math.Round(radius, 2));
                    break;
            }
        }
        private Mat filterImageToGrabCircle(Mat source)
        {
            Mat mat = source.Clone();
            CvInvoke.MedianBlur(mat, mat, 3);//中值濾波
            //CvInvoke.BilateralFilter(...)//雙邊過濾
            //中值濾波
            CvInvoke.MedianBlur(mat, mat, 3);
            //高斯滤波
            /*CvInvoke.GaussianBlur(mat, mat, new System.Drawing.Size(3, 3), 
                1.5,0, BorderType.Replicate);*/
            //灰質化
            CvInvoke.CvtColor(mat, mat, ColorConversion.Bgr2Gray);
            //閥值化
            CvInvoke.Threshold(mat, mat, 180, 255, ThresholdType.Binary);
            //CvInvoke.Imwrite(pathImg + "grabCircle.tif", mat);//輸出圖片
            return mat;
        }
        private CircleF[] grabCircle(Mat source, double min, double max)
        {
            Mat _grs = source.Clone();
            //霍夫圓變換 檢測圓
            return CvInvoke.HoughCircles(_grs,  
                HoughType.Gradient, //檢測方法
                2, //累加器分辨率與圖像分辨率的反比。例如，如果dp = 1，則累加器具有與輸入圖像相同的分辨率。如果dp = 2，則累加器的寬度和高度都是一半
                _grs.Size.Width, //檢測到的圓的中心之間的最小距離。太小會多檢，太大會漏檢
                150, //傳遞給Canny()檢測器的兩個閾值中的較高的閾值（較高的是較低的兩倍左右）
                50, //檢測階段圓心的累加器閾值。越小，可得到越多的圓
                (int)min, //最小圓半徑
                (int)max);//最大圓半徑
        }
        private Mat drawingCircles(Mat source, CircleF[] cfs)
        {
            Mat mat = source.Clone();
            foreach (CircleF circleF in cfs)
            {
                System.Drawing.Point point = new System.Drawing.Point(
                    (int)circleF.Center.X, (int)circleF.Center.Y);
                MCvScalar mCvScalar = new MCvScalar(0, 0, 255);

                CvInvoke.Circle(mat, point, (int)circleF.Radius,
                    mCvScalar,
                    (int)(thickness * scaleX)); //圓輪廓的粗細（如果為正），否則表示必須繪製實心圓
            }
            return cfs.Length > 0 ? mat : source;
        }
        
        
        private void cvIb_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (isStartingDraw)
            {
                endPoint = e.Location;
                cvIb.Invalidate();
            }
        }
        private void cvIb_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            if(drawingCircle.Center != null && enableSelect 
                && isStartingDraw && catchIntent != 3)
            {
                drawingCircle = new CircleF();
                drawingCircle.Center = startPoint;
                drawingCircle.Radius = (float)Math.Abs(Math.Sqrt(
                    Math.Pow(startPoint.X - endPoint.X, 2)
                    +Math.Pow(startPoint.Y - endPoint.Y, 2)
                    ));

                System.Drawing.Pen pen = new System.Drawing.Pen(
                    System.Drawing.Color.Red, thickness);
                e.Graphics.DrawEllipse(pen,
                    (drawingCircle.Center.X - drawingCircle.Radius), 
                    (drawingCircle.Center.Y - drawingCircle.Radius), 
                    drawingCircle.Radius * 2, drawingCircle.Radius * 2);
            }
        
            else if(isStartingDraw && enableSelect && catchIntent == 3)
            {
                ROIrect = new System.Drawing.Rectangle();
                ROIrect.X = Math.Min(startPoint.X, endPoint.X);
                ROIrect.Y = Math.Min(startPoint.Y, endPoint.Y);
                ROIrect.Width = Math.Abs(startPoint.X - endPoint.X);
                ROIrect.Height = Math.Abs(startPoint.Y - endPoint.Y);

                e.Graphics.DrawRectangle(Pens.Red, ROIrect);
            }
        }
        private void cvIb_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if(isStartingDraw && catchIntent == 3)
            {
                endPoint = e.Location;
                isStartingDraw = false;
                if (ROIrect != null)
                {
                    Image<Bgr, byte> imgInput = SetterImg.ToImage<Bgr, byte>();
                    ROIrect.X = (int)(ROIrect.X * scaleX);
                    ROIrect.Y = (int)(ROIrect.Y * scaleY);
                    ROIrect.Width = (int)(ROIrect.Width * scaleX);
                    ROIrect.Height = (int)(ROIrect.Height * scaleY);
                    imgInput.ROI = ROIrect;
                    Image<Bgr, byte> temp = imgInput.CopyBlank();
                    imgInput.CopyTo(temp);
                    imgInput.ROI = System.Drawing.Rectangle.Empty;
                    ROISelectWindow rOISelectWindow = new ROISelectWindow(temp.Mat);
                    
                    bool? result = rOISelectWindow.ShowDialog();
                    if (result == true) tb_ROI.Text = getCfgValue<string>("ROI");
                    enableSelect = false;
                }
            }
        }
        
        private void btn_test_Click(object sender, RoutedEventArgs e)
        {
            double _retVal;
            bool isSucceed;
            Mat teach = brakeplateInspection(
                SetterImg,
                (int)getCfgValue<double>("idr"), //範圍內半徑?
                (int)getCfgValue<double>("odr"), //範圍外半徑?
                (int)getCfgValue<double>("ROI"),
                out _retVal,
                out isSucceed);
            setCvIbImg(teach);
        }

        private Mat brakeplateInspection(Mat source, int idr, int odr, 
            double ROI, out double retVal, out bool isSucceed)
        {
            Mat process1 = filterImageToGrabCircle(source);

            #region process2: find centerPoint
            CircleF[] cfs = grabCircle(process1, odr * 0.9, odr * 1.1);
            // 判定抓取圓心是否成功
            Mat process2 = drawingCircles(process1, cfs);
            if (cfs.Length != 1)
            {
                retVal = 0;
                isSucceed = false;
                return process2;
            }

            System.Drawing.Point centerPoint = 
                new System.Drawing.Point((int)cfs[0].Center.X, (int)cfs[0].Center.Y);
            #region drawing center circle & print center point

            CvInvoke.PutText(process2, "X:"+ centerPoint.X + ",Y:"+ centerPoint.Y,
                centerPoint, FontFace.HersheySimplex, 2,
                new MCvScalar(0, 0, 255), 5);
            CvInvoke.Imwrite(pathImg + "process2.tif", process2);
            #endregion
            #endregion

            #region process3: dilate+Erode+dilate
            Mat process3 = process1.Clone();
            Mat element = CvInvoke.GetStructuringElement(
                ElementShape.Ellipse, 
                new System.Drawing.Size(3, 3), 
                new System.Drawing.Point(-1, -1));
            CvInvoke.Dilate(process3, process3,
                element, 
                new System.Drawing.Point(-1, -1), 
                3, BorderType.Default, new MCvScalar(0, 0, 0));
            CvInvoke.Erode(process3, process3,
                element, 
                new System.Drawing.Point(-1, -1), 
                3, BorderType.Default, new MCvScalar(0, 0, 0));
            CvInvoke.Dilate(process3, process3,
                element,
                new System.Drawing.Point(-1, -1), 
                3, BorderType.Default, new MCvScalar(0, 0, 0));
            CvInvoke.Imwrite(pathImg + "process3.tif", process3);
            #endregion

            #region process4: canny+dilate+Erode
            Mat process4 = new Mat(source.Size, DepthType.Cv8U, 1);//存放 找邊+膨脹+腐蝕
            //使用Canny算法 在輸入圖像上找到邊緣並將其標記在輸出圖像邊緣上。threshold1和threshold2中最小的用於邊緣鏈接，最大的用於查找強邊緣的初始片段。
            CvInvoke.Canny(process3,
                process4,
                200,//第一個閾值。
                250//第二個閾值。
                );
            //返回指定大小和形狀的結構元素，以進行形態學操作。
            Mat cverodstru = CvInvoke.GetStructuringElement(
                ElementShape.Ellipse, 
                new System.Drawing.Size(3, 3), 
                new System.Drawing.Point(-1, -1));
            //圖像形態學:膨脹
            CvInvoke.Dilate(process4, process4, cverodstru, 
                new System.Drawing.Point(-1, -1), 2, 
                BorderType.Default, new MCvScalar(0, 0, 0));
            //圖像形態學:腐蝕
            CvInvoke.Erode(process4, process4, cverodstru, 
                new System.Drawing.Point(-1, -1), 2, 
                BorderType.Default, new MCvScalar(0, 0, 0));
            CvInvoke.Imwrite(pathImg + "process4.tif", process4);
            #endregion

            #region process5: capture part of photo for need detect
            Mat maskIDCircle = new Mat(process4.Size, DepthType.Cv8U, 1);
            Mat maskODCircle = new Mat(process4.Size, DepthType.Cv8U, 1);
            Mat maskRange = new Mat(process4.Size, DepthType.Cv8U, 1);
            Mat process5 = new Mat(process4.Size, DepthType.Cv8U, 3);//存放要檢測的圖
            CvInvoke.Circle(maskIDCircle, centerPoint, odr, 
                new MCvScalar(255, 255, 255), -1);
            CvInvoke.Circle(maskODCircle, centerPoint, idr, 
                new MCvScalar(255, 255, 255), -1);
            maskODCircle = ~maskODCircle;
            maskIDCircle.CopyTo(maskRange, maskODCircle);//根據引數2範圍,將本身圖像為參考身拷貝到引數1
            process4.CopyTo(process5, maskRange);
            CvInvoke.Imwrite(pathImg + "process5-1.tif", maskRange);//ROI檢測範圍
            CvInvoke.Imwrite(pathImg + "process5-2.tif", process5);//輸出要檢測的圖
            #endregion

            #region process6: floodFill
            Mat process6 = process5.Clone();
            Mat g_maskImage = new Mat();
            //尺寸長寬要+2，給洪水填充用
            g_maskImage.Create(process6.Rows + 2, process6.Cols + 2, DepthType.Cv8U, 1); 
            System.Drawing.Rectangle _ccomp = new System.Drawing.Rectangle();
            //洪水填充
            CvInvoke.Imwrite(pathImg + "process6-1.tif", process6);//輸出要檢測的圖
            CvInvoke.FloodFill(
                process6, //輸入1或3通道，8位或浮點圖像。除非設置了CV_FLOODFILL_MASK_ONLY標誌，否則該函數會對其進行修改。
                g_maskImage, /*操作掩碼，應為單通道8位圖像，比圖像寬2像素，高2像素。
                如果不是IntPtr.Zero，則該功能將使用並更新遮罩，因此用戶將負責初始化遮罩內容。
                填充不能跨越遮罩中的非零像素，例如，邊緣檢測器的輸出可以用作遮罩以停止在邊緣填充。
                或者可以在多次調用函數時使用相同的遮罩，以確保填充區域不重疊。
                注意：由於遮罩大於填充的圖像，
                因此遮罩中與圖像中的（x，y）像素相對應的像素將具有（x + 1，y + 1）坐標。
                */
                new System.Drawing.Point(1, 1), //起點。
                new MCvScalar(255, 255, 255), //重繪的域像素的新值。
                out _ccomp, //該函數設置的輸出參數為重繪域的最小邊界矩形。
                new MCvScalar(10), /*當前觀察到的
                像素與其相鄰像素之一之間的最大較低亮度/色差屬於組件或種子像素，
                以將像素添加到組件。對於8位彩色圖像，它是打包值。*/
                new MCvScalar(10, 10, 10) /*當前觀察到的
                像素與其相鄰像素之一之間的最大上部亮度/色差屬於組件或種子像素，
                以將像素添加到組件。對於8位彩色圖像，它是打包值。*/
                );
            CvInvoke.Imwrite(pathImg + "process6-2.tif", process6);//輸出要檢測的圖
            process6 = ~process6;
            CvInvoke.Imwrite(pathImg + "process6-3.tif", process6);//輸出要檢測的圖
            #endregion

            #region process7: dilate+Erode+dilate
            Mat process7 = process6.Clone();
            CvInvoke.Dilate(process7, process7,
                element,
                new System.Drawing.Point(-1, -1),
                3, BorderType.Default, new MCvScalar(0, 0, 0));
            CvInvoke.Erode(process7, process7,
                element,
                new System.Drawing.Point(-1, -1),
                3, BorderType.Default, new MCvScalar(0, 0, 0));
            CvInvoke.Dilate(process7, process7,
                element,
                new System.Drawing.Point(-1, -1),
                3, BorderType.Default, new MCvScalar(0, 0, 0));
            CvInvoke.Imwrite(pathImg + "process7.tif", process7);
            #endregion

            #region process8: canny+dilate+Erode
            Mat process8 = new Mat();
            CvInvoke.Canny(process7, process8, 200, 255);
            CvInvoke.Dilate(process8, process8, element, 
                new System.Drawing.Point(-1, -1), 2, 
                BorderType.Default, new MCvScalar(0, 0, 0));
            CvInvoke.Erode(process8, process8, element, 
                new System.Drawing.Point(-1, -1), 2, 
                BorderType.Default, new MCvScalar(0, 0, 0));
            CvInvoke.Imwrite(pathImg + "process8.tif", process8);
            #endregion

            #region process9: findContours
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            //找輪廓
            CvInvoke.FindContours(process8, contours, null, RetrType.External, ChainApproxMethod.ChainApproxNone);
            //在源圖繪製輪廓
            Mat process9 = source.Clone();
            CvInvoke.DrawContours(process9, contours, -1, new MCvScalar(0, 0, 255), thickness, LineType.Filled);
            CvInvoke.Imwrite(pathImg + "process9.tif", process9);
            #endregion

            #region filter the contours which of area > ROI by setting, and check quantity
            VectorOfVectorOfPoint targContour = new VectorOfVectorOfPoint();
            //篩選要的輪廓
            for (int i = 0; i < contours.Size; i++)
            {
                //計算輪廓面積
                double area = CvInvoke.ContourArea(contours[i]);
                if (area > ROI)
                {
                    targContour.Push(contours[i]);
                }
            }
            int contourNums = targContour.Size;// 範圍面積大於條件直的contour
            // 判斷標籤便識是否成功
            if (contourNums != 3)
            {
                retVal = 0;
                isSucceed = false;
                return null;
            }
            #endregion

            #region featurePoint: calculate & draw featurePoint
            //找輪廓的幾何中心
            double[] m00 = new double[contourNums];
            double[] m10 = new double[contourNums];
            double[] m01 = new double[contourNums];
            Mat featurePoint = source.Clone();
            System.Drawing.Point[] gravity = new System.Drawing.Point[contourNums];
            Moments[] hu = new Moments[contourNums];
            for (int i = 0; i < contourNums; i++)
            {
                VectorOfPoint _tmomentcal = targContour[i];
                hu[i] = CvInvoke.Moments(_tmomentcal, false);
                m00[i] = hu[i].M00;
                m10[i] = hu[i].M10;
                m01[i] = hu[i].M01;
                double X = m10[i] / m00[i]; //get center X
                double Y = m01[i] / m00[i]; //get center Y

                var gc = hu[i].GravityCenter;
                gravity[i] = new System.Drawing.Point((int)X, (int)Y);

                CvInvoke.Circle(featurePoint,
                new System.Drawing.Point
                {
                    X = (int)X,
                    Y = (int)Y
                }, thickness*5, new MCvScalar(0, 0, 255), thickness);
            }

            //找3點中心
            System.Drawing.Point _COG = new System.Drawing.Point();
            foreach (System.Drawing.Point cent in gravity)
            {
                _COG.X += cent.X;
                _COG.Y += cent.Y;
            }
            //幾何中心
            _COG.X = _COG.X / 3;
            _COG.Y = _COG.Y / 3;

            int radius = thickness * 5;
            CvInvoke.Circle(featurePoint, _COG, radius, new MCvScalar(0, 0, 255), thickness);
            CvInvoke.DrawContours(featurePoint, targContour, -1, new MCvScalar(0, 0, 255), thickness*5, LineType.Filled);
            CvInvoke.Circle(featurePoint, centerPoint, radius, new MCvScalar(0, 0, 255), thickness);

            System.Drawing.Point textPos = new System.Drawing.Point(centerPoint.X, centerPoint.Y);
            textPos.X += radius;
            textPos.Y += radius / 2;
            int fontScale = thickness;
            CvInvoke.PutText(featurePoint, "X:" + centerPoint.X,
                textPos, FontFace.HersheySimplex, fontScale,
                new MCvScalar(0, 0, 255), thickness);
            textPos.Y += radius * fontScale;
            CvInvoke.PutText(featurePoint, "Y:" + centerPoint.Y,
                textPos, FontFace.HersheySimplex, fontScale,
                new MCvScalar(0, 0, 255), thickness);
            CvInvoke.Imwrite(pathImg + "featurePoint.tif", featurePoint);


            // 角度換算
            double _TargM = 0;
            int maxIndex = 0;
            if (contourNums == 3)//輪廓=3個
            {
                double[] tv = new double[3];
                for (int _i = 0; _i < contourNums; _i++)
                {
                    tv[_i] = Math.Pow((gravity[_i].X - _COG.X), 2) + Math.Pow((gravity[_i].Y - _COG.Y), 2);
                }
                maxIndex = tv.ToList().IndexOf(tv.Max());//找出離幾何中心最遠的index

                double deltaX = gravity[maxIndex].X - _COG.X;//
                double deltaY = -gravity[maxIndex].Y + _COG.Y;//
                _TargM = Math.Atan2(deltaY, deltaX) * (180 / Math.PI);//Math.Atan2出來是徑度，需*180/pi轉成角度
                _TargM = _TargM - 90;//將角度0度參考點轉為垂直向下為0度參考點
                if (Math.Abs(_TargM) > 180) _TargM = _TargM + 360;//讓數值介於-180~+180
            }
            #endregion

            #region Drawing teach data
            Mat teachMat = source.Clone();
            _TargM = Math.Round(_TargM, 2);
            CvInvoke.Circle(teachMat, 
                new System.Drawing.Point { 
                    X = (int)gravity[maxIndex].X, 
                    Y = (int)gravity[maxIndex].Y 
                }, 5, new MCvScalar(0, 0, 255), 10);
            CvInvoke.Line(teachMat, 
                new System.Drawing.Point { 
                    X = (int)gravity[maxIndex].X, 
                    Y = (int)gravity[maxIndex].Y 
                }, 
                new System.Drawing.Point { 
                    X = (int)cfs[0].Center.X, 
                    Y = (int)cfs[0].Center.Y 
                }, new MCvScalar(0, 0, 255), thickness);

            CvInvoke.PutText(teachMat, _TargM.ToString() + "Degree", 
                new System.Drawing.Point { 
                    X = (int)gravity[maxIndex].X, 
                    Y = (int)gravity[maxIndex].Y 
                }, FontFace.HersheySimplex, fontScale, 
                new MCvScalar(0, 0, 255), thickness);
            CvInvoke.Imwrite("Answer.tif", teachMat);
            #endregion

            retVal = _TargM;
            isSucceed = true;
            return teachMat;
        }

        private void btn_msg_Click(object sender, RoutedEventArgs e)
        {
            btn_msg.Content = string.Format("Width = {0},Height = {1}",
                cvIbImageRect.Width.ToString(), cvIbImageRect.Height.ToString());
            //SetterImg
            btn_msg.Content += string.Format("\nWidth = {0},Height = {1}",
                SetterImg.Width, SetterImg.Height);

            string path = Directory.GetCurrentDirectory();
            System.Diagnostics.Process prc = new System.Diagnostics.Process();
            prc.StartInfo.FileName = path;
            prc.Start();
        }

        private float scaleX => SetterImg == null ? 1 : (float)
            (SetterImg.Width / cvIbImageRect.Width);
        private float scaleY => SetterImg == null ? 1 : (float)
            (SetterImg.Height / cvIbImageRect.Height);
        private Rect cvIbImageRect {
            get
            {
                Rect rect = new Rect();

                Mat source = cvIb.Image.GetInputArray().GetMat();
                float scaleSource = (float)source.Width / (float)source.Height;
                float x = cvIb.Height * scaleSource;
                if (cvIb.Width > x)
                {
                    rect.Width = cvIb.Height * scaleSource;
                    rect.Height = cvIb.Height;
                }
                else
                {
                    rect.Width = cvIb.Width;
                    rect.Height = cvIb.Width / scaleSource;
                }
                return rect;
            }
        }
        private System.Drawing.Point cvIbImageOffestPoint
        {
            get
            {
                System.Drawing.Point point = new System.Drawing.Point();
                if(cvIbImageRect.Width == cvIb.Width)
                {
                    point.Y = (cvIb.Height - (int)cvIbImageRect.Height) / 2;
                }
                else if (cvIbImageRect.Height == cvIb.Height)
                {
                    point.X = (cvIb.Width - (int)cvIbImageRect.Width) / 2;
                }
                return point;
            }
        }
        
        #region Json
        private static readonly string P = Directory.GetCurrentDirectory() + "\\preference.cfg";
        private static void saveCfg(JObject jObject)
        {
            File.Delete(P);
            File.WriteAllText(P, jObject.ToString());
        }
        private static JObject readCfg()
        {
            try
            {
                return JObject.Parse(File.ReadAllText(P));
            }
            catch
            {
                return new JObject();
            }
        }
        public static void saveCfgValue(string propertyName, object o)
        {
            JObject jObject = readCfg();
            JToken value = JToken.FromObject(o);
            if (jObject.ContainsKey(propertyName))
            {
                jObject[propertyName] = value;
            }
            else
            {
                jObject.Add(propertyName, value);
            }
            saveCfg(jObject);
        }
        public static T getCfgValue<T>(string propertyName)
        {
            JObject jObject = readCfg();
            if (jObject.ContainsKey(propertyName))
            {
                return jObject[propertyName].ToObject<T>();
            }
            return default(T);
        }

        #endregion

    }
}
