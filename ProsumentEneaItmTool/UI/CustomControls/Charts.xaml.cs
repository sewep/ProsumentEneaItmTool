using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProsumentEneaItmTool.Model.Calculations;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;

namespace ProsumentEneaItmTool.UI.CustomControls
{
    /// <summary>
    /// Interaction logic for Charts.xaml
    /// </summary>
    public partial class Charts : UserControl
    {
        private Crosshair? _myCrosshair;
        private List<GraphFlow> _scatters = [];

        public IPowerCalculation Calculations
        {
            get { return (IPowerCalculation)GetValue(CalculationsProperty); }
            set { SetValue(CalculationsProperty, value); }
        }

        public static readonly DependencyProperty CalculationsProperty =
            DependencyProperty.Register("Calculations", typeof(IPowerCalculation), typeof(Charts), new PropertyMetadata(null, OnRecordsChange));

        private static void OnRecordsChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Charts charts)
            {
                return;
            }

            if (e.OldValue is IPowerCalculation oldCalculations)
            {
                oldCalculations.CalculationUpdated -= charts.OnCalculationsUpdated;
            }

            if (e.NewValue is IPowerCalculation newCalculations)
            {
                newCalculations.CalculationUpdated += charts.OnCalculationsUpdated;
            }

            charts.ReloadData();
        }

        private void OnCalculationsUpdated(object? sender, EventArgs e)
        {
            ReloadData();
        }

        public Charts()
        {
            InitializeComponent();

            WpfPlot.MouseMove += OnPlotMouseMove;

            ReloadData();
        }

        private void ReloadData()
        {
            bool isUiThread = Application.Current.Dispatcher.CheckAccess();
            if (!isUiThread)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ReloadData();
                });
                return;
            }

            WpfPlot.Plot.Clear();
            _scatters = [];
            _myCrosshair = null;

            if (Calculations is null)
            {
                return;
            }

            var chartPoints = Calculations.CalculationEnergyChart;

            DateTime[] dates = chartPoints.Select(x => x.Time).ToArray();
            var takenAB = chartPoints.Select(x => x.ConsumedAfterBalancing).ToArray();
            var fedAB = chartPoints.Select(x => x.FedAfterBalancing).ToArray();
            var diffAB = chartPoints.Select(x => x.DifferenceAfterBalancing).ToArray();
            var diffABn = chartPoints.Select(x => x.FreeToUseAfterBalancing).ToArray();

            var plot = WpfPlot.Plot;
            _scatters = new List<GraphFlow>()
            {
                new(plot, dates, takenAB, "Pob. po b.", Color.FromARGB(0xffff0000), "{0:yyyy.MM.dd}, Wolumen pobranej energii = {1:F0} kWh"),
                new(plot, dates, fedAB, "Wys. po b.", Color.FromARGB(0xff00bb00), "{0:yyyy.MM.dd}, Wolumen wysłanej energii = {1:F0} kWh"),
                new(plot, dates, diffAB, "Różnica po b.", Color.FromARGB(0xff0000ff), "{0:yyyy.MM.dd}, Różnica energii jeden do jeden = {1:F0} kWh"),
                new(plot, dates, diffABn, "Netto po b.", Color.FromARGB(0xffff00ff), "{0:yyyy.MM.dd}, Energia do wykorzystania = {1:F0} kWh"),
            };

            var horizontalLine = plot.Add.HorizontalLine(0.0);
            horizontalLine.Axes.YAxis = plot.Axes.Right;
            horizontalLine.Color = Color.FromARGB(0xff000000);

            plot.Axes.DateTimeTicksBottom();
            plot.Grid.YAxis = plot.Axes.Right;
            
            plot.ShowLegend(Alignment.UpperLeft);

            _myCrosshair = plot.Add.Crosshair(0, 0);
            _myCrosshair.IsVisible = false;
            _myCrosshair.MarkerShape = MarkerShape.OpenCircle;
            _myCrosshair.MarkerSize = 5;

            plot.Axes.Left.IsVisible = false;
            plot.Axes.Left.MinimumSize = 15;

            foreach (var scatter in _scatters)
            {
                if (scatter.Scatter != null)
                {
                    scatter.Scatter.Axes.YAxis = plot.Axes.Right;
                }
            }

            WpfPlot.Refresh();
        }

        private void OnPlotMouseMove(object sender, MouseEventArgs e)
        {
            if (_myCrosshair is null || _scatters.Count == 0)
            {
                return;
            }

            var plot = WpfPlot.Plot;
            Coordinates mouseLocation = GetMouseCursorPosition(e, plot);

            bool showCross = false;
            string pointDetails = string.Empty;

            foreach (var scatter in _scatters)
            {
                scatter.PointDetailsIfMouseOver(mouseLocation, _myCrosshair, ref showCross, ref pointDetails);
                if (showCross)
                {
                    break;
                }
            }

            _myCrosshair.IsVisible = showCross;
            PointInfo.Content = pointDetails;

            WpfPlot.Refresh();
        }

        private Coordinates GetMouseCursorPosition(MouseEventArgs e, Plot plot)
        {
            PresentationSource source = PresentationSource.FromVisual(this);
            double dpiXsc = 1;
            double dpiYsc = 1;
            if (source != null)
            {
                dpiXsc = source.CompositionTarget.TransformToDevice.M11;
                dpiYsc = source.CompositionTarget.TransformToDevice.M22;
            }

            Pixel mousePixel = new(e.GetPosition(WpfPlot).X * dpiXsc, e.GetPosition(WpfPlot).Y * dpiYsc);
            Coordinates mouseLocation = plot.GetCoordinates(mousePixel);
            return mouseLocation;
        }
    }
}
