using System.Collections.Generic;

namespace Amazon.StepFunction.Hosting.Visualizer.Layouts
{
  internal sealed class LayoutNode<T>
  {
    public LayoutNode(T item, LayoutNode<T>? parent = default)
    {
      Item   = item;
      Parent = parent;
    }

    public float X { get; set; }
    public int   Y { get; set; }

    public float Width  { get; set; }
    public int   Height { get; set; }

    public float Mod { get; set; }

    public LayoutNode<T>?      Parent   { get; set; }
    public List<LayoutNode<T>> Children { get; init; } = new();
    public T                   Item     { get; set; }

    public bool IsLeaf()
    {
      return Children.Count == 0;
    }

    public bool IsLeftMost()
    {
      if (Parent == null)
        return true;

      return Parent.Children[0] == this;
    }

    public bool IsRightMost()
    {
      if (Parent == null)
        return true;

      return Parent.Children[^1] == this;
    }

    public LayoutNode<T>? GetPreviousSibling()
    {
      if (Parent == null || IsLeftMost())
        return null;

      return Parent.Children[Parent.Children.IndexOf(this) - 1];
    }

    public LayoutNode<T>? GetNextSibling()
    {
      if (Parent == null || IsRightMost())
        return null;

      return Parent.Children[Parent.Children.IndexOf(this) + 1];
    }

    public LayoutNode<T>? GetLeftMostSibling()
    {
      if (Parent == null)
        return null;

      if (IsLeftMost())
        return this;

      return Parent.Children[0];
    }

    public LayoutNode<T>? GetLeftMostChild()
    {
      if (Children.Count == 0)
        return null;

      return Children[0];
    }

    public LayoutNode<T>? GetRightMostChild()
    {
      if (Children.Count == 0)
        return null;

      return Children[^1];
    }
  }
}