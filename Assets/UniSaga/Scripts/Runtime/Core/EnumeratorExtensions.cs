// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections;
using UnityEngine;

namespace UniSaga.Core
{
    internal static class EnumeratorExtensions
    {
        public static IEnumerator Flat(this IEnumerator self)
        {
            while (self.MoveNext())
            {
                var current = self.Current;
                switch (current)
                {
                    case null:
                    {
                        yield return null;
                        break;
                    }
                    case CustomYieldInstruction cyi:
                    {
                        while (cyi.keepWaiting) yield return null;
                        break;
                    }
                    case YieldInstruction yieldInstruction:
                    {
                        switch (yieldInstruction)
                        {
                            case AsyncOperation ao:
                            {
                                var ne = WaitAsyncOperation(ao);
                                while (ne.MoveNext()) yield return null;
                                break;
                            }
                            case WaitForSeconds wfs:
                            {
                                var ne = WaitWaitForSeconds(wfs);
                                while (ne.MoveNext()) yield return null;
                                break;
                            }
                            default:
                            {
                                throw new NotSupportedException(
                                    $"{yieldInstruction.GetType().Name} is not supported."
                                );
                            }
                        }

                        break;
                    }
                    case IEnumerator e:
                    {
                        var ne = e.Flat();
                        while (ne.MoveNext()) yield return null;
                        break;
                    }
                    default:
                    {
                        yield return current;
                        break;
                    }
                }
            }
        }


        private static IEnumerator WaitAsyncOperation(AsyncOperation asyncOperation)
        {
            while (!asyncOperation.isDone)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitWaitForSeconds(WaitForSeconds waitForSeconds)
        {
            var second = waitForSeconds.GetSeconds();
            var elapsed = 0.0f;
            while (true)
            {
                yield return null;

                elapsed += Time.deltaTime;
                if (elapsed >= second)
                {
                    break;
                }
            }
        }
    }
}