using System;
#if FEATURE_SERIALIZABLE
using System.Runtime.Serialization;
#endif

namespace Spatial4n.Core.Exceptions
{
    /// <summary>
    /// Spatial4n specific class - used to mimic Java's RuntimeException because we sometimes need to catch
    /// exceptions that are lower level than InvalidShapeException, but need something more specialized than
    /// <see cref="Exception"/>.
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class RuntimeException : Exception
    {
        public RuntimeException(string? message)
            : base(message)
        { }

        public RuntimeException(string? message, Exception? innerException)
            : base(message, innerException)
        { }

#if FEATURE_SERIALIZABLE
        public RuntimeException()
        { }

        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected RuntimeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
