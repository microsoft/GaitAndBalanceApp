// the SAVE_IMAGES is very handy for debugging. It will save all the frames for which it could not find any object
//#define SAVE_IMAGES
//#define SAVE_XYZ

using Microsoft.Kinect;
using ShoNS.Array;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#if SAVE_IMAGES
using System.Drawing;
using System.Drawing.Imaging;
#endif
#if SAVE_XYZ
using System.IO;
#endif


namespace GaitAndBalanceApp
{
    public sealed class KinectFactory
    {
        static readonly Kinect kinect = new Kinect();

        public static Kinect Instance { get { return kinect; } }

        KinectFactory() { }
    }
    public class Kinect : IDisposable
    {
         KinectSensor kinectSensor;
         CoordinateMapper cm;
         long frameNum = -1;
         TimeSpan curStartTime;
         Memory memory = new Memory();
         MultiSourceFrameReader multiSourceReader;

        // countdown is used to synchronize the shutdown
         public double fps = 0.0;

         Body[] bodies;

        // constants used for the creating the silhouette 
        private readonly ushort close = 500;
        private readonly short delta = 150;
        private readonly double minimalHeightFromGround = 0.1; 
        public double maximalDistanceFromGroundOfSubject = 0.5;
        public double minimalHeightOfSubject = 1;
        public double minimalDistanceFromCeilingOfSubject = 0.25;
        public int minimalNumberOfPixelsInSubject = 50;
        float _xRange, _yRange, _zRange;
        public float XRange { get { return _xRange; } }
        public float YRange { get { return _yRange; } }
        public float ZRange { get { return _zRange; } }
        public readonly int silhouetteBlockSize = 8;
        public ushort intensityBasedCutoffThreshold = 0;

        // memory pre-allocated for creating the silhouette. part of it is made public for debug
        public ushort[] depthImageBuffer, depthImageBuffer2, infraRedBuffer; // current depth image
        private double[] heights; // heights (from the ground) of points in the current image
        private float[] silhouetteSumX; // internal storage for computing silhouette
        private float[] silhouetteSumY; // internal storage for computing silhouette
        private float[] silhouetteSumZ; // internal storage for computing silhouette
        private int[] silhouetteCount; // internal storage for computing silhouette
        CameraSpacePoint[] cameraPointBuffer;
        CameraSpacePoint[] cameraPointBufferForFloor; // we copy the point buffer such that the floor computation can be async
        public byte[] playerMask;
        public byte[] playerMaskMemory, playerMaskMemory2;
        public Vector4 ground = new Vector4();
        public Vector4 newGround = new Vector4();
        public double newFloorConfidence = 0;
        public double ceiling; // the height of the ceiling 
        public double newCeiling; // the height of the ceiling 
        public double halfWidthOfROI = 0.75; // the width of the interest region around the kinect sensor. The walkable region is twice this number.
        public double depthOfROI = 10; // the depth of the interest region around the kinect sensor
        public double lowerROI = 0.5; // the lower bound on the height of the area of interest
        public double upperROI = 1.5; // the upper bound on the height of the area of interest
        public double mixTarget = 0.02; // The target mixing rate for the IIR for ground calculation
        double currentMix = 1.0; // Current mix rate - starts high to allow fast tracking of the ground
        double mixUpdateRate = 0.1; // the rate in which the updates converge to the desired update rate
        readonly int blockSize = 8; 
        Vector4[,] grounds;
        Vector[,] means;
        double[,] X, Y, Z, XX, XY, XZ, YY, YZ, ZZ;
        int[,] n;
        List<int> componentMap = new List<int>();
        List<int> sizeOfComponent = new List<int>();
        List<double> minHeight = new List<double>();
        List<double> maxHeight = new List<double>();
        int[] membership;
        DateTime lastFrameUpdate = DateTime.Now;
        Task groundTask = null; // this is an async task to look for the ground in the frame


        private int bodyIndex = -1; // the index of the tracked body

        readonly object _lockForFrameInProcess = new object();
        DateTime currentTime;
        TimeSpan frameTime;
        Frame frame;

        public Kinect()
        {
            Init();
        }

