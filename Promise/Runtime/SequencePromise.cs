using System;
using System.Collections.Generic;
using System.Linq;

namespace Skipper.Promise.Runtime
{
    public class SequencePromise<T> : Promise<IList<T>>
    {
        private Action<SequenceEntryException> _entryErrorCallbacks;
        private Action<SequenceEntrySuccess<T>> _entrySuccessCallbacks;

        private List<SequenceEntryException> _errors = new List<SequenceEntryException>();
        private List<SequenceEntrySuccess<T>> _successes = new List<SequenceEntrySuccess<T>>();

        private Dictionary<int, object> _indexToResult = new Dictionary<int, object>();

        public int SuccessCount => _successes.Count;
        public int ErrorCount => _errors.Count;
        public int Total => _errors.Count + _successes.Count;
        public int Count { get; }

        public float Ratio => HasProcessedAllEntries ? 1 : Total / (float) Count;
        public bool HasProcessedAllEntries => Total == Count;

        public IEnumerable<T> SuccessfulResults => _successes.Select(s => s.Result);

        public SequencePromise(int count)
        {
            Count = count;
            if (Count == 0)
            {
                CompleteSuccess();
            }
        }

        public SequencePromise<T> OnElementError(Action<SequenceEntryException> handler)
        {
            foreach (var existingError in _errors)
            {
                handler?.Invoke(existingError);
            }

            _entryErrorCallbacks += handler;
            return this;
        }

        public SequencePromise<T> OnElementSuccess(Action<SequenceEntrySuccess<T>> handler)
        {
            foreach (var success in _successes)
            {
                handler?.Invoke(success);
            }

            _entrySuccessCallbacks += handler;
            return this;
        }

        public void CompleteSuccess()
        {
            base.CompleteSuccess(SuccessfulResults.ToList());
        }

        public void ReportEntryError(SequenceEntryException exception)
        {
            if (_indexToResult.ContainsKey(exception.Index) || exception.Index >= Count) return;

            _errors.Add(exception);
            _indexToResult.Add(exception.Index, exception);
            _entryErrorCallbacks?.Invoke(exception);

            CompleteError(exception.InnerException);
        }

        public void ReportEntrySuccess(SequenceEntrySuccess<T> success)
        {
            if (_indexToResult.ContainsKey(success.Index) || success.Index >= Count) return;

            _successes.Add(success);
            _indexToResult.Add(success.Index, success);
            _entrySuccessCallbacks?.Invoke(success);

            if (HasProcessedAllEntries)
            {
                CompleteSuccess();
            }
        }

        public void ReportEntrySuccess(int index, T result) =>
            ReportEntrySuccess(new SequenceEntrySuccess<T>(index, result));

        public void ReportEntryError(int index, Exception err) =>
            ReportEntryError(new SequenceEntryException(index, err));
    }
}