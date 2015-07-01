ImmutableObjectGraph
====================

## Getting Started

1. Add the [GenerateImmutable] attribute on classes that define the readonly fields of an immutable type.

    using ImmutableObjectGraph.CodeGeneration;

    [GenerateImmutable]
    partial class Person
    {
        readonly string firstName;
        readonly string lastName;
    }

2. On each source file that contains the [GenerateImmutable] attribute, set the file's
   Custom Tool property to "MSBuild:GenerateCodeFromAttributes".

Send issues to https://github.com/aarnott/immutableobjectgraph/issues
