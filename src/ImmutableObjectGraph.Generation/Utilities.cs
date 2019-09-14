namespace ImmutableObjectGraph.Generation
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System;
    using System.Collections.Generic;
    using System.Data.Entity.Design.PluralizationServices;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Validation;

    internal static class Utilities
    {
        internal static readonly PluralizationService PluralizationService = PluralizationService.CreateService(new CultureInfo("en-US"));

        internal static string ToPascalCase(this string name)
        {
            Requires.NotNullOrEmpty(name, "name");
            return name.Substring(0, 1).ToUpperInvariant() + name.Substring(1);
        }

        internal static string ToCamelCase(this string name)
        {
            Requires.NotNullOrEmpty(name, "name");
            return name.Substring(0, 1).ToLowerInvariant() + name.Substring(1);
        }

        internal static string ToPlural(this string word)
        {
            return PluralizationService.Pluralize(word);
        }

        internal static string ToSingular(this string word)
        {
            return PluralizationService.Singularize(word);
        }

        internal static string GetFieldSummary(FieldDeclarationSyntax field)
        {
            var xmldocComment = field.GetLeadingTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));
            var x = xmldocComment.GetStructure() as DocumentationCommentTriviaSyntax;
            var summaryElement = x?.Content.OfType<XmlElementSyntax>().FirstOrDefault(e => e.StartTag.Name.GetText().ToString() == "summary");
            return Utilities.TrimCommentMarkersAndWhitespace(summaryElement?.Content.ToString());
        }

        internal static SyntaxTriviaList ConvertFieldSummaryToProperty(FieldDeclarationSyntax field, bool hasGetter = true, bool hasSetter = false)
        {
            if (field == null)
            {
                return default;
            }

            var xmldocComment = field.GetLeadingTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));
            var x = xmldocComment.GetStructure() as DocumentationCommentTriviaSyntax;
            var summaryElement = x?.Content.OfType<XmlElementSyntax>().FirstOrDefault(e => e.StartTag.Name.GetText().ToString() == "summary");
            string propertySummary = Utilities.ConvertFieldSummaryToProperty(
                Utilities.TrimCommentMarkersAndWhitespace(summaryElement?.Content.ToString()),
                hasGetter: hasGetter,
                hasSetter: hasSetter);
            var xmldocComments = propertySummary != null ? SyntaxFactory.ParseLeadingTrivia($"/// <summary>{propertySummary}</summary>\r\n") : SyntaxFactory.TriviaList();
            return xmldocComments;
        }

        private static string ConvertFieldSummaryToProperty(string fieldSummary, bool hasGetter = true, bool hasSetter = false)
        {
            if (string.IsNullOrWhiteSpace(fieldSummary))
            {
                return null;
            }

            fieldSummary = fieldSummary.Trim();
            var builder = new StringBuilder(fieldSummary);
            builder[0] = char.ToLower(builder[0]);

            if (hasGetter && hasSetter)
            {
                builder.Insert(0, "Gets or sets ");
            }
            else if (hasSetter)
            {
                builder.Insert(0, "Sets ");
            }
            else
            {
                builder.Insert(0, "Gets ");
            }

            return builder.ToString();
        }

        private static string TrimCommentMarkersAndWhitespace(string xmlDocElementContent)
        {
            if (xmlDocElementContent == null)
            {
                return null;
            }

            xmlDocElementContent = xmlDocElementContent.Trim();
            string trimmed = Regex.Replace(xmlDocElementContent, @"\n*\s*///\s*", " ", RegexOptions.Multiline);
            return trimmed;
        }
    }
}