        // Initialize the sensor, open reader
        public void Init()
        {
            kinectSensor = KinectSensor.GetDefault();                
            cm = kinectSensor.CoordinateMapper;
            kinectSensor.Open();
            bodies = new Body[kinectSensor.BodyFrameSource.BodyCount];   // you can get up to BodyCount # of bodies from the Kinect... (we'll just use the first)
            int bufferLength = kinectSensor.DepthFrameSource.FrameDescription.Width * kinectSensor.DepthFrameSource.FrameDescription.Height;
            depthImageBuffer = new ushort[bufferLength];
            depthImageBuffer2 = new ushort[bufferLength];
            infraRedBuffer = new ushort[bufferLength];
            heights = new double[bufferLength];
            playerMaskMemory = new byte[bufferLength];
            playerMaskMemory2 = new byte[bufferLength];
            playerMask = null;
            membership = new int[bufferLength];
            silhouetteCount = new int[1 + bufferLength / (silhouetteBlockSize * silhouetteBlockSize)];
            silhouetteSumX = new float[1 + bufferLength / (silhouetteBlockSize * silhouetteBlockSize)];
            silhouetteSumY = new float[1 + bufferLength / (silhouetteBlockSize * silhouetteBlockSize)];
            silhouetteSumZ = new float[1 + bufferLength / (silhouetteBlockSize * silhouetteBlockSize)];

            cameraPointBuffer = new CameraSpacePoint[bufferLength];
            cameraPointBufferForFloor = new CameraSpacePoint[bufferLength];
            multiSourceReader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.BodyIndex | FrameSourceTypes.Depth | FrameSourceTypes.Infrared);
            multiSourceReader.MultiSourceFrameArrived += MultiSourceReader_MultiSourceFrameArrived;                   
            int xSize = (kinectSensor.DepthFrameSource.FrameDescription.Width + blockSize - 1) / blockSize;
            int ySize = (kinectSensor.DepthFrameSource.FrameDescription.Height + blockSize - 1) / blockSize;
            X = new double[xSize, ySize];
            Y = new double[xSize, ySize];
            Z = new double[xSize, ySize];
            XX = new double[xSize, ySize];
            XY = new double[xSize, ySize];
            XZ = new double[xSize, ySize];
            YY = new double[xSize, ySize];
            YZ = new double[xSize, ySize];
            ZZ = new double[xSize, ySize];
            n = new int[xSize, ySize];
            grounds = new Vector4[xSize, ySize];
            for (int i = 0; i < grounds.GetLength(0); i++)
                for (int j = 0; j < grounds.GetLength(1); j++)
                    grounds[i, j] = new Vector4();
            means = new Vector[xSize, ySize];
            for (int i = 0; i < grounds.GetLength(0); i++)
                for (int j = 0; j < grounds.GetLength(1); j++)
                    means[i, j] = new Vector();
            float.TryParse(ConfigurationManager.AppSettings["xRange"], out _xRange);
            float.TryParse(ConfigurationManager.AppSettings["yRange"], out _yRange);
            float.TryParse(ConfigurationManager.AppSettings["zRange"], out _zRange);
            double.TryParse(ConfigurationManager.AppSettings["mixRate"], out mixTarget);
            mixUpdateRate = Math.Sqrt(mixTarget);
            intensityBasedCutoffThreshold = (ConfigurationManager.AppSettings["reflectiveSeperation"] == "true") ? ushort.Parse(ConfigurationManager.AppSettings["reflectiveSeperationCutoff"]) : (ushort)0;

        }

        // Stop acquisition, free up resources
        public void Dispose()
        {
            if (multiSourceReader != null) multiSourceReader.Dispose();
            Thread.Sleep(100); // wait in case a frame is being processed
            kinectSensor.Close();
        }

        void UpdateFrameRate(DateTime currentTime)
        {
            var interval = (currentTime - lastFrameUpdate).TotalSeconds;
            lastFrameUpdate = currentTime;
            if (interval > 0)
                fps = 0.9 * fps + 0.1 / interval;
        }

        // A Kinect One Joint to our defined structure
         JointGait JointToJointGait(Joint joint)
        {
            JointGait jointGait = new JointGait
            {
                JointType = (JointTypeGait)joint.JointType,
                TrackingState = (TrackingStateGait)joint.TrackingState,
                X = joint.Position.X,
                Y = joint.Position.Y,
                Z = joint.Position.Z
            };
            DepthSpacePoint depthSpacePoint = cm.MapCameraPointToDepthSpace(joint.Position);
            jointGait.DepthX = depthSpacePoint.X;
            jointGait.DepthY = depthSpacePoint.Y;
            return jointGait;
        }

        unsafe void ComputeAllHeights(CameraSpacePoint[] cameraPoints, double[] heightsTable, Vector4 ground)
        {
            fixed (CameraSpacePoint* cameraPointer = cameraPoints)
            fixed (double* heights = heightsTable)
            {
                CameraSpacePoint* camera = cameraPointer;
                double* height = heights;
                for (int i = 0; i < cameraPoints.Length; i++, camera++, height++)
                {
                    var p = *camera;
                    *height = ground.X * p.X + ground.Y * p.Y + ground.Z * p.Z - ground.W;
                }
            }

        }
        double GetHeight(Vector p, Vector4 floor)
        {
            return floor.X * p.X + floor.Y * p.Y + floor.Z * p.Z - floor.W;
        }


        int MergeComponents(int k1, int k2)
        {
            if (k1 <= 0 || k1 == k2) return k2;
            if (k2 <= 0) return k1;
            int t1 = k1;
            while (componentMap[t1] != t1) t1 = componentMap[t1];

            int t2 = k2;
            while (componentMap[t2] != t2) t2 = componentMap[t2];

            if (t1 > t2)
            {
                int t = t1;
                t1 = t2;
                t2 = t;
            }
            componentMap[t2] = t1;
            // reduce computation for future rounds
            while (componentMap[k1] != k1)
            {
                int t = componentMap[k1];
                componentMap[k1] = t1;
                k1 = t;
            }

            while (componentMap[k2] != k2)
            {
                int t = componentMap[k2];
                componentMap[k2] = t1;
                k2 = t;
            }
            return t1;
        }        

