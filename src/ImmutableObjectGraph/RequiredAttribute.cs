namespace ImmutableObjectGraph {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	/// <summary>
	/// Indicates that the field to which it is applied should not be optional when
	/// constructing instances of the declaring type or derived types.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	public sealed class RequiredAttribute : Attribute {
	}
}
