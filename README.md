
[![NuGet](https://img.shields.io/nuget/v/Reloaded.SolverFoundation.svg)](https://www.nuget.org/packages/Reloaded.SolverFoundation/)

# Microsoft Solver Foundation Express Edition for NET. Standard 2.0 / NET. Core
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

## Legal info
- The last known official version from "Microsoft.SolverFoundation" is "3.1.0" from "19.01.2017" licensed unter the MS-PL.
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
