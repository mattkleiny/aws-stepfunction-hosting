using System;
using System.Collections.Generic;

namespace Amazon.StepFunction.Hosting.Example
{
  public sealed class ExampleContext
  {
    public int        IterationCount { get; set; } = 0;
    public bool       IsWaiting      { get; set; } = false;
    public List<Guid> PayeeIds       { get; set; } = new();
  }
}