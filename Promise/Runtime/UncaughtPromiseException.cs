using System;

namespace Skipper.Promise.Runtime
{
    public class UncaughtPromiseException : Exception
    {
        public readonly PromiseCore Promise;

        public UncaughtPromiseException(PromiseCore promise, Exception ex) : base(
            $"Uncaught promise innerMsg=[{ex.Message}]", ex) =>
            Promise = promise;
    }
}