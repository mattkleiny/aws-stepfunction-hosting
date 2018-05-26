using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

// ReSharper disable CheckNamespace

/// <summary>Static utilities for precondition checking.</summary>
internal static class Check
{
  /// <summary>Asserts that the given <see cref="reference" /> is not null, and generates an exception if it is.</summary>
  /// <exception cref="ArgumentNullException">If <see cref="ArgumentNullException" /> is null.</exception>
  [Conditional("DEBUG"), MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void NotNull<T>([NoEnumeration] T reference, [InvokerParameterName] string name)
    where T : class
  {
    if (reference == null)
    {
      throw new ArgumentNullException(name);
    }
  }

  /// <summary>Asserts that the given string <see cref="value" /> is not <see cref="string.IsNullOrEmpty" />, and generates an exception if it is.</summary>
  /// <exception cref="ArgumentNullException">If <see cref="value" /> is null or empty.</exception>
  [Conditional("DEBUG"), MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void NotNullOrEmpty(string value, [InvokerParameterName] string name)
  {
    if (string.IsNullOrEmpty(value))
    {
      throw new ArgumentNullException(name);
    }
  }

  /// <summary>Asserts that the given <see cref="condition" /> is true, and generates an exception if it is not.</summary>
  /// <exception cref="ArgumentException">If <see cref="condition" /> is false.</exception>
  [Conditional("DEBUG"), MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void That(bool condition, string message)
  {
    if (!condition)
    {
      throw new ArgumentException(message);
    }
  }
}