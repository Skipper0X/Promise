using System;

namespace Skipper.Promise.Runtime
{
    public class SequenceEntryException : Exception
    {
        public int Index { get; }

        public SequenceEntryException(int index, Exception inner) : base($"index[{index}]. {inner.Message}", inner)
        {
            Index = index;
        }
    }
}