# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this package adheres to Semantic Versioning.

## [1.0.0]

### Added
- `AssetScope` — owns Addressables handles for a lifetime (`LoadAsync`, `LoadAllAsync`, `InstantiateAsync`, `Release`); `Dispose` releases everything it loaded, in reverse order. Failed or cancelled loads release their handle.
- `AddressablesContent` (`InitializeAsync`, `UpdateCatalogsAsync`, `GetDownloadSizeAsync`, `DownloadAsync`) and `AddressablesSteps` (`Initialize`, `UpdateCatalogs`, `Download`) for use as Commons.AsyncPipeline steps.
- Folder convention: assets under `<any folder>/Addressables/<Group>/…` become addressable in group `<Group>` with address `<Group>/<path>` and label `<Group>`, applied on import.
- Generated key constants (addresses and labels) as nested `const string` classes, regenerated when Addressables settings change.
- Build-time validation: missing assets, assets both addressable and in `Resources`, ambiguous addresses, convention drift, unmarked convention assets and optional duplicate bundle dependencies. Can fail the build.
- **Project Settings ▸ Commons Addressables** page for all of the above.
- **Window ▸ Analysis ▸ Addressable Scopes** — live view of every `AssetScope`, its owner and handles (with optional load stack traces), flagging scopes whose owner was destroyed without disposing them.