        unsafe void MaskLargestConnectedComponent(ushort[] depthImageBuffer, CameraSpacePoint[] cameraPointBuffer, ushort[] infraRedBuffer, int width, int height)
        {
            sizeOfComponent.Clear();
            componentMap.Clear();
            minHeight.Clear();
            maxHeight.Clear();
            Array.Clear(membership, 0, membership.Length);
            int numberOfComponents = 0;
            int i = 0;
            double closeInMeters = close / 1000.0;

            componentMap.Add(0);
            sizeOfComponent.Add(0); // the zero component is reserved for background
            minHeight.Add(0);
            maxHeight.Add(0);

            fixed (ushort* buffer = depthImageBuffer) fixed (ushort* irBuffer = infraRedBuffer)
            {


                ushort* b = buffer;
                ushort* bMinusOne = buffer - 1;
                ushort* bMinusWidth = buffer - width;
                ushort* bMinusTwo = buffer - 2;
                ushort* bMinusTwoWidth = buffer - 2 * width;
                ushort* bMinusDiagonal1 = buffer - width - 1;
                ushort* bMinusDiagonal2 = buffer - width + 1;
                ushort* ir = irBuffer;
                fixed (CameraSpacePoint* cameraPoint = cameraPointBuffer)
                {
                    CameraSpacePoint* camera = cameraPoint;
                    for (int y = 0; y < height; y++)
                        for (int x = 0; x < width; x++, i++, b++, bMinusWidth++, camera++, bMinusOne++, bMinusTwo++, bMinusTwoWidth++, bMinusDiagonal1++, bMinusDiagonal2++, ir++)
                        {
                            double curHeight = heights[i];
                            var p = *camera;
                            
                            if (curHeight <= minimalHeightFromGround || p.Z > depthOfROI || p.Z < closeInMeters || *ir < intensityBasedCutoffThreshold)
                            {
                                membership[i] = 0;
                                continue;
                            }
                            int k1 = -1, k2 = -1, k3 = -1, k4 = -1, k5 = -1, k6 = -1;
                            if ((x > 0) && (Math.Abs(*b - *bMinusOne) < delta))
                            {
                                k1 = membership[i - 1];
                            }
                            if ((x > 1) && (Math.Abs(*b - *bMinusTwo) < delta))
                            {
                                k2 = membership[i - 2];
                            }
                            if ((y > 0) && (Math.Abs(*b - *bMinusWidth) < delta))
                            {
                                k3 = membership[i - width];
                            }
                            if ((y > 1) && (Math.Abs(*b - *bMinusTwoWidth) < delta))
                            {
                                k4 = membership[i - 2 * width];
                            }
                            if ((y > 0) && (x > 0) && (Math.Abs(*b - *bMinusDiagonal1) < delta))
                            {
                                k5 = membership[i - width - 1];
                            }
                            if ((y > 0) && (x < width - 1) && (Math.Abs(*b - *bMinusDiagonal2) < delta))
                            {
                                k6 = membership[i - width + 1];
                            }

                            int k = -1;
                            k1 = MergeComponents(k1, k2);
                            k3 = MergeComponents(k3, k4);
                            k5 = MergeComponents(k5, k6);
                            k3 = MergeComponents(k3, k5);
                            k = MergeComponents(k1, k3);

                            if (k <= 0)
                            {
                                // try to see if we can connect components of skip2

                                k = ++numberOfComponents;
                                // add the point to the size of the component only if it is in the 
                                // bounding box
                                if (Math.Abs(p.X) < halfWidthOfROI && curHeight < upperROI && curHeight > lowerROI && p.Z < depthOfROI)
                                    sizeOfComponent.Add(1);
                                else
                                    sizeOfComponent.Add(0);
                                minHeight.Add(curHeight);
                                maxHeight.Add(curHeight);
                                componentMap.Add(k);
                            }
                            else
                            {
                                // add the point to the size of the component only if it is in the 
                                // bounding box
                                if (Math.Abs(p.X) < halfWidthOfROI && curHeight < upperROI && curHeight > lowerROI && p.Z < depthOfROI && p.Z > closeInMeters)
                                    sizeOfComponent[k]++;
                                minHeight[k] = Math.Min(minHeight[k], curHeight);
                                maxHeight[k] = Math.Max(maxHeight[k], curHeight);
                            }
                            membership[i] = k;
                        }
                }
            }
            // merge components


            int comp = 0;
            for (i = 1; i < numberOfComponents + 1; i++ )
            {
                if (componentMap[i] != i)
                {
                    int t = componentMap[componentMap[i]]; // this must be the head node of this component;
                    sizeOfComponent[t] += sizeOfComponent[i];
                    minHeight[t] = Math.Min(minHeight[t], minHeight[i]);
                    maxHeight[t] = Math.Max(maxHeight[t], maxHeight[i]);
                    componentMap[i] = t;
                }
            }
            if (frame.FrameNumber > 50)
            {
                comp = 0;
            }

            // find largest component
            for (i = 1; i <= numberOfComponents; i++ )
            {
                if (componentMap[i] == i && sizeOfComponent[i] > sizeOfComponent[comp] && minHeight[i] < maximalDistanceFromGroundOfSubject
                    && maxHeight[i] > minimalHeightOfSubject && maxHeight[i] < (ceiling - minimalDistanceFromCeilingOfSubject))
                    comp = i;
            }
            // create Icon and compute the data for the frame
            Array.Clear(silhouetteCount, 0, silhouetteCount.Length);
            Array.Clear(silhouetteSumX, 0, silhouetteSumX.Length);
            Array.Clear(silhouetteSumY, 0, silhouetteSumY.Length);
            Array.Clear(silhouetteSumZ, 0, silhouetteSumZ.Length);
            float sumX = 0.0f;
            float sumY = 0.0f;
            float sumZ = 0.0f;
            float n = 0.0f;
            int numberOfPixelsInBody = 0;
            float sumXX = 0.0f;
            float sumXY = 0.0f;
            float sumXZ = 0.0f;
            float sumYY = 0.0f;
            float sumYZ = 0.0f;
            float sumZZ = 0.0f;

            if (sizeOfComponent[comp] > minimalNumberOfPixelsInSubject)
            {

                int silhouetteWidth = (width + silhouetteBlockSize - 1) / silhouetteBlockSize;
                int line = 0, row = 0;
                int silhouetteLine = 0, silhouetteRow = 0;
                int j = 0;
                for (i = 0; i < height * width; i++, row++)
                {
                    if (row >= width)
                    {
                        row = 0;
                        silhouetteRow = 0;
                        j -= silhouetteWidth - 1;
                        line++;
                        if (line - silhouetteLine * silhouetteBlockSize >= silhouetteBlockSize)
                        {
                            silhouetteLine++;
                            j += silhouetteWidth;
                        }
                    }
                    else
                    {
                        if (row - silhouetteRow * silhouetteBlockSize >= silhouetteBlockSize)
                        {
                            j++;
                            silhouetteRow++;
                        }
                    }
                    var p = cameraPointBuffer[i];
                    double h = heights[i];            
                    if (frame.blockTypes == null) frame.blockTypes = new Frame.EBlockType[width, height];

                    frame.blockTypes[row, line] = Frame.EBlockType.background;
                    if (Math.Abs(h) < minimalHeightFromGround) frame.blockTypes[row, line] = Frame.EBlockType.ground;
                    else if (Math.Abs(p.X) < halfWidthOfROI && h < upperROI && h > lowerROI && p.Z < depthOfROI)
                        frame.blockTypes[row, line] = Frame.EBlockType.unknown;

                    if (componentMap[membership[i]] == comp)
                    {
                        if (p.Z <= 1e-10) continue;
                        float weight = 1;//p.Z * p.Z; // closer pixel represent smaller area
                        sumX += p.X * weight;
                        sumY += p.Y * weight;
                        sumZ += p.Z * weight;
                        sumXX += p.X * p.X * weight;
                        sumXY += p.X * p.Y * weight;
                        sumXZ += p.X * p.Z * weight;
                        sumYY += p.Y * p.Y * weight;
                        sumYZ += p.Y * p.Z * weight;
                        sumZZ += p.Z * p.Z * weight;
                        n += weight;
                        numberOfPixelsInBody++;
                        silhouetteSumX[j] += p.X;
                        silhouetteSumY[j] += p.Y;
                        silhouetteSumZ[j] += p.Z;
                        silhouetteCount[j]++;
                        if (frame.blockTypes != null) frame.blockTypes[row , line] = Frame.EBlockType.body;
                    }
                }
            }
            float m = n;
            if (n == 0)
            {
                m = 1;
            }
#if SAVE_IMAGES
                saveDepthImage(depthImageBuffer, "C:\\gb1\\images\\" + frame.FrameNumber.ToString(), componentMap, membership);
#endif
#if SAVE_XYZ
                saveXYZImage(cameraPointBuffer, "C:\\gb1\\XYZimages\\" + frame.FrameNumber.ToString(), componentMap, membership, comp);
#endif
            
            byte quality = (byte)Math.Min(255, Math.Min(n * 8, newFloorConfidence * 255));
            frame.silhouette = new SilhouetteData()
            {
                trackQuality = quality,
                depthMeanX = sumX / m,
                depthMeanY = sumY / m,
                depthMeanZ = sumZ / m,
                depthMeanXX = sumXX / m,
                depthMeanXY = sumXY / m,
                depthMeanXZ = sumXZ / m,
                depthMeanYY = sumYY / m,
                depthMeanYZ = sumYZ / m,
                depthMeanZZ = sumZZ / m,
                groundW = ground.W,
                groundX = ground.X,
                groundY = ground.Y,
                groundZ = ground.Z,
                points = GetPoints().ToArray(),
                xRange = XRange,
                yRange = YRange,
                zRange = ZRange,
                numberOfPixelsInBody = (int)numberOfPixelsInBody
            };

        }

#if SAVE_IMAGES
         Color[] colors = null;
         List<Color> componentsColors = new List<Color>();
         Random randomGenerator = new Random();
        private  void saveDepthImage(ushort[] depthImageBuffer, string name, List<int> componentMap, int[] membership)
        {
            int height = kinectSensor.DepthFrameSource.FrameDescription.Height;
            int width = kinectSensor.DepthFrameSource.FrameDescription.Width;
            // save the depth map
            if (colors == null)
            {
                colors = new Color[65536];
                for (int i = 0; i < colors.Length; i++ )
                {
                    colors[i] = Color.FromArgb(255, (int) (15.5 * Math.Log(i + 1) / Math.Log(2)), 0, 0);
                }
            }
            using (Bitmap bmp = new Bitmap(width, height))
            {

                int i = 0;
                for (int l = 0; l < height; l++)
                    for (int w = 0; w < width; w++, i++ )
                    {
                        bmp.SetPixel(w, l, colors[depthImageBuffer[i]]);
                    }

                    bmp.Save(name + ".png");
            }

            //save the component map
            using (Bitmap bmp = new Bitmap(width, height))
            {

                int i = 0;
                for (int l = 0; l < height; l++)
                    for (int w = 0; w < width; w++, i++)
                    {
                        int c = componentMap[membership[i]];
                        while (componentsColors.Count <= c)
                            componentsColors.Add(Color.FromArgb(255, randomGenerator.Next(255), randomGenerator.Next(255), randomGenerator.Next(255)));
                        bmp.SetPixel(w, l, componentsColors[c]);
                    }

                bmp.Save(name + "components.png");
            }
        }
#endif

#if SAVE_XYZ
        private  void saveXYZImage(CameraSpacePoint[] cameraPointBuffer, string name, List<int> componentMap, int[] membership, int comp)
        {
            using (var sw = new StreamWriter(name + ".tsv"))
            {
                for (int i = 0; i < cameraPointBuffer.Length; i++)
                {
                    if (componentMap[membership[i]] != comp) continue;
                    var p = cameraPointBuffer[i];
                    double h = heights[i];
                    sw.WriteLine("{0}\t{1}\t{2}\t{3}", p.X, p.Y, p.Z, h);
                }
            }
        }
#endif


