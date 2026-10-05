# Unity C# vs LuaJIT rules benchmark

- Machine: Apple M1 Max, macOS 27.0 arm64
- Unity: 6000.6.4f1
- Workload: 500 deterministic runs per sample, serializing complete state after every action
- Samples: 5 alternating LuaJIT/C# runs
- Correctness gate: 100 seeded runs produced identical state; the legacy edition crashed in 2 runs on the known Double Down issue

| Runtime | Median | Best | Relative |
|---|---:|---:|---:|
| LuaJIT 2.1 | 1.399 s | 1.139 s | 1.00x |
| C# / .NET 8 | 0.613 s | 0.571 s | 2.28x faster |

The C# executable uses the same engine-independent rule classes compiled into the Unity player. This measures rule simulation and serialization, not rendering, startup time, or Unity editor overhead. Raw samples are in `unity-vs-lua-rules.json`.
