using System.Windows;
using System.Windows.Controls;
using DWM.Shared.Tooling;
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

        /// <summary>
        /// Push the tree's selection into the ViewModel.
        ///
        /// CODE-BEHIND BECAUSE WPF LEAVES NO CHOICE: TreeView.SelectedItem is read-only, so it
        /// cannot be data-bound the way ListBox.SelectedItem can. The alternatives are a
        /// behaviour assembly or a two-way IsSelected style on every item; a four-line handler
        /// is less machinery than either and does not hide where the value comes from.
        /// </summary>
        private void ContentsTree_SelectedItemChanged(
            object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is ToolWorkspaceViewModel vm)
                vm.SelectedNode = e.NewValue as WorldTreeNode;
        }
    }
}
