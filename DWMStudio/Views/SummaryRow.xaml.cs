using System.Windows;
using System.Windows.Controls;

namespace DWMStudio.Views
{
    public partial class SummaryRow : UserControl
    {
        public SummaryRow()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string),
                typeof(SummaryRow), new PropertyMetadata(string.Empty));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        // object so both int counts and string paths bind without converters
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(object),
                typeof(SummaryRow), new PropertyMetadata(null));

        public object? Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
    }
}
