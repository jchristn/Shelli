<img src="https://github.com/jchristn/Shelli/raw/main/assets/icon.png" width="150" height="100">

# Shelli

Shelli is a simple static class to run things in your shell, tested on both Windows and Ubuntu.

 [![NuGet Version](https://img.shields.io/nuget/v/Shelli.svg?style=flat)](https://www.nuget.org/packages/Shelli/) [![NuGet](https://img.shields.io/nuget/dt/Shelli.svg)](https://www.nuget.org/packages/Shelli) 

## Usage
```csharp
using HeyShelli;
int returnCode = Shelli.Go("dir /w");
```
Want console output from the command that was executed?
```csharp
Shelli.OutputDataReceived = (s) => if (!String.IsNullOrEmpty(s)) Console.WriteLine(s);
Shelli.ErrorDataReceived = (s) => if (!String.IsNullOrEmpty(s)) Console.WriteLine(s);
```
Want to specify the shell used?
```csharp
Shelli.WindowsShell = "cmd.exe"; 
Shelli.LinuxShell = "sh";
```
## Need More Capabilities?

The library is designed to be really light with not much configuration.  If you have an enhancement, please feel free to either 1) file an issue, 2) submit a PR, or 3) simply clone and use the code as you see fit (MIT license).

## Special Thanks

Thanks to the authors that provided the free logo found here: https://www.clipartmax.com/middle/m2i8d3G6m2b1b1b1_conch-shell-free-icon-conch-icon/
