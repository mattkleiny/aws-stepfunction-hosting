using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>
  /// An opaque wrapper for the data that passes through a Step Function during it's execution.
  /// <para/>
  /// This type supports coercion into a different forms, JPath queries, and input/output transformation.
  /// It also supports dynamic type coercion through the `dynamic` keyword, which allows runtime dispatch
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
        StepFunctionData data                  => data.value, // don't nest StepFunctionData types
        StepFunctionData[] array               => new JArray(array.Where(_ => _.value != null).Select(_ => _.value)),
        bool raw                               => new JValue(raw),
        string raw                             => new JValue(raw),
        int raw                                => new JValue(raw),
        float raw                              => new JValue(raw),
        TimeSpan raw                           => new JValue(raw),
        DateTime raw                           => new JValue(raw),
        JToken token                           => token,
        IEnumerable<StepFunctionData> sequence => new JArray(sequence.Select(_ => _.value).Cast<object>().ToArray()),
        IEnumerable sequence                   => new JArray(sequence.Cast<object>().ToArray()),
        _ when value != null                   => JObject.FromObject(value),
        _                                      => null
      };
    }

    public bool IsNull    => value == null;
    public bool IsPresent => value != null;

    /// <summary>Casts the data to the given data type.</summary>
    public T? Cast<T>()
    {
      if (value != null)
      {
        return value.ToObject<T>();
      }

      return default;
    }

    /// <summary>Casts the data at the given path to the given data type.</summary>
    public T? CastPath<T>(string jpath)
    {
      return Query(jpath).Cast<T>();
    }

    /// <summary>Casts the data to the given data type.</summary>
    public object? Cast(Type type)
    {
      return value?.ToObject(type);
    }

    /// <summary>Casts the data at the given path to the given data type.</summary>
    public object? CastPath(Type type, string jpath)
    {
      return Query(jpath).Cast(type);
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
    public StepFunctionData Transform(string jpath, StepFunctionData template, StepFunctionData context)
    {
      if (value != null && template.IsPresent)
      {
        var output = RecursivelyExpand(value, template.Cast<JToken>()!.DeepClone(), context);

        switch (value.SelectToken(jpath))
        {
          case JValue { Root: JObject root, Path: var path }:
            root.Property(path).Value = output;
            break;

          case JObject { Root: JObject root, Path: var path } when !string.IsNullOrEmpty(path):
            root.Property(path).Value = output;
            break;

          case null when value is JObject result:
          {
            // TODO: make this work at arbitrary nesting levels? (assuming that's how AWS works)
            // couldn't find the property in the source, so lets try and add it ourselves
            var copy = (JObject) result.DeepClone();
            copy.Add(jpath.Replace("$.", string.Empty), output);

            return new StepFunctionData(copy);
          }
        }

        return new StepFunctionData(output);
      }

      return this;

      // expands the JSON template into a form that can be returned as output
      [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
      static JToken RecursivelyExpand(JToken? input, JToken template, StepFunctionData context, int depth = 0, int maxDepth = 16)
      {
        static void Rename(JToken? token, string newName)
        {
          var parent = token?.Parent;
          if (parent == null)
            throw new InvalidOperationException("The parent is missing.");
          var newToken = new JProperty(newName, token);
          parent.Replace(newToken);
        }

        if (depth >= maxDepth)
        {
          throw new InvalidOperationException("Recursive expansion has exceeded max depth");
        }

        if (template is JObject rawObject)
        {
          foreach (var property in rawObject.Properties().ToArray())
          {
            RecursivelyExpand(input, property, context, ++depth, maxDepth);
          }
        }
        else if (template is JProperty rawProperty)
        {
          switch (rawProperty.Value)
          {
            case JValue { Type: JTokenType.String, Value: string query } when query.Contains("$$"):
            {
              rawProperty.Value = context.Query(query.Replace("$$", "$")).value;
              Rename(rawProperty.Value, rawProperty.Name.Replace(".$", string.Empty));
              break;
            }
            case JValue { Type: JTokenType.String, Value: string query } when query.Contains('$'):
            {
              rawProperty.Value = input?.SelectToken(query);
              Rename(rawProperty.Value, rawProperty.Name.Replace(".$", string.Empty));
              break;
            }
            case JArray subArray:
            {
              foreach (var element in subArray)
              {
                RecursivelyExpand(input, element, context, ++depth);
              }

              break;
            }
            case JObject subObject:
            {
              RecursivelyExpand(input, subObject, context, ++depth);

              break;
            }
          }
        }

        return template;
      }
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