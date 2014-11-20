namespace ImmutableObjectGraph.CodeGeneration.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.CodeAnalysis.MSBuild;
    using Microsoft.ImmutableObjectGraph_SFG;
    using Task = System.Threading.Tasks.Task;

    public class GenerateCodeFromAttributes : Microsoft.Build.Utilities.Task, ICancelableTask
    {
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        [Required]
        public string ProjectFile { get; set; }

        [Required]
        public ITaskItem[] InputFiles { get; set; }

        [Required]
        public string IntermediateOutputDirectory { get; set; }

        [Output]
        public ITaskItem[] OutputFiles { get; set; }

        public override bool Execute()
        {
            return Task.Run(async delegate
            {
                var project = await MSBuildWorkspace.Create()
                    .OpenProjectAsync(this.ProjectFile, this.cts.Token);
                var outputFiles = new List<ITaskItem>();

                foreach (var inputFile in this.InputFiles)
                {
                    if (this.cts.Token.IsCancellationRequested)
                    {
                        this.Log.LogMessage(MessageImportance.High, "Canceled.");
                        return false;
                    }

                    string outputFilePath = Path.Combine(this.IntermediateOutputDirectory, Path.GetFileNameWithoutExtension(inputFile.ItemSpec) + ".generated.cs");
                    this.Log.LogMessage(MessageImportance.Normal, "{0} -> {1}", inputFile.ItemSpec, outputFilePath);

                    var inputDocumentId = project.Solution.GetDocumentIdsWithFilePath(inputFile.GetMetadata("FullPath")).First();
                    var inputDocument = project.GetDocument(inputDocumentId);
                    var outputDocument = await DocumentTransform.TransformAsync(inputDocument, new ProgressLogger(this.Log, inputFile.ItemSpec));
                    var outputText = await outputDocument.GetTextAsync(this.cts.Token);
                    using (var outputFileStream = File.OpenWrite(outputFilePath))
                    using (var outputWriter = new StreamWriter(outputFileStream))
                    {
                        outputText.Write(outputWriter);
                    }

                    var outputItem = new TaskItem(outputFilePath);
                    inputFile.CopyMetadataTo(outputItem);
                    outputFiles.Add(outputItem);
                }

                this.OutputFiles = outputFiles.ToArray();
                return !this.Log.HasLoggedErrors;
            }).Result;
        }

        public void Cancel()
        {
            cts.Cancel();
        }

        private class ProgressLogger : IProgressAndErrors
        {
            private readonly TaskLoggingHelper logger;
            private readonly string inputFilename;

            internal ProgressLogger(TaskLoggingHelper logger, string inputFilename)
            {
                this.logger = logger;
                this.inputFilename = inputFilename;
            }

            public void Error(string message, uint line, uint column)
            {
                this.logger.LogError(
                    subcategory: string.Empty,
                    errorCode: string.Empty,
                    helpKeyword: string.Empty,
                    file: inputFilename,
                    lineNumber: (int)line,
                    columnNumber: (int)column,
                    endLineNumber: -1,
                    endColumnNumber: -1,
                    message: message);
            }

            public void Report(uint progress, uint total)
            {
            }

            public void Warning(string message, uint line, uint column)
            {
                this.logger.LogWarning(
                    subcategory: string.Empty,
                    warningCode: string.Empty,
                    helpKeyword: string.Empty,
                    file: inputFilename,
                    lineNumber: (int)line,
                    columnNumber: (int)column,
                    endLineNumber: -1,
                    endColumnNumber: -1,
                    message: message);
            }
        }
    }
}
