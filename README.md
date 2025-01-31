[![NuGet](https://img.shields.io/nuget/v/Reloaded.SolverFoundation.svg)](https://www.nuget.org/packages/MyAwesomePackage/)

# MicrosoftSolverFoundationReloaded
Last version of Microsoft Solver Foundation Express Edition decompiled and ported to .NET 2.0 Standard

Latest legacy version taken from: https://www.nuget.org/packages/Microsoft.Solver.Foundation and decompiled via ILSpy.

### Important
- The code were just changed to be compilable as .NET 2.0 Standrd class library.
- There is no unit test coverage as these tests are not included in the original library.
- There is just a functionality test with my custom scenario to verify that the original .NET Framework version and the ported .NET 2.0 Standard library produce the same output.

### What was changed during porting?
- The class "CSharpWriter" was ported with Copilot to use "Microsoft.CodeAnalysis.CSharp" instead of unsupported "System.CodeDOM". No functionality test were done so no support. I also commented it out in v0.0.2 so I could drop the dependency to "Microsoft.CodeAnalysis.CSharp"
- Configuration classes "ConfigurationElement", "ConfigurationSection" and "ConfigurationPropertyAttribute" were replaced by stubs.
- License checks were disabled

### What to verify if you use this port
- Check the projects "Microsoft.SolverFoundation.ReferenceTests" and "Microsoft.SolverFoundation.Tests" to verify that your solver model compiles and works correctly

### Legal info
- The last official version from "Microsoft.SolverFoundation" is from 2016 licensed unter the MS-PL.

Here is the ChatGPT answer of my question "Is decompilation and recompilation of MS-PL licensed binaries allowed?". So I assume there is no copyright violation:
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
