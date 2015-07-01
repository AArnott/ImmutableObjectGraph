// Guids.cs
// MUST match guids.h
using System;

namespace ImmutableObjectGraph.SFG
{
    static class GuidList
    {
        public const string guidImmutableObjectGraph_SFGPkgString = "bb421361-2b95-4586-8b4c-9047aa9bcdaa";
        public const string guidImmutableObjectGraph_SFGCmdSetString = "6caaa80e-f901-4c85-89c4-0587a422f258";

        public static readonly Guid guidImmutableObjectGraph_SFGCmdSet = new Guid(guidImmutableObjectGraph_SFGCmdSetString);
    };
}