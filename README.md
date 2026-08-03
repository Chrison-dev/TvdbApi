# TvdbClient

[![NuGet](https://img.shields.io/nuget/v/TvdbClient.svg)](https://www.nuget.org/packages/TvdbClient/)
[![Downloads](https://img.shields.io/nuget/dt/TvdbClient.svg)](https://www.nuget.org/packages/TvdbClient/)
[![Build & Test](https://github.com/Chrison-dev/TvdbApi/actions/workflows/build.yml/badge.svg)](https://github.com/Chrison-dev/TvdbApi/actions/workflows/build.yml)
[![built with Fallout](https://img.shields.io/badge/built%20with-Fallout-F5C800?logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI%2BPGNpcmNsZSBjeD0iMTIiIGN5PSIxMiIgcj0iMTIiIGZpbGw9IiNGNUM4MDAiLz48ZyBmaWxsPSIjMTExIj48Y2lyY2xlIGN4PSIxMiIgY3k9IjEyIiByPSIzLjEiLz48cGF0aCBkPSJNOS45OSA5LjAyQTMuNiAzLjYgMCAwIDEgMTQuMDEgOS4wMkwxOC40OSAyLjM4QTExLjYgMTEuNiAwIDAgMCA1LjUxIDIuMzhaTTE1LjU5IDExLjc1QTMuNiAzLjYgMCAwIDEgMTMuNTggMTUuMjRMMTcuMDkgMjIuNDNBMTEuNiAxMS42IDAgMCAwIDIzLjU3IDExLjE5Wk0xMC40MiAxNS4yNEEzLjYgMy42IDAgMCAxIDguNDEgMTEuNzVMMC40MyAxMS4xOUExMS42IDExLjYgMCAwIDAgNi45MSAyMi40M1oiLz48L2c%2BPC9zdmc%2B)](https://github.com/Fallout-build/Fallout)
[![License](https://img.shields.io/github/license/Chrison-dev/TvdbApi.svg)](LICENSE)

A C# `HttpClient`-based API client for [TheTVDB](https://thetvdb.com) **v4 API**.

- [TheTVDB API](https://thetvdb.com) · [v4-api GitHub](https://github.com/thetvdb/v4-api) · [API docs](https://thetvdb.github.io/v4-api)

> This is a thin client **AS IS** — it does not implement the caching/proxying that
> TheTVDB recommends. Add your own caching layer if you call the API at volume.

## Packages

The client is split into three packages so the API-versioned models can be
regenerated independently of the generic client code:

| Package | What | Versioning |
|---|---|---|
| **`TvdbClient`** | Client core: generated clients, auth, DI wiring. | Generic SemVer |
| **`TvdbClient.Models`** | The generated DTOs (`Tvdb.Models`). | Tracks the TheTVDB API version (`Major.Minor`) |
| **`TvdbClient.Abstractions`** | Generic contracts, configuration, response envelope. | Generic SemVer |

Installing `TvdbClient` pulls in the other two.

```sh
dotnet add package TvdbClient
```

## Usage

### Configuration

Provide your TheTVDB API key (and optional subscriber PIN) via configuration —
either `appsettings.json` or a standalone `TvdbClientConfig.json`:

```json
{
  "TvdbConfiguration": {
    "BaseUrl": "https://api4.thetvdb.com/v4",
    "ApiKey": "<your-api-key>",
    "Pin": "<optional-subscriber-pin>"
  }
}
```

Get an API key from [TheTVDB's API Key dashboard](https://www.thetvdb.com/dashboard/account/apikey).

### DI registration

```csharp
using Microsoft.Extensions.DependencyInjection;

builder.Configuration.AddTvdbClient();
builder.Services.AddTvdbClient(builder.Configuration);
```

### Using the clients

Resolve the per-resource client you need. Login/token acquisition and the
`Authorization` header are handled automatically by the registered handler.

```csharp
using Tvdb.Clients;

var series = serviceProvider.GetRequiredService<ISeriesClient>();
var response = await series.SeriesGetAsync(121361);   // ids are long
var record = response.Data;                            // { data, status } envelope
```

## Building

This project builds with **[Fallout](https://github.com/Fallout-build/Fallout)**
(a NUKE fork) — the build lives in `build/Build.cs` and runs through the `./build.ps1`
bootstrapper (no global tool required). CI (`build.yml`) invokes the same targets.

```sh
./build.ps1 Test       # run the *.Specs test suite
./build.ps1 Pack       # produce the three NuGet packages in artifacts/packages
./build.ps1 Generate   # regenerate the client + models from TheTVDB's OpenAPI spec
```

## Regenerating the models

Models + clients are generated from TheTVDB's OpenAPI spec via the Fallout
`Generate` target (NSwag under the hood):

```sh
./build.ps1 Generate
```

This downloads the live v4 spec, applies a small overlay (integer-id coercion,
inline-enum hoisting), and regenerates `TvdbClient.Models` + the clients.

## Versioning

`Major.Minor` track the TheTVDB API version; the patch is this library's own
release counter. Managed with [GitVersion](https://gitversion.net) (`dotnet-gitversion`).

## License

[MIT](LICENSE)
