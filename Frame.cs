using ShoNS.Array;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;


namespace GaitAndBalanceApp
{
    public struct Vector
    {
        public Vector(float x, float y, float z) { X = x; Y = y; Z = z; W = 0; }
        public Vector(float w, float x, float y, float z) { W = w;  X = x; Y = y; Z = z; }
        public float X;
        public float Y;
        public float Z;
        public float W;
        public void scale(float alpha) {X *= alpha; Y*= alpha; Z*=alpha;}
        public DoubleArray toArray() { return DoubleArray.From(new double[] { X, Y, Z }); }

    }

    public struct Extreams
    {
        public double minX, maxX, minZ, maxZ;
        public long timeStamp;

        public Extreams(double minX, double maxX, double minZ, double maxZ, long timeStamp)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minZ = minZ;
            this.maxZ = maxZ;
            this.timeStamp = timeStamp;
        }
    }


    public enum JointTypeGait
    {
        // The joints from Kinect One:
        SpineBase = 0, 
        SpineMid = 1,           
        Neck = 2,              
        // Joints in common:
        Head = 3,
        ShoulderLeft = 4,    
        ElbowLeft = 5,
        WristLeft = 6,
        HandLeft = 7,
        ShoulderRight = 8,
        ElbowRight = 9,
        WristRight = 10,
        HandRight = 11,
        HipLeft = 12,
        KneeLeft = 13,
        AnkleLeft = 14,
        FootLeft = 15,
        HipRight = 16,
        KneeRight = 17,
        AnkleRight = 18,
        FootRight = 19,
        // Joints from Kinect One:
        SpineShoulder = 20,    
        HandTipLeft = 21,     
        ThumbLeft = 22,      
        HandTipRight = 23,    
        ThumbRight = 24,     
        // And the joints from Kinect 360:
        HipCenter,
        Spine,
        ShoulderCenter,
    }

    public enum SensorType
    {
        One,
        ThreeSixty,
        Wii
    }

    public enum EinputMode { wii, neck, mean, line, silhouetteMean, silhouetteLine };

    public enum EprojectionMode { ground, none };


    public enum TrackingStateGait
    {
        NotTracked,
        Inferred,
        Tracked
    }

    [Flags]
    public enum FrameEdgesGait
    {
        None,
        Right,
        Left,
        Top,
        Bottom
    }


    [DataContract]
    public struct JointGait
    {
        [DataMember]
        public JointTypeGait JointType { get; set; }
        [DataMember]        
        public float X { get; set; }
        [DataMember]        
        public float Y { get; set; }
        [DataMember]        
        public float Z { get; set; }
        [DataMember]
        public float DepthX { get; set; }
        [DataMember]
        public float DepthY { get; set; }
        [DataMember]
        public TrackingStateGait TrackingState { get; set; }
    }

    [DataContract]
    public struct WiiSensorData
    {
        [DataMember]
        public short TopLeft { get; set; }
        [DataMember]
        public short TopRight { get; set; }
        [DataMember]
        public short BottomLeft { get; set; }
        [DataMember]
        public short BottomRight { get; set; }
    }

    [DataContract]
    public struct WiiData
    {
        [DataMember]
        public float CenterOfPressureX { get; set; }
        [DataMember]
        public float CenterOfPressureY { get; set; }
        [DataMember]
        public WiiSensorData Raw { get; set; }
        [DataMember]
        public WiiSensorData Calib0kg { get; set; }
        [DataMember]
        public WiiSensorData Calib17kg { get; set; }
        [DataMember]
        public WiiSensorData Calib34kg { get; set; }    
    }

    [DataContract]
    public struct SilhouettePoint
    {
        [DataMember]
        public byte X { get; set;}
        [DataMember]
        public byte Y { get; set; }
        [DataMember]
        public byte Z { get; set; }
        [DataMember]
        public byte Confidence { get; set; }
    }

    [DataContract]
    public struct SilhouetteData
    {
        [DataMember]
        public SilhouettePoint[] points { get; set; } // the depth of each pixel. To save space we use byte to describe the depth. The depth, in millimeters, is computed by multiplying this value by 4

        [DataMember]
        public float resolution {get; set;}

        [DataMember]
        public float xRange {get; set;}

        [DataMember]
        public float yRange { get; set; }

        [DataMember]
        public float zRange { get; set; }

        [DataMember]
        public byte trackQuality { get; set; } // the quality of tracking. 1 is bad and 255 is good. 0 means that a reference frame is being acquired

        [DataMember]
        public float groundW { get; set; }
        [DataMember]
        public float groundX { get; set; }
        [DataMember]
        public float groundY { get; set; }
        [DataMember]
        public float groundZ { get; set; }

        // Mean X,Y,Z over the whole depth image (using the player mask)
        [DataMember]
        public float depthMeanX { get; set; }
        [DataMember]
        public float depthMeanY { get; set; }
        [DataMember]
        public float depthMeanZ { get; set; }

        [DataMember]
        public float depthMeanXX { get; set; }
        [DataMember]
        public float depthMeanXY { get; set; }
        [DataMember]
        public float depthMeanXZ { get; set; }
        [DataMember]
        public float depthMeanYY { get; set; }
        [DataMember]
        public float depthMeanYZ { get; set; }
        [DataMember]
        public float depthMeanZZ { get; set; }
        [DataMember]
        public int numberOfPixelsInBody { get; set; }

    }

    [DataContract]
    public class Frame : IComparable<Frame>, IComparable
    {
        [DataMember]
        public long FrameNumber { get; set; }
        [DataMember]
        public long FrameTime { get; set; }             // FrameTime is relative to the first frame acquired
        [DataMember]
        public SensorType Sensor { get; set; }
        [DataMember]
        public List<JointGait> Joints { get; set; }

        [DataMember]
        public long CurrentTime { get; set; }       // CurrentTime is DateTime.Now.Ticks (maybe needed to synchronize Wii and Kinect)
        
        [DataMember]
        public float GroundW { get; set; }
        [DataMember]
        public float GroundX { get; set; }
        [DataMember]
        public float GroundY { get; set; }
        [DataMember]
        public float GroundZ { get; set; }
        [DataMember]
        public ulong TrackingId { get; set; }

        [DataMember]
        public int FrameEdges { get; set; }

        // Kinect One lean
        [DataMember]
        public float LeanX { get; set; }
        [DataMember]
        public float LeanY { get; set; }
        [DataMember]
        public TrackingStateGait LeanTrackingState { get; set; }

        // Silhouette tracking data
        [DataMember]
        public SilhouetteData silhouette;// { get; set; }
        // For the Wii balance board
        [DataMember]
        public WiiData WiiBoardData { get; set; }

        // Mean X,Y,Z over the whole depth image (using the player mask)
        [DataMember]
        public float depthMeanX;
        [DataMember]
        public float depthMeanY;
        [DataMember]
        public float depthMeanZ;

        [DataMember]
        public float depthMeanXX;
        [DataMember]
        public float depthMeanXY;
        [DataMember]
        public float depthMeanXZ;
        [DataMember]
        public float depthMeanYY;
        [DataMember]
        public float depthMeanYZ;
        [DataMember]
        public float depthMeanZZ;

        public enum EBlockType { unknown, ground, body, background};
        public EBlockType[,] blockTypes;


        static float missingX = -100;
        static float missingY = -100;
        static float missingZ = -100;

        public Frame() { }

        public Frame(SensorType sensor, long frameNum, long frameTime, long currentTime,
                     ulong trackingId, List<JointGait> joints,
                     float groundW, float groundX, float groundY, float groundZ, int edges)
        {
            Sensor = sensor;
            FrameNumber = frameNum;
            FrameTime = frameTime;
            CurrentTime = currentTime;
            TrackingId = trackingId;
            Joints = joints;
            GroundW = groundW;
            GroundX = groundX;
            GroundY = groundY;
            GroundZ = groundZ;
            FrameEdges = edges;
            LeanX = missingX;
            LeanY = missingY;
            LeanTrackingState = TrackingStateGait.NotTracked;
        }

        public Frame(SensorType sensor, long frameNum, long frameTime, long currentTime,
                     ulong trackingId, List<JointGait> joints,
                     float groundW, float groundX, float groundY, float groundZ, int edges,
                     float leanX, float leanY, TrackingStateGait leanTrackingState)
        {
            Sensor = sensor;
            FrameNumber = frameNum;
            FrameTime = frameTime;
            CurrentTime = currentTime;
            TrackingId = trackingId;
            Joints = joints;
            GroundW = groundW;
            GroundX = groundX;
            GroundY = groundY;
            GroundZ = groundZ;
            FrameEdges = edges;
            LeanX = leanX;
            LeanY = leanY;
            LeanTrackingState = leanTrackingState;
        }

        // Constructor for Wii balance board data
        public Frame(long frameNum, long frameTime, long currentTime, WiiData wiidata)
        {
            Sensor = SensorType.Wii;
            FrameNumber = frameNum;
            FrameTime = frameTime;
            CurrentTime = currentTime;
            WiiBoardData = wiidata;
        }

        public void getJoints(out Dictionary<JointTypeGait, JointGait> jointDict, out Dictionary<JointTypeGait, Tuple<double,double>> jointPoints)
        {
            jointDict = new Dictionary<JointTypeGait, JointGait>();
            jointPoints = new Dictionary<JointTypeGait, Tuple<double, double>>();
            foreach (JointGait joint in Joints)
            {
                jointDict.Add(joint.JointType, joint);
                jointPoints.Add(joint.JointType, new Tuple<double, double>(joint.DepthX, joint.DepthY));
            }
        }

        public int CompareTo(Frame other)
        {
            return (other.FrameNumber > FrameNumber) ? -1 : (other.FrameNumber == FrameNumber) ? 0 : 1;
        }

        public int CompareTo(object obj)
        {
            var f = (long)obj;
            return (f > FrameNumber) ? -1 : (f == FrameNumber) ? 0 : 1;
        }

        public string silhouettePointsToString(SilhouettePoint[] points)
        {
            if (points == null) return "";
            return String.Join("", points.Select(p => String.Format("{0:x2}{1:x2}{2:x2}{3:x2}", p.X, p.Y, p.Z, p.Confidence)));
        }

        public void writeXml(System.Xml.XmlWriter writer)
        {
            writer.WriteElementString("timeStamp", FrameTime.ToString());
            writer.WriteElementString("frameNumber", FrameNumber.ToString());
            writer.WriteElementString("sensor", Sensor.ToString());
            writer.WriteElementString("ground", String.Format("{0},{1},{2},{3}", GroundW, GroundX, GroundY, GroundZ));
            writer.WriteElementString("trackingID", TrackingId.ToString());
            writer.WriteStartElement("skeleton");            
            if (Joints != null)
            {
                foreach (var joint in Joints)
                {
                    string jointStr = string.Format("{0},{1},{2},{3},{4},{5}", joint.TrackingState.ToString(), joint.X.ToString(), joint.Y.ToString(), joint.Z.ToString(), joint.DepthX.ToString(), joint.DepthY.ToString());
                    writer.WriteElementString(Enum.GetName(typeof(JointTypeGait), joint.JointType), jointStr);
                }
            }
            writer.WriteElementString("frameEdges", FrameEdges.ToString());
            writer.WriteEndElement(); // skeleton
            writer.WriteElementString("currentTime", CurrentTime.ToString());
            writer.WriteElementString("lean", String.Format("{0},{1},{2}", LeanX, LeanY, LeanTrackingState));
            writer.WriteStartElement("wii");
            writer.WriteElementString("COP", String.Format("{0},{1}", WiiBoardData.CenterOfPressureX, WiiBoardData.CenterOfPressureY));
            writer.WriteElementString("raw", String.Format("{0},{1},{2},{3}", WiiBoardData.Raw.TopLeft, WiiBoardData.Raw.TopRight, WiiBoardData.Raw.BottomLeft, WiiBoardData.Raw.BottomRight));
            writer.WriteElementString("cal0", String.Format("{0},{1},{2},{3}", WiiBoardData.Calib0kg.TopLeft, WiiBoardData.Calib0kg.TopRight, WiiBoardData.Calib0kg.BottomLeft, WiiBoardData.Calib0kg.BottomRight));
            writer.WriteElementString("cal17", String.Format("{0},{1},{2},{3}", WiiBoardData.Calib17kg.TopLeft, WiiBoardData.Calib17kg.TopRight, WiiBoardData.Calib17kg.BottomLeft, WiiBoardData.Calib17kg.BottomRight));
            writer.WriteElementString("cal34", String.Format("{0},{1},{2},{3}", WiiBoardData.Calib34kg.TopLeft, WiiBoardData.Calib34kg.TopRight, WiiBoardData.Calib34kg.BottomLeft, WiiBoardData.Calib34kg.BottomRight));
            writer.WriteEndElement();  // wii
            writer.WriteStartElement("depth");
            writer.WriteElementString("mean", String.Format("{0},{1},{2}", depthMeanX, depthMeanY, depthMeanZ));
            writer.WriteElementString("cov", String.Format("{0},{1},{2},{3},{4},{5}", depthMeanXX, depthMeanXY, depthMeanXZ, depthMeanYY, depthMeanYZ, depthMeanZZ));
            writer.WriteEndElement();  // depth
            writer.WriteStartElement("silhouette");
            writer.WriteElementString("points", silhouettePointsToString(silhouette.points));
            writer.WriteElementString("resolution", silhouette.resolution.ToString());
            writer.WriteElementString("xRange", silhouette.xRange.ToString());
            writer.WriteElementString("yRange", silhouette.yRange.ToString());
            writer.WriteElementString("zRange", silhouette.zRange.ToString());
            writer.WriteElementString("trackQuality", silhouette.trackQuality.ToString());
            writer.WriteElementString("numberOfPixelsInBody", silhouette.numberOfPixelsInBody.ToString());
            writer.WriteElementString("ground", String.Format("{0},{1},{2},{3}", silhouette.groundW, silhouette.groundX, silhouette.groundY, silhouette.groundZ));
            writer.WriteElementString("mean", String.Format("{0},{1},{2}", silhouette.depthMeanX, silhouette.depthMeanY, silhouette.depthMeanZ));
            writer.WriteElementString("Cov", String.Format("{0},{1},{2},{3},{4},{5}", silhouette.depthMeanXX, silhouette.depthMeanXY, silhouette.depthMeanXZ, silhouette.depthMeanYY, silhouette.depthMeanYZ, silhouette.depthMeanZZ));
            writer.WriteEndElement();  // silhouette
            
        }

        public TrackingStateGait getJointPosition(JointTypeGait joint, bool checkTracked, out float X, out float Y, out float Z)
        {
            if (Joints == null)
            {
                X = missingX;
                Y = missingY;
                Z = missingZ;
                return TrackingStateGait.NotTracked;
            }
            if (checkTracked)
            {
                int s = Joints.Select(x => (x.TrackingState == TrackingStateGait.Tracked) ? 0 : 1).Sum();
                if (s > 5)
                {
                    X = missingX;
                    Y = missingY;
                    Z = missingZ;
                    return TrackingStateGait.NotTracked;
                }
            }

            var jointPos = Joints[(int)joint];
            X = jointPos.X;
            Y = jointPos.Y;
            Z = jointPos.Z;
            return jointPos.TrackingState;
        }

        public void getGround(out float W, out float X, out float Y, out float Z, bool useKinectGround)
        {
            if (useKinectGround)
            {
                W = GroundW;
                X = GroundX;
                Y = GroundY;
                Z = GroundZ;
            }
            else
            {
                W = silhouette.groundW;
                X = silhouette.groundX;
                Y = silhouette.groundY;
                Z = silhouette.groundZ;
            }
        }

        public double getJointPositionProjection(JointTypeGait joint, float w, float x, float y, float z, bool checkTracked)
        {
            float jointX, jointY, jointZ;
            getJointPosition(joint, checkTracked, out jointX, out jointY, out jointZ);
            if (jointX == missingX && jointY == missingY && jointZ == missingZ) return -100;
            return dot(jointX, jointY, jointZ, x, y, z) + w;
        }

        public TrackingStateGait getJointPositionFromGround(JointTypeGait joint, bool checkTracked, out float X, out float Y, out float Z)
        {
            var t = getJointPosition(joint, checkTracked, out X, out Y, out Z);
            Y = dot(X, Y, Z, GroundX, GroundY, GroundZ) + GroundW;
            return t;
        }

        public static float dot(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            return x1 * x2 + y1 * y2 + z1 * z2 ;
        }

        public static float dot(Vector v1, Vector v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        Vector diff(JointGait j1, JointGait j2)
        {
            return new Vector(j1.X - j2.X, j1.Y - j2.Y, j1.Z - j2.Z);
        }

        public static Vector orthogonal(Vector v1, Vector v2)
        {
            Vector res = new Vector();
            var norm = dot(v2, v2);
            var product = dot(v1, v2);
            var ratio = product / norm;
            res.X = v1.X - ratio * v2.X;
            res.Y = v1.Y - ratio * v2.Y;
            res.Z = v1.Z - ratio * v2.Z;
            return res;
        }

        Vector orthogonal(JointGait v1, Vector v2)
        {
            Vector v1t = new Vector();
            v1t.X = v1.X;
            v1t.Y = v1.Y;
            v1t.Z = v1.Z;
            return orthogonal(v1t, v2);
        }

        public void getJointPositionInBodyCoordinates(JointTypeGait joint, float w, float x, float y, float z, bool checkTracked, out float xRes, out float yRes, out float zRes)
        {
            if (Joints == null)
            {
                xRes = -100; yRes = -100; zRes = -100;
                return;
            }
            if (checkTracked)
            {
                int s = Joints.Select(f => (f.TrackingState == TrackingStateGait.Tracked) ? 0 : 1).Sum();
                if (s > 5)
                {
                    xRes = -100; yRes = -100; zRes = -100;
                    return;
                }
            }

            Vector ground = new Vector(w, x, y, z);
            var jointPos = Joints[(int)joint];
            var hipLeft = Joints[(int)JointTypeGait.HipLeft];
            var hipRight = Joints[(int)JointTypeGait.HipRight];
            var hipCenter = (Sensor == SensorType.One) ? Joints[(int)JointTypeGait.SpineBase] : Joints[(int)JointTypeGait.HipCenter];
            Vector vw = diff(jointPos, hipCenter);
            Vector hipDirection = diff(hipLeft, hipRight);
            yRes = dot(vw, ground);
 
            vw = orthogonal(vw, ground); 
            hipDirection = orthogonal(hipDirection, ground);
            float norm = (float)Math.Sqrt(dot(hipDirection, hipDirection));
    
            xRes = dot(vw, hipDirection) / norm;
  
            vw = orthogonal(vw, hipDirection); //Left with Z direction
            zRes = (float)Math.Sqrt(dot(vw, vw)) * ((vw.Z > 0) ? 1 : -1); //Get norm and maintain sign
        }

        

        public void getJointPositionInBodyCoordinates(JointTypeGait joint, bool checkTracked, out float xRes, out float yRes, out float zRes)
        {
            getJointPositionInBodyCoordinates(joint, GroundW, GroundX, GroundY, GroundZ, checkTracked, out xRes, out yRes, out zRes);
        }

        static DoubleArray computeProjectionMatrix(Frame fr, bool useKinectGround)
        {
            float gx, gy, gz, gw;
            fr.getGround(out gw, out gx, out gy, out gz, useKinectGround);
            DoubleArray ground = DoubleArray.From(new double[] { gx, gy, gz });
            DoubleArray xv = DoubleArray.From(new double[] { 1, 0, 0 });
            DoubleArray zv = DoubleArray.From(new double[] { 0, 0, 1 });
            var ratio = xv.Dot(ground) / ground.Dot(ground);
            xv = xv - ratio * ground;
            xv = xv / Math.Sqrt(xv.Dot(xv));

            ratio = zv.Dot(ground) / ground.Dot(ground);
            zv = zv - ratio * ground;
            zv = zv / Math.Sqrt(zv.Dot(zv));

            var m = DoubleArray.VertStack(new DoubleArray[] { xv, ground, zv });
            return m;
        }

        static public void projectPointToTheGround(Frame fr, ref float x, ref float y, ref float z, bool useKinectGround)
        {
            using (var p = computeProjectionMatrix(fr, useKinectGround) * DoubleArray.From(new double[] { x, y, z }).Transpose())
            {
                x = (float)p[0];
                y = (float)p[1];
                z = (float)p[2];
            }
        }

        static public DoubleArray projectCovariance(Frame fr, bool useKinectGround)
        {
            using (var m = computeProjectionMatrix(fr, useKinectGround))
            {
                DoubleArray cov;
                if (useKinectGround)
                {
                    cov = DoubleArray.From(new double[,]
                        {
                        {fr.depthMeanXX, fr.depthMeanXY, fr.depthMeanXZ},
                        {fr.depthMeanXY, fr.depthMeanYY, fr.depthMeanYZ},
                        {fr.depthMeanXZ, fr.depthMeanYZ, fr.depthMeanZZ}
                        });
                }
                else
                {
                    cov = DoubleArray.From(new double[,]
                        {
                        {fr.silhouette.depthMeanXX, fr.silhouette.depthMeanXY, fr.silhouette.depthMeanXZ},
                        {fr.silhouette.depthMeanXY, fr.silhouette.depthMeanYY, fr.silhouette.depthMeanYZ},
                        {fr.silhouette.depthMeanXZ, fr.silhouette.depthMeanYZ, fr.silhouette.depthMeanZZ}
                        });
                }

                var rotate = m * cov * m.Transpose();
                if (cov != null) cov.Dispose();
                return rotate;
            }

        }

        static public void projectCovarianceToTheGround(Frame fr, out float xy, out float yy, out float yz, bool useKinectGround)
        {
            var rotate = projectCovariance(fr, useKinectGround);
            xy = (float)rotate[0, 1];
            yy = (float)rotate[1, 1];
            yz = (float)rotate[2, 1];
        }

        /// <summary>
        /// estimates the center of mass
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <param name="slopeX"></param>
        /// <param name="slopeZ"></param>
        /// <param name="input"></param>
        /// <param name="projection"></param>
        /// <returns>returns false if the quality of the frame is too low</returns>
        public bool getCOM(out float x, out float z, out float slopeX, out float slopeZ, out Extreams extreamValues, EinputMode input = EinputMode.line, EprojectionMode projection = EprojectionMode.ground)
        {
            slopeX = slopeZ = 0;
            float minX = -1, maxX = -1, minZ = -1, maxZ = -1;
            float y;
            if (input != EinputMode.wii && silhouette.points.Length > 0)
            {
                var halfX = silhouette.xRange / 2;
                minX = silhouette.points.Min(p => p.X) * silhouette.xRange / 256 - halfX;
                maxX = silhouette.points.Max(p => p.X) * silhouette.xRange / 256 - halfX;
                minZ = silhouette.points.Min(p => p.Z) * silhouette.zRange / 256;
                maxZ = silhouette.points.Max(p => p.Z) * silhouette.zRange / 256;
            }
            extreamValues = new Extreams(minX, maxX, minZ, maxZ, FrameTime);
            
            switch (input)
            {
                case EinputMode.wii: 
                    x = WiiBoardData.CenterOfPressureY / 1000;
                    z = WiiBoardData.CenterOfPressureX / 1000;
                    break;
                case EinputMode.mean:
                    if (projection == EprojectionMode.ground)
                    {
                        x = depthMeanX; y = depthMeanY; z = depthMeanZ;
                        projectPointToTheGround(this, ref x, ref y, ref z, true);
                    }
                    else
                    {
                        x = depthMeanX;
                        z = depthMeanZ;
                    }
                    break;
                case EinputMode.silhouetteMean:
                    if (silhouette.numberOfPixelsInBody < 10)
                    {
                        x = z = -2;
                        return false;
                    }
                    if (projection == EprojectionMode.ground)
                    {
                        x = silhouette.depthMeanX; y = silhouette.depthMeanY; z = silhouette.depthMeanZ;
                        projectPointToTheGround(this, ref x, ref y, ref z, false);
                    }
                    else
                    {
                        x = silhouette.depthMeanX;
                        z = silhouette.depthMeanZ;
                    }
                    break;
                case EinputMode.neck:
                    getJointPosition(JointTypeGait.Neck, false, out x, out y, out z);
                    if (projection == EprojectionMode.ground)
                        projectPointToTheGround(this, ref x, ref y, ref z, true);
                    break;
                case EinputMode.line:
                    float xy = depthMeanXY;
                    float yy = depthMeanYY;
                    float yz = depthMeanYZ;
                    x = depthMeanX;
                    y = depthMeanY;
                    z = depthMeanZ;
                    if (projection == EprojectionMode.ground)
                    {
                        projectPointToTheGround(this, ref x, ref y, ref z, true);
                        projectCovarianceToTheGround(this, out xy, out yy, out yz, true);
                    }
                    slopeX = (xy - x * y) / (yy - y * y);
                    slopeZ = (yz - z * y) / (yy - y * y);
                    float offsetX = x - slopeX * y;
                    float offsetZ = z - slopeZ * y;
                    x = slopeX + offsetX;
                    z = slopeZ + offsetZ;
                    break;
                case EinputMode.silhouetteLine:
                    if (silhouette.numberOfPixelsInBody < 10)
                    {
                        x = z = -2;
                        return false;
                    }
                    float silhouetteOffsetX;
                    float silhouetteOffsetZ;
                    getStickModelFromSilhouette(out slopeX, out slopeZ, out silhouetteOffsetX, out silhouetteOffsetZ, projection);
                    x = slopeX + silhouetteOffsetX;
                    z = slopeZ + silhouetteOffsetZ;
                    break;
                default:
                    throw new Exception("Unknown input mode");
            }
            return true;
        }

        public void getStickModelFromSilhouette(out float slopeX, out float slopeZ, out float offsetX, out float offsetZ, EprojectionMode projection)
        {
            float x, y, z;
            float silhouettexy = silhouette.depthMeanXY;
            float silhouetteyy = silhouette.depthMeanYY;
            float silhouetteyz = silhouette.depthMeanYZ;
            slopeX = slopeZ = offsetX = offsetZ = -1;
            x = silhouette.depthMeanX;
            y = silhouette.depthMeanY;
            z = silhouette.depthMeanZ;
            if (projection == EprojectionMode.ground)
            {
                projectPointToTheGround(this, ref x, ref y, ref z, false);
                projectCovarianceToTheGround(this, out silhouettexy, out silhouetteyy, out silhouetteyz, false);
            }
            if ((silhouetteyy - y * y) < 1e-10)
            {
                x = z = 0;
                return;
            }
            slopeX = (silhouettexy - x * y) / (silhouetteyy - y * y);
            slopeZ = (silhouetteyz - z * y) / (silhouetteyy - y * y);
            offsetX = x - slopeX * y;
            offsetZ = z - slopeZ * y;

        }
       
    }
}
