// NewWorldWizardViewModel.cs
// Backs the New World Wizard overlay — collects metadata to create a WorldProject.

using CommunityToolkit.Mvvm.ComponentModel;

namespace DWMStudio.ViewModels
{
    public sealed partial class NewWorldWizardViewModel : ObservableObject
    {
        [ObservableProperty] private string _worldName   = string.Empty;
        [ObservableProperty] private string _description = string.Empty;
        [ObservableProperty] private int    _currentStep;

        public void Reset()
        {
            WorldName   = string.Empty;
            Description = string.Empty;
            CurrentStep = 0;
        }
    }
}
