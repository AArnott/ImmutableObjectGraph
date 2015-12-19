namespace ImmutableObjectGraph.CodeGeneration
{
    using System;
    using System.Diagnostics;
    using global::CodeGeneration.Roslyn;

    [AttributeUsage(AttributeTargets.Class)]
    [Conditional("CodeGeneration")]
    [CodeGenerationAttribute("ImmutableObjectGraph.CodeGeneration.Roslyn.CodeGenerator, ImmutableObjectGraph.CodeGeneration.Roslyn, Version=" + ThisAssembly.AssemblyVersion + ", Culture=neutral, PublicKeyToken=bfd91f1bd601e0d7")]
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
