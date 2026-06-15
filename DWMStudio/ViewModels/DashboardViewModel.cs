// DashboardViewModel.cs
// Backs the Dashboard view — shows the list of world projects and summary stats.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DWMStudio.Models;

namespace DWMStudio.ViewModels
{
    public sealed partial class DashboardViewModel : ObservableObject
    {
        public ObservableCollection<WorldProject> Worlds { get; } = new();
    }
}
