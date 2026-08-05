// WorldLibraryService.cs
// Bridges DWMStudio's WorldProject to DWM.Shared's persistence records.
//
// The mapping lives here rather than in DWM.Shared because WorldProject is a WPF-project
// type and the store must stay testable -- DWMStudio targets net10.0-windows and cannot be
// built or referenced by the test project. So the store handles files and schema and knows
// nothing about WorldProject; this class knows both and does nothing else.

using System;
using System.Collections.Generic;
using System.Linq;
using DWM.Shared.Projects;
using DWMStudio.Models;

namespace DWMStudio.Services
{
    public sealed class WorldLibraryService
    {
        private readonly WorldLibraryStore _store;

        /// <summary>
        /// Set when the last load could not use an existing file. Surfaced so the shell can
        /// say so: a library that silently comes back empty is indistinguishable from a first
        /// run, and that is the one confusion worth spending a status line to prevent.
        /// </summary>
        public string? LoadMessage { get; private set; }

        public string Path => _store.Path;

        public WorldLibraryService(string? path = null) => _store = new WorldLibraryStore(path);

        public IReadOnlyList<WorldProject> Load()
        {
            var result = _store.Load();
            LoadMessage = result.Recovered || result.Message is not null ? result.Message : null;
            return result.Worlds.Select(ToProject).ToList();
        }

        public void Save(IEnumerable<WorldProject> worlds) =>
            _store.Save(worlds.Select(ToRecord));

        // ------------------------------------------------------------------
        private static WorldProjectRecord ToRecord(WorldProject world) => new()
        {
            WorldId = world.WorldId,
            Name = world.Name,
            Description = world.Description,
            Version = world.Version,
            FusionDocumentPath = NullIfBlank(world.FusionDocumentPath),
            SimulinkModelPath = NullIfBlank(world.SimulinkModelPath),
            FeaDeckPath = NullIfBlank(world.FeaDeckPath),
            RequirementCount = world.RequirementCount,
            ActorCount = world.ActorCount,
            UseCaseCount = world.UseCaseCount,
            UserStoryCount = world.UserStoryCount,
            LastModifiedOn = world.LastModifiedOn,
            Stages = world.Stages.Select(s => new StageStatusRecord
            {
                // The enum NAME, not its integer value. An ordinal would silently rebind every
                // saved stage the moment a stage is inserted -- which is exactly what the move
                // to a data-driven pipeline will do (see TOOLING.md step 1).
                StageId = s.Stage.ToString(),
                Status = s.Status,
                IsComplete = s.IsComplete
            }).ToList()
        };

        private static WorldProject ToProject(WorldProjectRecord record)
        {
            var world = new WorldProject
            {
                WorldId = record.WorldId,
                Name = record.Name,
                Description = record.Description,
                Version = record.Version,
                FusionDocumentPath = record.FusionDocumentPath ?? string.Empty,
                SimulinkModelPath = record.SimulinkModelPath ?? string.Empty,
                FeaDeckPath = record.FeaDeckPath ?? string.Empty,
                RequirementCount = record.RequirementCount,
                ActorCount = record.ActorCount,
                UseCaseCount = record.UseCaseCount,
                UserStoryCount = record.UserStoryCount,
                LastModifiedOn = record.LastModifiedOn
            };

            foreach (var saved in record.Stages)
            {
                // An unrecognised stage id is SKIPPED, not fatal. A project saved by a build
                // with an extra stage must still open here; refusing the whole world because
                // one stage name is unfamiliar would turn a forward-compatibility question
                // into a lost project.
                if (!Enum.TryParse<PipelineStage>(saved.StageId, out var stage)) continue;

                var target = world.GetStage(stage);
                target.Status = saved.Status;
                target.IsComplete = saved.IsComplete;
            }

            return world;
        }

        private static string? NullIfBlank(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
