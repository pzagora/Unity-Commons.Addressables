# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this package adheres to Semantic Versioning.

## [1.3.1]

### Added
- Package-owned EditMode tests for folder-convention parsing, generated-key identifier safety and deterministic output,
  and content-delivery environment routing.

## [1.3.0]

### Changed
- Requires Commons 3.0.0: the Editor assembly references Commons' new `Commons.Editor` assembly.

## [1.2.0]

### Added
- Player builds fail when remote content loads through Content Delivery but couldn't load: no Content Delivery
  settings asset, an empty URL for the environment the build uses (development or release; Local falls back to
  Development), or, with Remote Only, Addressables settings that don't match Content Delivery. Previously these only
  logged an error on the device.
- `-checkDuplicateDependencies` on the command line runs the duplicate bundle dependency check during build validation
  without turning it on in the settings (e.g. on CI).
- `ContentDeliverySettings.EnvironmentFor(bool developmentBuild)`.

## [1.1.2]

### Fixed
- Generated keys no longer contain empty classes for groups without entries (e.g. the read-only Built In Data group).

## [1.1.1]

### Fixed
- **Keys Path** set to a folder threw `UnauthorizedAccessException` when generating keys; a folder now gets
  `<Keys Class>.cs` written into it.

## [1.1.0]

### Added
- **Content delivery**: remote groups load from a base URL picked at runtime per `ContentEnvironment` (`Live`,
  `Development`, `Local`). `ContentDeliverySettings` holds the URLs and which environment the Editor, development builds
  and release builds use. The Live and Development URLs are set per project (none ship with the package; a missing one
  logs an error once); Local defaults to the project's `ServerData` folder in the Editor. `ContentDelivery.Override` switches environments before Addressables
  initialises.
- Project Settings ▸ zagora.dev ▸ Commons Addressables ▸ **Content delivery**: pick or create the settings asset (shipped
  as a Preloaded Asset), then **Apply Content Delivery Setup**: every profile's `Remote.LoadPath` (or the pre-1.19
  `RemoteLoadPath`) becomes `{Commons.Addressables.ContentDelivery.RemoteUrl}/[BuildTarget]`, the remote catalog builds
  to and loads from the remote paths, and the catalog name is set.
- **Remote Only**: keeps every group on the CDN (nothing built into the app); **Make All Groups Remote** converts
  existing groups, and validation fails builds with a local group.
- **Versioned Catalogs**: off, the catalog is `catalog_content` and each release overwrites it; on, it's
  `catalog_<Catalog Version>`, so older builds keep the content of their version.
- **Groups Allowed In App** (with Remote Only): groups validation lets build into the app.
- Apply Content Delivery Setup and Make All Groups Remote give remote groups a 30 s request timeout and 2 retries
  where none is set, so a stalled download can't hang loading forever.

### Changed
- `AddressablesContent.UpdateCatalogsAsync` (and `AddressablesSteps.UpdateCatalogs`) no longer throws when the catalog
  check or update fails (offline, CDN unreachable): it logs a warning and returns `false`, so the game boots on the
  catalogs it already has. Cancellation still throws.

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
