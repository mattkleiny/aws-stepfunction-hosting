﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Amazon.StepFunction.Hosting.Visualizer.Layouts
{
  /// <summary>Reingold Tilford is a common graph layout algorithm that distributes nodes over one axis whilst stepping down the other.</summary>
  /// <remarks>This is not an ideal implementation, but it'll do for now</remarks>
  internal static class ReingoldTilfordLayout
  {
    public const int NodeSize = 150;

    private const float SiblingDistance = 150.0F;
    private const float TreeDistance    = 150.0F;

    public static void CalculateNodePositions<T>(LayoutNode<T> rootNode)
    {
      InitializeNodes(rootNode, 0);
      CalculateInitialX(rootNode);
      CheckAllChildrenOnScreen(rootNode);
      CalculateFinalPositions(rootNode, 0);
    }

    // recursively initialize x, y, and mod values of nodes
    private static void InitializeNodes<T>(LayoutNode<T> node, int depth)
    {
      node.X   = -1;
      node.Y   = depth;
      node.Mod = 0;

      foreach (var child in node.Children)
      {
        InitializeNodes(child, depth + 1);
      }
    }

    private static void CalculateFinalPositions<T>(LayoutNode<T> node, float modSum)
    {
      node.X += modSum;
      modSum += node.Mod;

      foreach (var child in node.Children)
      {
        CalculateFinalPositions(child, modSum);
      }

      if (node.Children.Count == 0)
      {
        node.Width  = node.X;
        node.Height = node.Y;
      }
      else
      {
        node.Width  = node.Children.OrderByDescending(p => p.Width).First().Width;
        node.Height = node.Children.OrderByDescending(p => p.Height).First().Height;
      }
    }

    private static void CalculateInitialX<T>(LayoutNode<T> node)
    {
      foreach (var child in node.Children)
      {
        CalculateInitialX(child);
      }

      // if no children
      if (node.IsLeaf)
      {
        // if there is a previous sibling in this set, set X to previous sibling + designated distance
        if (!node.IsLeftMost)
        {
          node.X = node.PreviousSibling!.X + NodeSize + SiblingDistance;
        }
        else
        {
          // if this is the first node in a set, set X to 0
          node.X = 0;
        }
      }
      // if there is only one child
      else if (node.Children.Count == 1)
      {
        // if this is the first node in a set, set it's X value equal to it's child's X value
        if (node.IsLeftMost)
        {
          node.X = node.Children[0].X;
        }
        else
        {
          node.X   = node.PreviousSibling!.X + NodeSize + SiblingDistance;
          node.Mod = node.X - node.Children[0].X;
        }
      }
      else
      {
        var leftChild  = node.LeftMostChild!;
        var rightChild = node.RightMostChild!;
        var mid        = (leftChild.X + rightChild.X) / 2;

        if (node.IsLeftMost)
        {
          node.X = mid;
        }
        else
        {
          node.X   = node.PreviousSibling!.X + NodeSize + SiblingDistance;
          node.Mod = node.X - mid;
        }
      }

      if (node.Children.Count > 0 && !node.IsLeftMost)
      {
        // Since subtrees can overlap, check for conflicts and shift tree right if needed
        CheckForConflicts(node);
      }
    }

    private static void CheckForConflicts<T>(LayoutNode<T> node)
    {
      var minDistance = TreeDistance + NodeSize;
      var shiftValue  = 0F;

      var nodeContour = new Dictionary<int, float>();
      GetLeftContour(node, 0, ref nodeContour);

      var sibling = node.LeftMostSibling;

      while (sibling != null && sibling != node)
      {
        var siblingContour = new Dictionary<int, float>();
        GetRightContour(sibling, 0, ref siblingContour);

        for (var level = node.Y + 1; level <= Math.Min(siblingContour.Keys.Max(), nodeContour.Keys.Max()); level++)
        {
          var distance = nodeContour[level] - siblingContour[level];
          if (distance + shiftValue < minDistance)
          {
            shiftValue = minDistance - distance;
          }
        }

        if (shiftValue > 0)
        {
          node.X   += shiftValue;
          node.Mod += shiftValue;

          CenterNodesBetween(node, sibling);

          shiftValue = 0;
        }

        sibling = sibling.NextSibling;
      }
    }

    private static void CenterNodesBetween<T>(LayoutNode<T> leftNode, LayoutNode<T> rightNode)
    {
      var leftIndex  = leftNode.Parent!.Children.IndexOf(rightNode);
      var rightIndex = leftNode.Parent.Children.IndexOf(leftNode);

      var numNodesBetween = rightIndex - leftIndex - 1;

      if (numNodesBetween > 0)
      {
        var distanceBetweenNodes = (leftNode.X - rightNode.X) / (numNodesBetween + 1);

        var count = 1;
        for (var i = leftIndex + 1; i < rightIndex; i++)
        {
          var middleNode = leftNode.Parent.Children[i];

          var desiredX = rightNode.X + distanceBetweenNodes * count;
          var offset   = desiredX - middleNode.X;
          middleNode.X   += offset;
          middleNode.Mod += offset;

          count++;
        }

        CheckForConflicts(leftNode);
      }
    }

    private static void CheckAllChildrenOnScreen<T>(LayoutNode<T> node)
    {
      var nodeContour = new Dictionary<int, float>();
      GetLeftContour(node, 0, ref nodeContour);

      float shiftAmount = 0;

      foreach (var y in nodeContour.Keys)
      {
        if (nodeContour[y] + shiftAmount < 0)
        {
          shiftAmount = nodeContour[y] * -1;
        }
      }

      if (shiftAmount > 0)
      {
        node.X   += shiftAmount;
        node.Mod += shiftAmount;
      }
    }

    private static void GetLeftContour<T>(LayoutNode<T> node, float modSum, ref Dictionary<int, float> values)
    {
      if (!values.ContainsKey(node.Y))
      {
        values.Add(node.Y, node.X + modSum);
      }
      else
      {
        values[node.Y] = Math.Min(values[node.Y], node.X + modSum);
      }

      modSum += node.Mod;

      foreach (var child in node.Children)
      {
        GetLeftContour(child, modSum, ref values);
      }
    }

    private static void GetRightContour<T>(LayoutNode<T> node, float modSum, ref Dictionary<int, float> values)
    {
      if (!values.ContainsKey(node.Y))
      {
        values.Add(node.Y, node.X + modSum);
      }
      else
      {
        values[node.Y] = MathF.Max(values[node.Y], node.X + modSum);
      }

      modSum += node.Mod;

      foreach (var child in node.Children)
      {
        GetRightContour(child, modSum, ref values);
      }
    }
  }
}