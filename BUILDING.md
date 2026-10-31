# Building and validation

Use PowerShell, a Roslyn C# compiler (`csc.exe`, tested on Windows), a local Valheim installation, BepInEx 5, and Epic Loot. Dependencies are not redistributed.

Run `build.ps1` with all four explicit arguments:

- `ManagedPath`: your Valheim `valheim_Data/Managed` directory.
- `BepInExPath`: your mod profile's `BepInEx` directory.
- `EpicLootAssembly`: full path to `EpicLoot.dll`.
- `CompilerPath`: full path to Roslyn `csc.exe`.

The script builds the plugin and creates a Thunderstore ZIP in `artifacts/`.

## Tests

`tests/Tests.cs` is a standalone executable with `src/Policy.cs`.
`tests/StockTests.cs` additionally needs Newtonsoft.Json.
`tests/BountyRewardTests.cs` uses `src/Policy.cs`, `src/BountyRules.cs`, and Newtonsoft.Json.
`tests/CompatibilityTests.cs` uses `src/Compatibility.cs` and `src/ApiContract.cs`; pass the Epic Loot DLL path followed by dependency directories. Run each Epic Loot version in a separate process.

Stock, bounty, and assembly inspection tests were run on .NET 8. When using the game's netstandard Newtonsoft.Json assembly, compile against its matching reference assemblies and provide a .NET 8 runtimeconfig.json for the test executable. Never ship the game assemblies with test or release packages.

`src/ApiContract.cs` is the reviewed compatibility contract. Update it only after inspecting changes in the APIs and Harmony patch targets. Matching signatures do not prove future gameplay compatibility.

The 1.5 series has API/build checks against Epic Loot 0.15.1–0.15.4, 5,215 progression/stock checks, and 100,217 bounty checks. Full in-game verification remains pending. Existing tests also cover historical 0.5% probability behavior; current production defaults are 1% creatures, 5% bounties, and 5% vendor offers. Triple-coin bounties remain 20%.
