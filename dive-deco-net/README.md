DiveDeco .NET wrapper
======================

This project contains a small .NET 10 class library that wraps the C ABI exported by the `dive-deco-bridge` Rust crate using P/Invoke.

It requires the following repo in the same folder as the other two projects:

https://github.com/RalfMengwasser/dive-deco.git

Quick start (Windows):

1. Build the Rust bridge (from repository root):

```powershell
cd dive-deco-bridge
cargo build --release
```

2. Copy the produced native library into the sample app output folder or add it to PATH. On Windows the DLL will be in `target\release\` as `dive_deco_bridge.dll`.

3. Build and run the sample console app:

```powershell
cd ..\dive-deco-net\samples\ConsoleApp
dotnet build
dotnet run
```

Notes:
- The wrapper depends on the native `dive_deco_bridge` library. Make sure the native DLL is available to the runtime.
- The wrapper methods return JSON strings (or null). Strings are freed via the provided `FreeCString` function.