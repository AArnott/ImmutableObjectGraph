namespace ImmutableObjectGraph.Generation
{
    using System;
    using System.Diagnostics;

    [AttributeUsage(AttributeTargets.Class)]
    [Conditional("CodeGeneration")]
    public class GenerateImmutableAttribute : Attribute
    {
        public GenerateImmutableAttribute()
        {
        }

        public bool GenerateBuilder { get; set; }

        public bool Delta { get; set; }

        public bool DefineInterface { get; set; }

        public bool DefineRootedStruct { get; set; }

        public bool DefineWithMethodsPerProperty { get; set; }
    }
}
