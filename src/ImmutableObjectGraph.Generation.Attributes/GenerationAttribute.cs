namespace ImmutableObjectGraph.Generation
{
    using System;
    using System.Diagnostics;
    using Validation;

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Conditional("CodeGeneration")]
    public class GenerationAttribute : Attribute
    {
        public GenerationAttribute(int generation)
        {
            Requires.Range(generation > 0, nameof(generation), "A positive integer is required.");
            this.Generation = generation;
        }

        public int Generation { get; }
    }
}
