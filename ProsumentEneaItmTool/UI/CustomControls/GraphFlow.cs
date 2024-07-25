using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;

namespace ProsumentEneaItmTool.UI.CustomControls
{
    internal class GraphFlow
    {
        private readonly Plot _plot;
        private Scatter _scatter;
        private DateTime[] _dates;
        private double[] _values;
        private string _legend;
        private Color _color;
        private string _pointDescriptionFormat;

        public GraphFlow(Plot plot, DateTime[] dates, double[] values, string legend, Color color, string pointDescriptionFormat)
        {
            _plot = plot;
            _dates = dates;
            _values = values;
            _legend = legend;
            _color = color;
            _pointDescriptionFormat = pointDescriptionFormat;

            AddToPlot();
        }

        public void PointDetailsIfMouseOver(Coordinates mouseLocation, Crosshair crosshair, ref bool showCross, ref string pointDetails)
        {
            DataPoint nearest = _scatter.Data.GetNearest(mouseLocation, _plot.LastRender);
            if (nearest.IsReal)
            {
                crosshair.IsVisible = true;
                crosshair.Position = nearest.Coordinates;
                pointDetails = string.Format(_pointDescriptionFormat, DateTime.FromOADate(nearest.X), nearest.Y);
                showCross = true;
            }
        }

        private void AddToPlot()
        {
            _scatter = _plot.Add.Scatter(_dates, _values);
            _scatter.LegendText = _legend;
            _scatter.Color = _color;
            _scatter.MarkerSize = 2;
        }
    }
}
