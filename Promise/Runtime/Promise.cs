using System;
using System.Runtime.CompilerServices;

namespace Skipper.Promise.Runtime
{
    public delegate void PromiseEvent(PromiseCore promise, Exception err);

    public class Promise<T> : PromiseCore, ICriticalNotifyCompletion
    {
        private T _promiseValue;
        private Action<T> _promiseHandlers;

        /// <summary>
        /// Complete This Promise With Success & Finalize This <see cref="PromiseCore"/>
        /// </summary>
        /// <param name="value">this Promise's Success Value of Type <see cref="T"/></param>
        public void CompleteSuccess(T value)
        {
            if (IsDone) return;

            IsDone = true;
            _promiseValue = value;

            _promiseHandlers.Dispatch(_promiseValue);
            _promiseHandlers = null;
            ErrorHandlers = null;
        }

        /// <summary>
        /// Complete This Promise With Error & Throw Given Exception & Finalize This <see cref="PromiseCore"/>
        /// </summary>
        public void CompleteError(Exception exception)
        {
            if (IsDone) return;

            Exception = exception;
            IsDone = true;

            if (HadAnyErrorHandlers == false) InvokeUncaughtPromise();
            else ErrorHandlers.Dispatch(exception);

            _promiseHandlers = null;
            ErrorHandlers = null;
        }

        /// <summary>
        /// Register A Success Handler For This Promise...
        /// </summary>
        public Promise<T> Then(Action<T> handler)
        {
            if (IsDone)
            {
                if (Exception == null) handler.Dispatch(_promiseValue);
                return this;
            }

            _promiseHandlers += handler;
            return this;
        }

        /// <summary>
        /// Register A Failure Handle For This Promise...
        /// </summary>
        public Promise<T> Error(Action<Exception> errorHandler)
        {
            HadAnyErrorHandlers = true;
            if (IsDone)
            {
                if (Exception != null) errorHandler.Dispatch(Exception);
                return this;
            }

            ErrorHandlers += errorHandler;
            return this;
        }

        /// <summary>
        ///  Map This Promise With The Given Provider.....
        /// </summary>
        public Promise<TU> Map<TU>(Func<T, TU> callback)
        {
            var result = new Promise<TU>();
            Then(value =>
                {
                    try
                    {
                        var nextResult = callback(value);
                        result.CompleteSuccess(nextResult);
                    }
                    catch (Exception ex)
                    {
                        result.CompleteError(ex);
                    }
                })
                .Error(ex => result.CompleteError(ex));
            return result;
        }

        /// <summary>
        ///  FlatMap This Promise On Given Handler & Factory Provider To Resolve.....
        /// </summary>
        public TPromiseU FlatMap<TPromiseU, TU>(Func<T, TPromiseU> handler, Func<TPromiseU> factory)
            where TPromiseU : Promise<TU>
        {
            var pu = factory();
            FlatMap(handler)
                .Then(pu.CompleteSuccess)
                .Error(pu.CompleteError);
            return pu;
        }

        /// <summary>
        ///  FlatMap This Promise On Given Handler & Factory Provider To Resolve.....
        /// </summary>
        public Promise<TU> FlatMap<TU>(Func<T, Promise<TU>> callback)
        {
            var result = new Promise<TU>();
            Then(value =>
            {
                try
                {
                    callback(value)
                        .Then(valueInner => result.CompleteSuccess(valueInner))
                        .Error(ex => result.CompleteError(ex));
                }
                catch (Exception ex)
                {
                    result.CompleteError(ex);
                }
            }).Error(ex => { result.CompleteError(ex); });
            return result;
        }

        /// <summary>
        /// Get A New <see cref="Promise{T}"/> Already Marked Done & Has Resolved Value....
        /// </summary>
        public static Promise<T> Successful(T value)
        {
            return new Promise<T>
            {
                IsDone = true,
                _promiseValue = value
            };
        }

        /// <summary>
        /// Get A New <see cref="Promise{T}"/> Already Marked Done & Has Given Ex....
        /// </summary>
        public static Promise<T> Failed(Exception exception)
        {
            return new Promise<T>
            {
                IsDone = true,
                Exception = exception
            };
        }

        /// <inheritdoc />
        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation)
        {
            Then(_ => continuation());
            Error(_ => continuation());
        }

        /// <inheritdoc />
        void INotifyCompletion.OnCompleted(Action continuation)
            => ((ICriticalNotifyCompletion) this).UnsafeOnCompleted(continuation);

        /// <summary>
        /// Get Resolved Result From This Promise.....
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public T GetResult()
        {
            if (Exception != null) throw Exception;
            return _promiseValue;
        }

        /// <summary>
        /// Get Awaiter Over This Promise.....
        /// </summary>
        /// <returns></returns>
        public Promise<T> GetAwaiter() => this;
    }
}