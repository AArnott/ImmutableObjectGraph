//-----------------------------------------------------------------------
// <copyright file="ProjectTreeCapabilities.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// String constants that may appear in <see cref="IProjectTree.Capabilities"/>.
    /// </summary>
    public static class ProjectTreeCapabilities
    {
        /// <summary>
        /// An empty set of project tree capabilities with the case-insensitive comparer.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "Is indeed immutable.")]
        public static readonly ImmutableHashSet<string> EmptyCapabilities = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Indicates that this node is the root project node.
        /// </summary>
        /// <remarks>
        /// This is useful for <see cref="IProjectTreeModifier"/> extensions so they know which node is actually the root one,
        /// since all nodes look like root nodes at the time they are being modified.
        /// </remarks>
        public const string ProjectRoot = "ProjectRoot";

        /// <summary>
        /// Indicates that this node is the well-known "References" folder.
        /// </summary>
        public const string ReferencesFolder = "ReferencesFolder";

        /// <summary>
        /// Indicates that this node is the special project Properties folder.
        /// </summary>
        public const string AppDesignerFolder = "AppDesignerFolder";

        /// <summary>
        /// Indicates that this item represents a reference (e.g. Assembly, COM, or Project reference).
        /// </summary>
        /// <remarks>
        /// This capability should only appear on <see cref="IProjectItemTree"/> instances.
        /// </remarks>
        public const string Reference = "Reference";

        /// <summary>
        /// Indicates that this item is a reference that has been successfully resolved.
        /// </summary>
        /// <remarks>
        /// This capability should only appear on <see cref="IProjectItemTree"/> instances.
        /// </remarks>
        public const string ResolvedReference = "ResolvedReference";

        /// <summary>
        /// Indicates that this item is a reference that failed to resolve.
        /// </summary>
        /// <remarks>
        /// This capability should only appear on <see cref="IProjectItemTree"/> instances.
        /// </remarks>
        public const string BrokenReference = "BrokenReference";

        /// <summary>
        /// Indicates that this item represents a source file (e.g. *.cs, *.resx, *.bmp) that is included in the build.
        /// </summary>
        /// <remarks>
        /// This capability should only appear on <see cref="IProjectItemTree"/> instances.
        /// </remarks>
        public const string SourceFile = "SourceFile";

        /// <summary>
        /// Indicates that this item represents a folder on disk, and may contain sub-items that can be manipulated by the user.
        /// </summary>
        /// <remarks>
        /// This capability may appear on any <see cref="IProjectTree"/> instance.
        /// </remarks>
        public const string Folder = "Folder";

        /// <summary>
        /// Indicates that this item represents a file on disk (not a folder, and not a virtual node).
        /// </summary>
        public const string FileOnDisk = "FileOnDisk";

        /// <summary>
        /// Indicates that this item represents a file or folder on disk (not a virtual node).
        /// </summary>
        public const string FileSystemEntity = "FileSystemEntity";

        /// <summary>
        /// Indicates that this item should appear near the top of its containing list with other similarly tagged nodes.
        /// </summary>
        public const string BubbleUp = "BubbleUp";

        /// <summary>
        /// Indicates that this item does not exist in the project, but does exist on disk and might be included in the project later.
        /// </summary>
        public const string IncludeInProjectCandidate = "IncludeInProjectCandidate";

        /// <summary>
        /// Indicates that this item does not exist in the project, but does exist on disk and might be included in the project later,
        /// but that the file entity on disk is marked as hidden in the file system.
        /// </summary>
        public const string HiddenIncludeInProjectCandidate = "HiddenIncludeInProjectCandidate";

        /// <summary>
        /// Indicates that this item may not exist in the project but implies nothing about includability into a project.
        /// </summary>
        /// <remarks>
        /// Node providers can use this capability to claim responsibility for resolving a node to a path
        /// if path resolution fails because the node is not a project item.  SDK references are an example.
        /// </remarks>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "NonMember", Justification = "Ignored")]
        public const string NonMemberItem = "NonMemberItem";

        /// <summary>
        /// Indicates that this item should always be copyable.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Copyable", Justification = "Ignored")]
        public const string AlwaysCopyable = "AlwaysCopyable";

        /// <summary>
        /// Indicates that this item should be implicitly copied, moved, or dragged whenever its immediate parent is.
        /// </summary>
        public const string FollowsParent = "FollowsParent";
    }
}