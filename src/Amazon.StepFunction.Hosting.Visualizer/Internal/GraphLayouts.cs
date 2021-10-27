using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;

namespace Amazon.StepFunction.Hosting.Visualizer.Internal
{
  internal delegate void GraphLayoutAlgorithm(IGraphLayoutTarget target);

  /// <summary>Permits laying a graph out using Microsoft Graph Layout.</summary>
  internal interface IGraphLayoutTarget
  {
    GeometryGraph ToGeometryGraph();
    void          FromGeometryGraph(GeometryGraph graph);
  }

  internal static class GraphLayouts
  {
    public static GraphLayoutAlgorithm Standard { get; } = WithSettings(new SugiyamaLayoutSettings
    {
      MinNodeWidth  = 200f,
      MinNodeHeight = 9 / 16f * 200f,
      GridSizeByX   = 8,
      GridSizeByY   = 8,
      SnapToGridByY = SnapToGridByY.Top,
      EdgeRoutingSettings = new EdgeRoutingSettings
      {
        // we only support straight lines in the renderer so make sure the graph is arranged that way
        EdgeRoutingMode = EdgeRoutingMode.StraightLine
      }
    });

    private static GraphLayoutAlgorithm WithSettings(LayoutAlgorithmSettings settings) => target =>
    {
      var graph = target.ToGeometryGraph();

      LayoutHelpers.CalculateLayout(graph, settings, null);

      // center graph in the layout
      graph.UpdateBoundingBox();
      graph.Translate(new Point(-graph.Left, -graph.Bottom));

      target.FromGeometryGraph(graph);
    };
  }
}