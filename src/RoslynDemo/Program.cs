using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var me = Contact.Create("Andrew Arnott", "andrewarnott@gmail.com");
            var myself = me.WithEmail("thisistheplace@hotmail.com");
            var i = me.WithEmail("andrewarnott@live.com");

            var message = Message.Create(
                author: me,
                to: ImmutableList.Create(myself),
                subject: "Hello, World",
                body: "What's happening?");

            var messageBuilder = message.ToBuilder();
            messageBuilder.Author.Name = "And again, myself";
            messageBuilder.Author.Email = "oh@so.mutable";
            messageBuilder.To.Add(i);
            messageBuilder.CopyInto(message);

            var updatedMessage = messageBuilder.ToImmutable();
        }
    }
}
