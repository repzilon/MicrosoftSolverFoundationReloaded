using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
    /// <summary> Helper class for the solver plugin section in the configuration file
    /// </summary>
    internal sealed class PluginSolverCollection
	{
		private struct PluginSolverInfo
		{
			public Assembly _assembly;

			public string _directiveClassName;

			public Type _paramsType;

			public SolverRegistration _registration;

			public Type _solverType;

			public Type _interfaceType;
		}

		internal const string _gurobiSolverClassName = "SolverFoundation.Plugin.Gurobi.GurobiSolver";

		private const string _gurobiSolverAssemblyName = "GurobiPlugIn.dll";

		internal const string _gurobiSolverDirectiveName = "SolverFoundation.Plugin.Gurobi.GurobiDirective";

		private const string _gurobiSolverParamsClassName = "SolverFoundation.Plugin.Gurobi.GurobiParams";

		private const string _gurobiCutsEnumName = "SolverFoundation.Plugin.Gurobi.CutLevel";

		private const string _gurobiLpMethodEnumName = "SolverFoundation.Plugin.Gurobi.LPAlgorithm";

		private const string _gurobiPricingEnumName = "SolverFoundation.Plugin.Gurobi.SimplexPricing";

		private readonly Dictionary<SolverRegistration, PluginSolverInfo> _pluginSolversByRegistration;

		private readonly Dictionary<SolverCapability, List<PluginSolverInfo>> _pluginSolversByCapability;

		private bool _fIsInitialized;

		private string _pluginSolverPath;

		private static readonly Type[] _interfaceTypes = new Type[3]
		{
			typeof(ILinearSolver),
			typeof(INonlinearSolver),
			typeof(ITermSolver)
		};

		private Type _gurobiSolverType;

		private Type _gurobiDirectiveType;

		private Type _gurobiSolverParamsType;

		/// <summary>The number of registered solvers.
		/// </summary>
		public int Count => GetSolverRegistrations().Count();

		/// <summary>Determine if a registration entry exists.
		/// </summary>
		/// <param name="item">The solver registration information.</param>
		/// <returns>Returns true if the solver has been registered.</returns>
		public bool Contains(SolverRegistration item)
		{
			return GetSolverRegistrations().Contains(item);
		}

		/// <summary>All registration entries.
		/// </summary>
		/// <returns>All registration entries as an IEnumerable.</returns>
		public IEnumerable<SolverRegistration> GetSolverRegistrations()
		{
			return from info in _pluginSolversByCapability.SelectMany((KeyValuePair<SolverCapability, List<PluginSolverInfo>> k) => k.Value)
				select info._registration;
		}

		/// <summary>Return the Type of the directive associated with a registration entry.
		/// </summary>
		/// <returns>A Type object for the directive, or null if not found.</returns>
		public Type GetDirectiveType(SolverRegistration registration)
		{
			if (registration == null)
			{
				throw new ArgumentNullException("registration");
			}
			if (_pluginSolversByRegistration.TryGetValue(registration, out var value) && IsValidClass(value._assembly, value._directiveClassName, null, null, out var solverType))
			{
				return solverType;
			}
			return null;
		}

		/// <summary>Copy registration entries to an Array.
		/// </summary>
		/// <param name="array">The destination array.</param>
		/// <param name="arrayIndex">The start index.</param>
		public void CopyTo(SolverRegistration[] array, int arrayIndex)
		{
			GetSolverRegistrations().ToArray().CopyTo(array, arrayIndex);
		}

		/// <summary> Test if class exists in the assembly and implements correct interface or is a subclass of the given class
		/// </summary>
		private static bool IsValidClass(Assembly assembly, string className, string interfaceName, Type rootClassType, out Type solverType)
		{
			solverType = null;
			if (assembly == null)
			{
				return false;
			}
			try
			{
				solverType = assembly.GetType(className, throwOnError: false);
				if (solverType == null)
				{
					return false;
				}
				if (interfaceName != null && solverType.GetInterface(interfaceName) == null)
				{
					return false;
				}
				if (rootClassType != null && !solverType.IsSubclassOf(rootClassType) && solverType != rootClassType)
				{
					return false;
				}
				return true;
			}
			catch (FileLoadException)
			{
				return false;
			}
		}

		/// <summary>
		/// Load the solver assembly defined in reg
		/// </summary>
		/// <returns>true if and only if the assembly has not been loaded into the current AppDomain yet</returns>
		private bool LoadSolverAssembly(string solverClassName, string solverAssemblyName, out Assembly solverAssembly)
		{
			solverAssembly = null;
			try
			{
				solverAssembly = Assembly.Load(solverAssemblyName);
				return true;
			}
			catch (BadImageFormatException)
			{
			}
			catch (FileNotFoundException)
			{
			}
			catch (FileLoadException)
			{
			}
			try
			{
				solverAssembly = Assembly.LoadFrom(solverAssemblyName);
				return true;
			}
			catch (BadImageFormatException)
			{
			}
			catch (SecurityException)
			{
			}
			catch (FileNotFoundException)
			{
			}
			catch (FileLoadException)
			{
			}
			catch (PathTooLongException)
			{
			}
			try
			{
				string assemblyFile = Path.Combine(_pluginSolverPath, solverAssemblyName);
				solverAssembly = Assembly.LoadFrom(assemblyFile);
				return true;
			}
			catch (BadImageFormatException)
			{
			}
			catch (SecurityException)
			{
			}
			catch (FileNotFoundException)
			{
			}
			catch (FileLoadException)
			{
			}
			catch (PathTooLongException)
			{
			}
			return false;
		}

		private string GetPluginSolverPathFromRegistry()
		{
			_pluginSolverPath = "";
			return _pluginSolverPath;
			// SKIPPED
			//try
			//{
			//	using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Microsoft Solver Foundation", writable: false))
			//	{
			//		if (registryKey != null && registryKey.GetValueKind("PluginDirectory") == RegistryValueKind.String)
			//		{
			//			_pluginSolverPath = (string)registryKey.GetValue("PluginDirectory");
			//			if (!_pluginSolverPath.EndsWith("\\", StringComparison.Ordinal))
			//			{
			//				_pluginSolverPath += "\\";
			//			}
			//		}
			//		if (_pluginSolverPath == null)
			//		{
			//			return _pluginSolverPath = "";
			//		}
			//	}
			//}
			//catch (SecurityException)
			//{
			//}
			//catch (ObjectDisposedException)
			//{
			//}
			//catch (IOException)
			//{
			//}
			//catch (UnauthorizedAccessException)
			//{
			//}
			//return _pluginSolverPath;
		}

		private void InitializeGurobiSolverType()
		{
			LoadSolverAssembly("SolverFoundation.Plugin.Gurobi.GurobiSolver", "GurobiPlugIn.dll", out var solverAssembly);
			if (!IsValidClass(solverAssembly, "SolverFoundation.Plugin.Gurobi.GurobiSolver", typeof(ISolver).FullName, null, out _gurobiSolverType) || !IsValidClass(solverAssembly, "SolverFoundation.Plugin.Gurobi.GurobiDirective", null, typeof(Directive), out _gurobiDirectiveType) || !IsValidClass(solverAssembly, "SolverFoundation.Plugin.Gurobi.GurobiParams", typeof(ISolverParameters).FullName, null, out _gurobiSolverParamsType))
			{
				_gurobiSolverType = null;
				_gurobiDirectiveType = null;
				_gurobiSolverParamsType = null;
			}
		}

		private ISolver GetGurobiSolver(ISolverEnvironment context)
		{
			if (_gurobiSolverType != null)
			{
				return GetPluginSolver(_gurobiSolverType, context);
			}
			return null;
		}

		/// <summary> Create an ISolver instance from the given registration info
		/// </summary>
		private static ISolver GetPluginSolver(Type solverType, ISolverEnvironment context)
		{
			try
			{
				return Activator.CreateInstance(solverType, context) as ISolver;
			}
			catch (ArgumentNullException ex)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex);
			}
			catch (ArgumentException ex2)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex2);
			}
			catch (NotSupportedException ex3)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex3);
			}
			catch (TargetInvocationException ex4)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex4);
			}
			catch (MethodAccessException ex5)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex5);
			}
			catch (MissingMemberException ex6)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex6);
			}
			catch (MemberAccessException ex7)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex7);
			}
			catch (InvalidComObjectException ex8)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex8);
			}
			catch (COMException ex9)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex9);
			}
			catch (TypeLoadException ex10)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex10);
			}
		}

		/// <summary> Decide which Solver Foundation solver is suitable for capability 
		///           given no directive
		/// </summary>
		private IEnumerable<Tuple<ISolver, Type>> GetSolverFoundationSolver(SolverCapability cap, ISolverEnvironment context, SolverCapabilityFlags flags)
		{
			return GetSolverFoundationSolver(cap, new Directive(), context, flags);
		}

		/// <summary> Check if the solver has been registered with the given capability and interface type.
		/// </summary>
		private bool HasRegistered(SolverCapability cap, Type solverType, Type interfaceType, out PluginSolverInfo? solverInfo)
		{
			int num = _pluginSolversByCapability[cap].FindIndex((PluginSolverInfo target) => target._solverType == solverType && target._interfaceType == interfaceType);
			if (num < 0)
			{
				solverInfo = null;
				return false;
			}
			solverInfo = _pluginSolversByCapability[cap][num];
			return true;
		}

		/// <summary> Validate if the access to this instance is OK.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">Thrown when the instance is not initialized</exception>
		private void ValidateAccess()
		{
			if (!_fIsInitialized)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverCollectionUninitialized);
			}
		}

		/// <summary> Create a plugin solver collection class instance.
		/// </summary>
		/// <remarks>Must call Initialize() before using the instance</remarks>
		private PluginSolverCollection()
		{
			_fIsInitialized = false;
			_pluginSolversByCapability = new Dictionary<SolverCapability, List<PluginSolverInfo>>();
			_pluginSolversByRegistration = new Dictionary<SolverRegistration, PluginSolverInfo>();
			foreach (SolverCapability value in Enum.GetValues(typeof(SolverCapability)))
			{
				_pluginSolversByCapability.Add(value, new List<PluginSolverInfo>());
			}
		}

		/// <summary> Initialize the instance by preloading the registered assemblies and gathering the type info.
		/// </summary>
		private void Initialize()
		{
			GetPluginSolverPathFromRegistry();
			foreach (ConfigMsfPluginSolverElement item2 in GetPluginSolverSection())
			{
				SolverRegistration item = new SolverRegistration(item2);
				AddPluginSolver(item);
			}
			InitializeGurobiSolverType();
			_fIsInitialized = true;
		}

		public void AddPluginSolver(SolverRegistration item)
		{
			PluginSolverInfo pluginSolverInfo = default(PluginSolverInfo);
			pluginSolverInfo._registration = item;
			LoadSolverAssembly(item.SolverClassName, item.AssemblyName, out pluginSolverInfo._assembly);
			if (pluginSolverInfo._assembly == null)
			{
				throw new MsfSolverConfigurationException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownPluginAssembly, new object[1] { item.AssemblyName }));
			}
			if (!IsValidClass(pluginSolverInfo._assembly, item.SolverClassName, typeof(ISolver).FullName, null, out pluginSolverInfo._solverType))
			{
				throw new MsfSolverConfigurationException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownPluginSolverType, new object[2]
				{
					item.SolverClassName,
					typeof(ISolver).FullName
				}));
			}
			if (item.DirectiveClassName.Length == 0)
			{
				pluginSolverInfo._directiveClassName = typeof(Directive).FullName;
			}
			else
			{
				pluginSolverInfo._directiveClassName = item.DirectiveClassName;
			}
			Type[] interfaceTypes = _interfaceTypes;
			foreach (Type type in interfaceTypes)
			{
				if (item.InterfaceName == type.FullName)
				{
					pluginSolverInfo._interfaceType = type;
					break;
				}
			}
			if (pluginSolverInfo._interfaceType == null)
			{
				throw new MsfSolverConfigurationException(string.Format(CultureInfo.InvariantCulture, Resources.SolverInterfaceNotSupported, new object[1] { item.InterfaceName }));
			}
			if (!IsValidClass(pluginSolverInfo._assembly, item.ParameterClassName, typeof(ISolverParameters).FullName, null, out pluginSolverInfo._paramsType))
			{
				throw new MsfSolverConfigurationException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownPluginSolverParameterType, new object[1] { item.ParameterClassName }));
			}
			if (HasRegistered(item.Capability, pluginSolverInfo._solverType, pluginSolverInfo._interfaceType, out var _))
			{
				throw new MsfSolverConfigurationException(string.Format(CultureInfo.InvariantCulture, Resources.PluginSolverInconsistentRegistration, new object[1] { pluginSolverInfo._solverType.FullName }));
			}
			_pluginSolversByCapability[pluginSolverInfo._registration.Capability].Add(pluginSolverInfo);
			_pluginSolversByRegistration.Add(pluginSolverInfo._registration, pluginSolverInfo);
		}

		/// <summary> Decide which Solver Foundation solver is suitable given the capability and the directive
		/// </summary>
		internal IEnumerable<Tuple<ISolver, Type>> GetSolverFoundationSolver(SolverCapability cap, Directive dir, ISolverEnvironment context, SolverCapabilityFlags flags)
		{
			bool flag = true;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			bool flag6 = false;
			bool flag7 = false;
			bool flag8 = false;
			bool flag9 = false;
			if (dir != null)
			{
				string fullName = dir.GetType().FullName;
				flag = fullName == typeof(Directive).FullName;
				flag2 = fullName == typeof(SimplexDirective).FullName;
				flag3 = fullName == typeof(MixedIntegerProgrammingDirective).FullName;
				flag4 = fullName == "SolverFoundation.Plugin.Gurobi.GurobiDirective";
				flag5 = fullName == typeof(InteriorPointMethodDirective).FullName;
				flag6 = fullName == typeof(ConstraintProgrammingDirective).FullName;
				flag7 = fullName == typeof(NelderMeadDirective).FullName;
				flag8 = fullName == typeof(CompactQuasiNewtonDirective).FullName;
				flag9 = fullName == typeof(HybridLocalSearchDirective).FullName;
			}
			ISolver gurobi = null;
			if (flag2)
			{
				switch (cap)
				{
				case SolverCapability.LP:
				case SolverCapability.MILP:
					return new Tuple<ISolver, Type>[1] { Tuple.Create((ISolver)new SimplexSolver(), typeof(ILinearSolver)) };
				default:
					return new Tuple<ISolver, Type>[0];
				}
			}
			if (flag3)
			{
				switch (cap)
				{
				case SolverCapability.LP:
				case SolverCapability.MILP:
					gurobi = GetGurobiSolver(context);
					if (gurobi != null)
					{
						return new Tuple<ISolver, Type>[1] { Tuple.Create(gurobi, typeof(ILinearSolver)) };
					}
					return new Tuple<ISolver, Type>[1] { Tuple.Create((ISolver)new SimplexSolver(), typeof(ILinearSolver)) };
				default:
					return Enumerable.Empty<Tuple<ISolver, Type>>();
				}
			}
			if (flag4)
			{
				switch (cap)
				{
				case SolverCapability.LP:
				case SolverCapability.QP:
				case SolverCapability.MILP:
				case SolverCapability.MIQP:
					gurobi = GetGurobiSolver(context);
					if (gurobi != null)
					{
						return new Tuple<ISolver, Type>[1] { Tuple.Create(gurobi, typeof(ILinearSolver)) };
					}
					return Enumerable.Empty<Tuple<ISolver, Type>>();
				default:
					return Enumerable.Empty<Tuple<ISolver, Type>>();
				}
			}
			if (flag5)
			{
				switch (cap)
				{
				case SolverCapability.LP:
				case SolverCapability.QP:
					return new Tuple<ISolver, Type>[1] { Tuple.Create((ISolver)new InteriorPointSolver(), typeof(ILinearSolver)) };
				default:
					return Enumerable.Empty<Tuple<ISolver, Type>>();
				}
			}
			if (flag6)
			{
				if (cap == SolverCapability.CP)
				{
					return new Tuple<ISolver, Type>[1] { Tuple.Create((ISolver)new ConstraintSystem(), typeof(ConstraintSystem)) };
				}
				return Enumerable.Empty<Tuple<ISolver, Type>>();
			}
			if (flag8)
			{
				if (cap == SolverCapability.NLP)
				{
					return new Tuple<ISolver, Type>[1] { Tuple.Create((ISolver)new CompactQuasiNewtonSolver(), typeof(INonlinearSolver)) };
				}
				return Enumerable.Empty<Tuple<ISolver, Type>>();
			}
			if (flag7)
			{
				if (cap == SolverCapability.NLP)
				{
					return new Tuple<ISolver, Type>[1] { Tuple.Create((ISolver)new NelderMeadSolver(), typeof(INonlinearSolver)) };
				}
				return Enumerable.Empty<Tuple<ISolver, Type>>();
			}
			if (flag9)
			{
				switch (cap)
				{
				case SolverCapability.NLP:
				case SolverCapability.MINLP:
					return new Tuple<ISolver, Type>[1] { Tuple.Create((ISolver)new HybridLocalSearchSolver(), typeof(ITermSolver)) };
				default:
					return Enumerable.Empty<Tuple<ISolver, Type>>();
				}
			}
			if (!flag)
			{
				return Enumerable.Empty<Tuple<ISolver, Type>>();
			}
			return GetSolverFoundationDefaultSolver(cap, context, ref gurobi);
		}

		private IEnumerable<Tuple<ISolver, Type>> GetSolverFoundationDefaultSolver(SolverCapability cap, ISolverEnvironment context, ref ISolver gurobi)
		{
			switch (cap)
			{
			case SolverCapability.LP:
				return new Tuple<ISolver, Type>[1] { Tuple.Create((ISolver)new SimplexSolver(), typeof(ILinearSolver)) };
			case SolverCapability.MILP:
				gurobi = GetGurobiSolver(context);
				if (gurobi != null)
				{
					return new Tuple<ISolver, Type>[1] { Tuple.Create(gurobi, typeof(ILinearSolver)) };
				}
				return new Tuple<ISolver, Type>[1] { Tuple.Create((ISolver)new SimplexSolver(), typeof(ILinearSolver)) };
			case SolverCapability.QP:
				return new Tuple<ISolver, Type>[1] { Tuple.Create((ISolver)new InteriorPointSolver(), typeof(ILinearSolver)) };
			case SolverCapability.MIQP:
				gurobi = GetGurobiSolver(context);
				if (gurobi != null)
				{
					return new Tuple<ISolver, Type>[1] { Tuple.Create(gurobi, typeof(ILinearSolver)) };
				}
				return Enumerable.Empty<Tuple<ISolver, Type>>();
			case SolverCapability.CP:
				return new Tuple<ISolver, Type>[1] { Tuple.Create((ISolver)new ConstraintSystem(), typeof(ConstraintSystem)) };
			case SolverCapability.NLP:
			case SolverCapability.MINLP:
				return new Tuple<ISolver, Type>[3]
				{
					Tuple.Create((ISolver)new NelderMeadSolver(), typeof(INonlinearSolver)),
					Tuple.Create((ISolver)new CompactQuasiNewtonSolver(), typeof(INonlinearSolver)),
					Tuple.Create((ISolver)new HybridLocalSearchSolver(), typeof(ITermSolver))
				};
			default:
				return Enumerable.Empty<Tuple<ISolver, Type>>();
			}
		}

		/// <summary>
		/// Return the parameter object for the given solver.
		/// </summary>
		internal ISolverParameters GetSolverParams(SolverCapability cap, Type solverInterface, ISolver solver, Directive directive)
		{
			try
			{
				ValidateAccess();
				if (HasRegistered(cap, solver.GetType(), solverInterface, out var solverInfo))
				{
					return Activator.CreateInstance(solverInfo.Value._paramsType, directive) as ISolverParameters;
				}
				if (solver is SimplexSolver)
				{
					return new SimplexSolverParams(directive);
				}
				if (solver is InteriorPointSolver)
				{
					return new InteriorPointSolverParams(directive);
				}
				if (solver is ConstraintSystem)
				{
					return new ConstraintSolverParams(directive);
				}
				if (solver.GetType() == _gurobiSolverType && _gurobiSolverParamsType != null && _gurobiDirectiveType != null)
				{
					return Activator.CreateInstance(_gurobiSolverParamsType, directive) as ISolverParameters;
				}
				if (solver is HybridLocalSearchSolver)
				{
					return new HybridLocalSearchParameters(directive);
				}
				if (solver is CompactQuasiNewtonSolver)
				{
					return new CompactQuasiNewtonSolverParams(directive);
				}
				if (solver is NelderMeadSolver)
				{
					return new NelderMeadSolverParams(directive);
				}
				return null;
			}
			catch (ArgumentNullException ex)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex);
			}
			catch (ArgumentException ex2)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex2);
			}
			catch (NotSupportedException ex3)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex3);
			}
			catch (TargetInvocationException ex4)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex4);
			}
			catch (MethodAccessException ex5)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex5);
			}
			catch (MissingMemberException ex6)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex6);
			}
			catch (MemberAccessException ex7)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex7);
			}
			catch (InvalidComObjectException ex8)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex8);
			}
			catch (COMException ex9)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex9);
			}
			catch (TypeLoadException ex10)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex10);
			}
			catch (TargetException ex11)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex11);
			}
			catch (AmbiguousMatchException ex12)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex12);
			}
			catch (InvalidOperationException ex13)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex13);
			}
		}

		/// <summary> Retrieve all registered solvers under MsfPluginSolvers section group
		/// </summary>
		/// <remarks>Current we do not support hosted environment</remarks>
		internal static IEnumerable<ConfigMsfPluginSolverElement> GetPluginSolverSection()
		{
			return new List<ConfigMsfPluginSolverElement>();
			// SKIPPED
			//Configuration config;
			//try
			//{
			//	if (CallContext.HostContext == null)
			//	{
			//		if (OperationContext.Current != null)
			//		{
			//			VirtualPathExtension virtualPathExtension = OperationContext.Current.Host.Extensions.Find<VirtualPathExtension>();
			//			config = ((virtualPathExtension == null) ? ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None) : WebConfigurationManager.OpenWebConfiguration(virtualPathExtension.VirtualPath));
			//		}
			//		else
			//		{
			//			config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			//		}
			//	}
			//	else if (CallContext.HostContext is HttpContext httpContext)
			//	{
			//		string applicationPath = httpContext.Request.ApplicationPath;
			//		config = WebConfigurationManager.OpenWebConfiguration(applicationPath);
			//	}
			//	else
			//	{
			//		config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			//	}
			//}
			//catch (FileNotFoundException)
			//{
			//	yield break;
			//}
			//catch (FileLoadException)
			//{
			//	yield break;
			//}
			//catch (PathTooLongException)
			//{
			//	yield break;
			//}
			//catch (ConfigurationErrorsException)
			//{
			//	yield break;
			//}
			//ConfigurationSection msfConfig = config.Sections["MsfConfig"];
			//if (!(msfConfig is MsfConfigSection csg))
			//{
			//	yield break;
			//}
			//foreach (ConfigMsfPluginSolverElement pluginSolver in csg.Solvers)
			//{
			//	if (pluginSolver != null)
			//	{
			//		yield return pluginSolver;
			//	}
			//}
		}

		/// <summary> Create a PluginSolverCollection instance and initialize it by preloading all registered solver assemblies and gathering necessary type info
		/// </summary>
		public static PluginSolverCollection CreatePluginSolverCollection()
		{
			try
			{
				PluginSolverCollection pluginSolverCollection = new PluginSolverCollection();
				pluginSolverCollection.Initialize();
				return pluginSolverCollection;
			}
			catch (ArgumentNullException ex)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex);
			}
			catch (ArgumentException ex2)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex2);
			}
			catch (BadImageFormatException ex3)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex3);
			}
			catch (SecurityException ex4)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex4);
			}
			catch (FileNotFoundException ex5)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex5);
			}
			catch (FileLoadException ex6)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex6);
			}
			catch (PathTooLongException ex7)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex7);
			}
			catch (ConfigurationErrorsException ex8)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex8);
			}
			catch (AmbiguousMatchException ex9)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverError, ex9);
			}
		}

		/// <summary> Return the default registered ISolver instance with the given capability (could be null)
		/// </summary>
		/// <param name="cap">Capability required</param>
		/// <param name="isModelStochastic">Is model Stochastic</param>
		/// <param name="dir">Directive</param>
		/// <param name="context">Solver context (cannot be null)</param>
		/// <param name="flags">Capability flags</param>
		/// <remarks>Return the registered solver such that, if we can find a registered solver that has the directive class 
		/// equals the type of dir, we return that solver; otherwise, we return the first registered solver with the capability. If
		/// no such solvers are registered, we return our solvers.
		/// </remarks>
		public IEnumerable<Tuple<ISolver, Type>> GetSolvers(SolverCapability cap, bool isModelStochastic, Directive dir, ISolverEnvironment context, SolverCapabilityFlags flags)
		{
			ValidateAccess();
			if (context == null)
			{
				throw new ArgumentNullException("context", Resources.SolverContextCannotBeNull);
			}
			if (dir != null && dir.GetType().FullName != typeof(Directive).FullName)
			{
				bool nonDefaultSolverFound = false;
				for (int i = 0; i < _pluginSolversByCapability[cap].Count; i++)
				{
					PluginSolverInfo solverInfo = _pluginSolversByCapability[cap][i];
					if (solverInfo._directiveClassName == dir.GetType().FullName)
					{
						nonDefaultSolverFound = true;
						yield return Tuple.Create(GetPluginSolver(solverInfo._solverType, context), solverInfo._interfaceType);
					}
				}
				if (nonDefaultSolverFound)
				{
					yield break;
				}
				IEnumerable<Tuple<ISolver, Type>> sfSolvers = GetSolverFoundationSolver(cap, dir, context, flags);
				{
					foreach (Tuple<ISolver, Type> sfSolver in sfSolvers)
					{
						if (sfSolver != null)
						{
							yield return sfSolver;
						}
					}
					yield break;
				}
			}
			if (_pluginSolversByCapability[cap].Count > 0 && !isModelStochastic)
			{
				foreach (PluginSolverInfo solverInfo2 in _pluginSolversByCapability[cap])
				{
					yield return Tuple.Create(GetPluginSolver(solverInfo2._solverType, context), solverInfo2._interfaceType);
				}
				yield break;
			}
			foreach (Tuple<ISolver, Type> item in GetSolverFoundationSolver(cap, context, flags))
			{
				yield return item;
			}
		}

		/// <summary>Return a sequence of registered ISolver instance with the given capability.
		/// </summary>
		/// <param name="cap">Capability required</param>
		/// <param name="dir">Directive</param>
		/// <param name="context">Solver context (cannot be null)</param>
		/// <remarks>Registered default solver is always returned first</remarks>
		internal IEnumerable<ISolver> GetSolvers(SolverCapability cap, Directive dir, ISolverEnvironment context)
		{
			ValidateAccess();
			if (context == null)
			{
				throw new ArgumentNullException("context", Resources.SolverContextCannotBeNull);
			}
			foreach (PluginSolverInfo item in _pluginSolversByCapability[cap])
			{
				ISolver solver = GetPluginSolver(item._solverType, context);
				if (solver != null)
				{
					yield return solver;
				}
			}
			foreach (Tuple<ISolver, Type> sfSolver in GetSolverFoundationSolver(cap, dir, context, (SolverCapabilityFlags)0))
			{
				ISolver solver = sfSolver.Item1;
				if (solver != null)
				{
					yield return solver;
				}
			}
		}
	}
}
