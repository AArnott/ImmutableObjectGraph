using System.Collections.Immutable;
using ImmutableObjectGraph;

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true, Delta = true, DefineRootedStruct = true)]
abstract partial class FileSystemEntry
{
    [Required]
    readonly string pathSegment;

    readonly RichData data;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true, Delta = true, DefineRootedStruct = true)]
partial class FileSystemFile : FileSystemEntry
{
    readonly ImmutableHashSet<string> attributes;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true, Delta = true, DefineRootedStruct = true)]
partial class FileSystemDirectory : FileSystemEntry
{
    readonly ImmutableSortedSet<FileSystemEntry> children;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true, Delta = true)]
partial class RichData
{
    readonly int someCoolProperty;
}
