using System;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction.Hosting.Utilities
{
  internal static class JsonExtensions
  {
    /// <summary>Attempts to rename a token to some other name.</summary>
    public static void Rename(this JToken? token, string newName)
    {
      var parent = token?.Parent;
      if (parent == null)
      {
        throw new InvalidOperationException($"Unable to rename to {newName} as parent is missing.");
      }

      parent.Replace(new JProperty(newName, token));
    }
  }
}