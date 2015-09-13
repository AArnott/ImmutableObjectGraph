namespace ImmutableObjectGraph
{
    using System;

    /// <summary>
    /// Applied to types and fields that should be ignored by the code generator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Field)]
    public class IgnoreAttribute : Attribute
    {
    }
}
