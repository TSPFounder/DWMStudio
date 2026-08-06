// FusionStageServiceTests.cs
// Covers the Fusion CAD stage without Fusion, and without the add-in running.
//
// The transport is faked, which leaves the part worth testing: what this service REFUSES to
// believe. The routes are pinned from DWM-Fusion-AddIn v0.2.0's source rather than guessed --
// an earlier draft assumed an "ok" envelope where the add-in actually sends "success", which
// would have read every failure as a success.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DWM.Shared.Tooling;
using DWM.Shared.Tooling.Cad;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class FusionStageServiceTests
    {
        [Fact]
        public async Task ZeroMass_IsRefused_NotPassedOnAsALightComponent()
        {
            // THE TEST THIS FILE EXISTS FOR, and the reason is already in SCOPE.md.
            //
            // Fusion's free tier allows 10 active documents. A design built from External
            // Component References pushes the surplus to Inactive (Read-Only), where
            // physicalProperties CAN RETURN 0 RATHER THAN RAISING. Zero mass with no error,
            // straight into a Simulink model that runs and produces plausible nonsense.
            //
            // It is refused here because nothing downstream can tell 0 kg from a light part.
            var session = new FakeFusionSession(Json(@"
                { ""components"": [
                    { ""name"": ""Nacelle"", ""mass"": 12.5 },
                    { ""name"": ""Blade_1"", ""mass"": 0.0 } ] }"));

            var result = await new FusionStageService(() => session).ReadMassPropertiesAsync();

            Assert.False(result.Succeeded);
            Assert.Contains("ZERO OR NEGATIVE MASS", result.Run.FailureMessage);
            Assert.Contains("Blade_1", result.Run.FailureMessage);

            // The message names the cause and the fix, because the fix is in the CAD file and
            // not in any code this project owns.
            Assert.Contains("External Component References", result.Run.FailureMessage);
            Assert.Contains("monolithic", result.Run.FailureMessage);
        }

        [Fact]
        public async Task AnEmptyComponent_IsNotMistakenForTheInactiveBug()
        {
            // A GENUINELY EMPTY COMPONENT is exempt -- and note what that does NOT mean, since
            // the first version of this comment got it wrong. It said a component holding only
            // sub-components "has no bodies and therefore no mass of its own". The 2026-08-06
            // read disproves that: Fusion aggregates children into the parent, so the rotor
            // root came back with bodyCount 0 AND three thousand tonnes.
            //
            // Which makes the exemption narrower than it looked. No bodies AND zero mass means
            // nothing beneath it has mass either -- a component someone created and has not
            // filled yet. That is legitimate, and refusing it would reject a healthy model
            // mid-build.
            var session = new FakeFusionSession(Json(@"
                { ""components"": [
                    { ""name"": ""Rotor"",     ""mass"": 6500.0, ""bodyCount"": 0 },
                    { ""name"": ""Hub_TBD"",   ""mass"": 0.0,    ""bodyCount"": 0 },
                    { ""name"": ""Blade"",     ""mass"": 6500.0, ""bodyCount"": 1 } ] }"));

            var result = await new FusionStageService(() => session).ReadMassPropertiesAsync();

            Assert.True(result.Succeeded);
            Assert.Equal(3, result.Components.Count);
        }

        [Fact]
        public async Task ZeroMassWithBodiesPresent_IsStillRefused()
        {
            // The distinction has to cut both ways: bodies AND zero mass is the Inactive
            // (Read-Only) failure, and bodyCount must not become a way to wave it through.
            var session = new FakeFusionSession(Json(@"
                { ""components"": [
                    { ""name"": ""Blade"", ""mass"": 0.0, ""bodyCount"": 3 } ] }"));

            var result = await new FusionStageService(() => session).ReadMassPropertiesAsync();

            Assert.False(result.Succeeded);
            Assert.Contains("Blade", result.Run.FailureMessage);
        }

        [Fact]
        public async Task ZeroMassWithNoBodyCountReported_IsRefused_BecauseTheCautiousReadingIsSafer()
        {
            // An older add-in that omits bodyCount must not turn the check off. Assuming
            // "it probably had bodies" risks a false alarm; assuming it did not risks letting
            // a real zero through into a Simulink model. Only one of those is recoverable.
            var session = new FakeFusionSession(Json(@"
                { ""components"": [ { ""name"": ""Blade"", ""mass"": 0.0 } ] }"));

            var result = await new FusionStageService(() => session).ReadMassPropertiesAsync();

            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task AnEmptyDesign_IsAWrongDocumentReport_NotASuccessfulReadOfNothing()
        {
            // FOUND BY A REAL READ, 2026-08-05, which this service would have passed. An empty
            // Fusion design returns its root component alone: no bodies, therefore no mass,
            // therefore the zero-mass guard correctly stays quiet -- a bodiless component IS
            // legitimately massless.
            //
            // Every individual judgement was right and the answer was still useless. The exact
            // reply was:
            //   {"components":[{"name":"(Unsaved)","bodyCount":0,"mass":0.0, ...}]}
            //
            // A design where NOTHING has mass is not a light design, it is the wrong document.
            var session = new FakeFusionSession(Json(@"
                { ""components"": [
                    { ""name"": ""(Unsaved)"", ""bodyCount"": 0, ""mass"": 0.0 } ] }"));

            var result = await new FusionStageService(() => session).ReadMassPropertiesAsync();

            Assert.False(result.Succeeded);
            Assert.Contains("WRONG DOCUMENT", result.Run.FailureMessage);
            Assert.Contains("(Unsaved)", result.Run.FailureMessage);

            // The other half of the trap: an unsaved document does not survive a restart, and
            // restarting is exactly what someone does while getting an add-in to appear.
            Assert.Contains("restart", result.Run.FailureMessage);
        }

        [Fact]
        public async Task TheRealRotorReply_IsAccepted_AndTheDoubleCountIsCalledOut()
        {
            // THE ACTUAL NUMBERS, read out of Fusion on 2026-08-06 after build_rotor ran.
            // Three things in this one reply, all of which had been guessed wrongly before it:
            //
            //   1. The root has NO BODIES and still weighs 3,275,458.72 kg. Fusion aggregates.
            //   2. That is exactly 3 x the blade, so the list overlaps itself -- summing all
            //      four gives 4.37 million kg for a rotor that weighs 3.27 million.
            //   3. It must still pass. Nothing here is wrong with the model.
            var session = new FakeFusionSession(Json(@"
                { ""components"": [
                    { ""name"": ""Rotor"",   ""bodyCount"": 0, ""mass"": 3275458.7247 },
                    { ""name"": ""Blade_1"", ""bodyCount"": 1, ""mass"": 1091819.5749 },
                    { ""name"": ""Blade_2"", ""bodyCount"": 1, ""mass"": 1091819.5749 },
                    { ""name"": ""Blade_3"", ""bodyCount"": 1, ""mass"": 1091819.5749 } ] }"));

            var result = await new FusionStageService(() => session).ReadMassPropertiesAsync();

            Assert.True(result.Succeeded);
            Assert.Equal(4, result.Components.Count);

            // The warning names the aggregating component, because "do not sum this" is
            // useless without saying which entry contains the others.
            var warning = result.Run.Warnings.Single(w => w.Contains("double-count"));
            Assert.Contains("Rotor", warning);
            Assert.DoesNotContain("Blade_1", warning);
        }

        [Fact]
        public void TheAggregateRoot_IsTheBodilessCandidate_WhenTheSumIsAmbiguous()
        {
            // With a root and ONE child the identity "equals everything else combined" is true
            // of both of them, and naming the child would tell the caller to discard the only
            // component that has any bodies. The parent is the one holding nothing itself.
            var components = new[]
            {
                new FusionMassProperties { ComponentName = "Blade",  MassKg = 6500.0, BodyCount = 1 },
                new FusionMassProperties { ComponentName = "Rotor",  MassKg = 6500.0, BodyCount = 0 },
            };

            Assert.Equal("Rotor", FusionStageService.FindAggregateRoot(components)?.ComponentName);
        }

        [Fact]
        public async Task UnrelatedComponents_AreNotAccusedOfDoubleCounting()
        {
            // The warning has to stay quiet on a flat list of real parts, or it becomes noise
            // that gets ignored on the one read where it matters.
            var session = new FakeFusionSession(Json(@"
                { ""components"": [
                    { ""name"": ""Tower"",   ""bodyCount"": 1, ""mass"": 210500.0 },
                    { ""name"": ""Nacelle"", ""bodyCount"": 1, ""mass"": 82000.0 },
                    { ""name"": ""Hub"",     ""bodyCount"": 1, ""mass"": 15600.0 } ] }"));

            var result = await new FusionStageService(() => session).ReadMassPropertiesAsync();

            Assert.True(result.Succeeded);
            Assert.DoesNotContain(result.Run.Warnings, w => w.Contains("double-count"));
        }

        [Fact]
        public async Task AMissingMassField_IsSkippedWithAWarning_NotDefaultedToZero()
        {
            // Absent mass and zero mass are different problems, and defaulting the first into
            // the second would manufacture the exact bug the test above exists to catch.
            var session = new FakeFusionSession(Json(@"
                { ""components"": [
                    { ""name"": ""Nacelle"", ""mass"": 12.5 },
                    { ""name"": ""Ghost"" } ] }"));

            var result = await new FusionStageService(() => session).ReadMassPropertiesAsync();

            Assert.True(result.Succeeded);
            Assert.Single(result.Components);
            Assert.Contains(result.Run.Warnings, w => w.Contains("Ghost") && w.Contains("skipped"));
        }

        [Fact]
        public async Task AGoodRead_ReturnsTheComponentsAndNamesNoOutputFiles()
        {
            // This reads numbers out of a live document; it writes nothing. Naming an expected
            // output would make the freshness check fail every time and mean nothing when it
            // passed -- the same reasoning as the FEMAP hand-off.
            var session = new FakeFusionSession(Json(@"
                { ""components"": [
                    { ""name"": ""Tower"", ""mass"": 210500.0,
                      ""centreOfMass"": [0.0, 0.0, 39.5],
                      ""inertia"": [1.1, 2.2, 3.3, 0.0, 0.0, 0.0] } ] }"));

            var result = await new FusionStageService(() => session).ReadMassPropertiesAsync();

            Assert.True(result.Succeeded);
            var tower = Assert.Single(result.Components);
            Assert.Equal("Tower", tower.ComponentName);
            Assert.Equal(210500.0, tower.MassKg);
            Assert.Equal(3, tower.CentreOfMass.Length);
            Assert.Empty(result.Run.Outputs);
        }

        [Fact]
        public async Task NoPing_FailsBeforeAskingForAnything_AndRefusesToGuessWhy()
        {
            // A closed Fusion and a running Fusion without the add-in are the SAME EVENT from
            // out here -- both refuse the connection. Naming one would be a coin toss printed
            // as a diagnosis.
            var session = new FakeFusionSession(Json("{}")) { Reachable = false };

            var result = await new FusionStageService(() => session).ReadMassPropertiesAsync();

            Assert.False(result.Succeeded);
            Assert.Contains("cannot tell", result.Run.FailureMessage);
            Assert.Empty(session.Commands);   // nothing was asked for
        }

        [Fact]
        public async Task AnEmptyComponentList_SaysTheAddInMayBeReadingTheWrongDocument()
        {
            // Nothing outside Fusion can make it open a file: the add-in operates on the
            // ACTIVE document, which is whatever has focus rather than whatever this project
            // names. An empty result is far more often that than an empty design.
            var session = new FakeFusionSession(Json(@"{ ""components"": [] }"));

            var result = await new FusionStageService(() => session).ReadMassPropertiesAsync();

            Assert.False(result.Succeeded);
            Assert.Contains("ACTIVE document", result.Run.FailureMessage);
        }

        [Fact]
        public async Task AnAddInErrorInsideA200_IsAFailure_NotAnAcceptedCall()
        {
            // A Python handler that catches its own exception and returns 200 with
            // {"ok": false} is the normal shape. Treating HTTP 200 as success would be this
            // project's oldest mistake in a new place -- the status living somewhere other
            // than where a caller would naturally look, exactly like FEMAP's return code and
            // MYSTRAN's exit code.
            var session = new FakeFusionSession(Json("{}"))
            {
                Failure = "RuntimeError: no active design"
            };

            var result = await new FusionStageService(() => session).ReadMassPropertiesAsync();

            Assert.False(result.Succeeded);
            Assert.Contains("no active design", result.Run.FailureMessage);
        }

        [Fact]
        public async Task ANonJsonReply_IsReportedWithTheBody_NotAsAParseCrash()
        {
            // A Python traceback comes back as plain text. Swallowing it would throw away the
            // one thing that says what went wrong.
            var session = new FakeFusionSession(null) { RawBody = "Traceback (most recent call last): ..." };

            var result = await new FusionStageService(() => session).ReadMassPropertiesAsync();

            Assert.False(result.Succeeded);
            Assert.Contains("Traceback", result.Run.FailureMessage);
        }

        [Fact]
        public void TheRoutesMatchTheAddInsContractV1()
        {
            // PINNED FROM THE ADD-IN'S SOURCE, not guessed. DWM-Fusion-AddIn v0.2.0 exposes
            // /scripts/execute and /documents/active/export; it has NO mass-properties route,
            // which is why that command rides on the script executor.
            var p = new FusionProtocol();

            Assert.Equal("scripts/execute", p.Commands["massProperties"].Path);
            Assert.Equal("scripts/execute", p.Commands["build"].Path);
            Assert.Equal("documents/active/export", p.Commands["export"].Path);
            Assert.Equal(18750, p.BaseAddress.Port);
        }

        [Fact]
        public void ReadOk_ChecksSuccess_NotOk()
        {
            // THE FIELD IS "success". An earlier draft assumed "ok" -- a reasonable guess that
            // would have read every failure as a success, because a missing flag is treated as
            // "worked". Reading the add-in's source is what settled it.
            var p = new FusionProtocol();

            Assert.False(p.ReadOk(Json(@"{ ""success"": false }")));
            Assert.True(p.ReadOk(Json(@"{ ""success"": true }")));

            // Tolerant about absence: a route that reports nothing is assumed to have worked.
            Assert.True(p.ReadOk(Json(@"{ ""components"": [] }")));

            // And the old guess must NOT be honoured, or a real failure carrying "ok": false
            // alongside "success": true would flip the verdict.
            Assert.True(p.ReadOk(Json(@"{ ""ok"": false, ""success"": true }")));
        }

        [Fact]
        public void ScriptOutput_IsUnwrappedFromTheEnvelope()
        {
            // /scripts/execute answers {"success": true, "output": "<what was printed>"}, so a
            // script printing JSON has its payload inside a STRING one level down. Reading the
            // envelope as the result hands back a body with no components and no clue why.
            var p = new FusionProtocol();
            var envelope = Json(@"{ ""success"": true, ""output"": ""{\""components\"": []}"" }");

            var result = p.Commands["massProperties"].ReadResult(envelope);

            Assert.NotNull(result);
            Assert.True(result!.Value.TryGetProperty("components", out _));
        }

        [Fact]
        public void ATracebackInOutput_UnwrapsToNull_RatherThanCrashing()
        {
            // A script that raised prints a traceback, not JSON. Null is honest -- the caller
            // then reports the raw body instead of failing to parse it.
            var p = new FusionProtocol();
            var envelope = Json(@"{ ""success"": false, ""output"": ""Traceback (most recent call last):"" }");

            Assert.Null(p.Commands["massProperties"].ReadResult(envelope));
        }

        [Fact]
        public void TheMassPropertiesScript_ReportsBodyCount_BecauseTheServiceNeedsIt()
        {
            // The service distinguishes an empty assembly component from the Inactive
            // (Read-Only) failure using bodyCount. If the script stopped printing it, that
            // check would silently weaken into "refuse every zero".
            Assert.Contains("bodyCount", FusionScripts.MassProperties);
            Assert.Contains("getXYZMomentsOfInertia", FusionScripts.MassProperties);

            // Centimetres to metres, once, explicitly.
            Assert.Contains("/ 100.0", FusionScripts.MassProperties);
        }

        [Fact]
        public void InertiaIsConvertedToKgM2_NowThatTheUnitIsEvidencedRatherThanGuessed()
        {
            // This was deliberately NOT converted at first, and the reason was written down:
            // "applying a factor that might be wrong is worse than labelling it". The
            // 2026-08-06 read supplies the missing evidence by radius of gyration --
            // sqrt(I/m) = 1973, which is 19.73 m in centimetres and 1973 m in metres, against
            // a blade spanning 1.5 to 60 m with its centre of mass at 15.98 m. Only one of
            // those can be a rotor.
            //
            // So the label goes too. Leaving UNVERIFIED on a number that has now been checked
            // trains the next reader to ignore the warning where it is still true.
            Assert.Contains("1e-4", FusionScripts.MassProperties);
            Assert.Contains("kg*m^2", FusionScripts.MassProperties);
            Assert.DoesNotContain("UNVERIFIED", FusionScripts.MassProperties);
        }

        [Fact]
        public void TheBuildScript_CallsBuildRotor_NeverRun()
        {
            // run() creates a document and ends in a modal ui.messageBox. Inside
            // /scripts/execute that dialog holds Fusion's main thread -- the one thread the
            // add-in needs to answer anything -- so the request times out and every later
            // request queues behind it.
            var script = FusionScripts.BuildRotor(null);

            Assert.Contains("build_rotor", script);
            Assert.DoesNotContain("wtb.run(", script);
            Assert.DoesNotContain("messageBox", script);
        }

        [Fact]
        public void ConfigOverrides_AreEmbeddedAsJson()
        {
            var script = FusionScripts.BuildRotor(new { n_stations = 20, build_structure = true });

            Assert.Contains("\"n_stations\":20", script);
            Assert.Contains("unknown", script);   // typo'd keys are reported, not ignored
        }

        // ------------------------------------------------------------------
        private static JsonElement Json(string text) =>
            JsonDocument.Parse(text).RootElement.Clone();

        private sealed class FakeFusionSession : IFusionSession
        {
            private readonly JsonElement? _json;

            public FakeFusionSession(JsonElement? json) => _json = json;

            public bool Reachable { get; set; } = true;
            public string? Failure { get; set; }
            public string RawBody { get; set; } = "{}";
            public List<string> Commands { get; } = new();

            public Task<bool> PingAsync(CancellationToken ct = default) => Task.FromResult(Reachable);

            public Task<FusionResponse> InvokeAsync(string command, object? payload = null,
                CancellationToken ct = default)
            {
                Commands.Add(command);

                return Task.FromResult(new FusionResponse
                {
                    Ok = Failure is null,
                    Error = Failure,
                    Json = _json,
                    RawBody = RawBody
                });
            }

            public void Dispose() { }
        }
    }
}
