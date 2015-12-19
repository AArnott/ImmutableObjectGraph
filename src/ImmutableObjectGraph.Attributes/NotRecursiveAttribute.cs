namespace ImmutableObjectGraph.Generation
{
    using System;
    using System.Diagnostics;

    [Conditional("CodeGeneration")]
    [AttributeUsage(AttributeTargets.Field)]
    public class NotRecursiveAttribute : Attribute
    {
    }
}
