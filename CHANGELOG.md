# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this package adheres to Semantic Versioning.

## [1.0.5]

### Changed
- Re-signed under the renamed Unity organization: the package signature now names **zagora.dev** (previously p_zagora). No code changes.

## [1.0.4]

### Changed
- Published packages are signed with the zagora.dev Unity organization (Unity Package Manager CLI in the publish workflow), so Unity 6.3+ no longer warns that the package has no signature.

## [1.0.3]

### Changed
- Dependencies are declared as versions instead of git URLs, so they resolve from the zagora.dev UPM registry (`https://upm.zagora.dev`) and install automatically; UniTask comes from OpenUPM.
- Installation instructions use the registry.

## [1.0.2]

### Added
- `documentationUrl`, `changelogUrl` and `licensesUrl` in `package.json`, so Package Manager links to the README, CHANGELOG and LICENSE on GitHub.

## [1.0.1]

### Changed
- The Addressable Scopes debugger moved to **Tools ▸ Commons ▸ Addressable Scopes** (was Window ▸ Analysis), matching the other packages' `Tools/<Package>` menus.
- The Project Settings page moved under a shared **zagora.dev** group: **Project Settings ▸ zagora.dev ▸ Commons Addressables**.
- **Tools ▸ Commons ▸ Addressables Settings** opens the Project Settings page.

## [1.0.0]

### Added
- `AssetScope` — owns Addressables handles for a lifetime (`LoadAsync`, `LoadAllAsync`, `InstantiateAsync`, `Release`); `Dispose` releases everything it loaded, in reverse order. Failed or cancelled loads release their handle.
- `AddressablesContent` (`InitializeAsync`, `UpdateCatalogsAsync`, `GetDownloadSizeAsync`, `DownloadAsync`) and `AddressablesSteps` (`Initialize`, `UpdateCatalogs`, `Download`) for use as Commons.AsyncPipeline steps.
- Folder convention: assets under `<any folder>/Addressables/<Group>/…` become addressable in group `<Group>` with address `<Group>/<path>` and label `<Group>`, applied on import.
- Generated key constants (addresses and labels) as nested `const string` classes, regenerated when Addressables settings change.
- Build-time validation: missing assets, assets both addressable and in `Resources`, ambiguous addresses, convention drift, unmarked convention assets and optional duplicate bundle dependencies. Can fail the build.
- **Project Settings ▸ Commons Addressables** page for all of the above.
- **Window ▸ Analysis ▸ Addressable Scopes** — live view of every `AssetScope`, its owner and handles (with optional load stack traces), flagging scopes whose owner was destroyed without disposing them.
