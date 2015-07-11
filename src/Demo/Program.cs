namespace Demo
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using ImmutableObjectGraph;
    using ImmutableObjectGraph.Tests;

    class Program
    {
        static void Main(string[] args)
        {
            MeasureTests();
            return;

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

            var updatedMessage = messageBuilder.ToImmutable();
        }

        private static void MeasureTests()
        {
            var tests = new ProjectTreeTests(new LogHelper());

            RootedProjectTree templateTree = ProjectTreeTests.ConstructVeryLargeTree(new Random(21748171), 4, 100, 10000);
            Console.WriteLine("Template tree contains {0} nodes.", templateTree.GetSelfAndDescendents().Count());

            MeasureReport(tests.CloneProjectTreeLeafToRoot, templateTree, "Optimal clone");
            MeasureReport(tests.CloneProjectTreeRootToLeafWithBuilders, templateTree, "Sub-optimal (using builders)");
            MeasureReport(tests.CloneProjectTreeRootToLeafWithoutBuilders, templateTree, "Sub-optimal");
        }

        private static void MeasureReport(Func<RootedProjectTree, RootedProjectTree> action, RootedProjectTree templateTree, string name)
        {
            GC.Collect();
            var timer = Stopwatch.StartNew();
            var result = action(templateTree);
            timer.Stop();

            if (result.GetSelfAndDescendents().Count() != templateTree.GetSelfAndDescendents().Count())
            {
                Console.WriteLine("FAIL: invalid clone");
            }

            Console.WriteLine("{0} {1}", timer.Elapsed, name);
        }

        private class LogHelper : Xunit.Abstractions.ITestOutputHelper
        {
            public void WriteLine(string message)
            {
                Console.WriteLine(message);
            }

            public void WriteLine(string format, params object[] args)
            {
                Console.WriteLine(format, args);
            }
        }
    }

    [DebuggerDisplay("{Name,nq} <{Email,nq}>")]
    partial class Contact
    {
        [DebuggerDisplay("{Name,nq} <{Email,nq}>")]
        partial class Builder
        {
        }
    }
}
