using System;
using Newtonsoft.Json;

namespace Amazon.StepFunction.Definition
{
  /// <summary>Defines the metadata used to drive a step as defined by the StepFunction machine language</summary>
  public sealed class StepDefinition
  {
    /// <summary>Parses the <see cref="MachineDefinition"/> from the given json.</summary>
    public static StepDefinition Parse(string json)
    {
      Check.NotNullOrEmpty(json, nameof(json));

      return JsonConvert.DeserializeObject<StepDefinition>(json);
    }

    public string Name           { get; set; }
    public string Type           { get; set; }
    public string Resource       { get; set; }
    public string Next           { get; set; }
    public string Default        { get; set; }
    public int?   Seconds        { get; set; }
    public int?   TimeoutSeconds { get; set; }
    public bool?  End            { get; set; }

    /// <summary>Builds the <see cref="Step"/> from this definition.</summary>
    internal Step Create(StepHandlerFactory factory)
    {
      Check.NotNull(factory, nameof(factory));

      switch (Type.ToLower())
      {
        case "pass":
          return new Step.Pass
          {
            Name  = Name,
            IsEnd = End.GetValueOrDefault(false),
            Next  = Next ?? Default
          };

        case "task":
          return new Step.Invoke(() => factory(this))
          {
            Name    = Name,
            IsEnd   = End.GetValueOrDefault(false),
            Timeout = TimeSpan.FromSeconds(TimeoutSeconds.GetValueOrDefault(300)),
            Next    = Next ?? Default
          };

        case "choice":
          return new Step.Choice
          {
            Name    = Name,
            Default = Default
          };

        case "wait":
          return new Step.Wait
          {
            Name     = Name,
            IsEnd    = End.GetValueOrDefault(false),
            Duration = TimeSpan.FromSeconds(Seconds.GetValueOrDefault(5)),
            Next     = Next ?? Default
          };

        case "succeed":
          return new Step.Succeed
          {
            Name = Name
          };

        case "fail":
          return new Step.Fail
          {
            Name = Name
          };

        case "parallel":
          return new Step.Parallel
          {
            Name  = Name,
            IsEnd = End.GetValueOrDefault(false)
          };

        default:
          throw new ArgumentException($"An unrecognized step type was requested: {Type}");
      }
    }
  }
}