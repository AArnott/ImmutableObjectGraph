using System.Collections.Immutable;
using ImmutableObjectGraph;

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
abstract partial class FileSystemEntry
{
    [Required]
    readonly string pathSegment;

    readonly RichData data;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class FileSystemFile : FileSystemEntry
{
    readonly ImmutableHashSet<string> attributes;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class FileSystemDirectory : FileSystemEntry
{
    readonly ImmutableSortedSet<FileSystemEntry> children;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class RichData
{
    readonly int someCoolProperty;
}
