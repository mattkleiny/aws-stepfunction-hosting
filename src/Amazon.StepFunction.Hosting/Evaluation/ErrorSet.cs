using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>A set of error type names for use in error handling.</summary>
  internal sealed class ErrorSet
  {
    private const string CatchAll = "States.ALL";

    public static ErrorSet FromTypes(params Type[] errorTypes) => new(errorTypes.Select(_ => _.FullName!));

    private readonly ImmutableHashSet<string> errorTypes;

    public ErrorSet(IEnumerable<string> errorTypes)
    {
      this.errorTypes = errorTypes.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public bool Contains(Exception exception)
    {
      return errorTypes.Contains(exception.GetType().FullName!) || errorTypes.Contains(CatchAll);
    }

    public override string ToString()
    {
      return $"ErrorSet({string.Join(", ", errorTypes)})";
    }
  }
}