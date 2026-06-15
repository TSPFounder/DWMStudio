using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DWMStudio.Views
{
    public partial class WorldCard : UserControl
    {
        public WorldCard()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty OpenWorldCommandProperty =
            DependencyProperty.Register(nameof(OpenWorldCommand), typeof(ICommand),
                typeof(WorldCard), new PropertyMetadata(null));

        public ICommand? OpenWorldCommand
        {
            get => (ICommand?)GetValue(OpenWorldCommandProperty);
            set => SetValue(OpenWorldCommandProperty, value);
        }
    }
}
