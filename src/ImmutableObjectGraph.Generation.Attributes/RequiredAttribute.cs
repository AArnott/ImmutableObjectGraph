namespace ImmutableObjectGraph.Generation
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Indicates that the field to which it is applied should not be optional when
    /// constructing instances of the declaring type or derived types.
    /// </summary>
    [Conditional("CodeGeneration")]
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class RequiredAttribute : Attribute
    {
    }
}
