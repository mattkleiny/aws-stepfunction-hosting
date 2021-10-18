using System;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq.Expressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>
  /// An opaque wrapper for the data that passes through a Step Function during it's execution.
  /// <para/>
  /// This type supports coercion into a different forms, JPath queries, and input/output transformation.
  /// It  also supports dynamic type coercion through the `dynamic` keyword, which allows runtime dispatch
  /// for varying implementation types.
  /// </summary>
  public readonly struct StepFunctionData : IEquatable<StepFunctionData>, IDynamicMetaObjectProvider
  {
    public static StepFunctionData Empty => default;

    private readonly JToken? value;

    public StepFunctionData(object? value)
    {
      this.value = value switch
      {
        StepFunctionData data => data.value, // don't nest StepFunctionData types
        bool raw              => new JValue(raw),
        string raw            => new JValue(raw),
        int raw               => new JValue(raw),
        float raw             => new JValue(raw),
        TimeSpan raw          => new JValue(raw),
        DateTime raw          => new JValue(raw),
        JToken token          => token,
        _ when value != null  => JObject.FromObject(value),
        _                     => null
      };
    }

    /// <summary>Casts the data to the given data type.</summary>
    public T? Cast<T>()
    {
      if (value != null)
      {
        return value.ToObject<T>();
      }

      return default;
    }

    /// <summary>Casts the data to the given data type.</summary>
    public object? Cast(Type type)
    {
      return value?.ToObject(type);
    }

    /// <summary>Queries for sub-data on the object at the given JPath position.</summary>
    public StepFunctionData Query(string jpath)
    {
      if (value != null && !string.IsNullOrEmpty(jpath))
      {
        return new StepFunctionData(value.SelectToken(jpath));
      }

      return this;
    }

    /// <summary>Recursively transforms the JSON structure at the given path in the data to match the given shape.</summary>
    public StepFunctionData Transform(string jpath, string shape, object? context = default)
    {
      // TODO: give this a once-over

      [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
      static void RecursivelyExpand(JToken root, JToken? value, JToken? context, int depth = 0, int maxDepth = 4)
      {
        if (depth >= maxDepth)
        {
          throw new Exception("Recursive expansion has exceeded max depth");
        }

        if (root is JObject rawObject)
        {
          foreach (var property in rawObject.Properties())
          {
            RecursivelyExpand(property, value, context, ++depth, maxDepth);
          }
        }
        else if (root is JProperty rawProperty)
        {
          switch (rawProperty.Value)
          {
            case JValue { Type: JTokenType.String, Value: string query } when query.Contains("$$"):
            {
              rawProperty.Value = context?.SelectToken(query.Replace("$$", "$"));
              break;
            }
            case JValue { Type: JTokenType.String, Value: string query } when query.Contains("$"):
            {
              rawProperty.Value = value?.SelectToken(query);
              break;
            }
            case JArray subArray:
            {
              foreach (var element in subArray)
              {
                RecursivelyExpand(element, value, context, ++depth);
              }

              break;
            }
            case JObject subObject:
            {
              RecursivelyExpand(subObject, value, context, ++depth);

              break;
            }
          }
        }
      }

      if (value != null && !string.IsNullOrEmpty(shape))
      {
        var result     = value.DeepClone();
        var rawShape   = JToken.Parse(shape);
        var rawContext = JToken.FromObject(context);

        RecursivelyExpand(rawShape, value, rawContext);

        var rawTarget = result.SelectToken(jpath);
        if (rawTarget is JValue { Root: JObject root, Path: var path })
        {
          root.Property(path).Value = rawShape;
        }

        return new StepFunctionData(result);
      }

      return this;
    }

    public override string ToString()         => value?.ToString(Formatting.None) ?? "null";
    public          string ToIndentedString() => value?.ToString(Formatting.Indented) ?? "null";

    public          bool Equals(StepFunctionData other) => JToken.DeepEquals(value, other.value);
    public override bool Equals(object? obj)            => obj is StepFunctionData other && Equals(other);

    public override int GetHashCode() => throw new NotSupportedException();

    public static bool operator ==(StepFunctionData left, StepFunctionData right) => left.Equals(right);
    public static bool operator !=(StepFunctionData left, StepFunctionData right) => !left.Equals(right);

    DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
    {
      if (value is IDynamicMetaObjectProvider provider)
      {
        return provider.GetMetaObject(parameter);
      }

      return null!;
    }
  }
}