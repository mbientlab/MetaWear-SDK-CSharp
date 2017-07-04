# MetaWear C# SDK#
C# SDK for creating MetaWear apps on Windows 10.

# Install #
Two variants of the SDK are available on NuGet targeting both Universal Windows apps (10.0.15063.0) and .NET console applications (4.6.2).  NuGet will automatically add the correct assembly 
to your project depending on the application type.

First, use the package manager console to install the *MetaWear.CSharp* package:  

```bat
PM> Install-Package MetaWear.CSharp
```

Then, after you have obtained the [BluetoothLEDevice](https://docs.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.bluetoothledevice) object corresponding to your MetaWear device, call 
**GetMetaWearBoard** to retrieve an **IMetaWearBoard** reference for the device.

```csharp
public async IMetaWearBoard macAddrToIMetaWearBoard(ulong mac) {
    var device = await BluetoothLEDevice.FromBluetoothAddressAsync(mac);
    return MbientLab.MetaWear.Win10.Application.GetMetaWearBoard(device);
}
```

# Build #
The SDK code base is partitioned into various shared projects that are referenced by the dotnet, uwp, and test projects.  By default, MSBuild will build all 3 projects.

Make sure that your C# compiler supports C# 7.0 features.

```bat
C:\Program Files (x86)\Microsoft Visual Studio\2017\Community>csc
Microsoft (R) Visual C# Compiler version 2.2.0.61624
Copyright (C) Microsoft Corporation. All rights reserved.
```

## Unit Tests ##
Unit tests are written with the NUnit unit-testing framework.  They can be run using the NUnit console runner (https://www.nuget.org/packages/NUnit.ConsoleRunner/).

```bat
nunit3-console.exe MetaWear.Test\bin\Debug\MetaWear.Test.dll
```