using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>A set of error type names for use in error handling.</summary>
  internal sealed class ErrorSet
  {
    private const string CatchAll   = "States.ALL";
    private const string TaskFailed = "States.TaskFailed";

    private readonly ImmutableHashSet<string> errorTypes;

    public static ErrorSet FromTypes(params Type[] errorTypes)
    {
      return new ErrorSet(errorTypes.Select(_ => _.Name));
    }

    public ErrorSet(IEnumerable<string> errorTypes)
    {
      this.errorTypes = errorTypes.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public bool Contains(Exception exception)
    {
      return errorTypes.Contains(exception.GetType().Name) ||
             errorTypes.Contains(CatchAll) ||
             errorTypes.Contains(TaskFailed);
    }

    public override string ToString()
    {
      return $"ErrorSet({string.Join(", ", errorTypes)})";
    }
  }
}