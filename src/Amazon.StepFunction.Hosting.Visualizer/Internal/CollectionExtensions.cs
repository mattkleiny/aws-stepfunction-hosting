using System;
using System.Collections.Generic;
using System.Linq;

namespace Amazon.StepFunction.Hosting.Visualizer.Internal
{
  public static class CollectionExtensions
  {
    /// <summary>Builds a dictionary by taking the most recent <see cref="TValue"/> from a particular sequence</summary>
    public static Dictionary<TKey, TValue> ToDictionaryByLatest<TKey, TValue>(
      this IEnumerable<TValue> values,
      Func<TValue, TKey> keySelector,
      Func<TValue, DateTime> dateSelector,
      IEqualityComparer<TKey> keyComparer)
      where TKey : notnull
    {
      var results = new Dictionary<TKey, TValue>(keyComparer);

      foreach (var value in values.OrderByDescending(dateSelector))
      {
        var key = keySelector(value);

        results.TryAdd(key, value);
      }

      return results;
    }
  }
}