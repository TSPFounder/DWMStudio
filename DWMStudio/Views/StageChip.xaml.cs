using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DWMStudio.Views
{
    public partial class StageChip : UserControl
    {
        public StageChip()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string),
                typeof(StageChip), new PropertyMetadata(string.Empty));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public static readonly DependencyProperty IsCompleteProperty =
            DependencyProperty.Register(nameof(IsComplete), typeof(bool),
                typeof(StageChip), new PropertyMetadata(false));

        public bool IsComplete
        {
            get => (bool)GetValue(IsCompleteProperty);
            set => SetValue(IsCompleteProperty, value);
        }

        public static readonly DependencyProperty ChipColorProperty =
            DependencyProperty.Register(nameof(ChipColor), typeof(Brush),
                typeof(StageChip), new PropertyMetadata(Brushes.Gray));

        public Brush ChipColor
        {
            get => (Brush)GetValue(ChipColorProperty);
            set => SetValue(ChipColorProperty, value);
        }
    }
}
