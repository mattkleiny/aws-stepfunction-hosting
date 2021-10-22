using System;
using System.Linq;
using System.Net;
using System.Text;

namespace Amazon.StepFunction.Hosting.Utilities
{
  public static class ExecutionExtensions
  {
    /// <summary>Converts a <see cref="IStepFunctionExecution"/> into a DOT graph format for use in visualization (e.g. during integration tests or so forth)</summary>
    public static string ToDotGraph(this IStepFunctionExecution execution)
    {
      static string GetColorForStatus(bool isSuccessful) => isSuccessful ? "green" : "red";

      var builder       = new StringBuilder();
      var historyByName = execution.History.ToDictionary(_ => _.StepName, StringComparer.OrdinalIgnoreCase);

      builder.AppendLine($"digraph \"{execution.ExecutionId}\" {{");

      foreach (var step in execution.Definition.Steps)
      {
        if (historyByName.TryGetValue(step.Name, out var history))
        {
          builder.AppendLine($"\t\"{step.Name}\" [color={GetColorForStatus(history.IsSuccessful)}]");
        }
        else
        {
          builder.AppendLine($"\t\"{step.Name}\"");
        }

        foreach (var connection in step.PotentialConnections)
        {
          if (!string.IsNullOrEmpty(connection))
          {
            builder.AppendLine($"\t\"{step.Name}\" -> \"{connection}\"");
          }
        }
      }

      builder.AppendLine("}");

      return builder.ToString();
    }

    /// <summary>Converts a <see cref="IStepFunctionExecution"/> into a DOT graph format and provides a link for it's visualization</summary>
    public static string ToDotGraphLink(this IStepFunctionExecution execution)
    {
      var dotGraph     = execution.ToDotGraph();
      var encodedGraph = WebUtility.UrlDecode(dotGraph);

      return $"https://dreampuf.github.io/GraphvizOnline/#{encodedGraph.ReplaceLineEndings(string.Empty)}";
    }
  }
}