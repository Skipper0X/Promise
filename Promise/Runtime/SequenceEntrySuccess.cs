namespace Skipper.Promise.Runtime
{
    public class SequenceEntrySuccess<T>
    {
        public int Index { get; private set; }
        public T Result { get; private set; }

        public SequenceEntrySuccess(int index, T result)
        {
            Index = index;
            Result = result;
        }
    }
}