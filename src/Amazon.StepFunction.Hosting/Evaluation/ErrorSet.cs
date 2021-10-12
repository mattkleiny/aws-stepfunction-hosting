﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>A set of error type names for use in error handling.</summary>
  internal sealed class ErrorSet
  {
    private readonly ImmutableHashSet<string> errorTypes;

    public ErrorSet(params Type[] errorTypes)
      : this(errorTypes.Select(_ => _.FullName!))
    {
    }

    public ErrorSet(IEnumerable<string> errorTypes)
    {
      this.errorTypes = errorTypes.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public bool CanHandle(Exception exception)
    {
      return errorTypes.Contains(exception.GetType().FullName!);
    }

    public override string ToString()
    {
      return $"ErrorSet({string.Join(", ", errorTypes)})";
    }
  }
}