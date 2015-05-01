using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.SemanticModelWorkspaceService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense2;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense
{
    internal abstract class AbstractDebuggerIntelliSenseContext : IDisposable
    {
        private readonly IWpfTextView textView;
        private readonly IContentType contentType;
        protected readonly IProjectionBufferFactoryService ProjectionBufferFactoryService;
        protected readonly Microsoft.VisualStudio.TextManager.Interop.TextSpan CurrentStatementSpan;
        private readonly IVsTextLines debuggerTextLines;
        private IProjectionBuffer projectionBuffer;
        private DebuggerTextView debuggerTextView;
        private DebuggerIntelliSenseWorkspace workspace;
        private ImmediateWindowContext immediateWindowContext;
        private readonly IBufferGraphFactoryService bufferGraphFactoryService;
        private readonly bool isImmediateWindow;

        private class ImmediateWindowContext
        {
            public int CurrentLineIndex = -1;
            public int QuestionIndex = -2;
            public IProjectionBuffer ElisionBuffer;
            public IProjectionBuffer ProjectionBuffer;
        }

        protected AbstractDebuggerIntelliSenseContext(
            IWpfTextView wpfTextView,
            IVsTextView vsTextView,
            IVsTextLines vsDebuggerTextLines,
            ITextBuffer contextBuffer,
            Microsoft.VisualStudio.TextManager.Interop.TextSpan[] currentStatementSpan,
            IComponentModel componentModel,
            IServiceProvider serviceProvider,
            IContentType contentType)
        {
            this.textView = wpfTextView;
            this.debuggerTextLines = vsDebuggerTextLines;
            this.ContextBuffer = contextBuffer;
            this.CurrentStatementSpan = currentStatementSpan[0];
            this.contentType = contentType;
            this.ProjectionBufferFactoryService = componentModel.GetService<IProjectionBufferFactoryService>();
            this.bufferGraphFactoryService = componentModel.GetService<IBufferGraphFactoryService>();
            this.isImmediateWindow = IsImmediateWindow((IVsUIShell)serviceProvider.GetService(typeof(SVsUIShell)), vsTextView);
        }

        // Constructor for testing
        protected AbstractDebuggerIntelliSenseContext(IWpfTextView wpfTextView,
            ITextBuffer contextBuffer,
            Microsoft.VisualStudio.TextManager.Interop.TextSpan[] currentStatementSpan,
            IComponentModel componentModel,
            IContentType contentType,
            bool isImmediateWindow)
        {
            this.textView = wpfTextView;
            this.ContextBuffer = contextBuffer;
            this.CurrentStatementSpan = currentStatementSpan[0];
            this.contentType = contentType;
            this.ProjectionBufferFactoryService = componentModel.GetService<IProjectionBufferFactoryService>();
            this.bufferGraphFactoryService = componentModel.GetService<IBufferGraphFactoryService>();
            this.isImmediateWindow = isImmediateWindow;
        }

        public IVsTextLines DebuggerTextLines { get { return this.debuggerTextLines; } }

        public ITextView DebuggerTextView { get { return this.debuggerTextView; } }

        public ITextBuffer Buffer { get { return this.projectionBuffer; } }

        public IContentType ContentType { get { return this.contentType; } }

        protected bool InImmediateWindow { get { return this.immediateWindowContext != null; } }

        protected ITextBuffer ContextBuffer { get; private set; }

        public abstract bool CompletionStartsOnQuestionMark { get; }

        protected abstract string StatementTerminator { get; }

        protected abstract int GetAdjustedContextPoint(int contextPoint, Document document);

        protected abstract ITrackingSpan GetPreviousStatementBufferAndSpan(int lastTokenEndPoint, Document document);

        // Since the immediate window doesn't actually tell us when we change lines, we'll have to
        // determine ourselves when to rebuild our tracking spans to include only the last (input)
        // line of the buffer.
        public void RebuildSpans()
        {
            // Not in the immediate window, no work to do.
            if (!this.InImmediateWindow)
            {
                return;
            }

            // Reset the question mark location, since we may have to search for one again.
            this.immediateWindowContext.QuestionIndex = -2;
            SetupImmediateWindowProjectionBuffer();
        }

        internal bool TryInitialize()
        {
            return this.TrySetContext(this.isImmediateWindow);
        }

        private bool TrySetContext(
            bool isImmediateWindow)
        {
            // Get the workspace, and from there, the solution and document containing this buffer.
            // If there's an ExternalSource, we won't get a document. Give up in that case.
            Document document = ContextBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                this.projectionBuffer = null;
                this.debuggerTextView = null;
                this.workspace = null;
                this.immediateWindowContext = null;
                return false;
            }

            var solution = document.Project.Solution;

            // Get the appropriate ITrackingSpan for the window the user is typing in
            var viewSnapshot = this.textView.TextSnapshot;
            this.immediateWindowContext = null;
            var debuggerMappedSpan = isImmediateWindow
                ? CreateImmediateWindowProjectionMapping(document, out this.immediateWindowContext)
                : viewSnapshot.CreateFullTrackingSpan(SpanTrackingMode.EdgeInclusive);

            this.projectionBuffer = AssembleProjection(debuggerMappedSpan, this.contentType, ProjectionBufferFactoryService);
            var bufferGraph = this.bufferGraphFactoryService.CreateBufferGraph(projectionBuffer);

            var debuggerView = new DebuggerTextView(this.textView, bufferGraph, isImmediateWindow);

            var start = ContextBuffer.CurrentSnapshot.GetLineFromLineNumber(CurrentStatementSpan.iStartLine).Start + CurrentStatementSpan.iStartIndex;
            var end = ContextBuffer.CurrentSnapshot.GetLineFromLineNumber(CurrentStatementSpan.iEndLine).Start + CurrentStatementSpan.iEndIndex;
            var span = new Microsoft.CodeAnalysis.Text.TextSpan(start, end - start);

            DebuggerWorkspace debuggerWorkspace = CreateDebuggerWorkspace(debuggerView, projectionBuffer, document);
            var semanticModelService = debuggerWorkspace.Context.GetLanguageService<IDebuggerSemanticModelLanguageService>();
            semanticModelService.UpdateContext(document, span);

            this.textView.TextBuffer.ChangeContentType(this.contentType, null);
            this.debuggerTextView = (DebuggerTextView)debuggerWorkspace.TextView;

            return true;
        }

        internal abstract IProjectionBuffer AssembleProjection(ITrackingSpan debuggerMappedSpan, IContentType contentType, IProjectionBufferFactoryService projectionFactory);
        internal abstract DebuggerWorkspace CreateDebuggerWorkspace(DebuggerTextView debuggerTextView, ITextBuffer textBuffer, Document contextDocument);

        private ITrackingSpan CreateImmediateWindowProjectionMapping(Document document, out ImmediateWindowContext immediateWindowContext)
        {
            var caretLine = this.textView.Caret.ContainingTextViewLine.Extent;
            var currentLineIndex = this.textView.TextSnapshot.GetLineNumberFromPosition(caretLine.Start.Position);

            var debuggerMappedSpan = this.textView.TextSnapshot.CreateFullTrackingSpan(SpanTrackingMode.EdgeInclusive);
            var projectionBuffer = this.ProjectionBufferFactoryService.CreateProjectionBuffer(null,
                new object[] { debuggerMappedSpan }, ProjectionBufferOptions.PermissiveEdgeInclusiveSourceSpans, this.contentType);

            // There's currently a bug in the editor (515925) where an elision buffer can't be projected into
            // another projection buffer.  So workaround by using a second projection buffer that only 
            // projects the text we care about
            var elisionProjectionBuffer = this.ProjectionBufferFactoryService.CreateProjectionBuffer(null,
                new object[] { projectionBuffer.CurrentSnapshot.CreateFullTrackingSpan(SpanTrackingMode.EdgeInclusive) },
                ProjectionBufferOptions.None, this.contentType);

            immediateWindowContext = new ImmediateWindowContext()
            {
                ProjectionBuffer = projectionBuffer,
                ElisionBuffer = elisionProjectionBuffer
            };

            this.textView.TextBuffer.PostChanged += TextBuffer_PostChanged;

            SetupImmediateWindowProjectionBuffer();

            return elisionProjectionBuffer.CurrentSnapshot.CreateFullTrackingSpan(SpanTrackingMode.EdgeInclusive);
        }

        private void TextBuffer_PostChanged(object sender, EventArgs e)
        {
            SetupImmediateWindowProjectionBuffer();
        }

        /// <summary>
        /// If there's a ? mark, we want to skip the ? mark itself, and include the text that follows it
        /// </summary>
        private void SetupImmediateWindowProjectionBuffer()
        {
            var caretLine = this.textView.Caret.ContainingTextViewLine.Extent;
            var currentLineIndex = this.textView.TextSnapshot.GetLineNumberFromPosition(caretLine.Start.Position);
            var questionIndex = GetQuestionIndex(caretLine.GetText());

            if (this.immediateWindowContext.QuestionIndex != questionIndex ||
                this.immediateWindowContext.CurrentLineIndex != currentLineIndex)
            {
                this.immediateWindowContext.QuestionIndex = questionIndex;
                this.immediateWindowContext.CurrentLineIndex = currentLineIndex;
                this.immediateWindowContext.ProjectionBuffer.DeleteSpans(0, this.immediateWindowContext.ProjectionBuffer.CurrentSnapshot.SpanCount);
                this.immediateWindowContext.ProjectionBuffer.InsertSpan(0, this.textView.TextSnapshot.CreateTrackingSpanFromIndexToEnd(caretLine.Start.Position + questionIndex + 1, SpanTrackingMode.EdgeInclusive));
            }
        }

        private int GetQuestionIndex(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    // Assume that the ? will be the first non-whitespace if it's being used as a
                    // command
                    return text[i] == '?' ? i : -1;
                }
            }

            return -1;
        }

        private bool IsImmediateWindow(IVsUIShell shellService, IVsTextView textView)
        {
            IEnumWindowFrames windowEnum = null;
            IVsTextLines buffer = null;
            Marshal.ThrowExceptionForHR(shellService.GetToolWindowEnum(out windowEnum));
            Marshal.ThrowExceptionForHR(textView.GetBuffer(out buffer));

            IVsWindowFrame[] frame = new IVsWindowFrame[1];
            uint value;

            var immediateWindowGuid = Guid.Parse(ToolWindowGuids80.ImmediateWindow);

            while (windowEnum.Next(1, frame, out value) == VSConstants.S_OK)
            {
                Guid toolWindowGuid;
                Marshal.ThrowExceptionForHR(frame[0].GetGuidProperty((int)__VSFPROPID.VSFPROPID_GuidPersistenceSlot, out toolWindowGuid));
                if (toolWindowGuid == immediateWindowGuid)
                {
                    IntPtr frameTextView;
                    Marshal.ThrowExceptionForHR(frame[0].QueryViewInterface(typeof(IVsTextView).GUID, out frameTextView));
                    try
                    {
                        var immediateWindowTextView = Marshal.GetObjectForIUnknown(frameTextView) as IVsTextView;
                        return textView == immediateWindowTextView;
                    }
                    finally
                    {
                        Marshal.Release(frameTextView);
                    }
                }
            }

            return false;
        }

        public void Dispose()
        {
            // Unsubscribe from events
            this.textView.TextBuffer.PostChanged -= TextBuffer_PostChanged;
            this.debuggerTextView.DisconnectFromIntellisenseControllers();

            // The buffer graph subscribes to events of its source buffers, we're no longer interested
            this.projectionBuffer.DeleteSpans(0, this.projectionBuffer.CurrentSnapshot.SpanCount);

            // The next request will use a new workspace
            if (this.workspace != null)
            {
                this.workspace.Dispose();
            }
        }
    }
}
