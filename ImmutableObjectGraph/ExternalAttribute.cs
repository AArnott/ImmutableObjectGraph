namespace ImmutableObjectGraph
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	/// <summary>
	/// Indicates that the we're forward declaring another type which will be declared elsewhere
	/// in the project, outside of the .tt file. This type will be referenced by the types in,
	/// the .tt file, but will not be part of the output.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
	public sealed class ExternalAttribute : Attribute
	{
	}
}
