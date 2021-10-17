using System.Collections.Generic;

namespace Amazon.StepFunction.Hosting.Visualizer.Layouts
{
  /// <summary>A node that can be positioned in 2-space using a layout algorithm</summary>
  internal sealed class LayoutNode<T>
  {
    public LayoutNode(T item, LayoutNode<T>? parent = default)
    {
      Item   = item;
      Parent = parent;
    }

    public float               X        { get; set; }
    public int                 Y        { get; set; }
    public float               Width    { get; set; }
    public int                 Height   { get; set; }
    public float               Mod      { get; set; }
    public LayoutNode<T>?      Parent   { get; set; }
    public List<LayoutNode<T>> Children { get; init; } = new();
    public T                   Item     { get; set; }

    public bool IsLeaf => Children.Count == 0;

    public bool IsLeftMost
    {
      get
      {
        if (Parent == null)
          return true;

        return Parent.Children[0] == this;
      }
    }

    public bool IsRightMost
    {
      get
      {
        if (Parent == null)
          return true;

        return Parent.Children[^1] == this;
      }
    }

    public LayoutNode<T>? PreviousSibling
    {
      get
      {
        if (Parent == null || IsLeftMost)
          return null;

        return Parent.Children[Parent.Children.IndexOf(this) - 1];
      }
    }

    public LayoutNode<T>? NextSibling
    {
      get
      {
        if (Parent == null || IsRightMost)
          return null;

        return Parent.Children[Parent.Children.IndexOf(this) + 1];
      }
    }

    public LayoutNode<T>? LeftMostSibling
    {
      get
      {
        if (Parent == null)
          return null;

        if (IsLeftMost)
          return this;

        return Parent.Children[0];
      }
    }

    public LayoutNode<T>? LeftMostChild
    {
      get
      {
        if (Children.Count == 0)
          return null;

        return Children[0];
      }
    }

    public LayoutNode<T>? RightMostChild
    {
      get
      {
        if (Children.Count == 0)
          return null;

        return Children[^1];
      }
    }
  }
}