// WorldLibraryStoreTests.cs
// Covers world-library persistence. The interesting cases are all failure cases: the happy
// round trip is one test and the other nine are about not losing the file.
//
// That balance is deliberate. This one file IS the library, so every failure mode is
// total rather than partial -- there is no "one world got damaged".

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DWM.Shared.Projects;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class WorldLibraryStoreTests : IDisposable
    {
        private readonly string _dir;
        private readonly string _path;

        public WorldLibraryStoreTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "dwm_library_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _path = Path.Combine(_dir, "worlds.json");
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); }
            catch (IOException) { }
        }

        [Fact]
        public void SaveThenLoad_RoundTripsEverything()
        {
            var store = new WorldLibraryStore(_path);
            store.Save(new[] { Sample() });

            var world = store.Load().Worlds.Single();

            Assert.Equal("Mountain Turbine", world.Name);
            Assert.Equal("0.3.0", world.Version);
            Assert.Equal(7, world.RequirementCount);
            Assert.Equal("Matlab.Application.7.12", world.ToolVersions["matlab"]);
            Assert.Equal(2, world.Stages.Count);
            Assert.True(world.Stages.Single(s => s.StageId == "matlab").IsComplete);
        }

        [Fact]
        public void StageStatusText_SurvivesTheRoundTrip_NotJustTheBoolean()
        {
            // "Complete with 2 warning(s) - ramp, 4/5 channels" is the whole point of the
            // status string. A round trip that kept only IsComplete would turn a qualified
            // result back into an unqualified green tick.
            var store = new WorldLibraryStore(_path);
            store.Save(new[] { Sample() });

            var stage = store.Load().Worlds.Single().Stages.Single(s => s.StageId == "matlab");

            Assert.Equal("Complete with 2 warning(s) - ramp, 4/5 channels", stage.Status);
        }

        [Fact]
        public void MissingFile_IsNotAnError()
        {
            var result = new WorldLibraryStore(_path).Load();

            Assert.True(result.WasMissing);
            Assert.Empty(result.Worlds);
            Assert.False(result.Recovered);
        }

        [Fact]
        public void Save_CreatesTheDirectory_SoFirstRunWorks()
        {
            var nested = Path.Combine(_dir, "AppData", "DWMStudio", "worlds.json");

            new WorldLibraryStore(nested).Save(new[] { Sample() });

            Assert.True(File.Exists(nested));
        }

        [Fact]
        public void CorruptFile_IsMovedAsideRatherThanCrashingTheApp()
        {
            // Throwing here would make DWMStudio refuse to start, which is worse than an
            // empty library and much harder to diagnose from a user's description.
            File.WriteAllText(_path, "{ this is not json");

            var result = new WorldLibraryStore(_path).Load();

            Assert.True(result.Recovered);
            Assert.Empty(result.Worlds);
            Assert.NotNull(result.BackupPath);
            Assert.True(File.Exists(result.BackupPath!), "the unusable file must be kept, not deleted");
            Assert.False(File.Exists(_path));
        }

        [Fact]
        public void CorruptFile_IsNeverDeleted_BecauseItIsTheOnlyCopy()
        {
            File.WriteAllText(_path, "{ broken");

            var backup = new WorldLibraryStore(_path).Load().BackupPath!;

            Assert.Equal("{ broken", File.ReadAllText(backup));
        }

        [Fact]
        public void FileFromANewerSchema_IsRefusedRatherThanPartiallyParsed()
        {
            // The failure this prevents is not a bad read -- it is the SAVE that follows one.
            // Parsing a newer file drops fields this build does not know about, and the next
            // save writes the reduced version back over the original. That is data loss with
            // a successful-looking save in front of it.
            File.WriteAllText(_path,
                """{ "SchemaVersion": 99, "Worlds": [ { "Name": "From the future" } ] }""");

            var result = new WorldLibraryStore(_path).Load();

            Assert.True(result.Recovered);
            Assert.Empty(result.Worlds);
            Assert.Contains("newer version", result.Message);
            Assert.True(File.Exists(result.BackupPath!));
        }

        [Fact]
        public void SavedFile_RecordsItsSchemaVersion_AndTheLoaderReadsIt()
        {
            // The world-package format writes SchemaVersion in three places and reads it in
            // none (SCOPE.md fragility audit, item 2). Not repeating that here was the point.
            new WorldLibraryStore(_path).Save(new[] { Sample() });

            Assert.Contains($"\"SchemaVersion\": {WorldLibraryStore.CurrentSchemaVersion}",
                File.ReadAllText(_path));
        }

        [Fact]
        public void OverwritingAnExistingLibrary_LeavesNoTempFileBehind()
        {
            var store = new WorldLibraryStore(_path);
            store.Save(new[] { Sample() });
            store.Save(new[] { Sample(), Sample("Second World") });

            Assert.Equal(2, store.Load().Worlds.Count);
            Assert.False(File.Exists(_path + ".tmp"), "the atomic-write temp file was not cleaned up");
        }

        [Fact]
        public void EmptyLibrary_SavesAndLoadsCleanly()
        {
            // Deleting the last world must be persistable. A store that only ever appends
            // would make removal look like it worked until the next restart.
            var store = new WorldLibraryStore(_path);
            store.Save(new[] { Sample() });
            store.Save(Array.Empty<WorldProjectRecord>());

            var result = store.Load();

            Assert.Empty(result.Worlds);
            Assert.False(result.WasMissing);
        }

        [Fact]
        public void DefaultPath_SitsUnderApplicationData()
        {
            var path = WorldLibraryStore.DefaultPath();

            Assert.Contains("DWMStudio", path);
            Assert.EndsWith("worlds.json", path);
            Assert.StartsWith(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), path);
        }

        private static WorldProjectRecord Sample(string name = "Mountain Turbine") => new()
        {
            WorldId = Guid.NewGuid().ToString(),
            Name = name,
            Description = "3 MW wind turbine, R2011a Simulink model",
            Version = "0.3.0",
            SimulinkModelPath = @"C:\DreamWorldMaker\Repos\DWM_Dev\Models\Simulink\MVP_WindTurbine",
            RequirementCount = 7,
            LastModifiedOn = DateTimeOffset.UtcNow,
            ToolVersions = new Dictionary<string, string> { ["matlab"] = "Matlab.Application.7.12" },
            Stages = new List<StageStatusRecord>
            {
                new() { StageId = "cad", Status = "Pending" },
                new()
                {
                    StageId = "matlab",
                    Status = "Complete with 2 warning(s) - ramp, 4/5 channels",
                    IsComplete = true
                }
            }
        };
    }
}
