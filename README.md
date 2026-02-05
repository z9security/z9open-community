# Z9/Open Community Profile

This repository contains the Google Protobuf specification for the Z9/Open binary protocol - Community Profile.

## Overview

The Community Profile is a profile of Z9 Security's Z9/Open protocol, designed to be an efficient, binary socket communications between an access control panel and a host.

## See Also

- [Z9/Flex Community Profile](https://github.com/z9security/z9flex-community) - OpenAPI specification for the Z9/Flex JSON/REST API Community Profile

## Z9/Flex and Z9/Open

For more information about the commercial version of Z9/Flex, visit [z9flex.com](https://z9flex.com).

For more information about the commercial version of Z9/Open, visit [z9open.com](https://z9open.com).

Z9/FL=X is a registered trademark of Z9 Security. z9/op=n is a registered certification mark of Z9 Security.

## License

Apache 2.0 - see [LICENSE](LICENSE) for details.

## Files

- `SpCoreProto.proto` - Top-level protocol messages (identification, events, database changes, device actions)
- `SpCoreProtoData.proto` - Data model definitions (credentials, devices, schedules, etc.)
- `SpCoreProtoElements.proto` - Common element definitions
- `SpCoreProtoEnums.proto` - Enumeration definitions

## Usage

The .proto files can be used with Google Protobuf tools (protoc) to:
- Generate serialization/deserialization code in various languages, for either side of the communications (host, panel)

## .NET SDK

The `dotnet/` folder contains a C# SDK for Z9/Open Community Profile:

- `Z9.Protobuf.Community` - .NET Standard 2.0 library with protobuf-generated classes and host-side connection management
- `Z9.Protobuf.Community.Test` - Unit tests

### Publishing to NuGet

Pushing a version tag triggers the CI pipeline to build, test, and publish the `Z9.Protobuf.Community` package to nuget.org:

```bash
git tag v1.0.1
git push origin v1.0.1
```

Note: This is done by the repo owner and requires a `NUGET_API_KEY` secret configured in the repository's GitHub Actions settings.
