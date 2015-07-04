namespace ImmutableObjectGraph.SFG
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeGeneration;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.ComponentModelHost;
    using Microsoft.VisualStudio.LanguageServices;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Validation;

    [ComVisible(true)]
    [Guid("163AA075-225C-4797-9019-FEB55E5CB392")]
    public class SingleFileGenerator : IVsSingleFileGenerator
    {
        public int DefaultExtension(out string pbstrDefaultExtension)
        {
            pbstrDefaultExtension = ".generated.cs";
            return VSConstants.S_OK;
        }

        public int Generate(string inputFilePath, string inputFileContents, string defaultNamespace, IntPtr[] outputFileContents, out uint outputLength, IVsGeneratorProgress generatorProgress)
        {
            if (outputFileContents != null && outputFileContents.Length > 0)
            {
                // Do this first, before input validation, so that the
                // catch block doesn't try to free memory with an uninitialized pointer.
                outputFileContents[0] = IntPtr.Zero;
            }

            try
            {
                Requires.NotNullOrEmpty(inputFilePath, "inputFilePath");
                Requires.NotNull(outputFileContents, "outputFileContents");
                Requires.Argument(outputFileContents.Length > 0, "outputFileContents", "Non-empty array expected.");

                string generated = null;
                ThreadHelper.JoinableTaskFactory.Run(async delegate
                {
                    VisualStudioWorkspace workspace = GetRoslynWorkspace();
                    var inputDocumentId = workspace.CurrentSolution.GetDocumentIdsWithFilePath(inputFilePath).First();
                    var inputDocument = workspace.CurrentSolution.GetDocument(inputDocumentId);
                    var outputDocument = await DocumentTransform.TransformAsync(inputDocument, new ProgressShim(generatorProgress));

                    // Now render as a complete string, as necessary by our single file generator.
                    var reducedDocumentText = await outputDocument.GetTextAsync();
                    generated = reducedDocumentText.ToString();
                });

                // Translate the string we've built up into the bytes of COM memory required.
                var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
                byte[] bytes = encoding.GetBytes(generated);
                outputLength = (uint)bytes.Length;
                outputFileContents[0] = Marshal.AllocCoTaskMem(bytes.Length);
                Marshal.Copy(bytes, 0, outputFileContents[0], bytes.Length);

                return VSConstants.S_OK;
            }
            catch (Exception ex)
            {
                if (outputFileContents[0] != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(outputFileContents[0]);
                    outputFileContents[0] = IntPtr.Zero;
                }

                outputLength = 0;
                return Marshal.GetHRForException(ex);
            }
        }

        private static VisualStudioWorkspace GetRoslynWorkspace()
        {
            var componentModel = Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel;
            Assumes.Present(componentModel);
            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            return workspace;
        }

        /// <summary>
        /// Gets the project and the itemid for the document with the given path.
        /// </summary>
        private static void GetSourceProjectItem(string inputFilePath, out IVsUIHierarchy hierarchy, out uint itemid)
        {
            var shellDocuments = Package.GetGlobalService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
            Assumes.Present(shellDocuments);
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider sp;
            int docInProject;
            ErrorHandler.ThrowOnFailure(shellDocuments.IsDocumentInAProject(inputFilePath, out hierarchy, out itemid, out sp, out docInProject));
        }

        private class ProgressShim : IProgressAndErrors
        {
            private readonly IVsGeneratorProgress progress;

            internal ProgressShim(IVsGeneratorProgress progress)
            {
                this.progress = progress;
            }

            public void Error(string message, uint line, uint column)
            {
                Marshal.ThrowExceptionForHR(this.progress.GeneratorError(0, 0, message, line, column));
            }

            public void Warning(string message, uint line, uint column)
            {
                Marshal.ThrowExceptionForHR(this.progress.GeneratorError(1, 0, message, line, column));
            }

            public void Report(uint progress, uint total)
            {
                this.progress.Progress(progress, total);
            }
        }
    }
}
