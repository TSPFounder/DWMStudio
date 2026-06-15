// LibraryViewModel.cs
// Backs the Library view — the asset collection browser.
// Two-level: collections on the left, assets within the selected collection
// on the right, with a cross-collection search box. Mirrors the
// DWM_AssetCollection / DWM_Asset registry design.

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DWMStudio.ViewModels
{
    // Lightweight display models — replace with DWM.Shared types once the
    // asset registry tables and API are wired up.
    public sealed partial class AssetCollectionItem : ObservableObject
    {
        public string CollectionId { get; init; } = string.Empty;
        public string Name         { get; init; } = string.Empty;
        public string Publisher    { get; init; } = string.Empty;
        public string Source       { get; init; } = string.Empty;  // Fab / Quixel / ThirdParty
        public int    AssetCount   { get; init; }
        public int    ImportedCount{ get; init; }

        public string ImportStatusLabel =>
            ImportedCount == 0          ? "Not imported"
            : ImportedCount < AssetCount ? $"{ImportedCount}/{AssetCount} imported"
            : "Imported";
    }

    public sealed class AssetItem
    {
        public string AssetId   { get; init; } = string.Empty;
        public string Name      { get; init; } = string.Empty;
        public string AssetType { get; init; } = string.Empty;  // StaticMesh, Material, etc.
        public bool   Imported  { get; init; }
    }

    public sealed partial class LibraryViewModel : ObservableObject
    {
        [ObservableProperty] private bool   _isLoading;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private AssetCollectionItem? _selectedCollection;

        public ObservableCollection<AssetCollectionItem> Collections { get; } = new();
        public ObservableCollection<AssetItem>           Assets      { get; } = new();

        public LibraryViewModel()
        {
            // Seed with sample collections so the view renders before the
            // asset scan import is wired up. Replace with DatabaseService.
            Collections.Add(new AssetCollectionItem
            {
                Name = "Modular Sci-Fi Corridors", Publisher = "KitBash3D",
                Source = "Fab", AssetCount = 142, ImportedCount = 142
            });
            Collections.Add(new AssetCollectionItem
            {
                Name = "Nordic Cliff Megascans", Publisher = "Quixel",
                Source = "Quixel", AssetCount = 1, ImportedCount = 1
            });
            Collections.Add(new AssetCollectionItem
            {
                Name = "Industrial Machinery Pack", Publisher = "Third Party",
                Source = "ThirdParty", AssetCount = 88, ImportedCount = 0
            });
        }

        partial void OnSelectedCollectionChanged(AssetCollectionItem? value)
        {
            Assets.Clear();
            if (value is null) return;

            // TODO: load assets for value.CollectionId from the database
            // Placeholder content:
            for (int i = 1; i <= System.Math.Min(value.AssetCount, 12); i++)
                Assets.Add(new AssetItem
                {
                    Name      = $"{value.Name.Split(' ')[0]}_Asset_{i:D2}",
                    AssetType = i % 3 == 0 ? "Material" : "StaticMesh",
                    Imported  = value.ImportedCount > 0,
                });
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            IsLoading = true;
            // TODO: await DatabaseService.LoadCollectionsAsync()
            await Task.Delay(200);
            IsLoading = false;
        }
    }
}
