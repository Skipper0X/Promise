using System;
using System.Collections.Generic;
using System.Linq;

namespace Skipper.Promise.Runtime
{
    /// <summary>
    /// Utility Class For Promise Core Operations.....i.e: Sequences, Batching, Rolling etc...
    /// </summary>
    public static class PromiseUtility
    {
        public static SequencePromise<T> ObservableSequence<T>(IList<Promise<T>> promises)
        {
            var result = new SequencePromise<T>(promises.Count);

            if (promises.Count == 0)
            {
                result.CompleteSuccess();
                return result;
            }

            for (var i = 0; i < promises.Count; i++)
            {
                var index = i;
                promises[i].Then(reply =>
                {
                    result.ReportEntrySuccess(new SequenceEntrySuccess<T>(index, reply));

                    if (result.Total == promises.Count)
                    {
                        result.CompleteSuccess();
                    }
                }).Error(err =>
                {
                    result.ReportEntryError(new SequenceEntryException(index, err));
                    result.CompleteError(err);
                });
            }

            return result;
        }

        public static Promise<List<T>> Sequence<T>(IList<Promise<T>> promises)
        {
            var result = new Promise<List<T>>();
            var replies = new List<T>();

            if (promises == null || promises.Count == 0)
            {
                result.CompleteSuccess(replies);
                return result;
            }

            for (var i = 0; i < promises.Count; i++)
            {
                promises[i].Then(reply =>
                {
                    replies.Add(reply);
                    if (replies.Count == promises.Count)
                    {
                        result.CompleteSuccess(replies);
                    }
                }).Error(err => result.CompleteError(err));
            }

            return result;
        }

        public static Promise<List<T>> Sequence<T>(params Promise<T>[] promises)
        {
            return Sequence((IList<Promise<T>>) promises);
        }

        /// <summary>
        /// Given a list of promise generator functions, process the whole list, but serially.
        /// Only one promise will be active at any given moment.
        /// </summary>
        /// <param name="generators"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>A single promise of Unit to represent the completion of the processing. Any other side effects need to be handled separately</returns>
        public static Promise<Unit> ExecuteSerially<T>(List<Func<Promise<T>>> generators)
        {
            if (generators.Count == 0)
            {
                return Promise<Unit>.Successful(PromiseCore.Unit);
            }
            else
            {
                var first = generators[0];
                var rest = generators.GetRange(1, generators.Count - 1);
                var promise = first();
                return promise.FlatMap(_ => ExecuteSerially(rest));
            }
        }

        /// <summary>
        /// Given a list of promise generator functions, process the list, but in a rolling fasion.
        /// At any given moment, the highest number of promises running will equal maxProcessSize. As soon a promise finishes, a new promise may start.
        /// </summary>
        /// <param name="maxProcessSize"></param>
        /// <param name="generators"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static SequencePromise<T> ExecuteRolling<T>(int maxProcessSize, List<Func<Promise<T>>> generators)
        {
            var current = 0;
            var running = 0;

            var completePromise = new SequencePromise<T>(generators.Count);

            void ProcessUpToLimit()
            {
                while (running < maxProcessSize && current < generators.Count)
                {
                    var index = current;
                    var generator = generators[index];

                    System.Threading.Interlocked.Increment(ref current);
                    System.Threading.Interlocked.Increment(ref running);
                    var promise = generator();
                    promise.Then(result =>
                    {
                        completePromise.ReportEntrySuccess(index, result);
                        System.Threading.Interlocked.Decrement(ref running);
                        ProcessUpToLimit();
                    });
                    promise.Error(err =>
                    {
                        completePromise.ReportEntryError(index, err);
                        System.Threading.Interlocked.Decrement(ref running);
                        ProcessUpToLimit();
                    });
                }
            }

            ProcessUpToLimit();
            return completePromise;
        }

        /// <summary>
        /// Given a list of promise generator functions, process the list, but in batches of some size.
        /// The batches themselves will run one at a time. Every promise in the current batch must finish before the next batch can start.
        /// </summary>
        /// <param name="maxBatchSize"></param>
        /// <param name="generators"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>A single promise of Unit to represent the completion of the processing. Any other side effects need to be handled separately</returns>
        public static Promise<Unit> ExecuteInBatch<T>(int maxBatchSize, List<Func<Promise<T>>> generators)
        {
            var batches = new List<List<Func<Promise<T>>>>();

            // create batches...
            for (var i = 0; i < generators.Count; i += maxBatchSize)
            {
                var start = i;
                var minBatchSize = generators.Count - start;
                var count = minBatchSize < maxBatchSize ? minBatchSize : maxBatchSize; // min()
                var batch = generators.GetRange(start, count);
                batches.Add(batch);
            }

            Promise<List<T>> ProcessBatch(List<Func<Promise<T>>> batch)
            {
                // start all generators in batch...
                return PromiseUtility.Sequence(batch.Select(generator => generator()).ToList());
            }

            // run each batch, serially...
            var batchRunners = batches.Select(batch => new Func<Promise<List<T>>>(() => ProcessBatch(batch))).ToList();

            return ExecuteSerially(batchRunners);
        }
    }
}