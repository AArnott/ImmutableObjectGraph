namespace ImmutableObjectGraph.CodeGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    [AttributeUsage(AttributeTargets.Class)]
    [Generators.CodeGeneration("ImmutableObjectGraph.CodeGeneration.Roslyn.CodeGenerator, ImmutableObjectGraph.CodeGeneration.Roslyn")]
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
