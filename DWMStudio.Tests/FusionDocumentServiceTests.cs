// FusionDocumentServiceTests.cs
// Covers create and open without Fusion, by faking ICADDocument.
//
// The routes underneath have never been exercised, so nothing here can say they work. What is
// pinned is what the service says about the two consequences that outlive the call: the active
// document changed, and a document slot was spent.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CAD;
using CAD.Scripting;
using DWM.Shared.Tooling.Cad;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class FusionDocumentServiceTests
    {
        [Fact]
        public async Task ACreate_WarnsThatTheActiveDocumentMoved()
        {
            // THE CONSEQUENCE THAT OUTLIVES THE CALL. Every other command reads whatever has
            // focus, so a create silently retargets massProperties, revolve and export. A
            // caller that does not know this reads the wrong model and gets plausible numbers.
            var session = new FakeDocumentSession("Rotor v2");

            var result = await new FusionDocumentService(() => session).CreateAsync("Rotor v2");

            Assert.True(result.Succeeded);
            Assert.Equal("Rotor v2", result.DocumentName);
            Assert.Contains(result.Run.Warnings, w => w.Contains("ACTIVE DOCUMENT IS NOW"));
        }

        [Fact]
        public async Task ACreate_WarnsAboutTheTenDocumentLimit_EveryTime()
        {
            // FIRES UNCONDITIONALLY, WHICH IS NORMALLY A MISTAKE. The FEMAP lesson was that a
            // warning on every run means nobody reads it. This one stays because nothing can
            // count the open documents -- contract v1 has no route that lists them -- and the
            // alternative is silence about a limit whose symptom appears three commands later
            // as mass properties reading 0 with no error.
            var session = new FakeDocumentSession("Scratch");

            var result = await new FusionDocumentService(() => session).CreateAsync("Scratch");

            var warning = result.Run.Warnings.Single(w => w.Contains("Inactive (Read-Only)"));
            Assert.Contains("10 active documents", warning);
            Assert.Equal(10, FusionDocumentService.FreeTierActiveDocumentLimit);
        }

        [Fact]
        public async Task AnOpen_WarnsWhenThePathIsAbsentHere_ButDoesNotRefuse()
        {
            // WARNED, NOT REFUSED, and the distinction is the deployment. Fusion resolves the
            // path and the add-in is reached over HTTP, so Fusion may be on another machine
            // where the path is perfectly good. Refusing would break the remote case to catch
            // a typo; warning catches the typo and breaks nothing.
            var session = new FakeDocumentSession("Turbine");

            var result = await new FusionDocumentService(() => session)
                .OpenAsync(@"C:\definitely\not\here\turbine.f3d");

            Assert.True(result.Succeeded);
            Assert.Contains(result.Run.Warnings, w => w.Contains("ON THIS MACHINE"));
        }

        [Fact]
        public async Task ATransportWithoutDocumentRoutes_SaysSo_AndFlagsThatTheAlternativeAlsoCosts()
        {
            var session = new FakeDocumentSession("x") { HasDocumentRoutes = false };

            var result = await new FusionDocumentService(() => session).CreateAsync("x");

            Assert.False(result.Succeeded);
            Assert.Contains("cannot create documents", result.Run.FailureMessage);

            // The IR fallback IMPORTS rather than opens, so it spends a slot too. Offering it
            // without that caveat would trade one silent cost for another.
            Assert.Contains("IMPORTS rather than opens", result.Run.FailureMessage);
        }

        [Fact]
        public async Task A404_SaysTheDocumentMayEXIST_BecauseTwoRoutesAreInvolved()
        {
            // THE TEST WORTH HAVING HERE. FusionApplication creates the document with one route
            // and then hydrates it with GET /documents/active/parameters. If the second is the
            // one missing, the document was ALREADY CREATED and the call still threw -- so
            // "it failed" and "nothing happened" are different claims, and a retry would spend
            // a second document slot.
            var session = new FakeDocumentSession("x")
            {
                Failure = new InvalidOperationException(
                    "Failed to fetch parameters: HTTP 404 — Not Found")
            };

            var result = await new FusionDocumentService(() => session).CreateAsync("x");

            Assert.False(result.Succeeded);
            Assert.Contains("TWO CANDIDATES", result.Run.FailureMessage);
            Assert.Contains("LOOK AT FUSION before retrying", result.Run.FailureMessage);
        }

        [Fact]
        public async Task NoPing_FailsBeforeCreatingAnything()
        {
            var session = new FakeDocumentSession("x") { Reachable = false };

            var result = await new FusionDocumentService(() => session).CreateAsync("x");

            Assert.False(result.Succeeded);
            Assert.Contains("cannot tell", result.Run.FailureMessage);
            Assert.Null(session.Created);
        }

        [Fact]
        public async Task AnEmptyName_IsRejectedBeforeAnythingIsSent()
        {
            var session = new FakeDocumentSession("x");
            var service = new FusionDocumentService(() => session);

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync("   "));
            await Assert.ThrowsAsync<ArgumentException>(() => service.OpenAsync(""));
            Assert.Null(session.Created);
        }

        [Fact]
        public void TheMcpTransport_CannotCreateOrOpen_AndSaysSoByReturningNull()
        {
            using var mcp = FusionSessionFactory.For(FusionTransport.Mcp)();

            Assert.Null(mcp.CreateDocumentAsync("x").GetAwaiter().GetResult());
            Assert.Null(mcp.OpenDocumentAsync("x").GetAwaiter().GetResult());
        }

        // ------------------------------------------------------------------
        private sealed class FakeDocumentSession : IFusionSession
        {
            private readonly string _name;

            public FakeDocumentSession(string name) => _name = name;

            public bool Reachable { get; set; } = true;
            public bool HasDocumentRoutes { get; set; } = true;
            public Exception? Failure { get; set; }
            public string? Created { get; private set; }
            public string? Opened { get; private set; }

            public Task<bool> PingAsync(CancellationToken ct = default) => Task.FromResult(Reachable);

            public Task<FusionResponse> InvokeAsync(string command, object? payload = null,
                CancellationToken ct = default)
                => throw new NotSupportedException("This fake covers the document path only.");

            public Task<ICADDocument?> CreateDocumentAsync(string name, CancellationToken ct = default)
            {
                if (Failure is not null) throw Failure;
                Created = name;
                return Task.FromResult<ICADDocument?>(
                    HasDocumentRoutes ? new FakeDocument(_name) : null);
            }

            public Task<ICADDocument?> OpenDocumentAsync(string path, CancellationToken ct = default)
            {
                if (Failure is not null) throw Failure;
                Opened = path;
                return Task.FromResult<ICADDocument?>(
                    HasDocumentRoutes ? new FakeDocument(_name) : null);
            }

            public void Dispose() { }
        }

        private sealed class FakeDocument : ICADDocument
        {
            public FakeDocument(string name) => Name = name;

            public string Name { get; }
            public string Id => "active";
            public ICADParameterCollection Parameters { get; } = new EmptyParameters();

            public Task SaveAsync(string? description = null, CancellationToken ct = default)
                => Task.CompletedTask;

            public Task<string> ExportAsync(ExportFormat format, string outputPath,
                CancellationToken ct = default)
                => Task.FromResult(outputPath);

            public Task ExecuteScriptAsync(GeneratedPackage script, CancellationToken ct = default)
                => Task.CompletedTask;
        }

        private sealed class EmptyParameters : ICADParameterCollection
        {
            public ICADParameter? FindByName(string name) => null;

            public Task<ICADParameter> SetAsync(string name, string expression, CancellationToken ct = default)
                => throw new NotSupportedException();

            public Task<ICADParameter> AddAsync(string name, string expression, string unit,
                string comment = "", CancellationToken ct = default)
                => throw new NotSupportedException();

            public IEnumerator<ICADParameter> GetEnumerator() =>
                Enumerable.Empty<ICADParameter>().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
