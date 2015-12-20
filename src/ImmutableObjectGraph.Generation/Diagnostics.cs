namespace ImmutableObjectGraph.Generation
{
    using System;
    using Microsoft.CodeAnalysis;

    public static class Diagnostics
    {
        public const string MissingReadOnly = "IOG0001";

        public const string NotApplicableSetting = "IOG0002";

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
