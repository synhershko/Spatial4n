using System;

namespace Spatial4n.Core.Exceptions
{
    [Serializable]
    public class ParseException : Exception
    {
        public ParseException(string message, int errorOffset)
            : base(message)
        {
            ErrorOffset = errorOffset;
        }

        public int ErrorOffset { get; private set; }
    }
}
