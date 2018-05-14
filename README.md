# MetaWear C# SDK
SDK for creating MetaWear apps using C#.  

The SDK is distributed on NuGet as a platform agnostic, .NET Standard 2.0 assembly.  Due to its platform agnostic nature, developers will need to plugin their own 
BLE stack and file i/o code specific to their target environment.

# Install
The C# SDK is distributed via NuGet and can be installed with the package manager console:  

```bat
PM> Install-Package MetaWear.CSharp
```

# Usage
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