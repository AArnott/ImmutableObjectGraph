namespace Microsoft.ImmutableObjectGraph_SFG
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        public int Generate(string inputFilePath, string inputFileContents, string defaultNamespace, IntPtr[] outputFileContents, out uint outputLength, IVsGeneratorProgress generateProgress)
        {
            Requires.NotNullOrEmpty(inputFilePath, "inputFilePath");
            Requires.NotNull(outputFileContents, "outputFileContents");
            Requires.Argument(outputFileContents.Length > 0, "outputFileContents", "Non-empty array expected.");

            outputFileContents[0] = IntPtr.Zero;
            try
            {
                string generated = null;
                ThreadHelper.JoinableTaskFactory.Run(async delegate
                {
                    IVsUIHierarchy uiHierarchy;
                    uint itemid;
                    GetSourceProjectItem(inputFilePath, out uiHierarchy, out itemid);
                    object projectNameObject;
                    ErrorHandler.ThrowOnFailure(uiHierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_Name, out projectNameObject));

                    VisualStudioWorkspace workspace = GetRoslynWorkspace();
                    var inputDocumentId = workspace.CurrentSolution.GetDocumentIdsWithFilePath(inputFilePath).First();
                    var inputDocument = workspace.CurrentSolution.GetDocument(inputDocumentId);
                    var inputSemanticModel = await inputDocument.GetSemanticModelAsync();
                    var syntaxTree = inputSemanticModel.SyntaxTree;
                    var typeNodes = from node in syntaxTree.GetRoot().DescendantNodes(n => n is CompilationUnitSyntax || n is NamespaceDeclarationSyntax || n is TypeDeclarationSyntax)
                                    let type = node as TypeDeclarationSyntax
                                    where type != null
                                    where type.Modifiers.OfType<SyntaxToken>().Any(t => t.CSharpKind() == SyntaxKind.PartialKeyword)
                                    select type;

                    var emittedTypes = new List<ClassDeclarationSyntax>();
                    foreach (var sourceType in typeNodes)
                    {
                        var emittedType = SyntaxFactory.ClassDeclaration(sourceType.Identifier)
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
                        emittedTypes.Add(emittedType);
                    }

                    var emittedTree = SyntaxFactory.CompilationUnit()
                        .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(emittedTypes))
                        .NormalizeWhitespace();

                    generated = emittedTree.ToString();
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
            VisualStudio.OLE.Interop.IServiceProvider sp;
            int docInProject;
            ErrorHandler.ThrowOnFailure(shellDocuments.IsDocumentInAProject(inputFilePath, out hierarchy, out itemid, out sp, out docInProject));
        }
    }
}
