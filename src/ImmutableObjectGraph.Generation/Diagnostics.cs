namespace ImmutableObjectGraph.Generation
{
    using System;
    using Microsoft.CodeAnalysis;

    internal static class Diagnostics
    {
        internal const string MissingReadOnly = "IOG0001";

        internal const string NotApplicableSetting = "IOG0002";

        internal static DiagnosticSeverity GetSeverity(string id)
        {
            switch (id)
            {
                case MissingReadOnly:
                case NotApplicableSetting:
                    return DiagnosticSeverity.Warning;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
