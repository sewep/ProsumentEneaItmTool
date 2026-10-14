using CommunityToolkit.Mvvm.ComponentModel;

namespace ProsumentEneaItmTool.UI.CustomControls
{
    public enum ChartMode
    {
        /// <summary>
        /// Values summed up from the beginning of the range.
        /// </summary>
        Cumulative,

        /// <summary>
        /// Values of each period separately.
        /// </summary>
        PerPeriod
    }

    /// <summary>
    /// What the chart shows. Every change redraws the chart.
    /// </summary>
    public class ChartOptions : ObservableObject
    {
        private ChartMode _mode = ChartMode.Cumulative;
        private bool _showAfterBalancing = true;
        private bool _showBeforeBalancing = false;
        private bool _showConsumed = true;
        private bool _showFed = true;
        private bool _showDifference = true;
        private bool _showFreeToUse = true;

        public ChartMode Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        public bool ShowAfterBalancing
        {
            get => _showAfterBalancing;
            set => SetProperty(ref _showAfterBalancing, value);
        }

        public bool ShowBeforeBalancing
        {
            get => _showBeforeBalancing;
            set => SetProperty(ref _showBeforeBalancing, value);
        }

        public bool ShowConsumed
        {
            get => _showConsumed;
            set => SetProperty(ref _showConsumed, value);
        }

        public bool ShowFed
        {
            get => _showFed;
            set => SetProperty(ref _showFed, value);
        }

        public bool ShowDifference
        {
            get => _showDifference;
            set => SetProperty(ref _showDifference, value);
        }

        public bool ShowFreeToUse
        {
            get => _showFreeToUse;
            set => SetProperty(ref _showFreeToUse, value);
        }
    }
}
