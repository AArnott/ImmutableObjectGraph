ImmutableObjectGraph
====================

## Getting Started

1. Add the [GenerateImmutable] attribute on classes that define the readonly fields of an immutable type.

    using ImmutableObjectGraph.Generation;

    [GenerateImmutable]
    partial class Person
    {
        readonly string firstName;
        readonly string lastName;
    }

2. Create a Person in another of your code files using the generated factory method:

    var author = Person.Create(firstName: "Andrew", lastName: "Arnott");

3. Now add more types, add arguments to the [GenerateImmutable(...)] attribute, etc.

Send issues to https://github.com/aarnott/immutableobjectgraph/issues
