// DashboardViewModel.cs
// Backs the Dashboard view — world cards and the New World command.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DWMStudio.Models;

namespace DWMStudio.ViewModels
{
    public sealed partial class DashboardViewModel : ObservableObject
    {
        public ObservableCollection<WorldProject> Worlds { get; } = new();

        public DashboardViewModel()
        {
            // Sample data so the dashboard renders before persistence is wired up
            Worlds.Add(new WorldProject
            {
                Name        = "Tracer Pendulum",
                Description = "Single rigid body — the Phase 3 tracer bullet world",
                Version     = "0.1.0",
            });
            Worlds.Add(new WorldProject
            {
                Name        = "Satellite Bus Demo",
                Description = "Structural modes under launch loading",
                Version     = "0.2.0",
            });
        }

        [RelayCommand]
        private void OpenNewWorldWizard() =>
            WeakReferenceMessenger.Default.Send(new OpenWizardMessage());

        [RelayCommand]
        private void OpenWorld(WorldProject? world)
        {
            if (world is null) return;
            WeakReferenceMessenger.Default.Send(new OpenWorldMessage(world));
        }
    }
}