        unsafe  void ComputeFloorsForPatchs(CameraSpacePoint[] cameraPointBuffer, Vector4[,] grounds, Vector[,] means, int blockSize)
        {
            int width = kinectSensor.DepthFrameSource.FrameDescription.Width;
            int len = X.Length;
            int xSize = X.GetLength(0);
            int ySize = X.GetLength(1);
            Array.Clear(X, 0, len);
            Array.Clear(Y, 0, len);
            Array.Clear(Z, 0, len);
            Array.Clear(XX, 0, len);
            Array.Clear(XY, 0, len);
            Array.Clear(XZ, 0, len);
            Array.Clear(YY, 0, len);
            Array.Clear(YZ, 0, len);
            Array.Clear(ZZ, 0, len);
            Array.Clear(n, 0, len);
            double closeInMeters = close * 0.001;
            fixed (CameraSpacePoint* point = cameraPointBuffer)
            {
                CameraSpacePoint* p = point;
                int xCor = 0, yCor = 0;
                int xBlock = 0, yBlock = 0;
                int inXBlockCount = 0, inYBlockCount = 0;
                for (int i = 0; i < cameraPointBuffer.Length; i++, p++, xCor++, inXBlockCount++)
                {
                    if (xCor >= width)
                    {
                        xCor = 0;
                        inXBlockCount = 0;
                        xBlock = 0;
                        yCor++;
                        inYBlockCount++;
                        if (inYBlockCount >= blockSize)
                        {
                            yBlock++;
                            inYBlockCount = 0;
                        }
                    }
                    else if (inXBlockCount >= blockSize)
                    {
                        xBlock++;
                        inXBlockCount = 0;
                    }


                    if (p->Z > closeInMeters && p->X < halfWidthOfROI && p->X > -halfWidthOfROI && p->Z < depthOfROI)
                    {
                        X[xBlock, yBlock] += p->X;
                        Y[xBlock, yBlock] += p->Y;
                        Z[xBlock, yBlock] += p->Z;
                        XX[xBlock, yBlock] += p->X * p->X;
                        XY[xBlock, yBlock] += p->X * p->Y;
                        XZ[xBlock, yBlock] += p->X * p->Z;
                        YY[xBlock, yBlock] += p->Y * p->Y;
                        YZ[xBlock, yBlock] += p->Y * p->Z;
                        ZZ[xBlock, yBlock] += p->Z * p->Z;
                        n[xBlock, yBlock]++;


                    }
                }
            }
            Parallel.For(0, 8, i => SolveLocalGrounds(8, i, xSize, ySize));                            

        }

