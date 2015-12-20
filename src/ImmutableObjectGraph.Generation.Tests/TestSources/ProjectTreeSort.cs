//-----------------------------------------------------------------------
// <copyright file="ProjectTreeSort.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Validation;

    /// <summary>
    /// Standard sort routine(s) for project tree nodes.
    /// </summary>
    internal class ProjectTreeSort : IComparer<ProjectTree>
    {
        /// <summary>
        /// Backing field for the <see cref="Default"/> property.
        /// </summary>
        private static IComparer<ProjectTree> defaultInstance = new ProjectTreeSort();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectTreeSort"/> class.
        /// </summary>
        private ProjectTreeSort()
        {
        }

        /// <summary>
        /// Gets the default sorting algorithm, which sorts alphabetically, puts folders on top, and any special folders or items above that.
        /// </summary>
        internal static IComparer<ProjectTree> Default
        {
            get { return defaultInstance; }
        }

        /// <summary>
        ///     Compares two objects and returns a value indicating whether one is less than,
        ///     equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>    
        /// <param name="y">The second object to compare.</param>   
        /// <returns>Standard -1, 0, 1 sorting responses.</returns>
        public int Compare(ProjectTree x, ProjectTree y)
        {
            Requires.NotNull(x, "x");
            Requires.NotNull(y, "y");

            // Start by putting "special" folders on top.
            bool xUp = x.Capabilities.Contains(ProjectTreeCapabilities.BubbleUp);
            bool yUp = y.Capabilities.Contains(ProjectTreeCapabilities.BubbleUp);
            if (xUp ^ yUp)
            {
                return xUp ? -1 : 1;
            }

            // Then folders should appear above files.
            bool xFolder = x.Capabilities.Contains(ProjectTreeCapabilities.Folder);
            bool yFolder = y.Capabilities.Contains(ProjectTreeCapabilities.Folder);
            if (xFolder ^ yFolder)
            {
                return xFolder ? -1 : 1;
            }

            // Finally, sort alphabetically.  Notice we use CurrentCulture here to get the right effect when sorting.
            int cultureIgnoreCaseSort = StringComparer.CurrentCultureIgnoreCase.Compare(x.Caption, y.Caption);
            if (cultureIgnoreCaseSort == 0)
            {
                // The rule is that two items are equivalent in this tree only if they are ordinal-equivalent, 
                // not just culture-equivalent.  For instance if one inserted an item with a Turkish ı and English I 
                // (for instance fıle.txt and fIle.txt), such items should be considered distinct, but a culture
                // comparison would return equivalence.  This causes problems as code to check for existence of items
                // in the tree uses explicit ordinal comparisons, but when an item is inserted, the comparison in this
                // method is used to validate the tree's state.
                return StringComparer.OrdinalIgnoreCase.Compare(x.Caption, y.Caption);
            }

            return cultureIgnoreCaseSort;
        }
    }
}
