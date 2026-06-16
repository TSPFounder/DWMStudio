using System.Windows;
using System.Windows.Controls;

namespace DWMStudio.Views
{
    public partial class ToolStatusDot : UserControl
    {
        public ToolStatusDot()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string),
                typeof(ToolStatusDot), new PropertyMetadata(string.Empty));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public static readonly DependencyProperty IsConnectedProperty =
            DependencyProperty.Register(nameof(IsConnected), typeof(bool),
                typeof(ToolStatusDot), new PropertyMetadata(false));

        public bool IsConnected
        {
            get => (bool)GetValue(IsConnectedProperty);
            set => SetValue(IsConnectedProperty, value);
        }
    }
}
