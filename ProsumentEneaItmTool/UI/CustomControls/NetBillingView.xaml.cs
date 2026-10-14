using System.Windows;
using System.Windows.Controls;
using ScottPlot;

namespace ProsumentEneaItmTool.UI.CustomControls
{
    /// <summary>
    /// Net-billing summary, monthly chart and table with RCEm prices.
    /// </summary>
    public partial class NetBillingView : UserControl
    {
        private static readonly Color _rceColor = Color.FromARGB(0xffff8c00);
        private static readonly Color _rcemColor = Color.FromARGB(0xff008080);

        private NetBillingVM? _viewModel;

        public NetBillingView()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;
            ReloadChart();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel is not null)
            {
                _viewModel.ResultUpdated -= OnResultUpdated;
            }

            _viewModel = DataContext as NetBillingVM;

            if (_viewModel is not null)
            {
                _viewModel.ResultUpdated += OnResultUpdated;
            }

            ReloadChart();
        }

        private void OnResultUpdated(object? sender, EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(ReloadChart);
                return;
            }

            ReloadChart();
        }

        private void ReloadChart()
        {
            var plot = WpfPlot.Plot;
            plot.Clear();
            plot.Legend.ManualItems.Clear();

            var months = _viewModel?.Result.Periods ?? [];
            if (months.Count == 0)
            {
                WpfPlot.Refresh();
                return;
            }

            // Two bars per month: value by hourly RCE and by monthly RCEm.
            var bars = new List<Bar>();
            for (var i = 0; i < months.Count; i++)
            {
                bars.Add(new Bar { Position = i - 0.2, Value = months[i].ValueRce, FillColor = _rceColor, Size = 0.4 });
                bars.Add(new Bar { Position = i + 0.2, Value = months[i].ValueRcem, FillColor = _rcemColor, Size = 0.4 });
            }

            plot.Add.Bars(bars);

            var step = Math.Max(1, (int)Math.Ceiling(months.Count / 15.0));
            var labelled = Enumerable.Range(0, months.Count).Where(i => i % step == 0).ToArray();
            var positions = labelled.Select(x => (double)x).ToArray();
            var labels = labelled.Select(x => months[x].Time.ToString("MM.yy")).ToArray();
            plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(positions, labels);
            plot.Axes.Left.Label.Text = "zł";

            plot.Legend.ManualItems.Add(new LegendItem { LabelText = "Wartość wg RCE", FillColor = _rceColor });
            plot.Legend.ManualItems.Add(new LegendItem { LabelText = "Wartość wg RCEm", FillColor = _rcemColor });
            plot.ShowLegend(Alignment.UpperLeft);

            plot.Axes.Margins(bottom: 0);
            plot.Axes.AutoScale();
            WpfPlot.Refresh();
        }
    }
}
