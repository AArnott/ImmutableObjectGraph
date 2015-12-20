namespace RoslynDemo
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ImmutableObjectGraph.Generation;

    [GenerateImmutable(GenerateBuilder = true)]
    partial class Message
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly Contact author;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly ImmutableList<Contact> to;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly ImmutableList<Contact> cc;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly ImmutableList<Contact> bcc;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly string subject;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly string body;
    }

    [GenerateImmutable(DefineWithMethodsPerProperty = true, GenerateBuilder = true)]
    partial class Contact
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly string name;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly string email;
    }
}
