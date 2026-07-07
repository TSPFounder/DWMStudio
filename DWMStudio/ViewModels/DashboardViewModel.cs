// DashboardViewModel.cs
// Backs the Dashboard view — world cards and the New World command.

using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DWMStudio.Models;
using DWM.Shared;
using DWM.Shared.Economy;

namespace DWMStudio.ViewModels
{
    public sealed partial class DashboardViewModel : ObservableObject
    {
        public ObservableCollection<WorldProject> Worlds { get; } = new();

        [ObservableProperty] private string _statusMessage = string.Empty;

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
        private void ExportTestPackage()
        {
            const string outputPath = @"C:\DreamWorldMaker\Repos\DWM_Dev\Content\Databases\pendulum.db";
            const string simResultsCsv = @"C:\DWM\out\pendulum_results.csv";

            var exporter = new DWM.Shared.WorldPackageExporter();
            exporter.WritePendulum(outputPath, "pendulum", simResultsCsv);

            var source = File.Exists(simResultsCsv) ? "Simscape CSV" : "analytic small-angle fallback";
            StatusMessage = $"Package written to {outputPath} ({source}).";
        }

        [RelayCommand]
        private void ExportEconomyPackage()
        {
            const string outputPath = @"C:\DreamWorldMaker\Repos\DWM_Dev\Content\Databases\economy.db";

            var seeder = new EconomySeeder();
            seeder.WriteEconomy(outputPath);

            StatusMessage = $"Economy package written to {outputPath}.";
        }

        [RelayCommand]
        private void OpenWorld(WorldProject? world)
        {
            if (world is null) return;
            WeakReferenceMessenger.Default.Send(new OpenWorldMessage(world));
        }
    }
}
