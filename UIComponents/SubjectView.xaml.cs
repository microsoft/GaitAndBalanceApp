using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GaitAndBalanceApp.UIComponents
{
    /// <summary>
    /// Interaction logic for silhouetteView.xaml
    /// </summary>
    public partial class SubjectView : UserControl
    {
        Frame _frame;
        public Frame Frame
        {
            get { return _frame; }
            set
            {
                if (_frame != value)
                {
                    _frame = value;
                    try { DrawSubject(); }
                    catch (TaskCanceledException) { }
                }
            }
        }

        public Color skeletonColor = Colors.Navy;
        public Color silhouetteColor = Colors.Cyan;
        public bool drawSkeleton = true;
        public bool drawLineModel = true;
        public bool drawSilhouette = true;
        public bool drawGrid = true;
        


        Line[] frontViewBones = null, sideViewBones = null;
        Line[] sideGrid = null;
        Line[] frontGrid = null;
        Label[] sideLabels = null;
        Label[] frontLabels = null;
        Line sideStick, frontStick;
        Label sideAngel, frontAngel;
        EinputMode input;
        EprojectionMode projection;

        float xRange, yRange, zRange;

        JointTypeGait[] bonesEndPoints = new JointTypeGait[] {
            JointTypeGait.Neck, JointTypeGait.Head,
            JointTypeGait.Neck, JointTypeGait.SpineMid,
            JointTypeGait.SpineBase, JointTypeGait.SpineMid,
            JointTypeGait.SpineBase, JointTypeGait.HipRight,
            JointTypeGait.KneeRight, JointTypeGait.HipRight,
            JointTypeGait.KneeRight, JointTypeGait.AnkleRight,
            JointTypeGait.SpineBase, JointTypeGait.HipLeft,
            JointTypeGait.KneeLeft, JointTypeGait.HipLeft,
            JointTypeGait.KneeLeft, JointTypeGait.AnkleLeft,
            JointTypeGait.Neck, JointTypeGait.ShoulderRight,
            JointTypeGait.ElbowRight, JointTypeGait.ShoulderRight,
            JointTypeGait.ElbowRight, JointTypeGait.WristRight,
            JointTypeGait.HandRight, JointTypeGait.WristRight,
            JointTypeGait.Neck, JointTypeGait.ShoulderLeft,
            JointTypeGait.ElbowLeft, JointTypeGait.ShoulderLeft,
            JointTypeGait.ElbowLeft, JointTypeGait.WristLeft,
            JointTypeGait.HandLeft, JointTypeGait.WristLeft};

        public SubjectView()
        {
            input = (EinputMode)Enum.Parse(typeof(EinputMode), ConfigurationManager.AppSettings["inputMode"], true);
            projection = (EprojectionMode)Enum.Parse(typeof(EprojectionMode), ConfigurationManager.AppSettings["projectionMode"], true);
            Boolean.TryParse(ConfigurationManager.AppSettings["drawSkeleton"], out drawSkeleton);
            Boolean.TryParse(ConfigurationManager.AppSettings["drawLineModel"], out drawLineModel);
            Boolean.TryParse(ConfigurationManager.AppSettings["drawSilhouette"], out drawSilhouette);
            Boolean.TryParse(ConfigurationManager.AppSettings["drawGrid"], out drawGrid);
            float.TryParse(ConfigurationManager.AppSettings["xRange"], out xRange);
            float.TryParse(ConfigurationManager.AppSettings["yRange"], out yRange);
            float.TryParse(ConfigurationManager.AppSettings["zRange"], out zRange);
            InitializeComponent();
            if (drawGrid)
            {
                sideGrid = new Line[17];
                for (int i = 0; i < sideGrid.Length; i++)
                {
                    Line l = new Line();
                    sideGrid[i] = l;
                    l.StrokeThickness = 1;
                    l.Stroke = new SolidColorBrush(Colors.Gray);
                    l.Opacity = 0.25;
                    sideView.Children.Add(l);
                }
                sideLabels = new Label[7];
                for (int i = 0; i < sideLabels.Length; i++)
                {
                    Label l = new Label
                    {
                        Content = (i + 1).ToString(),
                        FontSize = 10,
                        Opacity = 0.25
                    };
                    sideView.Children.Add(l);
                    sideLabels[i] = l;
                }
                frontGrid = new Line[7];
                for (int i = 0; i < frontGrid.Length; i++)
                {
                    Line l = new Line();
                    frontGrid[i] = l;
                    l.StrokeThickness = 1;
                    l.Stroke = new SolidColorBrush(Colors.Gray);
                    l.Opacity = 0.25;
                    frontView.Children.Add(l);
                }
                frontLabels = new Label[7];
                for (int i = 0; i < frontLabels.Length; i++)
                {
                    Label l = new Label();
                    if (i == 0) l.Content = "left";
                    else if (i == frontLabels.Length - 1) l.Content = "right";
                    else l.Content = ((0.5 + i - frontLabels.Length * 0.5) * 0.5).ToString();
                    l.FontSize = 10;
                    l.Opacity = 0.25;
                    frontView.Children.Add(l);
                    frontLabels[i] = l;
                }
            }
            if (drawSkeleton)
            {
                frontViewBones = new Line[bonesEndPoints.Length / 2];
                sideViewBones = new Line[bonesEndPoints.Length / 2];
                for (int i = 0; i < frontViewBones.Length; i++)
                {
                    Line l = new Line();
                    frontViewBones[i] = l;
                    l.StrokeThickness = 4;
                    frontView.Children.Add(l);
                    l = new Line();
                    sideViewBones[i] = l;
                    l.StrokeThickness = 4;
                    sideView.Children.Add(l);
                }
            }
            if (drawLineModel)
            {
                frontStick = new Line
                {
                    Stroke = Brushes.Maroon,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection() { 4 }
                };
                frontView.Children.Add(frontStick);

                frontAngel = new Label
                {
                    FontSize = 20,
                    Foreground = Brushes.Maroon
                };
                frontView.Children.Add(frontAngel);

                sideStick = new Line
                {
                    Stroke = Brushes.Maroon,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection() { 4 }
                };
                sideView.Children.Add(sideStick);

                sideAngel = new Label
                {
                    FontSize = 20,
                    Foreground = Brushes.Maroon
                };
                sideView.Children.Add(sideAngel);
            }
            this.SizeChanged += ViewChanged;

        }


        private double Positive(double x)
        {
            return (x < 0) ? 0 : x;
        }

        private void ViewChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            if (!drawGrid) return;
            sideView.Dispatcher.Invoke(new Action(delegate()
            {

                double xScale, yScale;
                xScale = sideView.ActualWidth / zRange;
                yScale = sideView.ActualHeight / 4;
                for (int i = 0; i < sideGrid.Length; i++)
                {
                    sideGrid[i].X1 = (double)(i) * xScale / 2;
                    sideGrid[i].X2 = (double)(i) * xScale / 2;
                    sideGrid[i].Y1 = 0.5 * yScale;
                    sideGrid[i].Y2 = 3.5 * yScale;

                }
                for (int i = 0; i < sideLabels.Length; i++)
                {
                    Canvas.SetLeft(sideLabels[i], (1.0 + i) * xScale - 0.5 * sideLabels[i].ActualWidth);
                    Canvas.SetTop(sideLabels[i], 3.5 * yScale);
                }

                xScale = frontView.ActualWidth / 4;
                yScale = frontView.ActualHeight / 4;
                for (int i = 0; i < frontGrid.Length; i++)
                {
                    frontGrid[i].X1 = ((0.5 + i - frontLabels.Length * 0.5) * 0.5 + 2) * xScale;
                    frontGrid[i].X2 = ((0.5 + i - frontLabels.Length * 0.5) * 0.5 + 2) * xScale;
                    frontGrid[i].Y1 = 0.5 * yScale;
                    frontGrid[i].Y2 = 3.5 * yScale;

                }
                for (int i = 0; i < frontLabels.Length; i++)
                {
                    Canvas.SetLeft(frontLabels[i], ((0.5 + i - frontLabels.Length * 0.5) * 0.5 + 2) * xScale - 0.5 * frontLabels[i].ActualWidth);
                    Canvas.SetTop(frontLabels[i], 3.5 * yScale);
                }

            }));


        }


        private void DrawSubject()
        {
            if (Frame == null) return;
            byte[] frontPixels = new byte[4 * 256 * 256];
            byte[] sidePixels = new byte[4 * 256 * 256];
            double xScaleSide, yScaleSide, xScaleFront, yScaleFront;
            xScaleSide = sideView.ActualWidth / zRange;
            yScaleSide = sideView.ActualHeight / yRange;
            xScaleFront = frontView.ActualWidth / xRange;
            yScaleFront = frontView.ActualHeight / yRange;
            float halfXRange = xRange / 2;

            if (Frame.silhouette.points != null && Frame.silhouette.points.Length > 0 && drawSilhouette)
            {

                xRange = Frame.silhouette.xRange;
                yRange = Frame.silhouette.yRange;
                zRange = Frame.silhouette.zRange;
                xScaleSide = sideView.ActualWidth / zRange;
                yScaleSide = sideView.ActualHeight / yRange;
                xScaleFront = frontView.ActualWidth / xRange;
                yScaleFront = frontView.ActualHeight / yRange;

                byte silhouetteRed = silhouetteColor.R;
                byte silhouetteGreen = silhouetteColor.G;
                byte silhouetteBlue = silhouetteColor.B;

                foreach (var p in Frame.silhouette.points)
                {
                    int x = p.X;
                    int y = 255 - p.Y;
                    int z = p.Z;

                    // we need to color multiple points since the resolution decreases as we get further away
                    for (int delta1 = -5; delta1 <=5; delta1++)
                    {
                        if (y + delta1 < 0 || y + delta1 > 255) continue;
                        for (int delta2 = -5; delta2 <=5; delta2++)
                        {
                            if (x + delta2 >= 0 && x + delta2 < 256)
                            {
                                int frontPixelPointer = 4 * (x + delta2 + (y + delta1) * 256);

                                frontPixels[frontPixelPointer] = silhouetteBlue;
                                frontPixels[frontPixelPointer + 1] = silhouetteGreen;
                                frontPixels[frontPixelPointer + 2] = silhouetteRed;
                                double norm = (delta1 * delta1) / (yRange * yRange) + (delta2 * delta2) / (xRange * xRange);
                                frontPixels[frontPixelPointer + 3] = (byte)Math.Min(100, frontPixels[frontPixelPointer + 3] + p.Confidence * Math.Exp(-512 * norm / (z + 1)));
                            }
                            if (z + delta2 >= 0 && z + delta2 < 256)
                            {
                                int sidePixelPointer = 4 * (z + delta2 + (y + delta1) * 256);
                                sidePixels[sidePixelPointer] = silhouetteBlue;
                                sidePixels[sidePixelPointer + 1] = silhouetteGreen;
                                sidePixels[sidePixelPointer + 2] = silhouetteRed;
                                double norm = (delta1 * delta1) / (yRange * yRange) + 4 * (delta2 * delta2) / (zRange * zRange);
                                sidePixels[sidePixelPointer + 3] = (byte)Math.Min(100, sidePixels[sidePixelPointer + 3] + p.Confidence * Math.Exp(-512 * norm / (z + 1)));
                          
                            }
                            
                        }
                    }
                };
            }
            if (Frame.Joints != null && drawSkeleton)
            {

                sideView.Dispatcher.Invoke(new Action(delegate()
                {
                    var brush = new SolidColorBrush(skeletonColor);
                    for (int i = 0; i < frontViewBones.Length; i++)
                    {
                        var t1 = Frame.getJointPositionFromGround(bonesEndPoints[i * 2], false, out float x, out float y, out float z);
                        frontViewBones[i].X1 = Positive((x + halfXRange) * xScaleFront);
                        frontViewBones[i].Y1 = Positive((yRange - y) * yScaleFront);
                        sideViewBones[i].X1 = Positive(z * xScaleSide);
                        sideViewBones[i].Y1 = Positive((yRange - y) * yScaleSide);
                        var t2 = Frame.getJointPositionFromGround(bonesEndPoints[i * 2 + 1], false, out x, out y, out z);
                        frontViewBones[i].X2 = Positive((x + halfXRange) * xScaleFront);
                        frontViewBones[i].Y2 = Positive((yRange - y) * yScaleFront);
                        sideViewBones[i].X2 = Positive(z * xScaleSide);
                        sideViewBones[i].Y2 = Positive((yRange - y) * yScaleSide);

                        if (t1 != TrackingStateGait.Tracked || t2 != TrackingStateGait.Tracked)
                        {
                            frontViewBones[i].Stroke = Brushes.Transparent;
                            sideViewBones[i].Stroke = Brushes.Transparent;
                        }
                        else
                        {
                            frontViewBones[i].Stroke = brush;
                            sideViewBones[i].Stroke = brush;
                        }
                    }
                }));
            }

            Frame.getCOM(out float xCom, out float zCom, out float slopeX, out float slopeZ, out Extreams extreamValues, input, projection);


            sideView.Dispatcher.Invoke(new Action(delegate()
            {
                if (drawSilhouette)
                {
                    BitmapSource frontSource = BitmapSource.Create(256, 256, 100, 100, PixelFormats.Bgra32, null, frontPixels, 256 * 4);
                    frontImage.Source = frontSource;
                    BitmapSource sideSource = BitmapSource.Create(256, 256, 100, 100, PixelFormats.Bgra32, null, sidePixels, 256 * 4);
                    sideImage.Source = sideSource;
                }
                calibration.Value = Frame.silhouette.trackQuality;
                if (Frame.silhouette.trackQuality == 0)
                    calibration.Background = Brushes.Orange;
                else if (Frame.silhouette.trackQuality < 100)
                    calibration.Background = Brushes.Red;
                else if (Frame.silhouette.trackQuality < 200)
                    calibration.Background = Brushes.Yellow;
                else calibration.Background = Brushes.AntiqueWhite;
                xPosition.Text = xCom.ToString("n2");
                zPosition.Text = zCom.ToString("n2");
                frameNumber.Content = Frame.FrameNumber.ToString();
                // draw line (stick) model
                if (drawLineModel)
                {
                    Frame.getStickModelFromSilhouette(out float silhouetteSlopeX, out float silhouetteSlopeZ, out float silhouetteOffsetX, out float silhouetteOffsetZ, EprojectionMode.ground);
                    int maxHeight = 0;
                    if (Frame.silhouette.points != null && Frame.silhouette.points.Length > 0)
                    {
                        foreach (var p in Frame.silhouette.points)
                            if (p.Y > maxHeight) maxHeight = p.Y;
                    }
                    double stickHeight = yRange * maxHeight / 256;
                    frontStick.X1 = (silhouetteOffsetX + halfXRange) * xScaleFront;
                    frontStick.Y1 = yRange * yScaleFront;
                    frontStick.X2 = (silhouetteOffsetX + stickHeight * silhouetteSlopeX + halfXRange) * xScaleFront;
                    frontStick.Y2 = (yRange - stickHeight) * yScaleFront;

                    sideStick.X1 = silhouetteOffsetZ * xScaleSide;
                    sideStick.Y1 = yRange * yScaleSide;
                    sideStick.X2 = (silhouetteOffsetZ + stickHeight * silhouetteSlopeZ) * xScaleSide;
                    sideStick.Y2 = (yRange - stickHeight) * yScaleSide;

                    frontAngel.Content = (Math.Asin(silhouetteSlopeX) * 180 / Math.PI).ToString("n1");
                    Canvas.SetBottom(frontAngel, 0);
                    Canvas.SetLeft(frontAngel, frontStick.X1 - 0.5 * frontAngel.ActualWidth);

                    sideAngel.Content = (Math.Asin(silhouetteSlopeZ) * 180 / Math.PI).ToString("n1");
                    Canvas.SetBottom(sideAngel, 0);
                    Canvas.SetLeft(sideAngel, sideStick.X1 - 0.5 * sideAngel.ActualWidth);
                }

            }));
        }

    }
}
