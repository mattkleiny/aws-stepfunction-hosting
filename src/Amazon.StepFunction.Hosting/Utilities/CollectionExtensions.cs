using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting.Utilities
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

    /// <summary>Try and retrieve a value from the dictionary, removing it if successfully located and returning a default value if not.</summary>
    public static TValue TryPopValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue? defaultValue = default)
      where TKey : notnull
    {
      if (dictionary.TryGetValue(key, out var value))
      {
        dictionary.Remove(key);

        return value;
      }

      return defaultValue!;
    }

    // TODO: replace this with Parallel.ForEachAsync in .NET 6+
    internal static Task ForEachAsync<T>(this IEnumerable<T> sequence, Func<T, Task> body, CancellationToken cancellationToken = default)
    {
      return ForEachAsync(sequence, body, Environment.ProcessorCount, cancellationToken);
    }

    internal static Task ForEachAsync<T>(this IEnumerable<T> sequence, Func<T, Task> body, int partitionCount, CancellationToken cancellationToken = default)
    {
      return Task.WhenAll(
        from partition in Partitioner.Create(sequence).GetPartitions(partitionCount)
        select Task.Run(async () =>
        {
          using (partition)
          {
            while (partition.MoveNext())
            {
              await body(partition.Current);
            }
          }
        }, cancellationToken)
      );
    }
  }
}