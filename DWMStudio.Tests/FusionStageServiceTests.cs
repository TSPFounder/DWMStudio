// FusionStageServiceTests.cs
// Covers the Fusion CAD stage without Fusion, and without the add-in that does not yet exist.
//
// The transport is faked, which leaves the part worth testing: what this service REFUSES to
// believe. Fusion is the one tool where DWM must supply the server as well as the client, so
// every field name in the protocol is a guess -- but the judgements made about the numbers
// that come back are not guesses, and those are what is pinned here.

using System;
using System.Collections.Generic;
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
        public void TheProtocolIsOverridable_SoTheRealAddInCostsOneObject()
        {
            // Every name in FusionProtocol is a guess: the Fusion Python for this project has
            // never been read by the code that has to talk to it. This is the FemapApiNames
            // pattern, adopted for the same reason and before the same mistake.
            var custom = new FusionProtocol
            {
                BaseAddress = new Uri("http://127.0.0.1:9001/"),
                MassPropertiesCommand = "dwm_mass_props",
                PathFor = c => "api/" + c
            };

            Assert.Equal("api/dwm_mass_props", custom.PathFor(custom.MassPropertiesCommand));
            Assert.Equal(9001, custom.BaseAddress.Port);
        }

        [Fact]
        public void ReadOk_TreatsAMissingFlagAsSuccess_ButAnExplicitFalseAsFailure()
        {
            // Tolerant about absence, strict about denial: an add-in that says nothing is
            // assumed to have worked, one that says "ok": false is believed.
            var p = new FusionProtocol();

            Assert.True(p.ReadOk(Json(@"{ ""components"": [] }")));
            Assert.True(p.ReadOk(Json(@"{ ""ok"": true }")));
            Assert.False(p.ReadOk(Json(@"{ ""ok"": false }")));
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
