using System;
using Skipper.Promise.Runtime;
using UnityEngine;

namespace Skipper.Promise.Demo
{
    public class MonoPromiseDemo : MonoBehaviour
    {
        private void Start()
        {
            DoSomethingAsync()
                .Then(result => { Debug.Log("--> Promise Result: " + result); })
                .Error(exception => throw exception);
        }

        private Promise<bool> DoSomethingAsync()
        {
            var promise = new Promise<bool>();

            // 0: heavy operation or anything is awaiting the execution here , after done
            promise.CompleteSuccess(true);

            return promise;
        }
    }
}