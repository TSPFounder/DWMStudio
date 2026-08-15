// FusionParameterServiceTests.cs
// Covers the parameter path without Fusion, by faking ICADParameterCollection.
//
// The transport underneath is FusionLibrary's, and its routes have never been exercised. What
// is pinned here is therefore not "does the HTTP work" -- nothing here can answer that -- but
// what this service REFUSES to do with the answers, and what it says when the route is not
// there at all.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CAD;
using DWM.Shared.Tooling.Cad;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class FusionParameterServiceTests
    {
        [Fact]
        public async Task AGoodRead_ReturnsEveryParameterWithItsExpression()
        {
            var session = new FakeParameterSession(
                ("blade_length", "58.5 m", 5850.0, "m"),
                ("n_blades", "3", 3.0, ""));

            var result = await new FusionParameterService(() => session).ReadAsync();

            Assert.True(result.Succeeded);
            Assert.Equal(2, result.Parameters.Count);
            Assert.Equal("58.5 m", result["blade_length"]!.Expression);

            // INTERNAL UNITS ARE CARRIED, NOT CONVERTED. 58.5 m is 5850 centimetres, and the
            // property is named ValueInternal so that nobody feeds it to a model expecting
            // metres. Inertia was converted only after a hand-computed solid settled the
            // factor; nothing has done that for parameters.
            Assert.Equal(5850.0, result["blade_length"]!.ValueInternal);
        }

        [Fact]
        public async Task NoParameters_IsASuccessWithAWarning_NotAFailure()
        {
            // DELIBERATELY DIFFERENT FROM THE MASS-PROPERTIES READ, where nothing weighing
            // anything is refused outright. A design with no parameters is ordinary -- plenty
            // of models are drawn rather than driven -- so refusing it would reject healthy
            // documents. The warning still names the wrong-document possibility, because that
            // is the other way to get an empty list.
            var session = new FakeParameterSession();

            var result = await new FusionParameterService(() => session).ReadAsync();

            Assert.True(result.Succeeded);
            Assert.Empty(result.Parameters);
            Assert.Contains(result.Run.Warnings, w => w.Contains("FOCUS"));
        }

        [Fact]
        public async Task ATransportWithNoParameterSurface_SaysSoAndNamesTheProvenAlternative()
        {
            // The MCP route is this in practice: Autodesk's server, whose tool names are still
            // unread, with nothing mapped to parameters. Null rather than an empty list, so a
            // missing capability cannot be mistaken for a document that has none.
            var session = new FakeParameterSession { HasParameterSurface = false };

            var result = await new FusionParameterService(() => session).ReadAsync();

            Assert.False(result.Succeeded);
            Assert.Contains("no parameter surface", result.Run.FailureMessage);
            Assert.Contains("SetParameterOp", result.Run.FailureMessage);
        }

        [Fact]
        public async Task AnUnknownName_IsRefused_RatherThanQuietlyCreated()
        {
            // THE TEST THIS FILE EXISTS FOR. FusionParameterCollection.AddAsync delegates
            // straight to SetAsync, so the add-in's route is create-if-missing. A typo would
            // therefore ADD a parameter that drives no geometry, leave the intended one
            // untouched, and report success -- a change that did nothing and said it worked.
            var session = new FakeParameterSession(("blade_length", "58.5 m", 5850.0, "m"));

            var result = await new FusionParameterService(() => session)
                .SetAsync("Blade_Length", "60 m");

            Assert.False(result.Succeeded);
            Assert.Contains("CASE-SENSITIVE", result.Run.FailureMessage);
            Assert.Contains("blade_length", result.Run.FailureMessage);   // names what does exist
            Assert.Empty(session.Sets);                                   // nothing was sent
        }

        [Fact]
        public async Task AGoodSet_ReportsTheValueThatCameBack()
        {
            var session = new FakeParameterSession(("blade_length", "58.5 m", 5850.0, "m"));

            var result = await new FusionParameterService(() => session)
                .SetAsync("blade_length", "60 m");

            Assert.True(result.Succeeded);
            Assert.Equal(("blade_length", "60 m"), session.Sets.Single());
            Assert.Equal("60 m", result.Parameters[0].Expression);
            Assert.Empty(result.Run.Warnings);
        }

        [Fact]
        public async Task AnEchoedOldValue_IsWarnedAbout_BecauseItLooksExactlyLikeSuccess()
        {
            // A route that accepts a PATCH and returns the UNCHANGED parameter is HTTP 200 with
            // nothing done -- the same shape as MATLAB's Execute returning error text as a
            // string, MYSTRAN exiting 0 after a FATAL, and /scripts/execute answering 200 with
            // success:false. The reply is compared against what was asked for.
            //
            // A WARNING RATHER THAN A REFUSAL, because Fusion legitimately rewrites an
            // expression: "60" against a metre parameter comes back as "60 m".
            var session = new FakeParameterSession(("blade_length", "58.5 m", 5850.0, "m"))
            {
                IgnoreSets = true
            };

            var result = await new FusionParameterService(() => session)
                .SetAsync("blade_length", "60 m");

            Assert.True(result.Succeeded);
            var warning = result.Run.Warnings.Single();
            Assert.Contains("58.5 m", warning);   // what came back
            Assert.Contains("60 m", warning);     // what was asked for
        }

        [Fact]
        public async Task AMissingRoute_BlamesTheAddIn_NotTheDocument()
        {
            // The parameter routes were read from FusionLibrary's client rather than exercised
            // against the add-in. A 404 means that assumption was wrong, and the message has to
            // say so -- otherwise the next hour goes on the parameter name and the document.
            var session = new FakeParameterSession
            {
                Failure = new InvalidOperationException(
                    "Failed to fetch parameters: HTTP 404 — Not Found")
            };

            var result = await new FusionParameterService(() => session).ReadAsync();

            Assert.False(result.Succeeded);
            Assert.Contains("DOES NOT IMPLEMENT THIS ROUTE", result.Run.FailureMessage);
            Assert.Contains("operations", result.Run.FailureMessage);
        }

        [Fact]
        public async Task NoPing_FailsBeforeAskingForAnything()
        {
            var session = new FakeParameterSession { Reachable = false };

            var result = await new FusionParameterService(() => session).ReadAsync();

            Assert.False(result.Succeeded);
            Assert.Contains("cannot tell", result.Run.FailureMessage);
        }

        [Fact]
        public void TheDefaultTransport_HasNoParameterSurface_UntilItOptsIn()
        {
            // IFusionSession's default implementation returns null, so a transport that cannot
            // do this says so by saying nothing rather than by throwing.
            using var mcp = FusionSessionFactory.For(FusionTransport.Mcp)();

            Assert.Null(mcp.GetParametersAsync().GetAwaiter().GetResult());
        }

        // ------------------------------------------------------------------
        private sealed class FakeParameterSession : IFusionSession
        {
            private readonly FakeParameterCollection _parameters;

            public FakeParameterSession(params (string Name, string Expression, double Value, string Unit)[] seed)
                => _parameters = new FakeParameterCollection(this, seed);

            public bool Reachable { get; set; } = true;
            public bool HasParameterSurface { get; set; } = true;
            public bool IgnoreSets { get; set; }
            public Exception? Failure { get; set; }
            public List<(string Name, string Expression)> Sets => _parameters.Sets;

            public Task<bool> PingAsync(CancellationToken ct = default) => Task.FromResult(Reachable);

            public Task<FusionResponse> InvokeAsync(string command, object? payload = null,
                CancellationToken ct = default)
                => throw new NotSupportedException("This fake covers the parameter path only.");

            public Task<ICADParameterCollection?> GetParametersAsync(CancellationToken ct = default)
            {
                if (Failure is not null) throw Failure;
                return Task.FromResult<ICADParameterCollection?>(
                    HasParameterSurface ? _parameters : null);
            }

            public void Dispose() { }

            internal bool ShouldIgnoreSets => IgnoreSets;
        }

        private sealed class FakeParameterCollection : ICADParameterCollection
        {
            private readonly FakeParameterSession _owner;
            private readonly List<FakeParameter> _items = new();

            public FakeParameterCollection(
                FakeParameterSession owner,
                IEnumerable<(string Name, string Expression, double Value, string Unit)> seed)
            {
                _owner = owner;
                foreach (var (name, expression, value, unit) in seed)
                    _items.Add(new FakeParameter
                    {
                        Name = name, Expression = expression, Value = value, Unit = unit
                    });
            }

            public List<(string Name, string Expression)> Sets { get; } = new();

            public ICADParameter? FindByName(string name) =>
                _items.Find(p => string.Equals(p.Name, name, StringComparison.Ordinal));

            public Task<ICADParameter> SetAsync(string name, string expression, CancellationToken ct = default)
            {
                Sets.Add((name, expression));

                var index = _items.FindIndex(p => string.Equals(p.Name, name, StringComparison.Ordinal));
                var current = index >= 0 ? _items[index] : new FakeParameter { Name = name };

                // IgnoreSets models the route that accepts the change and returns the old value.
                var updated = _owner.ShouldIgnoreSets
                    ? current
                    : new FakeParameter
                    {
                        Name = name,
                        Expression = expression,
                        Value = current.Value,
                        Unit = current.Unit
                    };

                if (index >= 0) _items[index] = updated; else _items.Add(updated);
                return Task.FromResult<ICADParameter>(updated);
            }

            public Task<ICADParameter> AddAsync(string name, string expression, string unit,
                string comment = "", CancellationToken ct = default)
                => SetAsync(name, expression, ct);

            public IEnumerator<ICADParameter> GetEnumerator() => _items.Cast<ICADParameter>().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class FakeParameter : ICADParameter
        {
            public string Name { get; init; } = string.Empty;
            public string Expression { get; init; } = string.Empty;
            public double Value { get; init; }
            public string Unit { get; init; } = string.Empty;
            public string Comment { get; init; } = string.Empty;
        }
    }
}
