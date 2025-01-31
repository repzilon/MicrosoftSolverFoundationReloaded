using System.Configuration;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Individual plugin solver section handler
	/// </summary>
	internal sealed class ConfigMsfPluginSolverElement : ConfigurationElement
	{
		/// <summary> Get the name. This is the key.
		/// </summary>
		[ConfigurationProperty("name", IsRequired = false)]
		public string Name
		{
			get
			{
				if (!(base["name"] is string result))
				{
					throw new MsfSolverConfigurationException(Resources.PluginSolverConfigInvalidName);
				}
				return result;
			}
		}

		/// <summary> Get the capability. Solverclass + capability is the key.
		/// </summary>
		[ConfigurationProperty("capability", IsRequired = true)]
		public SolverCapability Capability
		{
			get
			{
				SolverCapability? solverCapability = base["capability"] as SolverCapability?;
				if (!solverCapability.HasValue)
				{
					throw new MsfSolverConfigurationException(Resources.PluginSolverConfigInvalidCapability);
				}
				return solverCapability.Value;
			}
		}

		[ConfigurationProperty("interface", DefaultValue = "Microsoft.SolverFoundation.Services.ILinearSolver")]
		public string Interface => base["interface"] as string;

		/// <summary> Get the fully qualified name of the solver assembly
		/// </summary>
		[ConfigurationProperty("assembly", IsRequired = true)]
		public string Assembly
		{
			get
			{
				if (!(base["assembly"] is string result))
				{
					throw new MsfSolverConfigurationException(Resources.PluginSolverConfigInvalidAssembly);
				}
				return result;
			}
		}

		/// <summary> Get the class name of the solver. Solverclass + capability is the key.
		/// </summary>
		[ConfigurationProperty("solverclass", IsRequired = true)]
		public string SolverClassName
		{
			get
			{
				if (!(base["solverclass"] is string result))
				{
					throw new MsfSolverConfigurationException(Resources.PluginSolverConfigInvalidSolverClass);
				}
				return result;
			}
		}

		/// <summary> Get the class name of the solver directive
		/// </summary>
		[ConfigurationProperty("directiveclass", IsRequired = false)]
		public string DirectiveClassName
		{
			get
			{
				if (!(base["directiveclass"] is string result))
				{
					throw new MsfSolverConfigurationException(Resources.PluginSolverConfigInvalidDirectiveClass);
				}
				return result;
			}
		}

		/// <summary> Get the class name of the solver parameters
		/// </summary>
		[ConfigurationProperty("parameterclass", IsRequired = true)]
		public string ParameterClassName
		{
			get
			{
				if (!(base["parameterclass"] is string result))
				{
					throw new MsfSolverConfigurationException(Resources.PluginSolverConfigInvalidParameterClass);
				}
				return result;
			}
		}
	}
}
