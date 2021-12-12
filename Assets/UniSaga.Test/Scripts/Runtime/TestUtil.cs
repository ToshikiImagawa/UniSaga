// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections;
using UniSaga.Core;

namespace UniSaga.Test
{
    public static class TestUtil
    {
        public static void TestRun(this IEnumerator self, int maxRunningCount = 2000)
        {
            var flat = self.Flat();
            var count = 0;
            while (flat.MoveNext())
            {
                count++;
                if (count > maxRunningCount)
                {
                    throw new Exception("無限リストになっています");
                }
            }
        }
    }

    public static class Enumerator
    {
        public static readonly IEnumerator Empty = _Empty();

        private static IEnumerator _Empty()
        {
            yield break;
        }
    }
}