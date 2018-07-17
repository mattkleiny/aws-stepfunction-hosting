using System.IO;
using System.Reflection;

namespace Amazon.StepFunction.Runtime.Tests
{
  internal static class EmbeddedResources
  {
    public static string SimpleSpecification  => ReadResourceAsString("simple-spec.json");
    public static string ComplexSpecification => ReadResourceAsString("complex-spec.json");

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