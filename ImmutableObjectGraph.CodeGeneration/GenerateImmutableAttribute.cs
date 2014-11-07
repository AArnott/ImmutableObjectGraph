namespace ImmutableObjectGraph.CodeGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.ImmutableObjectGraph_SFG;

    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateImmutableAttribute : CodeGenerationAttribute
    {
        private string positionalArg;

        public GenerateImmutableAttribute()
        {
        }

        public GenerateImmutableAttribute(string positionalArg)
        {
            this.positionalArg = positionalArg;
        }

        public string NamedArg { get; set; }

        public override MemberDeclarationSyntax Generate(MemberDeclarationSyntax applyTo, Document document)
        {
            var classDeclaration = (ClassDeclarationSyntax)applyTo;

            return SyntaxFactory.ClassDeclaration(classDeclaration.Identifier)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
        }
    }
}
