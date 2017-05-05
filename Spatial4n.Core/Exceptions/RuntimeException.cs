using System;

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
        public RuntimeException(string message)
            : base(message)
        { }

        public RuntimeException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
