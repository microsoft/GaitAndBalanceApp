using System;
using System.Configuration;
using System.IO;
using System.Xml.Serialization;

namespace GaitAndBalanceApp
{
    public class Memory : IXmlSerializable
    {

        Frame[] frames;
        int nextPtr;
        object locker;
        bool _framesAvailable;
        int capacity;

        long startIndexForWrite;
        long endIndexForWrite;

        public Memory()
        {
            nextPtr = 0;
            locker = new object();
            capacity = 0;
            Int32.TryParse(ConfigurationManager.AppSettings["MemoryLength"], out capacity);
            if (capacity <= 0) capacity = 30 * 60 * 60;
            frames = new Frame[capacity];
        }

        public void Clear()
        {
            lock (locker)
            {
                Array.Clear(frames, 0, frames.Length);
                _firstFrame = _lastFrame = null;
                nextPtr = 0;
                _framesAvailable = false;
            }
        }

        Frame _lastFrame = null;
        public Frame lastFrame { get { lock (locker) { return _lastFrame; } } }
        Frame _firstFrame = null;
        public Frame firstFrame { get { lock (locker) { return _firstFrame; } } }

        public int count
        {
            get
            {
                lock (locker)
                {
                    return (frames[nextPtr] == null) ? nextPtr : frames.Length;
                }
            }

        }
        public Frame this[long frameNumber] { get { return getFrame(frameNumber); } }

        private long getFrameIndex(long frameNumber)
        {
            int p = -1;
            if (frames[0].FrameNumber <= frameNumber)
                p = Array.BinarySearch(frames, 0, nextPtr, frameNumber);
            else
                p = Array.BinarySearch(frames, nextPtr, frames.Length - nextPtr, frameNumber);
            return p;
        }

        private Frame getFrame(long frameNumber)
        {
            lock (locker)
            {
                long p = getFrameIndex(frameNumber);
                if (p < 0)
                    return null;
                return (p >= 0) ? frames[p] : null;
            }
        }

        public Frame next(long frameNumber)
        {
            if (firstFrame == null || frameNumber < firstFrame.FrameNumber)
                return firstFrame;
            if (frameNumber >= lastFrame.FrameNumber)
                return null;
            Frame f = null;
            while (++frameNumber <= lastFrame.FrameNumber && f == null)
            {
                f = getFrame(frameNumber);
            }
            return f;
        }

        public Frame prev(long frameNumber)
        {
            if (lastFrame == null || frameNumber > lastFrame.FrameNumber)
                return lastFrame;
            if (frameNumber <= firstFrame.FrameNumber)
                return null;
            Frame f = null;
            while (--frameNumber >= firstFrame.FrameNumber && f == null)
            {
                f = getFrame(frameNumber);
            }
            return f;
        }

        public void addFrame(Frame frame)
        {
            lock (locker)
            {
                if (frame != null)
                {
                    bool newFirstFrame = false;
                    if (_firstFrame == null) _firstFrame = frame;
                    else if (_firstFrame == frames[nextPtr]) newFirstFrame = true;
                    frames[nextPtr++] = frame;
                    if (nextPtr >= frames.Length) nextPtr = 0;
                    if (newFirstFrame) _firstFrame = frames[nextPtr];
                    _framesAvailable = true;
                    _lastFrame = frame;
                }
            }
        }


        public bool framesAvailable { get { return _framesAvailable; } }

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }


        // IXmlSerializable
        public void ReadXml(System.Xml.XmlReader reader)
        {

        }

        public void save(string filename, long startFrameId, long endFrameId)
        {
            lock (locker)
            {
                startIndexForWrite = startFrameId;
                endIndexForWrite = endFrameId;

                XmlSerializer writer = XmlSerializer.FromTypes(new[] { this.GetType() })[0];
                using (StreamWriter file = new StreamWriter(filename))
                {
                    writer.Serialize(file, this);
                }
            }
        }

        // IXmlSerializable
        public void WriteXml(System.Xml.XmlWriter writer)
        {
            for (long i = startIndexForWrite; i <= endIndexForWrite; ++i)
            {
                Frame frame = getFrame(i);
                writer.WriteStartElement("frame");
                frame.writeXml(writer);
                writer.WriteEndElement();
            }
        }
    }
}