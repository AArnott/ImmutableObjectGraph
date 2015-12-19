namespace ImmutableObjectGraph.Generation
{
    using System;
    using System.Diagnostics;

    [System.AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    [Conditional("CodeGeneration")]
    public sealed class DistinguisherAttribute : Attribute
    {
        public DistinguisherAttribute()
        {
        }

        public string CollectionModifierMethodSuffix { get; set; }
    }
}
