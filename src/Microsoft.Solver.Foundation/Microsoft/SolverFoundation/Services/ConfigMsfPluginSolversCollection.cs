using System.Configuration;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Customized element collection to handle plug-in solver elements
	/// </summary>
	internal sealed class ConfigMsfPluginSolversCollection : ConfigurationElementCollection
	{
		internal struct ConfigKey
		{
			public string SolverName;

			public SolverCapability Capability;

			public ConfigKey(string name, SolverCapability cap)
			{
				SolverName = name;
				Capability = cap;
			}

			public override bool Equals(object obj)
			{
				ConfigKey? configKey = obj as ConfigKey?;
				if (configKey.HasValue)
				{
					return configKey == this;
				}
				return false;
			}

			public override int GetHashCode()
			{
				return Statics.CombineHash(SolverName.GetHashCode(), Capability.GetHashCode());
			}

			public static bool operator ==(ConfigKey key1, ConfigKey key2)
			{
				if (key1.SolverName == key2.SolverName)
				{
					return key1.Capability == key2.Capability;
				}
				return false;
			}

			public static bool operator !=(ConfigKey key1, ConfigKey key2)
			{
				return !(key1 == key2);
			}
		}

		public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMap;

		protected override string ElementName => "MsfPluginSolver";

		public ConfigMsfPluginSolverElement this[int index]
		{
			get
			{
				if (!(BaseGet(index) is ConfigMsfPluginSolverElement result))
				{
					throw new MsfSolverConfigurationException(Resources.PluginSolverConfigInvalidElement);
				}
				return result;
			}
			set
			{
				if (BaseGet(index) != null)
				{
					BaseRemoveAt(index);
				}
				BaseAdd(index, value);
			}
		}

		public ConfigMsfPluginSolverElement this[ConfigKey solver]
		{
			get
			{
				if (!(BaseGet(solver) is ConfigMsfPluginSolverElement result))
				{
					throw new MsfSolverConfigurationException(Resources.PluginSolverConfigInvalidElement);
				}
				return result;
			}
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new ConfigMsfPluginSolverElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			if (element == null)
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverConfigNullElement);
			}
			if (!(element is ConfigMsfPluginSolverElement configMsfPluginSolverElement))
			{
				throw new MsfSolverConfigurationException(Resources.PluginSolverConfigInvalidElement);
			}
			return new ConfigKey(configMsfPluginSolverElement.SolverClassName, configMsfPluginSolverElement.Capability);
		}

		public void Add(ConfigMsfPluginSolverElement solver)
		{
			BaseAdd(solver);
		}

		public bool ContainsKey(ConfigKey key)
		{
			bool result = false;
			object[] array = BaseGetAllKeys();
			object[] array2 = array;
			foreach (object obj in array2)
			{
				ConfigKey? configKey = obj as ConfigKey?;
				if (configKey.HasValue && configKey.Value == key)
				{
					result = true;
					break;
				}
			}
			return result;
		}
	}
}
