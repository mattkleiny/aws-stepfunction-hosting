using System.Collections.Generic;

namespace Amazon.StepFunction.Hosting.Utilities
{
  internal static class DictionaryExtensions
  {
    /// <summary>Try and retrieve a value from the dictionary, removing it if successfully located and returning a default value if not.</summary>
    public static TValue TryPopValueOrDefault<TKey, TValue>(
      this IDictionary<TKey, TValue> dictionary,
      TKey key,
      TValue? defaultValue = default)
    {
      if (dictionary.TryGetValue(key, out var value))
      {
        dictionary.Remove(key);

        return value;
      }

      return defaultValue!;
    }
  }
}