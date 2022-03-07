using System;

namespace Skipper.Promise.Runtime
{
    public static class Extensions
    {
        /// <summary>
        /// <see cref="Action"/>'s Extensions, Used To Invoke A Safe Dispatch With Null-Check...
        /// </summary>
        /// <param name="action">this <see cref="Action{TValue}"/></param>
        /// <param name="value"><see cref="TValue"/>'s Object...</param>
        /// <typeparam name="TValue"></typeparam>
        public static void Dispatch<TValue>(this Action<TValue> action, TValue value) => action?.Invoke(value);
    }
}