        void SolveLocalGrounds(int stepSize, int offset, int xSize, int ySize)
        {

            for (int i = 0; i < xSize; i++)
                for (int j = offset; j < ySize; j += stepSize)
                {
                    double loss = SolveFloorOptimization(X[i, j], Y[i, j], Z[i, j], XX[i, j], XY[i, j], XZ[i, j], YY[i, j], YZ[i, j], ZZ[i, j], n[i, j],
                        out grounds[i, j]);
                    if (Double.IsInfinity(loss) || Double.IsNaN(loss)) // failed to find floor
                    {
                        grounds[i, j].W = -1000;
                        means[i, j].X = -1000;
                        means[i, j].Y = -1000;
                        means[i, j].Z = -1000;
                        continue;
                    }

                    means[i, j].X = (float)X[i, j] / n[i, j];
                    means[i, j].Y = (float)Y[i, j] / n[i, j];
                    means[i, j].Z = (float)Z[i, j] / n[i, j];

                }
        }
        double SolveFloorOptimization(double X, double Y, double Z, double XX, double XY, double XZ, double YY, double YZ, double ZZ, int n, 
            out Vector4 ground)
        {
            ground.W = ground.X = ground.Y = ground.Z = 0;
            if (n < 3)
                return Double.NaN;

            using (DoubleArray M = DoubleArray.From(new double[][] { new double[] { XX - X * X / n, XY - X * Y / n, XZ - X * Z / n},
                new double[] { XY - X * Y / n, YY - Y * Y / n, YZ - Y * Z / n},
                new double[] { XZ - X * Z / n, YZ - Y * Z / n, ZZ - Z * Z / n} }))
            {
                using (SVD svd = new SVD(M))
                {
                    double gX = svd.V[0, 2];
                    double gY = svd.V[1, 2];
                    double gZ = svd.V[2, 2];
                    double lambda = Math.Sqrt(1 / (gX * gX + gY * gY + gZ * gZ)); // scale it such that the result will be in meters
                    if (gY < 0) lambda *= -1;

                    ground.X = (float)(gX * lambda);
                    ground.Y = (float)(gY * lambda);
                    ground.Z = (float)(gZ * lambda);
                    ground.W = (float)((X * ground.X + Y * ground.Y + Z * ground.Z) / n);

                    // compute the L2 loss
                    double loss = 0.5 * (XX * ground.X * ground.X + YY * ground.Y * ground.Y + ZZ * ground.Z * ground.Z +
                        2 * XY * ground.X * ground.Y + 2 * XZ * ground.X * ground.Z + 2 * YZ * ground.Y * ground.Z) -
                        ground.W * (ground.X * X + ground.Y * Y + ground.Z * Z) +
                        0.5 * n * ground.W * ground.W;
                    return loss / n;
                }
            }
        }
        void ComputeNewFloor(CameraSpacePoint[] cameraPointBuffer)
        {
            int[] count = new int[grounds.Length];
            ComputeFloorsForPatchs(cameraPointBuffer, grounds, means, blockSize);
            int maxCountIndex = 0;
            int lineLength = grounds.GetLength(0);
            int i1 = 0, i2 = 0, j1 = 0, j2 = 0;
            newCeiling = -1000; // this is a signal that the computation failed
            for (int i = 0; i < count.Length; i++, i1++ )
            {
                if (i1 >= lineLength)
                {
                    i1 = 0;
                    i2++;
                }
                var groundI = grounds[i1, i2];
                if (groundI.Y < 0.8 || groundI.W < -100 || groundI.W > 0 || Double.IsNaN(groundI.X) || Double.IsInfinity(groundI.X)) continue;
                j1 = j2 = 0;
                count[i] = 1;
                for (int j = 0; j < i; j++, j1++)
                {
                    if (j1 >= lineLength)
                    {
                        j1 = 0;
                        j2++;
                    }
                    if (count[j] > 0)
                    {
                        var groundJ = grounds[j1, j2];
                        float deltaX = groundJ.X - groundI.X;
                        float deltaY = groundJ.Y - groundI.Y;
                        float deltaZ = groundJ.Z - groundI.Z;
                        float deltaW = groundJ.W - groundI.W;
                        double norm = Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
                        double offset = Math.Abs(deltaW);
                        double maxDistanceBetweenFloors = norm + offset; // the maximal distance between the planes in a radius of 1 meters



                        if (maxDistanceBetweenFloors < minimalHeightFromGround)
                        {
                            count[j]++;
                            if (count[j] > count[maxCountIndex]) maxCountIndex = j;
                            count[i]++;
                            continue;
                        }
                    }
                }
                if (count[i] > count[maxCountIndex]) maxCountIndex = i;
            }
            int m1 = maxCountIndex % lineLength, m2 = maxCountIndex / lineLength;
            var g = grounds[m1, m2];

            // we have found a rough estimate for the ground. We do another round where we use all the points that are close to the ground.
            double tX = 0, tY = 0, tZ = 0, tXX = 0, tXY = 0, tXZ = 0, tYY = 0, tYZ = 0, tZZ = 0;
            int tn = 0;
            for (j1 = 0; j1 < means.GetLength(0); j1++)
            {
                for (j2 = 0; j2 < means.GetLength(1); j2++)
                {
                    var p = means[j1, j2];
                    if (p.X < -100) continue;
                    var groundJ = grounds[j1, j2];
                    float deltaX = groundJ.X - g.X;
                    float deltaY = groundJ.Y - g.Y;
                    float deltaZ = groundJ.Z - g.Z;
                    float deltaW = groundJ.W - g.W;
                    double norm = Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
                    double offset = Math.Abs(deltaW);
                    double maxDistanceBetweenFloors = norm + offset; // the maximal distance between the planes in a radius of 1 meters
                  
                    if (maxDistanceBetweenFloors < minimalHeightFromGround)
                    {
                        tX += X[j1, j2];
                        tY += Y[j1, j2];
                        tZ += Z[j1, j2];
                        tXX += XX[j1, j2];
                        tXY += XY[j1, j2];
                        tXZ += XZ[j1, j2];
                        tYY += YY[j1, j2];
                        tYZ += YZ[j1, j2];
                        tZZ += ZZ[j1, j2];
                        tn += n[j1, j2];

                    }
                }
            }


            double loss = SolveFloorOptimization(tX, tY, tZ, tXX, tXY, tXZ, tYY, tYZ, tZZ, tn, out newGround);

            if (Double.IsInfinity(loss) || Double.IsNaN(loss)) // failed to find floor
            {
                newFloorConfidence = 0;
                return;
            }
            newFloorConfidence = 0.01 / Math.Sqrt(loss);

            // we are looking for the cieling height. We assume that the cieling is the heighst visible point
            double tempCeiling = 0;
            i1 = i2 = 0;
            for (int i = 0; i < count.Length; i++, i1++)
            {
                if (i1 >= lineLength)
                {
                    i1 = 0;
                    i2++;
                }
                if (Double.IsNaN(means[i1, i2].Y) || Double.IsInfinity(means[i1, i2].Y)) continue;
                double h = GetHeight(means[i1, i2], newGround);
                tempCeiling = Math.Max(tempCeiling, h);
            }
            if (tempCeiling > 2) newCeiling = tempCeiling;
            if (tempCeiling > 4) newCeiling = 4;
        }


