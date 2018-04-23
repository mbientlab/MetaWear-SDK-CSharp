# MetaWear C# SDK
SDK for creating MetaWear apps using C#.  

The SDK is distributed on NuGet as a platform agnostic, .NET Standard 2.0 assembly.  Due to its platform agnostic nature, developers will need to plugin their own 
BLE stack and file i/o code specific for their target environment by implementing the interfaces defined in the 
[MbientLab.MetaWear.Impl.Platform](https://mbientlab.com/documents/metawear/csharp/1/namespaceMbientLab_1_1MetaWear_1_1Impl_1_1Platform.html).

# Install
The C# SDK is distributed via NuGet and can be installed with the package manager console:  

```bat
PM> Install-Package MetaWear.CSharp
```

MbientLab has provided Windows 10 specific implementations of the aforementioned interfaces, which can be installed alongside the ``MetaWear.Csharp`` package.  

```bat
PM> Install-Package MetaWear.CSharp.Win10
```

# Usage
Developers using the ``MetaWear.Win10`` package can quickly get started by first retrieving the 
[BluetoothLEDevice](https://docs.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.bluetoothledevice) object corresponding to the desired BLE device, then 
calling **GetMetaWearBoard** to retrieve an **IMetaWearBoard** object corresponding to the device.

```csharp
public async IMetaWearBoard macAddrToIMetaWearBoard(ulong mac) {
    var device = await BluetoothLEDevice.FromBluetoothAddressAsync(mac);
    return MbientLab.MetaWear.Win10.Application.GetMetaWearBoard(device);
}
```

Developers only using the ``MetaWear`` package will need to implement the 
[IBluetoothLeGatt](https://mbientlab.com/documents/metawear/csharp/1/interfaceMbientLab_1_1MetaWear_1_1Impl_1_1Platform_1_1IBluetoothLeGatt.html) and 
[ILibraryIO](https://mbientlab.com/documents/metawear/csharp/1/interfaceMbientLab_1_1MetaWear_1_1Impl_1_1Platform_1_1ILibraryIO.html) interfaces, and pass 
those implementations to the **MetaWearBoard** constructor.  
