# MicrosoftSolverFoundationReloaded
Decompiled and ported to .Net2.0 Standard version of the Microsoft.Solver.Foundation

Latest legacy version taken from: https://www.nuget.org/packages/Microsoft.Solver.Foundation and decompiled via ILSpy.

### Important
- The code were just changed to be compilable as .NET 2.0 Standrd class library.
- There is no unit test coverage as these tests are not included in the original library.
- There is just a functionality test with my custom scenario to verify that the original .NET Framework version and the migrated .NET 2.0 Standard library produce the same output.
- I am not sure if this decompilation is legally allowed so if there are complains I can delete this repository. The last official version from "Microsoft.SolverFoundation" is from 2016. I also don't use this code in any commercial project.

### What was changed during migration?
- The class "CSharpWriter" was migrated with Copilot to use "Microsoft.CodeAnalysis.CSharp" instead of unsupported "System.CodeDOM". No functionality test were done so no support.
- Configuration classes "ConfigurationElement", "ConfigurationSection" and "ConfigurationPropertyAttribute" were replaced by stubs.
- License checks were disabled