        IEnumerable<SilhouettePoint> GetPoints()
        {
            float halfXRange = XRange / 2;
            for (int i = 0; i < silhouetteCount.Length; i++)
            {
                if (silhouetteCount[i] > 0)
                {
                    float avgX = silhouetteSumX[i] / silhouetteCount[i];
                    float avgY = silhouetteSumY[i] / silhouetteCount[i];
                    float avgZ = silhouetteSumZ[i] / silhouetteCount[i];
                    // we should adjust the coordinates to componsate for the ground vector
                    double heightProjection = ground.X * avgX + ground.Y * avgY + ground.Z * avgZ;
                    double h = heightProjection - ground.W;
                    // fix the Z coordinate in case it is not tangent to the ground vector
                    double dist = Math.Sqrt(avgY * avgY + avgZ * avgZ - heightProjection * heightProjection);
                    if (Math.Abs(avgX) > halfXRange) continue;
                    if (h > YRange || h < 0) continue;
                    if (dist > ZRange || dist < 0) continue;
                    SilhouettePoint p = new SilhouettePoint()
                    {
                        X = (byte)(255 * ((avgX + halfXRange) / XRange)),
                        Y = (byte)(255 * (h / YRange)),
                        Z = (byte)(255 * (dist / ZRange)),
                        Confidence = (byte)(255 * silhouetteCount[i] / (silhouetteBlockSize * silhouetteBlockSize))
                    };
                    yield return p;
                }
            }
        }

