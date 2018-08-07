using System.IO;
using System.Reflection;

namespace Amazon.StepFunction.Hosting.Example
{
  internal static class EmbeddedResources
  {
    public static string ExampleMachine => ReadResourceAsString("example-machine.json");

    private static string ReadResourceAsString(string name)
    {
      var assembly = Assembly.GetExecutingAssembly();
      var type     = typeof(EmbeddedResources);

      using (var stream = assembly.GetManifestResourceStream(type, $"Resources.{name}"))
      using (var reader = new StreamReader(stream))
      {
        return reader.ReadToEnd();
      }
    }
  }
}