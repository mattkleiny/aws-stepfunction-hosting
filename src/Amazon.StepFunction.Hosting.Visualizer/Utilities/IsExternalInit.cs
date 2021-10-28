using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
// ReSharper disable once UnusedMember.Global

namespace System.Runtime.CompilerServices
{
  // HACK: Allow record types in older versions of the .NET runtime  
  [SuppressMessage("ReSharper", "UnusedType.Global")]
  internal static class IsExternalInit
  {
  }
}