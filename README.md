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
