using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense2;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.DebuggerIntellisense2
{
    internal class CSharpDebuggerWorkspace : DebuggerWorkspace
    {
        private CSharpDebuggerWorkspace(Document context, IWpfTextView textView, ITextBuffer textBuffer, HostServices hostServices)
            : base(context, textView, textBuffer, context, hostServices)
        {
        }

        internal static DebuggerWorkspace Create(Document context, IWpfTextView textView, ITextBuffer textBuffer)
        {
            var workspace = new CSharpDebuggerWorkspace(context, textView, textBuffer, context.Project.Solution.Workspace.Services.HostServices);

            var solution = new Solution(workspace, SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()));
            var project = solution.AddProject("project", "project.dll", "C#");
            var document = project.AddDocument("document.cs", textBuffer.AsTextContainer().CurrentText);

            workspace.SetCurrentSolution(document.Project.Solution);
            workspace.OnDocumentOpened(document.Id, textBuffer.AsTextContainer());
            return workspace;
        }

        // For testing
        internal static DebuggerWorkspace Create(Document context, HostServices hostServices, ITextBufferFactoryService bufferFactory, ITextEditorFactoryService editorFactory)
        {
            var contentType = context.GetTextAsync().Result.Container.GetTextBuffer().ContentType;
            var textBuffer = bufferFactory.CreateTextBuffer(contentType);
            var textView = editorFactory.CreateTextView(textBuffer);

            var workspace = new CSharpDebuggerWorkspace(context, textView, textBuffer, hostServices);

            var solution = new Solution(workspace, SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()));
            var project = solution.AddProject("project", "project.dll", "C#");
            var document = project.AddDocument("document.cs", textBuffer.AsTextContainer().CurrentText);

            workspace.SetCurrentSolution(document.Project.Solution);
            workspace.OnDocumentOpened(document.Id, textBuffer.AsTextContainer());
            return workspace;
        }

        internal static bool ClassDesigner = false;
    }
}
