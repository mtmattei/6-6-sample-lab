# DevServer MCP startup, 6.5 vs 6.6

Measured on this machine, warm: both `uno.devserver` packages already in the
NuGet cache, repeated runs, no network fetch. `Measure-DevServer.ps1` drives the
real MCP stdio protocol and times from process start to the `tools/list`
response, which is what a client waits on before it can call a tool.

## Results

| Version | initialize | tools/list | tools returned |
|---|---|---|---|
| 6.6.166 | 1638 ms | **1667 ms** | 4 |
| 6.5.237 | varies with when you ask | **~9.2 s** | 24 |

6.5 bisected by holding the initialize back, to separate "when did I ask" from
"when could it answer":

| initialize sent at | initialize answered | tools/list answered |
|---|---|---|
| 2 s | 2463 ms | 9790 ms |
| 5 s | 5155 ms | 9147 ms |
| 9 s | 9059 ms | 9262 ms |
| 13 s | 13038 ms | 13096 ms |

`tools/list` converges on ~9.2 s no matter when it is requested, so that is 6.5's
readiness time rather than an artefact of the client.

## Why they differ

6.5 boots a DevServer on a port and then starts a separate MCP stdio proxy to
it, visible in its own output:

```
DevServer started on port 50731
Starting MCP stdio proxy to http://localhost:50731/mcp
```

6.6 serves MCP directly. The two-stage startup is the gap.

A single initialize sent at t=0 against 6.5 is consumed by the first transport
and never answered, which is why an unmodified client can appear to hang rather
than merely wait.

## Caveats before these numbers go in copy

- **Warm, not cold.** First-ever run with an empty NuGet cache pays download and
  JIT on top. If the "15 to 40 s" figure came from a cold machine, it is
  measuring something this benchmark does not.
- **Not apples to apples.** 6.5 returns 24 tools, 6.6 returns 4. With no app
  connected, 6.6's app tools are not listed yet, so "returning the tool list"
  does not mean the same thing in both.
