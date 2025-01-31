using System.Collections.Generic;
using System;
using System.Configuration;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using System.Collections;

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
                for (int i = 0; i < BaseGetAllKeys().Length; i++)
                {
                    if (BaseGet(i) is ConfigMsfPluginSolverElement element && GetElementKey(element).Equals(solver))
                    {
                        return element;
                    }
                }
                throw new MsfSolverConfigurationException(Resources.PluginSolverConfigInvalidElement);
            }
        }
        // SKIPPED
        //public ConfigMsfPluginSolverElement this[ConfigKey solver]
        //{
        //	get
        //	{
        //		if (!(BaseGet(solver) is ConfigMsfPluginSolverElement result))
        //		{
        //			throw new MsfSolverConfigurationException(Resources.PluginSolverConfigInvalidElement);
        //		}
        //		return result;
        //	}
        //}

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

    public abstract class ConfigurationElementCollection : ConfigurationElement, ICollection
    {
        private readonly List<ConfigurationElement> _elements = new List<ConfigurationElement>();

        public int Count => _elements.Count;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        public void CopyTo(Array array, int index)
        {
            ((ICollection)_elements).CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            return _elements.GetEnumerator();
        }

        protected void BaseAdd(ConfigurationElement element)
        {
            _elements.Add(element);
        }

        protected void BaseAdd(int index, ConfigurationElement element)
        {
            _elements.Insert(index, element);
        }

        protected void BaseRemoveAt(int index)
        {
            _elements.RemoveAt(index);
        }

        protected ConfigurationElement BaseGet(int index)
        {
            return _elements[index];
        }

        protected object[] BaseGetAllKeys()
        {
            var keys = new object[_elements.Count];
            for (int i = 0; i < _elements.Count; i++)
            {
                keys[i] = GetElementKey(_elements[i]);
            }
            return keys;
        }

        protected abstract ConfigurationElement CreateNewElement();

        protected abstract object GetElementKey(ConfigurationElement element);

        public abstract ConfigurationElementCollectionType CollectionType { get; }

        protected abstract string ElementName { get; }
    }

    public enum ConfigurationElementCollectionType
    {
        BasicMap,
        AddRemoveClearMap,
        BasicMapAlternate
    }
}
