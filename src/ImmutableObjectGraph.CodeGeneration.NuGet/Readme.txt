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

3. You may need to close and reopen the project/solution after first setting the
   Custom Tool property before IntelliSense includes the generated members.

4. Create a Person in another of your code files using the generated factory method:

    var author = Person.Create(firstName: "Andrew", lastName: "Arnott");

5. Now add more types, add arguments to the [GenerateImmutable(...)] attribute, etc.

Send issues to https://github.com/aarnott/immutableobjectgraph/issues
