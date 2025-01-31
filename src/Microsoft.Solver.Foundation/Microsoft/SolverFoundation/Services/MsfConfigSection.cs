using System;
using System.Collections.Generic;
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

    public class ConfigurationElement
    {
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();

        protected object this[string propertyName]
        {
            get
            {
                _properties.TryGetValue(propertyName, out var value);
                return value;
            }
            set
            {
                _properties[propertyName] = value;
            }
        }

        protected virtual bool OnDeserializeUnrecognizedAttribute(string name, string value)
        {
            _properties[name] = value;
            return true;
        }
    }

    public class ConfigurationSection : ConfigurationElement
    {
        protected object this[string propertyName]
        {
            get
            {
                return base[propertyName];
            }
            set
            {
                base[propertyName] = value;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ConfigurationPropertyAttribute : Attribute
    {
        public string Name { get; }
        public bool IsRequired { get; set; }
        public object DefaultValue { get; set; }
        public bool IsDefaultCollection { get; set; }

        public ConfigurationPropertyAttribute(string name)
        {
            Name = name;
        }
    }
}
