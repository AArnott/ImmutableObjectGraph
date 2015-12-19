namespace ImmutableObjectGraph.Generation
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Applied to types and fields that should be ignored by the code generator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Field)]
    [Conditional("CodeGeneration")]
    public class IgnoreAttribute : Attribute
    {
    }
}
