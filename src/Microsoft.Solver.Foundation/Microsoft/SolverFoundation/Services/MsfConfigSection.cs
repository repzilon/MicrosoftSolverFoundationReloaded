using System.Configuration;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Class that handles the plug-in solver section in the configuration file.
	/// </summary>
	internal sealed class MsfConfigSection : ConfigurationSection
	{
		/// <summary>Get registered solvers.
		/// </summary>
		[ConfigurationProperty("MsfPluginSolvers", IsDefaultCollection = true)]
		public ConfigMsfPluginSolversCollection Solvers
		{
			get
			{
				if (!(base["MsfPluginSolvers"] is ConfigMsfPluginSolversCollection result))
				{
					throw new MsfSolverConfigurationException(Resources.PluginSolverConfigInvalidMsfPluginSolversSection);
				}
				return result;
			}
		}
	}
}
