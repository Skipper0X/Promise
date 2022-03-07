using System;

namespace Skipper.Promise.Runtime
{
    /// <summary>
    /// <see cref="PromiseCore"/> Is The Main Facade Class For All Types Of Promises...
    /// </summary>
    public abstract class PromiseCore
    {
        public bool HadAnyErrorHandlers { protected set; get; }
        public bool IsCompleted => IsDone;
        public static readonly Unit Unit = new Unit();
        public static Promise<Unit> SuccessfulUnit => Promise<Unit>.Successful(Unit);

        protected bool IsDone;
        protected Exception Exception;
        protected Action<Exception> ErrorHandlers;

        private static PromiseEvent _onPotentialUncaughtError;

        public static void SetPotentialUncaughtErrorHandler(PromiseEvent handler)
            => _onPotentialUncaughtError = handler;

        protected void InvokeUncaughtPromise()
            => _onPotentialUncaughtError?.Invoke(this, Exception);
    }
}