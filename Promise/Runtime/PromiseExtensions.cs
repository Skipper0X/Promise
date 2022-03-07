using System;

namespace Skipper.Promise.Runtime
{
    public static class PromiseExtensions
    {
        public static Promise<T> Recover<T>(this Promise<T> promise, Func<Exception, T> callback)
        {
            var result = new Promise<T>();
            promise.Then(value => result.CompleteSuccess(value))
                .Error(err => result.CompleteSuccess(callback(err)));
            return result;
        }

        public static Promise<T> RecoverWith<T>(this Promise<T> promise, Func<Exception, Promise<T>> callback)
        {
            var result = new Promise<T>();
            promise.Then(value => result.CompleteSuccess(value)).Error(err =>
            {
                try
                {
                    var nextPromise = callback(err);
                    nextPromise.Then(value => result.CompleteSuccess(value)).Error(errInner =>
                    {
                        result.CompleteError(errInner);
                    });
                }
                catch (Exception ex)
                {
                    result.CompleteError(ex);
                }
            });
            return result;
        }

        public static Promise<Unit> ToPromise(this System.Threading.Tasks.Task task)
        {
            var promise = new Promise<Unit>();

            async void Helper()
            {
                try
                {
                    await task;
                    promise.CompleteSuccess(PromiseCore.Unit);
                }
                catch (Exception ex)
                {
                    promise.CompleteError(ex);
                }
            }

            Helper();

            return promise;
        }

        public static Promise<T> ToPromise<T>(this System.Threading.Tasks.Task<T> task)
        {
            var promise = new Promise<T>();

            async void Helper()
            {
                try
                {
                    var result = await task;
                    promise.CompleteSuccess(result);
                }
                catch (Exception ex)
                {
                    promise.CompleteError(ex);
                }
            }

            Helper();

            return promise;
        }

        public static Promise<Unit> ToUnit<T>(this Promise<T> self)
        {
            return self.Map(_ => PromiseCore.Unit);
        }
    }
}