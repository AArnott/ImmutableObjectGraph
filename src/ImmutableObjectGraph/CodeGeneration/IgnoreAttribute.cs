namespace ImmutableObjectGraph
{
    using System;

    /// <summary>
    /// Applied to types that should be ignored by the ImmutableObjectGraph T4 templates.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    public class IgnoreAttribute : Attribute
    {
    }
}
