using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProsumentEneaItmTool.Domain;
using ProsumentEneaItmTool.Model.Calculations;
using ScottPlot;
using ScottPlot.Plottables;

namespace ProsumentEneaItmTool.UI.CustomControls
{
    /// <summary>
    /// Interaction logic for Charts.xaml
    /// </summary>
    public partial class Charts : UserControl
    {
        private record SeriesDefinition(
            Func<ChartOptions, bool> IsVisible,
            string LegendText,
            string Description,
            Color Color,
            Func<CalculationEnergyResults, double> AfterBalancing,
            Func<CalculationEnergyResults, double> BeforeBalancing);

        private static readonly SeriesDefinition[] _series =
        [
            new(o => o.ShowConsumed, "Pobrana", "Wolumen pobranej energii", Color.FromARGB(0xffff0000), x => x.ConsumedAfterBalancing, x => x.ConsumedBeforeBalancing),
            new(o => o.ShowFed, "Wysłana", "Wolumen wysłanej energii", Color.FromARGB(0xff00bb00), x => x.FedAfterBalancing, x => x.FedBeforeBalancing),
            new(o => o.ShowDifference, "Różnica", "Różnica energii jeden do jeden", Color.FromARGB(0xff0000ff), x => x.DifferenceAfterBalancing, x => x.DifferenceBeforeBalancing),
            new(o => o.ShowFreeToUse, "Po opuście", "Energia do wykorzystania", Color.FromARGB(0xffff00ff), x => x.FreeToUseAfterBalancing, x => x.FreeToUseBeforeBalancing),
        ];

        private Crosshair? _myCrosshair;
        private List<GraphFlow> _scatters = [];

        public IPowerCalculation Calculations
        {
            get { return (IPowerCalculation)GetValue(CalculationsProperty); }
            set { SetValue(CalculationsProperty, value); }
        }

        public static readonly DependencyProperty CalculationsProperty =
            DependencyProperty.Register("Calculations", typeof(IPowerCalculation), typeof(Charts), new PropertyMetadata(null, OnRecordsChange));

        public ChartOptions? Options
        {
            get { return (ChartOptions?)GetValue(OptionsProperty); }
            set { SetValue(OptionsProperty, value); }
        }

        public static readonly DependencyProperty OptionsProperty =
            DependencyProperty.Register("Options", typeof(ChartOptions), typeof(Charts), new PropertyMetadata(null, OnOptionsChange));

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

        private static void OnOptionsChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Charts charts)
            {
                return;
            }

            if (e.OldValue is ChartOptions oldOptions)
            {
                oldOptions.PropertyChanged -= charts.OnOptionsPropertyChanged;
            }

            if (e.NewValue is ChartOptions newOptions)
            {
                newOptions.PropertyChanged += charts.OnOptionsPropertyChanged;
            }

            charts.ReloadData();
        }

        private void OnCalculationsUpdated(object? sender, EventArgs e)
        {
            ReloadData();
        }

        private void OnOptionsPropertyChanged(object? sender, PropertyChangedEventArgs e)
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
            PointInfo.Content = string.Empty;

            if (Calculations is null)
            {
                WpfPlot.Refresh();
                return;
            }

            var options = Options ?? new ChartOptions();
            var aggregation = Calculations.Aggregation;
            var chartPoints = options.Mode == ChartMode.Cumulative
                ? Calculations.CalculationEnergyChart
                : Calculations.PeriodResults;

            DateTime[] dates = chartPoints.Select(x => x.Time).ToArray();
            var dateFormat = DateFormat(aggregation);
            var periodText = options.Mode == ChartMode.Cumulative ? "narastająco" : PeriodText(aggregation);
            // Few points are easier to read with visible markers.
            var markerSize = dates.Length <= 120 ? 5 : 2;

            var plot = WpfPlot.Plot;

            foreach (var series in _series.Where(x => x.IsVisible(options)))
            {
                if (options.ShowAfterBalancing)
                {
                    _scatters.Add(new GraphFlow(
                        plot,
                        dates,
                        chartPoints.Select(series.AfterBalancing).ToArray(),
                        $"{series.LegendText} po bil.",
                        series.Color,
                        $"{{0:{dateFormat}}}, {series.Description} po bilansowaniu ({periodText}) = {{1:N1}} kWh",
                        false,
                        markerSize));
                }

                if (options.ShowBeforeBalancing)
                {
                    _scatters.Add(new GraphFlow(
                        plot,
                        dates,
                        chartPoints.Select(series.BeforeBalancing).ToArray(),
                        $"{series.LegendText} przed bil.",
                        series.Color,
                        $"{{0:{dateFormat}}}, {series.Description} przed bilansowaniem ({periodText}) = {{1:N1}} kWh",
                        true,
                        markerSize));
                }
            }

            var horizontalLine = plot.Add.HorizontalLine(0.0);
            horizontalLine.Axes.YAxis = plot.Axes.Right;
            horizontalLine.Color = Color.FromARGB(0xff000000);

            plot.Axes.DateTimeTicksBottom();
            plot.Grid.YAxis = plot.Axes.Right;

            if (_scatters.Count > 0)
            {
                plot.ShowLegend(Alignment.UpperLeft);
            }

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

            plot.Axes.AutoScale();
            WpfPlot.Refresh();
        }

        private static string DateFormat(Aggregation aggregation) => aggregation switch
        {
            Aggregation.Day => "yyyy.MM.dd",
            Aggregation.Month => "MM.yyyy",
            _ => "yyyy.MM.dd HH:mm"
        };

        private static string PeriodText(Aggregation aggregation) => aggregation switch
        {
            Aggregation.Day => "w dniu",
            Aggregation.Month => "w miesiącu",
            _ => "w godzinie"
        };

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
