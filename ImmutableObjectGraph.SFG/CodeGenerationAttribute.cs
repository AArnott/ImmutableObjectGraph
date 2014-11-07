namespace Microsoft.ImmutableObjectGraph_SFG
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;


    /// <summary>
    /// A base attribute type for code generation attributes.
    /// </summary>
    public abstract class CodeGenerationAttribute : Attribute
    {
        /// <summary>
        /// Create the syntax tree representing the expansion of some member to which this attribute is applied.
        /// </summary>
        /// <param name="applyTo">The syntax node this attribute is found on.</param>
        /// <param name="document">The document with the semantic model in which this attribute was found.</param>
        /// <returns>The generated member syntax to be added to the project.</returns>
        public abstract MemberDeclarationSyntax Generate(MemberDeclarationSyntax applyTo, Document document);
    }
}
