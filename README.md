# MicrosoftSolverFoundationReloaded
Last version of Microsoft Solver Foundation Express Edition decompiled and ported to .NET 2.0 Standard

Latest legacy version taken from: https://www.nuget.org/packages/Microsoft.Solver.Foundation and decompiled via ILSpy.

### Important
- The code were just changed to be compilable as .NET 2.0 Standrd class library.
- There is no unit test coverage as these tests are not included in the original library.
- There is just a functionality test with my custom scenario to verify that the original .NET Framework version and the ported .NET 2.0 Standard library produce the same output.
- I am not sure if this decompilation is legally allowed so if there are complains I can delete this repository. The last official version from "Microsoft.SolverFoundation" is from 2016. I also don't use this code in any commercial project.

### What was changed during porting?
- The class "CSharpWriter" was ported with Copilot to use "Microsoft.CodeAnalysis.CSharp" instead of unsupported "System.CodeDOM". No functionality test were done so no support. I also commented it out in v0.0.2 so I could drop the dependency to "Microsoft.CodeAnalysis.CSharp"
- Configuration classes "ConfigurationElement", "ConfigurationSection" and "ConfigurationPropertyAttribute" were replaced by stubs.
- License checks were disabled

### What to verify if you use this port
- Check the projects "Microsoft.SolverFoundation.ReferenceTests" and "Microsoft.SolverFoundation.Tests" to verify that your solver model compiles and works correctly