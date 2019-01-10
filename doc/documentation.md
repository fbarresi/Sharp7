# Sharp7 Documentation
Sharp7 is the C# port of Snap7 Client. It’s not a wrapper, i.e. you don’t have an interface code that loads snap7.dll (or .so) but it’s a pure C# implementation of the S7Protocol.

Sharp7 is deployed as a single source file that contains some classes that you can use directly in your .NET project to communicate with S7 PLCs.

It’s designed to work with small hardware .NET-based or even for large projects which don’t needs of extended control functions.

## Main features

- Fully standard “safe managed” C# code without any dependencies.
- Virtually every hardware with an Ethernet adapter able to run a .NET Core can be connected to an S7 PLC.
- Packed protocol headers to improve performances.
- Helper class to access to all S7 types without worrying about Little-Big endian conversion.
- Built against .Net 4.0 and dotnet stardard 2.0

