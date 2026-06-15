using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DWMStudio.Views
{
    public partial class SysmlCountCard : UserControl
    {
        public SysmlCountCard()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string),
                typeof(SysmlCountCard), new PropertyMetadata(string.Empty));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public static readonly DependencyProperty CountProperty =
            DependencyProperty.Register(nameof(Count), typeof(int),
                typeof(SysmlCountCard), new PropertyMetadata(0));

        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        public static readonly DependencyProperty ColourProperty =
            DependencyProperty.Register(nameof(Colour), typeof(Brush),
                typeof(SysmlCountCard), new PropertyMetadata(Brushes.White));

        public Brush Colour
        {
            get => (Brush)GetValue(ColourProperty);
            set => SetValue(ColourProperty, value);
        }
    }
}
