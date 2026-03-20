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

- `Z9.Protobuf.Community` - .NET Standard 2.0 library with protobuf-generated classes, host-side connection management, and controller-side `SpCoreHostConnection`
- `Z9.Protobuf.Community.Test` - Unit tests

### Controller-Side Connection

`SpCoreHostConnection` handles the full controller-side lifecycle: TCP connection, optional TLS, identification, ping keepalive, message dispatch, and automatic reconnection with exponential backoff.

```csharp
public class MyController : SpCoreHostConnectionObserverBase
{
    private SpCoreHostConnection _connection;

    public void Start()
    {
        _connection = new SpCoreHostConnection(
            observer: this,
            hostAddress: "host.example.com",
            hostPort: 9723,
            controllerId: "00:11:22:33:44:55",  // lowercase MAC address
            softwareVersion: "1.0.0",
            sslSettings: mySslSettings,          // optional, implements ISslSettings
            softwareVersionProduct: "MyProduct", // optional
            password: "secret");                 // optional
        _connection.Start();
    }

    public override void OnOnline(SpCoreHostConnection connection, Identification hostIdentification)
    {
        // Called after full identification handshake completes
    }

    public override string OnDbChange(SpCoreHostConnection connection, DbChange dbChange)
    {
        // Process database change; return error string or null for success
        // DbChangeResp is sent automatically
        return null;
    }
}
```

Reconnection uses exponential backoff (5s initial, doubling to 160s max). The backoff resets only after successful identification — if TCP connects but TLS or identification fails, the delay continues increasing.

### Logging

The library uses [log4net](https://logging.apache.org/log4net/) internally. All classes obtain loggers via:

```csharp
private static readonly log4net.ILog Logger =
    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
```

The library does **not** configure log4net itself — the consuming application must configure it. Without configuration, all library log output (connection lifecycle, TLS handshake, exponential backoff, message dispatch) is silently discarded.

To enable logging, configure log4net in your application startup. For example, a simple file appender:

```csharp
var layout = new log4net.Layout.PatternLayout("%date %-5level %logger - %message%newline");
layout.ActivateOptions();

var appender = new log4net.Appender.FileAppender
{
    File = "z9open-library.log",
    Layout = layout,
    AppendToFile = true
};
appender.ActivateOptions();

log4net.Config.BasicConfigurator.Configure(
    log4net.LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly()),
    appender);
```

### Publishing to NuGet

Pushing a version tag triggers the CI pipeline to build, test, and publish the `Z9.Protobuf.Community` package to nuget.org:

Find the latest version and tag the next:

```bash
git tag --sort=-v:refname | head -1
```

```bash
git tag v1.0.2
git push origin v1.0.2
```

Note: This requires a `NUGET_API_KEY` secret configured in the repository's GitHub Actions settings.

Verify the package is published:

```bash
curl -s https://api.nuget.org/v3-flatcontainer/z9.protobuf.community/index.json
```
