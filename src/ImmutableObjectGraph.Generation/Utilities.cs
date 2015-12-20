namespace ImmutableObjectGraph.Generation
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity.Design.PluralizationServices;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Validation;

    internal static class Utilities
    {
        internal static readonly PluralizationService PluralizationService = PluralizationService.CreateService(CultureInfo.GetCultureInfo("en-US"));

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
    }
}
