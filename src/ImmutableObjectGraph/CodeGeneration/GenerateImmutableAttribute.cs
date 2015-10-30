namespace ImmutableObjectGraph.CodeGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    [AttributeUsage(AttributeTargets.Class)]
    [Generators.CodeGeneration("ImmutableObjectGraph.CodeGeneration.Roslyn.CodeGenerator, ImmutableObjectGraph.CodeGeneration.Roslyn, Version=" + ThisAssembly.AssemblyVersion + ", Culture=neutral, PublicKeyToken=bfd91f1bd601e0d7")]
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
