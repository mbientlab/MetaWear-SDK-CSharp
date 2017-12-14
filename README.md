# MetaWear C# SDK
SDK for creating MetaWear apps using C# 7.0 and Visual Studio 2017.  

Three builds of the C# SDK are available targeting:

* Universal Windows Platform (10.0.16299.0)
* .NET Framework (4.6.2)
* .NET Standard (2.0).  

Unlike the former two, the .NET Standard build is a platform agnostic API that does not contain any Bluetooth LE or IO code.  Developers using this build, such as in a 
Xamarin Forms project, will need to plugin their own Bluetooth LE library to implement the interfaces defined in the 
[Mbientlab.MetaWear.Platform](https://github.com/mbientlab/MetaWear-SDK-CSharp/tree/master/MetaWear.Platform) namespace.

# Install
The C# SDK is distributed via NuGet and can be installed with the package manager console:

```bat
PM> Install-Package MetaWear.CSharp
```

NuGet will automatically use the appropriate build for your project.  

# Usage
This section only applies to developers using the .NET Framework or UWP builds though developers adding their own Bluetooth LE and IO plugins may want tp setup their API 
in a similar manner.

Before you can use the SDK, first retrieve the [BluetoothLEDevice](https://docs.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.bluetoothledevice) object 
corresponding to your MetaWear device.  When you have found your device, call **GetMetaWearBoard** to retrieve an **IMetaWearBoard** reference for the device.

```csharp
public async IMetaWearBoard macAddrToIMetaWearBoard(ulong mac) {
    var device = await BluetoothLEDevice.FromBluetoothAddressAsync(mac);
    return MbientLab.MetaWear.Win10.Application.GetMetaWearBoard(device);
}
```

# Build 
The SDK code base is partitioned into various shared projects that are referenced by the dotnet, uwp, netstandard and test projects.  By default, MSBuild will build all 4 
projects.

Make sure that your C# compiler supports C# 7.0 features.

```bat
C:\Program Files (x86)\Microsoft Visual Studio\2017\Community>csc
Microsoft (R) Visual C# Compiler version 2.6.0.62329 (5429b35d)
Copyright (C) Microsoft Corporation. All rights reserved.
```

## Unit Tests
Unit tests are written with the NUnit unit-testing framework.  They can be run using the NUnit console runner (https://www.nuget.org/packages/NUnit.ConsoleRunner/).

```bat
nunit3-console.exe MetaWear.Test\bin\Debug\MetaWear.Test.dll
```