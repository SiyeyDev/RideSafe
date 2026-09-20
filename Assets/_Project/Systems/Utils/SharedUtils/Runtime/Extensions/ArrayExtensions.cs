using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// Provides extension methods for arrays.
/// </summary>
public static class ArrayExtensions
{
    /// <summary>
    /// Gets the index of a specified value in a string array.
    /// </summary>
    /// <param name="array">The string array to search.</param>
    /// <param name="value">The value to find in the array.</param>
    /// <returns>The index of the value in the array if found; otherwise, 0.</returns>
    public static int GetIndex(this string[] array, string value)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == value)
                return i;
        }
        return 0;
    }

    private const int HASHSET_THRESHOLD = 20;
    private static ConditionalWeakTable<Array, object> _cachedHashSets = new ConditionalWeakTable<Array, object>();
    /// <summary>
    /// Determines whether the specified value exists in the array with optimized performance.
    /// For small arrays (length ≤ 20), it uses a linear search.
    /// For larger arrays, it uses a cached HashSet for faster lookup.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="array">The array to search.</param>
    /// <param name="value">The value to locate in the array.</param>
    /// <returns>
    /// <c>true</c> if the value is found in the array; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// The method caches a HashSet internally for arrays larger than the threshold to improve lookup speed.
    /// This cache does not prevent garbage collection of the array.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // Hint to compiler for performance
    public static bool ContainsOptimized<T>(this T[] array, T value)
    {
        if (array == null)
            return false;
        if (array.Length <= HASHSET_THRESHOLD)
            return Array.IndexOf(array, value) != -1;
        return GetOrAddHashSet(array).Contains(value);
    }

    private static HashSet<T> GetOrAddHashSet<T>(T[] array)
    {
        if (_cachedHashSets.TryGetValue(array, out object cachedSet))
            return (HashSet<T>)cachedSet;
        HashSet<T> newSet = new HashSet<T>(array);
        _cachedHashSets.Add(array, newSet);
        return newSet;
    }
}
