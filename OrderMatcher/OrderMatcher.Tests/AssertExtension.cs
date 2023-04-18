using System.Collections.Generic;

namespace OrderMatcher.Tests;

public static class AssertHelper
{
    public static void SequentiallyEqual<T>(IEnumerable<T> collection1, IEnumerable<T> collection2)
    {
        if (collection1 == null && collection2 == null)
        {
            return;
        }

        T[] arr1 = collection1.ToArray();
        T[] arr2 = collection2.ToArray();
        Assert.Equal(arr1.Length, arr2.Length);

        for (int i = 0; i < arr1.Length; i++)
        {
            Assert.StrictEqual(arr1[i], arr2[i]);
        }
    }
}
