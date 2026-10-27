# Unity Commons — Addressables

An Addressables workflow for Unity Commons: handles owned by a lifetime, content-update steps for boot pipelines, folder-convention auto-addressing, generated keys, build validation, and a live debugger for finding leaks.

Its own assemblies (`Commons.Addressables`, editor tooling in `Commons.Addressables.Editor`). Depends on [Unity Commons](https://github.com/pzagora/Unity-Commons), `com.unity.addressables` and [UniTask](https://github.com/Cysharp/UniTask). Logs go through Unity Reporter when it is in the project and the Unity console otherwise.

## Installation

Add to `Packages/manifest.json` (its dependencies are declared in `package.json`):

```json
"com.zagoradev.unity.commons.addressables": "https://github.com/pzagora/Unity-Commons.Addressables.git"
```

Requires Unity `6000.0+`.

## AssetScope

Every handle loaded through a scope is released when the scope is disposed, so nothing outlives its owner:

```csharp
using Commons.Addressables;

private AssetScope _assets;

private void OnEnable() => _assets = new AssetScope(name, this);
private void OnDisable() => _assets.Dispose();

var icon = await _assets.LoadAsync<Sprite>(AddressableKeys.UI.Icons.Coin, ct: ct);
var enemy = await _assets.InstantiateAsync(AddressableKeys.Enemies.Goblin, parent, ct: ct);
_assets.Release(enemy); // early release is optional
```

Passing an owner lets the debugger flag scopes whose owner was destroyed without `Dispose`.

## Content updates in a boot pipeline

```csharp
using Commons.AsyncPipeline;
using Commons.Addressables;

await AsyncPipeline
    .Start("Boot")
    .Then("Addressables", AsyncPipeline.StepFor(AddressablesSteps.Initialize()))
    .Then("Catalogs",     AsyncPipeline.StepFor(AddressablesSteps.UpdateCatalogs()))
    .Then("Download",     AsyncPipeline.StepFor(AddressablesSteps.Download(AddressableKeys.Labels.Preload)), weight: 3f)
    .RunAsync(progress, ct);
```

`AddressablesContent` exposes the same operations directly, plus `GetDownloadSizeAsync` for "download N MB?" prompts.

## Editor tooling

Configured in **Project Settings ▸ Commons Addressables**.

- **Folder convention.** Any asset under `<any folder>/Addressables/<Group>/…` becomes addressable in group `<Group>`, with address `<Group>/<path below the group>` (no extension) and label `<Group>`. The folder can sit anywhere (`Art/Addressables/UI/…` works); the innermost `Addressables` segment wins. Scripts and anything under `Editor` or `Resources` are skipped. New groups copy the default group's schemas.
- **Generated keys.** Set a keys path to generate `const string` addresses and labels (`AddressableKeys.UI.Icons.Coin`, `AddressableKeys.Labels.UI`). Put the file inside the assembly that uses it; a file outside any `.asmdef` lands in Assembly-CSharp, which asmdef code can't reference.
- **Build validation.** Runs before every player build: missing assets and Resources duplicates are errors (they can fail the build); convention drift, ambiguous addresses, unmarked convention assets and optional duplicate bundle dependencies are warnings.

## Addressable Scopes debugger

**Window ▸ Analysis ▸ Addressable Scopes** lists every live `AssetScope`: owner, handle count, age, and each handle's key, type, status and whether it is an instance. Turn on **Capture Load Stacks** to record where each handle was loaded. **Suspected Leaks Only** shows scopes whose owner was destroyed without disposing them, and **By Key** aggregates handles across scopes. Scopes still listed after leaving Play mode were never disposed.

Unity's own Addressables Profiler module (Window ▸ Analysis ▸ Profiler) shows bundles and reference counts; this window adds *who* holds them.
