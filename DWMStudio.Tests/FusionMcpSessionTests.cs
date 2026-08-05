// FusionMcpSessionTests.cs
// Covers the MCP route into Fusion without an MCP server, by faking the JSON-RPC channel.
//
// That split is why IJsonRpcChannel exists: the process plumbing is dull and untestable here,
// the protocol handling is neither. What is pinned below is the handshake, the mapping from
// DWM's command names onto Autodesk's tool names, and the one thing about MCP that will
// otherwise be read wrong -- where a tool failure actually appears.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DWM.Shared.Tooling.Cad;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class FusionMcpSessionTests
    {
        [Fact]
        public async Task AToolFailure_ArrivesAsASuccessfulCallCarryingIsError()
        {
            // THE TEST THIS FILE EXISTS FOR. In MCP a failed tool call is NOT a JSON-RPC
            // error -- it is an ordinary successful response whose result carries
            // isError: true. A client checking only the protocol layer reads every failure as
            // a success.
            //
            // Fourth instance of one bug on this project: FEMAP returns 0 without throwing,
            // MYSTRAN exits 0 after a FATAL, the bridge answers HTTP 200 with ok:false, and
            // MCP hides it here. Four tools, four places to put the verdict, none of them
            // where a caller would naturally look.
            var channel = new FakeChannel(Json(@"
                { ""isError"": true,
                  ""content"": [ { ""type"": ""text"", ""text"": ""RuntimeError: no active design"" } ] }"));

            var result = await new FusionMcpSession(channel).InvokeAsync("massProperties");

            Assert.False(result.Ok);
            Assert.Contains("no active design", result.Error);
        }

        [Fact]
        public async Task AGoodCall_ReturnsTheTextBlockParsedAsJson()
        {
            var channel = new FakeChannel(Json(@"
                { ""content"": [ { ""type"": ""text"",
                                   ""text"": ""{\""components\"": [{\""name\"": \""Blade\"", \""mass\"": 6500.0}]}"" } ] }"));

            var result = await new FusionMcpSession(channel).InvokeAsync("massProperties");

            Assert.True(result.Ok);
            Assert.NotNull(result.Json);
            Assert.True(result.Json!.Value.TryGetProperty("components", out _));
        }

        [Fact]
        public async Task TheServiceWorksOverMcp_UnchangedIncludingTheZeroMassRefusal()
        {
            // THE WHOLE POINT OF IFusionSession. FusionStageService does not know or care
            // which transport it is on, so the judgement that matters -- refusing a zero mass
            // that cannot be explained by an empty component -- is identical either way.
            // Without this, "supports both" would mean two code paths and one of them tested.
            var channel = new FakeChannel(Json(@"
                { ""content"": [ { ""type"": ""text"",
                                   ""text"": ""{\""components\"": [{\""name\"": \""Blade\"", \""mass\"": 0.0, \""bodyCount\"": 3}]}"" } ] }"));

            var result = await new FusionStageService(() => new FusionMcpSession(channel))
                .ReadMassPropertiesAsync();

            Assert.False(result.Succeeded);
            Assert.Contains("ZERO OR NEGATIVE MASS", result.Run.FailureMessage);
        }

        [Fact]
        public async Task TheHandshakeHappensOnce_AndSendsTheInitializedNotification()
        {
            // The notification is required by the spec and easy to omit, because nothing
            // answers it. A server that never receives it may refuse every later call, which
            // then looks like a wrong tool name rather than a missing handshake step.
            var channel = new FakeChannel(Json(@"{ ""content"": [] }"));
            var session = new FusionMcpSession(channel);

            await session.InvokeAsync("massProperties");
            await session.InvokeAsync("massProperties");

            Assert.Equal(1, channel.Methods.FindAll(m => m == "initialize").Count);
            Assert.Contains("notifications/initialized", channel.Notifications);
            Assert.Equal(2, channel.Methods.FindAll(m => m == "tools/call").Count);
        }

        [Fact]
        public async Task DwmCommandNames_AreMappedOntoAutodesksToolNames()
        {
            // Ours stay put; theirs are guesses. The mapping is the seam that keeps a wrong
            // guess from reaching FusionStageService.
            var channel = new FakeChannel(Json(@"{ ""content"": [] }"));
            var options = new FusionMcpOptions
            {
                ToolNames = new Dictionary<string, string> { ["massProperties"] = "fusion_mass_props" }
            };

            await new FusionMcpSession(channel, options).InvokeAsync("massProperties");

            Assert.Equal("fusion_mass_props", channel.LastToolName);
        }

        [Fact]
        public async Task AnUnmappedCommand_SaysSoAndPointsAtToolsList()
        {
            // Reported rather than thrown, and it names the way to get the real answer instead
            // of inviting another guess.
            var channel = new FakeChannel(Json(@"{ ""content"": [] }"));

            var result = await new FusionMcpSession(channel).InvokeAsync("somethingUnmapped");

            Assert.False(result.Ok);
            Assert.Contains("ListToolsAsync", result.Error);
            Assert.Null(channel.LastToolName);   // nothing was sent
        }

        [Fact]
        public async Task ListTools_ReadsTheServersRealNames()
        {
            var channel = new FakeChannel(Json(@"
                { ""tools"": [ { ""name"": ""run_script"" }, { ""name"": ""export_design"" } ] }"));

            var names = await new FusionMcpSession(channel).ListToolsAsync();

            Assert.Equal(new[] { "run_script", "export_design" }, names);
        }

        [Fact]
        public async Task ANonJsonTextBlock_IsCarriedAsRawBody_NotSwallowed()
        {
            // A Python traceback comes back as prose. Losing it would throw away the one thing
            // that explains the failure.
            var channel = new FakeChannel(Json(@"
                { ""content"": [ { ""type"": ""text"", ""text"": ""Traceback (most recent call last)..."" } ] }"));

            var result = await new FusionMcpSession(channel).InvokeAsync("build");

            Assert.True(result.Ok);
            Assert.Null(result.Json);
            Assert.Contains("Traceback", result.RawBody);
        }

        [Fact]
        public void TheFactory_RefusesMcpWithNothingToLaunch_RatherThanFailingLater()
        {
            // A misconfigured transport should fail where it is configured, not on the first
            // build three screens away.
            var ex = Assert.Throws<InvalidOperationException>(
                () => FusionSessionFactory.For(FusionTransport.Mcp));

            Assert.Contains("ServerCommand", ex.Message);
            Assert.Contains("Bridge", ex.Message);
        }

        [Fact]
        public void TheFactory_BuildsABridgeSessionWithoutAnyMcpConfiguration()
        {
            // The bridge is the transport that needs nothing beyond an add-in, and asking for
            // it must not drag MCP settings along.
            using var session = FusionSessionFactory.For(FusionTransport.Bridge)();

            Assert.IsType<FusionHttpSession>(session);
        }

        // ------------------------------------------------------------------
        private static JsonElement Json(string text) =>
            JsonDocument.Parse(text).RootElement.Clone();

        private sealed class FakeChannel : IJsonRpcChannel
        {
            private readonly JsonElement _result;

            public FakeChannel(JsonElement result) => _result = result;

            public List<string> Methods { get; } = new();
            public List<string> Notifications { get; } = new();
            public string? LastToolName { get; private set; }

            public Task<JsonElement> SendAsync(string method, object? parameters, CancellationToken ct)
            {
                Methods.Add(method);

                if (method == "tools/call" && parameters is not null)
                {
                    // Round-trip through JSON so the anonymous type is read the way a server
                    // would read it, rather than by reflecting over C# property names.
                    var sent = JsonDocument.Parse(JsonSerializer.Serialize(parameters)).RootElement;
                    if (sent.TryGetProperty("name", out var n)) LastToolName = n.GetString();
                }

                return Task.FromResult(_result);
            }

            public Task NotifyAsync(string method, object? parameters, CancellationToken ct)
            {
                Notifications.Add(method);
                return Task.CompletedTask;
            }

            public void Dispose() { }
        }
    }
}
