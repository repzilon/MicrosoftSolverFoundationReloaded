
[![NuGet](https://img.shields.io/nuget/v/Reloaded.SolverFoundation.svg)](https://www.nuget.org/packages/Reloaded.SolverFoundation/)

# Microsoft Solver Foundation Express Edition for NET. Standard 2.0/NET. Core
Last official version of "Microsoft.SolverFoundation" ported to .NET Standard 2.0 / NET. Core.

## Overview
- Latest legacy version was taken from [Nuget package "Microsoft.Solver.Foundation"](https://www.nuget.org/packages/Microsoft.Solver.Foundation)  and decompiled via ILSpy
- The code were just changed to be compilable as .NET Standard 2.0 class library.
- There is no unit test coverage as these tests were not included in the original library.
- There is just a functionality test with my custom scenario to verify that the original .NET Framework version produce the same output as the ported .NET 2.0 Standard library one.

## What was changed during porting?
- The class [CSharpWriter](https://github.com/Ralf1108/MicrosoftSolverFoundationReloaded/blob/main/src/Microsoft.SolverFoundation/Services/CSharpWriter.cs) was commented out as it required "System.CodeDOM" from .NET Framework and was only used for "ExportModelToCSharp()" which was never called. So this method now throws a NotSupportedException.
- Configuration classes "ConfigurationElement", "ConfigurationSection" and "ConfigurationPropertyAttribute" were replaced by stubs. It belongs to the configuration and it seems it doesn't have an effect in the main logic.
- That't it. The rest of the code was just compiled and worked for my use case.

## What to check if you use this port
- Run your model via .NET Framework and this port to verify correctness.
- You can use the projects [Microsoft.SolverFoundation.ReferenceTests](https://github.com/Ralf1108/MicrosoftSolverFoundationReloaded/tree/main/src/Microsoft.SolverFoundation.ReferenceTests) and [Microsoft.SolverFoundation.Tests](https://github.com/Ralf1108/MicrosoftSolverFoundationReloaded/tree/main/src/Microsoft.SolverFoundation.Tests) to verify your solver model.

## Benchmark
Running the custom scenario in .NET 9.0 gives a nice 30% performance improvement over .NET 4.7.2. Benchmark project is included.
```
BenchmarkDotNet v0.14.0, Windows 10 (10.0.19045.4529/22H2/2022Update)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
  [Host]               : .NET Framework 4.8.1 (4.8.9241.0), X64 RyuJIT VectorSize=256
  .NET 6.0             : .NET 6.0.36 (6.0.3624.51421), X64 RyuJIT AVX2
  .NET 7.0             : .NET 7.0.20 (7.0.2024.26716), X64 RyuJIT AVX2
  .NET 8.0             : .NET 8.0.12 (8.0.1224.60305), X64 RyuJIT AVX2
  .NET 9.0             : .NET 9.0.1 (9.0.124.61010), X64 RyuJIT AVX2
  .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9241.0), X64 RyuJIT VectorSize=256
```
| Method | Job                  | Runtime              | Mean     | Error    | StdDev   | Ratio | Gen0    | Gen1    | Allocated | Alloc Ratio |
|------- |--------------------- |--------------------- |---------:|---------:|---------:|------:|--------:|--------:|----------:|------------:|
| Solve  | .NET 6.0             | .NET 6.0             | 684.3 μs |  5.24 μs |  4.37 μs |  0.83 | 52.7344 | 25.3906 | 305.87 KB |        0.99 |
| Solve  | .NET 7.0             | .NET 7.0             | 678.9 μs |  5.42 μs |  5.07 μs |  0.82 | 49.8047 | 48.8281 | 305.84 KB |        0.99 |
| Solve  | .NET 8.0             | .NET 8.0             | 589.6 μs | 10.70 μs | 10.01 μs |  0.71 | 54.6875 | 31.2500 |  305.4 KB |        0.99 |
| Solve  | .NET 9.0             | .NET 9.0             | 570.0 μs |  6.19 μs |  5.79 μs |  0.69 | 55.6641 | 40.0391 | 305.26 KB |        0.99 |
| Solve  | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 825.3 μs |  6.45 μs |  6.03 μs |  1.00 | 58.5938 | 28.3203 |  309.1 KB |        1.00 |


## Legal info
- The last known official library version from "Microsoft.Solver.Foundation" is "3.0.2", targeting .NET Framework 4.0 from and licensed unter the MS-PL.
- The last known official [Nuget package](https://www.nuget.org/packages/Microsoft.Solver.Foundation) from "Microsoft.Solver.Foundation" is "3.1.0" from "19.01.2017".
- Here is the ChatGPT answer of my question "Is decompilation and recompilation of MS-PL licensed binaries allowed?". So I assume there is no copyright violation:

Yes, the **Microsoft Public License (Ms-PL)** allows **decompilation, modification, and recompilation** of binaries, but with certain conditions.

#### **Key Points of Ms-PL Regarding Decompilation & Recompilation**
✅ **You Can:**
-   **Decompile** the software to inspect, modify, or enhance it.
-   **Modify** the source code and create derivative works.
-   **Recompile** and distribute modified versions.
 
🚫 **But You Must Follow These Rules:**
1.  **No Warranty or Liability**    
    -   The license **disclaims all warranties** and does not provide liability protection.
2.  **Preserve the License for Distribution**    
    -   If you **redistribute** the modified software in **source or binary form**, it **must still be licensed under Ms-PL**.
    -   You **cannot change the license** to a more restrictive one (e.g., GPL) but can combine Ms-PL code with other permissively licensed code.
3.  **No Trademark or Branding Rights**    
    -   You cannot use Microsoft's trademarks, logos, or branding **without permission** when redistributing modified versions.
#### **Practical Example**
-   If you decompile a **Microsoft Solver Foundation Express** binary, modify it, and recompile it, you **must** distribute it under **Ms-PL** and cannot claim it is an official Microsoft product.
#### **Conclusion**
Yes, **decompilation and recompilation are allowed**, provided you **adhere to Ms-PL terms**—especially regarding licensing and disclaimers. 🚀
