> Upstream acknowledgment: [KdotJPG][kdotjpg-source] created OpenSimplex Noise.
> [digitalshadow][digitalshadow-source] produced the refactored C# port used by
> VoxelEngine and this package. The port remains available under its original
> Unlicense and public-domain dedication.

# Supprocom.OpenSimplexNoise

`Supprocom.OpenSimplexNoise` supplies deterministic OpenSimplex Noise for C#.
The public `OpenSimplexNoise` type evaluates two-dimensional, three-dimensional,
and four-dimensional coordinates.

The parameterless constructor uses the current clock value as its seed. The
`OpenSimplexNoise(long seed)` constructor gives the same output for the same seed.

## Installation

Use this command to install version 0.1.0.

```shell
dotnet add package Supprocom.OpenSimplexNoise --version 0.1.0
```

## Example

```csharp
using Supprocom.OpenSimplexNoise;

var noise = new OpenSimplexNoise(123456);
var height = noise.Evaluate(12.5, -4.25);
var density = noise.Evaluate(12.5, -4.25, 8.0);
var sample = noise.Evaluate(12.5, -4.25, 8.0, 0.5);
```

Package tests compare exact output bits with the authorized VoxelEngine source.
The tests include zero, negative, large, epsilon, and double-limit inputs.

## Source

The [public repository][repository] contains the preferred source form, tests,
build files, and package documents. This source remains available while the
binary package remains available.

## License

The digitalshadow source remains available under its original Unlicense and
public-domain dedication. Supprocom does not change that upstream license.

Supprocom publishes only its combined work and modifications under
AGPL-3.0-only. See [LICENSE.md](LICENSE.md) and
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for the complete terms and
notices.

[digitalshadow-source]: https://gist.github.com/digitalshadow/134a3a02b67cecd72181
[kdotjpg-source]: https://gist.github.com/KdotJPG/b1270127455a94ac5d19
[repository]: https://github.com/Supprocom/OpenSimplexNoise