        void ProcessBodyFrame(CameraSpacePoint[] cameraPointBuffer, Frame frame, int bodyIndex)
        {
            if (frame == null) return;

            float sumX = 0.0f;
            float sumY = 0.0f;
            float sumZ = 0.0f;
            float numberOfBodyPoints = 0.0f;

            float sumXX = 0.0f;
            float sumXY = 0.0f;
            float sumXZ = 0.0f;
            float sumYY = 0.0f;
            float sumYZ = 0.0f;
            float sumZZ = 0.0f;

            for (int i = 0; i < cameraPointBuffer.Length; ++i)
            {
                if (playerMask[i] == bodyIndex && !float.IsInfinity(cameraPointBuffer[i].X)
                    && !float.IsInfinity(cameraPointBuffer[i].Y) && !float.IsInfinity(cameraPointBuffer[i].Z))
                {
                    sumX += cameraPointBuffer[i].X;
                    sumY += cameraPointBuffer[i].Y;
                    sumZ += cameraPointBuffer[i].Z;
                    sumXX += cameraPointBuffer[i].X * cameraPointBuffer[i].X;
                    sumXY += cameraPointBuffer[i].X * cameraPointBuffer[i].Y;
                    sumXZ += cameraPointBuffer[i].X * cameraPointBuffer[i].Z;
                    sumYY += cameraPointBuffer[i].Y * cameraPointBuffer[i].Y;
                    sumYZ += cameraPointBuffer[i].Y * cameraPointBuffer[i].Z;
                    sumZZ += cameraPointBuffer[i].Z * cameraPointBuffer[i].Z;
                    numberOfBodyPoints++;
                
                }
            }

            if (numberOfBodyPoints > 0.0f)
            {
                frame.depthMeanX = sumX / numberOfBodyPoints;
                frame.depthMeanY = sumY / numberOfBodyPoints;
                frame.depthMeanZ = sumZ / numberOfBodyPoints;
                frame.depthMeanXX = sumXX / numberOfBodyPoints;
                frame.depthMeanXY = sumXY / numberOfBodyPoints;
                frame.depthMeanXZ = sumXZ / numberOfBodyPoints;
                frame.depthMeanYY = sumYY / numberOfBodyPoints;
                frame.depthMeanYZ = sumYZ / numberOfBodyPoints;
                frame.depthMeanZZ = sumZZ / numberOfBodyPoints;
            }
        }

        Frame GenerateDataFrame(BodyFrame bodyFrame, DateTime currentTime, TimeSpan frameTime)
        {
            Frame frame = null;

            if (bodyFrame != null)
            {

                bodyFrame.GetAndRefreshBodyData(bodies);
                var bodiesArray = bodies.ToArray();
                if (bodyIndex < 0 || bodiesArray.Length <= bodyIndex || !bodiesArray[bodyIndex].IsTracked)
                {
                    for (bodyIndex = 0; bodyIndex < bodiesArray.Length; bodyIndex++)
                        if (bodiesArray[bodyIndex].IsTracked) break;
                    if (bodyIndex >= bodiesArray.Length)
                        bodyIndex = -1;
                }
                if (bodyIndex >= 0)
                {
                    var body = bodiesArray[bodyIndex];
                    List<JointGait> jointList = body.Joints.Select(joint => JointToJointGait(joint.Value)).ToList();
                    frameTime = bodyFrame.RelativeTime - curStartTime;
                    frame = new Frame(SensorType.One, frameNum, (long)frameTime.TotalMilliseconds, currentTime.Ticks, body.TrackingId, jointList, bodyFrame.FloorClipPlane.W,
                                    bodyFrame.FloorClipPlane.X, bodyFrame.FloorClipPlane.Y, bodyFrame.FloorClipPlane.Z, (int)body.ClippedEdges, body.Lean.X, body.Lean.Y,
                                    (TrackingStateGait)body.LeanTrackingState);
                }
            }
            if (frame == null)
            {
                frame = new Frame(SensorType.One, frameNum, (long)frameTime.TotalMilliseconds, currentTime.Ticks, ulong.MaxValue , null, 0, 0, 0, 0, 0, 0, 0, TrackingStateGait.NotTracked);
            }
            return frame;
        }

