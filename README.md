# MetaWear  SDK for C# by MBIENTLAB

[![Platforms](https://img.shields.io/badge/platform-win--32%20%7C%20win--64-lightgrey?style=flat)](https://github.com/mbientlab/MetaWear-SDK-CSharp)
[![License](https://img.shields.io/cocoapods/l/MetaWear.svg?style=flat)](https://github.com/mbientlab/MetaWear-SDK-CSharp/blob/master/LICENSE.md)
[![Version](https://img.shields.io/nuget/v/MetaWear.CSharp)](https://www.nuget.org/packages/MetaWear.CSharp)

![alt tag](https://raw.githubusercontent.com/mbientlab/MetaWear-SDK-iOS-macOS-tvOS/master/Images/Metawear.png)

SDK for creating MetaWear apps on the Windows platform. The SDK is distributed on NuGet as a platform agnostic, .NET Standard 2.0 assembly.  

Also, check out the scripts in the [examples](https://github.com/mbientlab/MetaWear-SDK-CSharp/tree/master/examples) folder for sample code.

> ADDITIONAL NOTES  
Due to its platform agnostic nature, developers will need to plugin their own BLE stack and file i/o code specific to their target environment.

### Overview

[MetaWear](https://mbientlab.com) is a complete development and production platform for wearable and connected device applications.

MetaWear features a number of sensors and peripherals all easily controllable over Bluetooth 4.0 Low Energy using this SDK, no firmware or hardware experience needed!

The MetaWear hardware comes pre-loaded with a wirelessly upgradeable firmware, so it keeps getting more powerful over time.

### Requirements
- [MetaWear board](https://mbientlab.com/store/)
- A linux or Windows 10+ machine with Bluetooth 4.0

### License
See the [License](https://github.com/mbientlab/MetaWear-SDK-CSharp/blob/master/LICENSE.md).

### Support
Reach out to the [community](https://mbientlab.com/community/) if you encounter any problems, or just want to chat :)

## Getting Started

### Installation

The C# SDK is distributed via NuGet and can be installed with the package manager console:  

```bat
PM> Install-Package MetaWear.CSharp
```

### Usage

MbientLab provides an implementation of the MetaWear API in the ``Impl`` namespace.  Similiarly to the top level interfaces, the main class in the ``Impl`` namespace 
is the ``MetaWearBoard`` class.  

To instantiate a ``MetaWearBoard`` object, you will need to provide an implementation of the 
[IBluetoothLeGatt](https://mbientlab.com/documents/metawear/csharp/1/interfaceMbientLab_1_1MetaWear_1_1Impl_1_1Platform_1_1IBluetoothLeGatt.html) and the 
[ILibraryIO](https://mbientlab.com/documents/metawear/csharp/1/interfaceMbientLab_1_1MetaWear_1_1Impl_1_1Platform_1_1ILibraryIO.html) interfaces:  

```csharp
using MbientLab.MetaWear.Impl.Platform;

class BluetoothLeGatt : IBluetoothLeGatt {
    // Implementation here
}

class IO : ILibraryIO {
    // Implementation here
}

var metawear = new MetaWearBoard(new BluetoothLeGatt(), new IO());
```

Once you have your ``IMetaWearBoard`` object, you can begin using the SDK features are described in the [developers' guide](https://mbientlab.com/csdocs/1/metawearboard.html).  

### Tutorials

Tutorials can be found [here](https://mbientlab.com/tutorials/).
