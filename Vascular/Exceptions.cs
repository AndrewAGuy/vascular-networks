using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular
{
    [Serializable]
    public class VascularException : Exception
    {
        public VascularException() { }
        public VascularException(string message) : base(message) { }
        public VascularException(string message, Exception inner) : base(message, inner) { }
        protected VascularException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class GeometryException : VascularException
    {
        public GeometryException() { }
        public GeometryException(string message) : base(message) { }
        public GeometryException(string message, Exception inner) : base(message, inner) { }
        protected GeometryException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class TopologyException : VascularException
    {
        public TopologyException() { }
        public TopologyException(string message) : base(message) { }
        public TopologyException(string message, Exception inner) : base(message, inner) { }
        protected TopologyException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class PhysicalValueException : VascularException
    {
        public PhysicalValueException() { }
        public PhysicalValueException(string message) : base(message) { }
        public PhysicalValueException(string message, Exception inner) : base(message, inner) { }
        protected PhysicalValueException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class InputException : VascularException
    {
        public InputException() { }
        public InputException(string message) : base(message) { }
        public InputException(string message, Exception inner) : base(message, inner) { }
        protected InputException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class FileFormatException : VascularException
    {
        public FileFormatException() { }
        public FileFormatException(string message) : base(message) { }
        public FileFormatException(string message, Exception inner) : base(message, inner) { }
        protected FileFormatException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
