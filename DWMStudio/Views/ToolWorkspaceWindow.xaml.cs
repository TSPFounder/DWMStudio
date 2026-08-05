using System.Windows;
using DWMStudio.ViewModels;

namespace DWMStudio.Views
{
    public partial class ToolWorkspaceWindow : Window
    {
        public ToolWorkspaceWindow(ToolWorkspaceViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
