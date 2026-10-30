# Unity Commons — Addressables

An Addressables workflow for Unity Commons: handles owned by a lifetime, content-update steps for boot pipelines, folder-convention auto-addressing, generated keys, build validation, and a live debugger for finding leaks.

Its own assemblies (`Commons.Addressables`, editor tooling in `Commons.Addressables.Editor`). Depends on [Unity Commons](https://github.com/pzagora/Unity-Commons), `com.unity.addressables` and [UniTask](https://github.com/Cysharp/UniTask). Logs go through Unity Reporter when it is in the project and the Unity console otherwise.

## Installation

Install from the zagora.dev UPM registry — add the scoped registry (and OpenUPM, for UniTask) to `Packages/manifest.json`, then the package; its dependencies install automatically:

```json
"scopedRegistries": [
    { "name": "zagora.dev", "url": "https://upm.zagora.dev", "scopes": [ "com.zagoradev" ] },
    { "name": "OpenUPM", "url": "https://package.openupm.com", "scopes": [ "com.cysharp" ] }
],
"dependencies": {
  "com.zagoradev.unity.commons.addressables": "1.3.1"
}
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

## Content delivery

Remote groups load from a base URL picked at runtime, so the same content build can be served from several places:

| Environment | Base URL | Used by default by |
|---|---|---|
| `Live` | set per project, e.g. `https://cdn.example.com/MyGame/addressables/live` | release builds; only a release from Development publishes here |
| `Development` | set per project, e.g. `https://cdn.example.com/MyGame/addressables/development` | development builds; builds upload here |
| `Local` | the project's `ServerData` folder (Editor), or a configured URL such as a LAN server | the Editor |

1. In **Project Settings ▸ zagora.dev ▸ Commons Addressables ▸ Content delivery**, create a settings asset and set this
   project's Live and Development URLs (the package ships none; the page warns while they're empty). It also sets which
   environment the Editor, development builds and release builds use. The asset can live anywhere: it ships as a
   Preloaded Asset.
2. Choose how content ships:
   - **Remote Only**: every group builds to and loads from the remote paths, so the app carries no content: it downloads
     what it needs from the CDN, keeps it in the device cache and downloads a bundle again only when it changed there.
     Validation fails a build that would put content into the app; **Make All Groups Remote** fixes existing groups.
     A device needs a connection the first time it loads content. Groups listed under **Groups Allowed In App** may
     stay local (they can't be updated without a new build).
   - **Versioned Catalogs**: off (the default), the remote catalog is `catalog_content` and every release overwrites it,
     so all installed builds get the newest content. On, it's `catalog_<Catalog Version>`: raise the version when content
     starts needing code that older builds lack, and those builds keep loading the last content of their version.
3. Click **Apply Content Delivery Setup**. It sets every profile's `Remote.LoadPath` (`RemoteLoadPath` in projects
   created before Addressables 1.19) to `{Commons.Addressables.ContentDelivery.RemoteUrl}/[BuildTarget]`, which
   Addressables evaluates at runtime, sets the catalog name (Player Version Override), turns the remote catalog on with
   Remote Only and points it at the remote paths, and gives remote groups a 30 s request timeout and 2 retries where
   none is set. `Remote.BuildPath` stays `ServerData/[BuildTarget]`, the folder that gets uploaded. The page and validation warn when the Addressables settings drift from this.

A missing URL logs one error when remote content first loads from that environment. A failed catalog check (offline,
CDN unreachable) is a warning: the game keeps the catalogs it has.

To test against another environment, e.g. from a debug menu, switch before Addressables initialises:

```csharp
ContentDelivery.Override(ContentEnvironment.Live);
```

In the Editor, Local content is read in Play mode with the **Use Existing Build** play mode script, after building the
content (**Window ▸ Asset Management ▸ Addressables ▸ Groups ▸ Build**).

## Editor tooling

Configured in **Project Settings ▸ zagora.dev ▸ Commons Addressables** (shortcut: **Tools ▸ Commons ▸ Addressables Settings**).

- **Folder convention.** Any asset under `<any folder>/Addressables/<Group>/…` becomes addressable in group `<Group>`, with address `<Group>/<path below the group>` (no extension) and label `<Group>`. The folder can sit anywhere (`Art/Addressables/UI/…` works); the innermost `Addressables` segment wins. Scripts and anything under `Editor` or `Resources` are skipped. New groups copy the default group's schemas.
- **Generated keys.** Set a keys path (a `.cs` file, or a folder to write `<Keys Class>.cs` into) to generate `const string` addresses and labels (`AddressableKeys.UI.Icons.Coin`, `AddressableKeys.Labels.UI`). Put the file inside the assembly that uses it; a file outside any `.asmdef` lands in Assembly-CSharp, which asmdef code can't reference.
- **Build validation.** Runs before every player build: missing assets and Resources duplicates are errors (they can fail the build); convention drift, ambiguous addresses, unmarked convention assets and optional duplicate bundle dependencies are warnings. Pass `-checkDuplicateDependencies` on the command line (e.g. on CI) to run the duplicate check without turning it on in the settings. When remote content loads through Content Delivery, a player build also fails without a Content Delivery settings asset, without a URL for the environment that build uses, or (Remote Only) with Addressables settings that don't match Content Delivery.

## Addressable Scopes debugger

**Tools ▸ Commons ▸ Addressable Scopes** lists every live `AssetScope`: owner, handle count, age, and each handle's key, type, status and whether it is an instance. Turn on **Capture Load Stacks** to record where each handle was loaded. **Suspected Leaks Only** shows scopes whose owner was destroyed without disposing them, and **By Key** aggregates handles across scopes. Scopes still listed after leaving Play mode were never disposed.

Unity's own Addressables Profiler module (Window ▸ Analysis ▸ Profiler) shows bundles and reference counts; this window adds *who* holds them.

## Tests

Package-owned EditMode tests under `Tests/Editor` cover convention-path parsing and exclusions, deterministic generated
C# keys (safe identifiers, collisions and escaping), and content-delivery environment/URL selection. Add
`com.zagoradev.unity.commons.addressables` to the project's `testables` array, then run them with Unity Test Framework:

```bash
Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml
```
