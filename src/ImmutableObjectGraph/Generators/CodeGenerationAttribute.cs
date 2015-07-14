namespace ImmutableObjectGraph.Generators
{
    using System;

    /// <summary>
    /// A base attribute type for code generation attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class CodeGenerationAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeGenerationAttribute"/> class.
        /// </summary>
        /// <param name="generatorFullTypeName">
        /// The fully-qualified type name (including assembly information)
        /// of the code generator to activate.
        /// </param>
        public CodeGenerationAttribute(string generatorFullTypeName)
        {
            this.GeneratorFullTypeName = generatorFullTypeName;
        }

        public string GeneratorFullTypeName { get; private set; }
    }
}
