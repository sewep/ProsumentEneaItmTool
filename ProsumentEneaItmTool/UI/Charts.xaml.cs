using System.Windows;
using System.Windows.Controls;
using ProsumentEneaItmTool.Domain;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;

namespace ProsumentEneaItmTool.UI
{
    /// <summary>
    /// Interaction logic for Charts.xaml
    /// </summary>
    public partial class Charts : UserControl
    {
        private Crosshair? _myCrosshair;

        public List<ImportFileRecord> Records
        {
            get { return (List<ImportFileRecord>)GetValue(RecordsProperty); }
            set { SetValue(RecordsProperty, value); }
        }

        public static readonly DependencyProperty RecordsProperty =
            DependencyProperty.Register("Records", typeof(List<ImportFileRecord>), typeof(Charts), new PropertyMetadata(new List<ImportFileRecord>(), OnRecordsChange));

        private static void OnRecordsChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Charts charts)
            {
                charts.ReloadData();
            }
        }

        public Charts()
        {
            InitializeComponent();

            ReloadData();
        }

        private void ReloadData()
        {
            WpfPlot.Plot.Clear();

            DateTime[] dates = Records.Select(x => x.Date).ToArray();
            List<double> takenBB = [];
            List<double> takenAB = [];
            List<double> fedBB = [];
            List<double> fedAB = [];
            List<double> diffBB = [];
            List<double> diffAB = [];
            List<double> diffBBn = [];
            List<double> diffABn = [];

            for (int i = 0; i < Records.Count; i++)
            {
                var record = Records[i];

                takenBB.Add((i > 0 ? takenBB.Last() : 0) + record.TakenVolumeBeforeBanancing);
                takenAB.Add((i > 0 ? takenAB.Last() : 0) + record.TakenVolumeAfterBanancing);
                fedBB.Add((i > 0 ? fedBB.Last() : 0) + record.FedVolumeBeforeBanancing);
                fedAB.Add((i > 0 ? fedAB.Last() : 0) + record.FedVolumeAfterBanancing);

                diffBB.Add(fedBB.Last() - takenBB.Last());
                diffAB.Add(fedAB.Last() - takenAB.Last());
                var currBBn = record.FedVolumeBeforeBanancing * 0.8 - record.TakenVolumeBeforeBanancing;
                var currABn = record.FedVolumeAfterBanancing * 0.8 - record.TakenVolumeAfterBanancing;
                diffBBn.Add((i > 0 ? diffBBn.Last() : 0) + currBBn);
                diffABn.Add((i > 0 ? diffABn.Last() : 0) + currABn);
            }

            var plot = WpfPlot.Plot;

            //var takenBBsc = plot.Add.Scatter(dates, takenBB.ToArray());
            var takenABsc = plot.Add.Scatter(dates, takenAB.ToArray());
            //var fedBBsc = plot.Add.Scatter(dates, fedBB.ToArray());
            var fedABsc = plot.Add.Scatter(dates, fedAB.ToArray());
            //var diffBBsc = plot.Add.Scatter(dates, diffBB.ToArray());
            var diffABsc = plot.Add.Scatter(dates, diffAB.ToArray());
            //var diffBBnsc = plot.Add.Scatter(dates, diffBBn.ToArray());
            var diffABnsc = plot.Add.Scatter(dates, diffABn.ToArray());

            //takenBBsc.LegendText = "Pob. przed b.";
            takenABsc.LegendText = "Pob. po b.";
            //fedBBsc.LegendText = "Wys. przed b.";
            fedABsc.LegendText = "Wys. po b.";
            //diffBBsc.LegendText = "Różnica przed b.";
            diffABsc.LegendText = "Różnica po b.";
            //diffBBnsc.LegendText = "Netto przed b.";
            diffABnsc.LegendText = "Netto po b.";

            takenABsc.MarkerStyle.Size = 2;
            fedABsc.MarkerStyle.Size = 2;
            diffABsc.MarkerStyle.Size = 2;
            diffABnsc.MarkerStyle.Size = 2;

            plot.Add.HorizontalLine(0.0);

            plot.Axes.DateTimeTicksBottom();
            plot.ShowLegend(Alignment.UpperLeft);

            _myCrosshair = plot.Add.Crosshair(0, 0);
            _myCrosshair.IsVisible = false;
            _myCrosshair.MarkerShape = MarkerShape.OpenCircle;
            _myCrosshair.MarkerSize = 15;

            WpfPlot.MouseMove += (s, e) =>
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

                bool showCross = false;

                DataPoint takenABscNearest = takenABsc.Data.GetNearest(mouseLocation, plot.LastRender);
                if (takenABscNearest.IsReal)
                {
                    _myCrosshair.IsVisible = true;
                    _myCrosshair.Position = takenABscNearest.Coordinates;
                    WpfPlot.Refresh();
                    PoinyInfo.Content = $"{DateTime.FromOADate(takenABscNearest.X):yyyy.MM.dd}, Wolumin pobranej mocy = {takenABscNearest.Y:0.#} kWh";
                    showCross = true;
                }

                DataPoint fedABscNearest = fedABsc.Data.GetNearest(mouseLocation, plot.LastRender);
                if (fedABscNearest.IsReal)
                {
                    _myCrosshair.IsVisible = true;
                    _myCrosshair.Position = fedABscNearest.Coordinates;
                    WpfPlot.Refresh();
                    PoinyInfo.Content = $"{DateTime.FromOADate(fedABscNearest.X):yyyy.MM.dd}, Wolumin wysłanej mocy = {fedABscNearest.Y:0.#} kWh";
                    showCross = true;
                }

                DataPoint diffABscNearest = diffABsc.Data.GetNearest(mouseLocation, plot.LastRender);
                if (diffABscNearest.IsReal)
                {
                    _myCrosshair.IsVisible = true;
                    _myCrosshair.Position = diffABscNearest.Coordinates;
                    WpfPlot.Refresh();
                    PoinyInfo.Content = $"{DateTime.FromOADate(diffABscNearest.X):yyyy.MM.dd}, Różnica mocy jeden do jeden = {diffABscNearest.Y:0.#} kWh";
                    showCross = true;
                }

                DataPoint diffABnscNearest = diffABnsc.Data.GetNearest(mouseLocation, plot.LastRender);
                if (diffABnscNearest.IsReal)
                {
                    _myCrosshair.IsVisible = true;
                    _myCrosshair.Position = diffABnscNearest.Coordinates;
                    WpfPlot.Refresh();
                    PoinyInfo.Content = $"{DateTime.FromOADate(diffABnscNearest.X):yyyy.MM.dd}, Moc do wykorzystania = {diffABnscNearest.Y:0.#} kWh";
                    showCross = true;
                }

                if (!showCross && _myCrosshair.IsVisible)
                {
                    _myCrosshair.IsVisible = false;
                    WpfPlot.Refresh();
                    PoinyInfo.Content = $"";
                }
            };

            WpfPlot.Refresh();
        }
    }
}
