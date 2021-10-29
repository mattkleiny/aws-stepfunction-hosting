using System;
using System.Linq;
using System.Text;
using Amazon.StepFunction.Hosting.Definition;

namespace Amazon.StepFunction.Hosting.Utilities
{
  public static class ExecutionExtensions
  {
    /// <summary>Converts a <see cref="IStepFunctionExecution"/> into a DOT graph format for use in visualization (e.g. during integration tests or so forth)</summary>
    public static string ToDotGraph(this IStepFunctionExecution execution)
    {
      // TODO: improve visualization of sub-graphs

      static string GetColorForStatus(bool isSuccessful) => isSuccessful ? "green" : "red";

      var builder = new StringBuilder();
      var historyByName = execution
        .CollectAllHistory()
        .ToDictionaryByLatest(_ => _.StepName, _ => _.ExecutedAt, StringComparer.OrdinalIgnoreCase);

      builder.AppendLine($"digraph \"Execution {execution.ExecutionId}\" {{");
      builder.AppendLine("\tgraph [compound=true];");
      builder.AppendLine("\tnode [style=filled];");

      void AppendStepsRecursively(StepFunctionDefinition definition, int indent = 1)
      {
        var tab = new string('\t', indent);

        foreach (var step in definition.Steps)
        {
          if (step.NestedBranches.Any())
          {
            foreach (var branch in step.NestedBranches)
            {
              builder.AppendLine($"{tab}subgraph \"{step.Name}\" {{");

              AppendStepsRecursively(branch, indent + 1);

              builder.AppendLine($"{tab}}}");
            }
          }
          else
          {
            if (historyByName.TryGetValue(step.Name, out var history))
            {
              builder.AppendLine($"{tab}\"{step.Name}\" [color={GetColorForStatus(history.IsSuccessful)}]");
            }
            else
            {
              builder.AppendLine($"{tab}\"{step.Name}\"");
            }
          }

          foreach (var connection in step.PossibleConnections)
          {
            if (!string.IsNullOrEmpty(connection))
            {
              builder.AppendLine($"{tab}\"{step.Name}\" -> \"{connection}\"");
            }
          }
        }

        builder.AppendLine($"{tab}color=black;");
      }

      AppendStepsRecursively(execution.Definition);

      builder.AppendLine("}");

      return builder.ToString();
    }

    /// <summary>Converts a <see cref="IStepFunctionExecution"/> into a DOT graph format and provides a link for it's visualization</summary>
    public static string ToDotGraphLink(this IStepFunctionExecution execution)
    {
      return $"https://dreampuf.github.io/GraphvizOnline/#{execution.ToDotGraph().Replace(Environment.NewLine, string.Empty)}";
    }
  }
}