        bool HandleKinectFrame(MultiSourceFrameArrivedEventArgs e)
        {
            currentTime = DateTime.Now;
            MultiSourceFrameReference frameReference = e.FrameReference;
            BodyFrame bodyFrame = null;
            BodyIndexFrame bodyIndexFrame = null;
            DepthFrame depthFrame = null;
            MultiSourceFrame msFrame = null;
            InfraredFrame infraRedFrame = null;
            bool droppedFrame = false;

            try
            {
                msFrame = frameReference.AcquireFrame();
                if (msFrame == null) return false;
                depthFrame = msFrame.DepthFrameReference.AcquireFrame();
                if (depthFrame == null) return false;
                depthFrame.CopyFrameDataToArray(depthImageBuffer2);
                bodyFrame = msFrame.BodyFrameReference.AcquireFrame();
                bodyIndexFrame = msFrame.BodyIndexFrameReference.AcquireFrame();
                infraRedFrame = msFrame.InfraredFrameReference.AcquireFrame();
                if (infraRedFrame != null)
                    infraRedFrame.CopyFrameDataToArray(infraRedBuffer);
                if (bodyIndexFrame != null)
                {
                    bodyIndexFrame.CopyFrameDataToArray(playerMaskMemory2);
                }
                if (!Monitor.TryEnter(_lockForFrameInProcess))
                {
                    droppedFrame = true;
                    return false; // did not finish processing the previous frame. We have to skip this frame :-(
                }

                if (bodyIndexFrame != null)
                {
                    playerMask = playerMaskMemory2;
                    playerMaskMemory2 = playerMaskMemory;
                    playerMaskMemory = playerMask;
                }
                else
                {
                    playerMask = null;
                }
                var t = depthImageBuffer;
                depthImageBuffer = depthImageBuffer2;
                depthImageBuffer2 = t;

                frameNum++;
                if (frameNum == 0)
                    curStartTime = depthFrame.RelativeTime;   // start the clock
                frameTime = depthFrame.RelativeTime - curStartTime;
                frame = GenerateDataFrame(bodyFrame, currentTime, frameTime);
                Task bodyTask = Task.Run(() =>
                {
                    UpdateFrameRate(currentTime);
                    if (bodyIndexFrame != null)
                        ProcessBodyFrame(cameraPointBuffer, frame, bodyIndex);
                });
                cm.MapDepthFrameToCameraSpace(depthImageBuffer, cameraPointBuffer);
                if (groundTask == null || groundTask.IsCompleted)
                {
                    Array.Copy(cameraPointBuffer, cameraPointBufferForFloor, cameraPointBufferForFloor.Length);
                    if (groundTask != null && groundTask.Status == TaskStatus.RanToCompletion && newCeiling > 0)
                    {
                        //float mix = 1.0f, invMix = 1 - mix;
                        float mix = (float)currentMix, invMix = 1 - mix;
                        ground.W = ground.W * invMix + newGround.W * mix;
                        ground.X = ground.X * invMix + newGround.X * mix;
                        ground.Y = ground.Y * invMix + newGround.Y * mix;
                        ground.Z = ground.Z * invMix + newGround.Z * mix;
                        ceiling = ceiling * invMix + newCeiling * mix;
                        currentMix = currentMix * (1 - mixUpdateRate) + mixTarget * mixUpdateRate;
                    }
                    if (groundTask != null) groundTask.Dispose();
                    groundTask = Task.Run(() => ComputeNewFloor(cameraPointBuffer)); // we do not wait for this task to complete. If it takes multiple frames, thats OK.
                }
                var height = kinectSensor.DepthFrameSource.FrameDescription.Height;
                var width = kinectSensor.DepthFrameSource.FrameDescription.Width;
                ComputeAllHeights(cameraPointBuffer, heights, ground);
                MaskLargestConnectedComponent(depthImageBuffer, cameraPointBuffer, infraRedBuffer, width, height);
                bodyTask.Wait();
                memory.addFrame(frame);
                return true;
            }
            finally
            {
                if (bodyFrame != null) bodyFrame.Dispose();
                if (depthFrame != null) depthFrame.Dispose();
                if (bodyIndexFrame != null) bodyIndexFrame.Dispose();
                if (infraRedFrame != null) infraRedFrame.Dispose();
                if (Monitor.IsEntered(_lockForFrameInProcess))
                {
                    System.Diagnostics.Trace.WriteLine(String.Format("released Current thread: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId));
                    Monitor.Exit(_lockForFrameInProcess);

                }
                else if (droppedFrame || (msFrame == null) || (depthFrame == null))
                    System.Diagnostics.Trace.WriteLine(String.Format("OK:{0} dropped {1} ms {2} depth {3}", System.Threading.Thread.CurrentThread.ManagedThreadId, droppedFrame, msFrame == null, depthFrame == null));
                else
                {
                    int id = System.Threading.Thread.CurrentThread.ManagedThreadId;
                    System.Diagnostics.Trace.WriteLine(String.Format("Current thread: {0}", id));
                    throw new Exception("Internal error: a frame was not lost but the lock was not acquired");
                }

            }
        }

        void MultiSourceReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            Task.Run(() => HandleKinectFrame(e)); // run on a separate task so we can release the main thread
        }


        public Frame GetLastFrame() 
        {
            return memory?.lastFrame;
        }

        public Frame GetNextFrame(long frameId) 
        {
            return memory?.next(frameId);
        }

        public bool FramesAvailable()
        {
            if (memory != null) return memory.framesAvailable;
            else return false;
        }

        public bool WriteToFile(string filename, long startFrameId, long endFrameId)
        {
            if (memory != null)
            {
                memory.save(filename, startFrameId, endFrameId);
                return true;
            }
            return false;
        }

        public void ClearMemory()
        {
            memory.Clear();
            GC.Collect();
        }

    }
}
