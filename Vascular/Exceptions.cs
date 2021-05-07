using System;

namespace Vascular
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class VascularException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public VascularException() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public VascularException(string message) : base(message) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public VascularException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected VascularException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class GeometryException : VascularException
    {
        /// <summary>
        /// 
        /// </summary>
        public GeometryException() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public GeometryException(string message) : base(message) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public GeometryException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected GeometryException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class TopologyException : VascularException
    {
        /// <summary>
        /// 
        /// </summary>
        public TopologyException() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public TopologyException(string message) : base(message) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public TopologyException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected TopologyException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class PhysicalValueException : VascularException
    {
        /// <summary>
        /// 
        /// </summary>
        public PhysicalValueException() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public PhysicalValueException(string message) : base(message) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public PhysicalValueException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected PhysicalValueException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class InputException : VascularException
    {
        /// <summary>
        /// 
        /// </summary>
        public InputException() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public InputException(string message) : base(message) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public InputException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InputException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class FileFormatException : VascularException
    {
        /// <summary>
        /// 
        /// </summary>
        public FileFormatException() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public FileFormatException(string message) : base(message) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public FileFormatException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected FileFormatException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
