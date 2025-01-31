using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Information about a registered solver.
	/// </summary>
	public class SolverRegistration : IComparable<SolverRegistration>, IComparable
	{
		/// <summary> Get the name. 
		/// </summary>
		public string Name { get; internal set; }

		/// <summary> Get the capability. 
		/// </summary>
		public SolverCapability Capability { get; internal set; }

		/// <summary> Get the solver interface name (optional).
		/// </summary>
		public string InterfaceName { get; internal set; }

		/// <summary> Get the fully qualified name of the solver assembly.
		/// </summary>
		public string AssemblyName { get; internal set; }

		/// <summary> Get the class name of the solver. 
		/// </summary>
		public string SolverClassName { get; internal set; }

		/// <summary> Get the class name of the solver directive.
		/// </summary>
		public string DirectiveClassName { get; internal set; }

		/// <summary> Get the class name of the solver parameters.
		/// </summary>
		public string ParameterClassName { get; internal set; }

		/// <summary>Create a new instance.
		/// </summary>
		public SolverRegistration(string name, SolverCapability capability, string interfaceName, string assemblyName, string solverClassName, string directiveClassName, string parameterClassName)
		{
			Name = name;
			Capability = capability;
			InterfaceName = interfaceName;
			AssemblyName = assemblyName;
			SolverClassName = solverClassName;
			DirectiveClassName = directiveClassName;
			ParameterClassName = parameterClassName;
		}

		/// <summary>Create a new instance.
		/// </summary>
		/// <param name="element">A ConfigMsfPluginSolverElement.</param>
		internal SolverRegistration(ConfigMsfPluginSolverElement element)
		{
			Name = element.Name;
			Capability = element.Capability;
			InterfaceName = element.Interface;
			AssemblyName = element.Assembly;
			SolverClassName = element.SolverClassName;
			DirectiveClassName = element.DirectiveClassName;
			ParameterClassName = element.ParameterClassName;
		}

		/// <summary>Return a string representation of the entry.
		/// </summary>
		/// <returns>Returns a string representation of the entry.</returns>
		public override string ToString()
		{
			return string.Concat("[Name = ", Name, ", Assembly = ", AssemblyName, ", Capability = ", Capability, "]");
		}

		/// <summary>Compares the current object with another object of the same type.
		/// </summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>A value that indicates the relative order of the objects being compared.</returns>
		public int CompareTo(SolverRegistration other)
		{
			if ((object)other == null)
			{
				return 1;
			}
			int num = string.Compare(AssemblyName ?? string.Empty, other.AssemblyName ?? string.Empty, StringComparison.Ordinal);
			if (num != 0)
			{
				return num;
			}
			num = string.Compare(Name ?? string.Empty, other.Name ?? string.Empty, StringComparison.Ordinal);
			if (num != 0)
			{
				return num;
			}
			num = string.Compare(SolverClassName ?? string.Empty, other.SolverClassName ?? string.Empty, StringComparison.Ordinal);
			if (num != 0)
			{
				return num;
			}
			num = Capability.CompareTo(other.Capability);
			if (num != 0)
			{
				return num;
			}
			num = string.Compare(DirectiveClassName ?? string.Empty, other.DirectiveClassName ?? string.Empty, StringComparison.Ordinal);
			if (num != 0)
			{
				return num;
			}
			num = string.Compare(ParameterClassName ?? string.Empty, other.ParameterClassName ?? string.Empty, StringComparison.Ordinal);
			if (num != 0)
			{
				return num;
			}
			return string.Compare(InterfaceName ?? string.Empty, other.InterfaceName ?? string.Empty, StringComparison.Ordinal);
		}

		/// <summary>Compares the current object with another object of the same type.
		/// </summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>A value that indicates the relative order of the objects being compared.</returns>
		int IComparable.CompareTo(object other)
		{
			return CompareTo(other as SolverRegistration);
		}

		/// <summary>Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <param name="obj">An object to compare with this object.</param>
		/// <returns>True if the current object is equal to the other parameter; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			return CompareTo(obj as SolverRegistration) == 0;
		}

		/// <summary>Serves as a hash function for a particular type. 
		/// </summary>
		/// <returns>A hash code for the current item.</returns>
		public override int GetHashCode()
		{
			return Statics.CombineHash(AssemblyName.GetHashCode(), Statics.CombineHash(Name.GetHashCode(), Statics.CombineHash(SolverClassName.GetHashCode(), Statics.CombineHash(Capability.GetHashCode(), Statics.CombineHash(DirectiveClassName.GetHashCode(), Statics.CombineHash(ParameterClassName.GetHashCode(), InterfaceName.GetHashCode()))))));
		}

		/// <summary>Test for equality.
		/// </summary>
		public static bool operator ==(SolverRegistration key1, SolverRegistration key2)
		{
			if ((object)key1 == null)
			{
				return (object)key2 == null;
			}
			return key1.CompareTo(key2) == 0;
		}

		/// <summary>Test for inequality.
		/// </summary>
		public static bool operator !=(SolverRegistration key1, SolverRegistration key2)
		{
			if ((object)key1 == null)
			{
				return (object)key2 != null;
			}
			return !(key1 == key2);
		}
	}
}
