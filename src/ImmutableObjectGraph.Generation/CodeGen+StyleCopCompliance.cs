namespace ImmutableObjectGraph.Generation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Data.Entity.Design.PluralizationServices;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;
    using Validation;

    public partial class CodeGen
    {
        protected class StyleCopCompliance : FeatureGenerator
        {
            public StyleCopCompliance(CodeGen generator) : base(generator)
            {
            }

            public override bool IsApplicable
            {
                get { return true; }
            }

            protected override void GenerateCore()
            {
            }

            public override SyntaxList<MemberDeclarationSyntax> ProcessFinalGeneratedResult(SyntaxList<MemberDeclarationSyntax> applyToAndOtherTypes)
            {
                var result = base.ProcessFinalGeneratedResult(applyToAndOtherTypes);

                for (int i = 0; i < result.Count; i++)
                {
                    var member = result[i];
                    var types = member.DescendantNodesAndSelf(n => n is ClassDeclarationSyntax || n is StructDeclarationSyntax)
                        .OfType<TypeDeclarationSyntax>()
                        .ToArray();
                    var trackingMember = member.TrackNodes(types);

                    foreach (var type in types)
                    {
                        var currentMember = trackingMember.GetCurrentNode(type);
                        var updatedMember = currentMember;
                        var cl = currentMember as ClassDeclarationSyntax;
                        if (cl != null)
                        {
                            updatedMember = SortMembers(cl);
                        }

                        var str = currentMember as StructDeclarationSyntax;
                        if (str != null)
                        {
                            updatedMember = SortMembers(str);
                        }

                        trackingMember = trackingMember.ReplaceNode(currentMember, updatedMember);
                    }

                    result = result.Replace(member, trackingMember);
                }

                return result;
            }

            private static ClassDeclarationSyntax SortMembers(ClassDeclarationSyntax type)
            {
                var innerMembers = type.Members.ToList();
                innerMembers.Sort(StyleCop.Sort);
                type = type.WithMembers(SyntaxFactory.List(innerMembers));
                return type;
            }

            private static StructDeclarationSyntax SortMembers(StructDeclarationSyntax type)
            {
                var innerMembers = type.Members.ToList();
                innerMembers.Sort(StyleCop.Sort);
                type = type.WithMembers(SyntaxFactory.List(innerMembers));
                return type;
            }
        }
    }
}
