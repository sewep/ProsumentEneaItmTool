using ScottPlot;
using ScottPlot.Plottables;

namespace ProsumentEneaItmTool.UI.CustomControls
{
    internal class GraphFlow
    {
        private readonly Plot _plot;
        private Scatter? _scatter;
        private readonly DateTime[] _dates;
        private readonly double[] _values;
        private readonly string _legend;
        private readonly Color _color;
        private readonly string _pointDescriptionFormat;
        private readonly bool _isSecondary;
        private readonly float _markerSize;

        public GraphFlow(Plot plot, DateTime[] dates, double[] values, string legend, Color color, string pointDescriptionFormat, bool isSecondary, float markerSize)
        {
            _plot = plot;
            _dates = dates;
            _values = values;
            _legend = legend;
            _color = color;
            _pointDescriptionFormat = pointDescriptionFormat;
            _isSecondary = isSecondary;
            _markerSize = markerSize;

            AddToPlot();
        }

        public void PointDetailsIfMouseOver(Coordinates mouseLocation, Crosshair crosshair, ref bool showCross, ref string pointDetails)
        {
            ArgumentNullException.ThrowIfNull(_scatter);

            DataPoint nearest = _scatter.Data.GetNearest(mouseLocation, _plot.LastRender);
            if (nearest.IsReal)
            {
                crosshair.IsVisible = true;
                crosshair.Position = nearest.Coordinates;
                pointDetails = string.Format(_pointDescriptionFormat, DateTime.FromOADate(nearest.X), nearest.Y);
                showCross = true;
            }
        }

        public Scatter? Scatter => _scatter;

        private void AddToPlot()
        {
            _scatter = _plot.Add.Scatter(_dates, _values);
            _scatter.LegendText = _legend;
            _scatter.Color = _color;
            _scatter.MarkerSize = _markerSize;

            // A secondary series (before balancing) has the same color, but is lighter, dashed and with open markers.
            if (_isSecondary)
            {
                _scatter.Color = _color.WithAlpha(0.6);
                _scatter.LinePattern = LinePattern.Dashed;
                _scatter.MarkerShape = MarkerShape.OpenCircle;
                _scatter.MarkerSize = _markerSize + 1;
            }
        }
    }
}
