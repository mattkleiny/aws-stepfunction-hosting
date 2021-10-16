using System;
using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting.Example
{
  public static class StepHandlers
  {
    public static StepHandlerFactory Factory { get; } = resource =>
    {
      static StepHandler CreateHandler<T>(Func<T, T> factory)
        where T : notnull => (data, cancellationToken) =>
      {
        cancellationToken.ThrowIfCancellationRequested();

        var input  = data.Cast<T>();
        var result = factory(input!);

        return Task.FromResult(new StepFunctionData(result));
      };

      return resource.ToLower() switch
      {
        "format-message"     => CreateHandler<string>(Format),
        "capitalize-message" => CreateHandler<string>(Capitalize),
        "print-message"      => CreateHandler<string>(Print),

        _ => throw new Exception($"An unrecognized resource was requested: {resource}")
      };
    };

    public static string Format(string input)
    {
      return $"Hello, {input}!";
    }

    public static string Capitalize(string input)
    {
      return input.ToUpper();
    }

    public static string Print(string input)
    {
      Console.WriteLine(input);

      return input;
    }
  }
}