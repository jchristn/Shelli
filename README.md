<img src="https://github.com/jchristn/Shelli/raw/main/assets/icon.png" width="150" height="100">

# Shelli

Shelli is a simple class library to run things in your shell, tested on both Windows, Ubuntu, and Mac.

 [![NuGet Version](https://img.shields.io/nuget/v/Shelli.svg?style=flat)](https://www.nuget.org/packages/Shelli/) [![NuGet](https://img.shields.io/nuget/dt/Shelli.svg)](https://www.nuget.org/packages/Shelli) 

## Usage
```csharp
using HeyShelli;

Shelli shell = new Shelli();

int returnCode = shell.Go("dir /w");
```
Want console output from the command that was executed?
```csharp
shell.OutputDataReceived = (s) => if (!String.IsNullOrEmpty(s)) Console.WriteLine(s);
shell.ErrorDataReceived = (s) => if (!String.IsNullOrEmpty(s)) Console.WriteLine(s);
```
Want to specify the shell used?
```csharp
shell.WindowsShell = "cmd.exe"; 
shell.LinuxShell = "sh";
```
Want to see what your commands are doing in Grafana, Prometheus, Tempo, or any OTLP backend?  Shelli emits metrics and traces through a `Meter` and `ActivitySource` both named `Shelli`.  Subscribe from your host and every `Go` call shows up as a `shelli exec` span (with `stage:start` and `stage:wait` children) plus duration, outcome, in-flight, and output-line metrics.  The command text is never recorded.
```csharp
settings.Sources.AddMeter(ShelliTelemetryNames.MeterName);                   // Radiant
settings.Sources.AddActivitySource(ShelliTelemetryNames.ActivitySourceName);
```
When a trace is active, Shelli passes it to the child process in the `TRACEPARENT` environment variable.  Turn that off with:
```csharp
shell.PropagateTraceContext = false;
```
See [TELEMETRY.md](https://github.com/jchristn/Shelli/blob/main/TELEMETRY.md) for the full metric and span catalog, PromQL, and recommended alerts.

## Need More Capabilities?

The library is designed to be really light with not much configuration.  If you have an enhancement, please feel free to either 1) file an issue, 2) submit a PR, or 3) simply clone and use the code as you see fit (MIT license).

## Version History

Please refer to [CHANGELOG.md](https://github.com/jchristn/Shelli/blob/main/CHANGELOG.md) for version history.

## Special Thanks

Thanks to the authors that provided the free logo found here: https://www.clipartmax.com/middle/m2i8d3G6m2b1b1b1_conch-shell-free-icon-conch-icon/
