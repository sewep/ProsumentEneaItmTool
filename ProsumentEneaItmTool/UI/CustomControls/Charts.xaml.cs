using System.Windows;
using System.Windows.Controls;
using ProsumentEneaItmTool.Domain;
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
            var scatters = new List<GraphFlow>()
            {
                new(plot, dates, takenAB.ToArray(), "Pob. po b.", Color.FromARGB(0xffff0000), "{0):yyyy.MM.dd}, Wolumin pobranej energii = {1:F0} kWh"),
                new(plot, dates, fedAB.ToArray(), "Wys. po b.", Color.FromARGB(0xff00bb00), "{0):yyyy.MM.dd}, Wolumin wysłanej energii = {1:F0} kWh"),
                new(plot, dates, diffAB.ToArray(), "Różnica po b.", Color.FromARGB(0xff0000ff), "{0):yyyy.MM.dd}, Różnica energii jeden do jeden = {1:F0} kWh"),
                new(plot, dates, diffABn.ToArray(), "Netto po b.", Color.FromARGB(0xffff00ff), "{0):yyyy.MM.dd}, Energia do wykorzystania = {1:F0} kWh"),
            };

            var horizontalLine = plot.Add.HorizontalLine(0.0);
            horizontalLine.Color = Color.FromARGB(0xff00000);

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
                string pointDetails = string.Empty;

                foreach (var scatter in scatters)
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
            };

            WpfPlot.Refresh();
        }
    }
}
