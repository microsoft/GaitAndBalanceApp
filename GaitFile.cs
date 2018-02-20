using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace GaitAndBalanceApp
{
    public class GaitFile
    {
        Memory memory;

        TrackingStateGait StringToTrackingState(string str)
        {
            if (str == "Tracked") return TrackingStateGait.Tracked;
            else if (str == "Inferred") return TrackingStateGait.Inferred;
            return TrackingStateGait.NotTracked;
        }

        void AddToJointList(XmlNode skel, string jointstr, List<JointGait> joints)
        {
            JointTypeGait jointType;
            Enum.TryParse<JointTypeGait>(jointstr, out jointType);

            if (skel != null && skel[jointstr] != null)
            {
                string[] line = skel[jointstr].InnerText.Split(',');

                TrackingStateGait ts = StringToTrackingState(line[0]);

                float x, y, z, depthX, depthY;
                float.TryParse(line[1], out x);
                float.TryParse(line[2], out y);
                float.TryParse(line[3], out z);
                float.TryParse(line[4], out depthX);
                float.TryParse(line[5], out depthY);

                JointGait joint = new JointGait() { X = x, Y = y, Z = z, DepthX = depthX, DepthY = depthY, JointType = jointType, TrackingState = ts };
                joints.Add(joint);
            }
            else joints.Add(new JointGait() { JointType = jointType });
        }

        byte getByteFromString(string s, int i)
        {
            var lsb = s[i + 1] - '0';
            if (lsb > 9) lsb = s[i + 1] - 'a' + 10;
            var msb = s[i] - '0';
            if (msb > 9) msb = s[i] - 'a' + 10;
            return (byte)(lsb + (msb << 4));
        }

        SilhouettePoint[] hex2PointsArray(string hex)
        {
            
            SilhouettePoint[] points = new SilhouettePoint[hex.Length / 8];
            string lower = hex.ToLower();
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new SilhouettePoint()
                {
                    X = getByteFromString(lower, i * 8),
                    Y = getByteFromString(lower, i * 8 + 2),
                    Z = getByteFromString(lower, i * 8 + 4),
                    Confidence = getByteFromString(lower, i * 8 + 6)
                };
            }
            return points;
        }

        // The caller passes the contents of the file 
        // Parse the input and create the Memory object
        public void ReadFile(string fname)
        {
            // This is a perf thing: we try only once to get these fields. If we have an older file, there will
            // be an exception thrown, we will set the flag to false and not try again.
            bool tryCurrentTime = true;
            bool tryLean = true;
            bool tryDepth = true;
            bool tryDepthMean = true;
            bool tryCov = true;
            bool trySilhouette = true;

            Logger.log("GaitFile: reading file {0}", fname);
            string content = File.ReadAllText(fname);
            memory = new Memory();
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(content);
            XmlNodeList xnList = xml.SelectNodes("/Memory/frame");
            foreach (XmlNode xn in xnList)
            {
                Frame fr = new Frame();
                Int64 t;
                if (Int64.TryParse(xn["frameNumber"].InnerText, out t)) fr.FrameNumber = t;
                if (Int64.TryParse(xn["timeStamp"].InnerText, out t)) fr.FrameTime = t;
                string sensor = xn["sensor"].InnerText;
                if (sensor == "One") fr.Sensor = SensorType.One;
                else if (sensor == "Wii") fr.Sensor = SensorType.Wii;
                else fr.Sensor = SensorType.ThreeSixty;

                UInt64 ut;
                if (UInt64.TryParse(xn["trackingID"].InnerText, out ut)) fr.TrackingId = ut;

                string[] ground = xn["ground"].InnerText.Split(',');

                float ft;
                if (float.TryParse(ground[0], out ft)) fr.GroundW = ft;
                if (float.TryParse(ground[1], out ft)) fr.GroundX = ft;
                if (float.TryParse(ground[2], out ft)) fr.GroundY = ft;
                if (float.TryParse(ground[3], out ft)) fr.GroundZ = ft;

                if (tryCurrentTime)
                {
                    try
                    {
                        long currentTime = Convert.ToInt64(xn["currentTime"].InnerText);
                        fr.CurrentTime = currentTime;
                    }
                    catch (NullReferenceException)
                    {
                        tryCurrentTime = false;
                    }
                }

                var skel = xn.SelectNodes("skeleton")[0];
                fr.Joints = new List<JointGait>();
                if (fr.Sensor == SensorType.One)
                {
                    AddToJointList(skel, "SpineBase", fr.Joints);
                    AddToJointList(skel, "SpineMid", fr.Joints);
                    AddToJointList(skel, "Neck", fr.Joints);
                    AddToJointList(skel, "Head", fr.Joints);
                    AddToJointList(skel, "ShoulderLeft", fr.Joints);
                    AddToJointList(skel, "ElbowLeft", fr.Joints);
                    AddToJointList(skel, "WristLeft", fr.Joints);
                    AddToJointList(skel, "HandLeft", fr.Joints);
                    AddToJointList(skel, "ShoulderRight", fr.Joints);
                    AddToJointList(skel, "ElbowRight", fr.Joints);
                    AddToJointList(skel, "WristRight", fr.Joints);
                    AddToJointList(skel, "HandRight", fr.Joints);
                    AddToJointList(skel, "HipLeft", fr.Joints);
                    AddToJointList(skel, "KneeLeft", fr.Joints);
                    AddToJointList(skel, "AnkleLeft", fr.Joints);
                    AddToJointList(skel, "FootLeft", fr.Joints);
                    AddToJointList(skel, "HipRight", fr.Joints);
                    AddToJointList(skel, "KneeRight", fr.Joints);
                    AddToJointList(skel, "AnkleRight", fr.Joints);
                    AddToJointList(skel, "FootRight", fr.Joints);
                    AddToJointList(skel, "SpineShoulder", fr.Joints);
                    AddToJointList(skel, "HandTipLeft", fr.Joints);
                    AddToJointList(skel, "ThumbLeft", fr.Joints);
                    AddToJointList(skel, "HandTipRight", fr.Joints);
                    AddToJointList(skel, "ThumbRight", fr.Joints);

                    if (tryLean)
                    {
                        try
                        {
                            string[] lean = xn["lean"].InnerText.Split(',');
                            fr.LeanX = Convert.ToSingle(lean[0]);
                            fr.LeanY = Convert.ToSingle(lean[1]);
                            fr.LeanTrackingState = StringToTrackingState(lean[2]);
                        }
                        catch (NullReferenceException)
                        {
                            fr.LeanX = float.NaN;
                            fr.LeanY = float.NaN;
                            fr.LeanTrackingState = TrackingStateGait.NotTracked;
                            tryLean = false;
                        }
                    }
                    else
                    {
                        fr.LeanX = float.NaN;
                        fr.LeanY = float.NaN;
                        fr.LeanTrackingState = TrackingStateGait.NotTracked;
                    }
                    if (trySilhouette)
                    {
                        try
                        {
                            var xns = xn["silhouette"];
                            string[] sGround = xns["ground"].InnerText.Split(',');
                            string[] mean = xns["mean"].InnerText.Split(',');
                            string[] Cov = xns["Cov"].InnerText.Split(',');
                            fr.silhouette = new SilhouetteData()
                            {
                                points = hex2PointsArray(xns["points"].InnerText),
                                resolution = Convert.ToSingle(xns["resolution"].InnerText),
                                xRange = Convert.ToSingle(xns["xRange"].InnerText),
                                yRange = Convert.ToSingle(xns["yRange"].InnerText),
                                zRange = Convert.ToSingle(xns["zRange"].InnerText),
                                trackQuality = Convert.ToByte(xns["trackQuality"].InnerText),
                                numberOfPixelsInBody = Convert.ToInt32(xns["numberOfPixelsInBody"].InnerText),
                                groundW = Convert.ToSingle(sGround[0]),
                                groundX = Convert.ToSingle(sGround[1]),
                                groundY = Convert.ToSingle(sGround[2]),
                                groundZ = Convert.ToSingle(sGround[3]),
                                depthMeanX = Convert.ToSingle(mean[0]),
                                depthMeanY = Convert.ToSingle(mean[1]),
                                depthMeanZ = Convert.ToSingle(mean[2]),
                                depthMeanXX = Convert.ToSingle(Cov[0]),
                                depthMeanXY = Convert.ToSingle(Cov[1]),
                                depthMeanXZ = Convert.ToSingle(Cov[2]),
                                depthMeanYY = Convert.ToSingle(Cov[3]),
                                depthMeanYZ = Convert.ToSingle(Cov[4]),
                                depthMeanZZ = Convert.ToSingle(Cov[5]),
                            };
                        }
                        catch (NullReferenceException)
                        {
                            trySilhouette = false;
                        }
                    }
                }
                else if (fr.Sensor == SensorType.ThreeSixty)
                {
                    AddToJointList(null, "SpineBase", fr.Joints);
                    AddToJointList(null, "SpineMid", fr.Joints);
                    AddToJointList(null, "Neck", fr.Joints);

                    AddToJointList(skel, "Head", fr.Joints);
                    AddToJointList(skel, "ShoulderLeft", fr.Joints);
                    AddToJointList(skel, "ElbowLeft", fr.Joints);
                    AddToJointList(skel, "WristLeft", fr.Joints);
                    AddToJointList(skel, "HandLeft", fr.Joints);
                    AddToJointList(skel, "ShoulderRight", fr.Joints);
                    AddToJointList(skel, "ElbowRight", fr.Joints);
                    AddToJointList(skel, "WristRight", fr.Joints);
                    AddToJointList(skel, "HandRight", fr.Joints);
                    AddToJointList(skel, "HipLeft", fr.Joints);
                    AddToJointList(skel, "KneeLeft", fr.Joints);
                    AddToJointList(skel, "AnkleLeft", fr.Joints);
                    AddToJointList(skel, "FootLeft", fr.Joints);
                    AddToJointList(skel, "HipRight", fr.Joints);
                    AddToJointList(skel, "KneeRight", fr.Joints);
                    AddToJointList(skel, "AnkleRight", fr.Joints);
                    AddToJointList(skel, "FootRight", fr.Joints);
                    AddToJointList(null, "SpineShoulder", fr.Joints);
                    AddToJointList(null, "HandTipLeft", fr.Joints);
                    AddToJointList(null, "ThumbLeft", fr.Joints);
                    AddToJointList(null, "HandTipRight", fr.Joints);
                    AddToJointList(null, "ThumbRight", fr.Joints);

                    AddToJointList(skel, "HipCenter", fr.Joints);
                    AddToJointList(skel, "Spine", fr.Joints);
                    AddToJointList(skel, "ShoulderCenter", fr.Joints);
                }
                else if (fr.Sensor == SensorType.Wii)
                {
                    var wii = xn.SelectNodes("wii")[0];
                    
                    string[] cop = wii["COP"].InnerText.Split(',');
                    string[] raw = wii["raw"].InnerText.Split(',');
                    string[] cal0 = wii["cal0"].InnerText.Split(',');
                    string[] cal17 = wii["cal17"].InnerText.Split(',');
                    string[] cal34 = wii["cal34"].InnerText.Split(',');

                    var Raw = new WiiSensorData() { TopLeft = Convert.ToInt16(raw[0]), TopRight = Convert.ToInt16(raw[1]), BottomLeft = Convert.ToInt16(raw[2]), BottomRight = Convert.ToInt16(raw[3]) };
                    var Calib0kg = new WiiSensorData() { TopLeft = Convert.ToInt16(cal0[0]), TopRight = Convert.ToInt16(cal0[1]), BottomLeft = Convert.ToInt16(cal0[2]), BottomRight = Convert.ToInt16(cal0[3]) };
                    var Calib17kg = new WiiSensorData() { TopLeft = Convert.ToInt16(cal17[0]), TopRight = Convert.ToInt16(cal17[1]), BottomLeft = Convert.ToInt16(cal17[2]), BottomRight = Convert.ToInt16(cal17[3]) };
                    var Calib34kg = new WiiSensorData() { TopLeft = Convert.ToInt16(cal34[0]), TopRight = Convert.ToInt16(cal34[1]), BottomLeft = Convert.ToInt16(cal34[2]), BottomRight = Convert.ToInt16(cal34[3]) };

                    fr.WiiBoardData = new WiiData() { CenterOfPressureX = Convert.ToSingle(cop[0]), CenterOfPressureY = Convert.ToSingle(cop[1]), Raw = Raw, Calib0kg = Calib0kg, Calib17kg = Calib17kg, Calib34kg = Calib34kg };
                }
                fr.FrameEdges = Convert.ToInt32(skel["frameEdges"].InnerText);


                XmlNode depth = null;
                fr.depthMeanX = float.NaN;
                fr.depthMeanY = float.NaN;
                fr.depthMeanZ = float.NaN;

                if (tryDepth)
                {
                    try
                    {
                        depth = xn.SelectNodes("depth")[0];
                    }
                    catch (NullReferenceException)
                    {
                        tryDepth = false;   // don't try again
                    }
                }

                if (tryDepth && tryDepthMean)
                {
                    try
                    {
                        string[] mean = depth["mean"].InnerText.Split(',');
                        if (float.TryParse(mean[0], out ft)) fr.depthMeanX = ft;
                        if (float.TryParse(mean[1], out ft)) fr.depthMeanY = ft;
                        if (float.TryParse(mean[2], out ft)) fr.depthMeanZ = ft;
                    }
                    catch (NullReferenceException)
                    {
                        tryDepthMean = false;
                    }
                }
                if (tryDepth && tryCov)
                {
                    try
                    {
                        string[] cov = depth["cov"].InnerText.Split(',');
                        if (float.TryParse(cov[0], out ft)) fr.depthMeanXX = ft;
                        if (float.TryParse(cov[1], out ft)) fr.depthMeanXY = ft;
                        if (float.TryParse(cov[2], out ft)) fr.depthMeanXZ = ft;
                        if (float.TryParse(cov[3], out ft)) fr.depthMeanYY = ft;
                        if (float.TryParse(cov[4], out ft)) fr.depthMeanYZ = ft;
                        if (float.TryParse(cov[5], out ft)) fr.depthMeanZZ = ft;
                    }
                    catch (NullReferenceException)
                    {
                        tryCov = false;
                    }
                }
                memory.addFrame(fr);
            }
            Logger.log("GaitFile: done reading file {0}", fname);
        }

        public Frame GetFirstFrame()
        {
            return memory != null ? memory.firstFrame : null;
        }

        public Frame GetLastFrame()
        {
            return memory != null ? memory.lastFrame : null;
        }

        public Frame GetNextFrame(long frameId)
        {
            return memory != null ? memory.next(frameId) : null;
        }

        public Frame GetPreviousFrame(long frameId)
        {
            return memory != null ? memory.prev(frameId) : null;
        }

        public Frame GetFrameFromTime(long start, long end)
        {
            Frame fr = GetFirstFrame();
            if (fr != null)
            {
                if (fr.FrameTime >= start && fr.FrameTime <= end) return fr;
                while (true)
                {
                    fr = GetNextFrame(fr.FrameNumber);
                    if (fr == null) break;
                    if (fr.FrameTime >= start && fr.FrameTime <= end) return fr;
                }
            }
            return null;
        }
    }
}
