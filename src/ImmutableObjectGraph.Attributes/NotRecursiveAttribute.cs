namespace ImmutableObjectGraph.CodeGeneration
{
    using System;
    using System.Diagnostics;

    [Conditional("CodeGeneration")]
    [AttributeUsage(AttributeTargets.Field)]
    public class NotRecursiveAttribute : Attribute
    {
    }
}
