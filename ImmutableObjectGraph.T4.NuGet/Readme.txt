ImmutableObjectGraph
====================

Getting started is just a few steps away, due to weird dependency issues with T4:
 1. Clear the Custom Tool property from the YourImmutableTypeDefinition.tt file.
 2. Set the Build Action on your YourImmutableTypeDefinition.cs file to None.
 3. Build your project.
 4. Set the Custom Tool on YourImmutableTypeDefinition.tt back to TextTemplatingFileGenerator.
 5. Set the Build Action on YourImmutableTypeDefinition.cs back to Compile.

If you run into code generation issues again, make sure the ImmutableObjectGraph.dll file
already exists in your bin\$(Configuration) directory.

Now open the YourImmutableTypeDefinition.tt file and start making changes to it. 
You may want to start with:
 1. Fix the namespace of the generated code by changing the YourImmutableTypeDefinition.tt line:
    this.Namespace = "YourNamespace";
    Also fix the namespace in the YourImmutableTypeDefinition.cs file (partial class that you author).

Send issues to https://github.com/aarnott/immutableobjectgraph/issues
