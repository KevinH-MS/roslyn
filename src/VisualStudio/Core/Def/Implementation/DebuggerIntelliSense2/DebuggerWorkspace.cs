using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense2
{
    internal abstract class DebuggerWorkspace : Workspace
    {
        private ITextBuffer textBuffer;
        public IWpfTextView TextView;
        protected Document debuggerDocument;
        protected TextSpan span;

        protected DebuggerWorkspace(Document context, IWpfTextView textView, ITextBuffer textBuffer, Document debuggerDocument, HostServices hostServices)
            : base(hostServices, "Debugger")
        {
            this.Context = context;
            this.textBuffer = textBuffer;
            this.TextView = textView;
            this.debuggerDocument = debuggerDocument;
            semanticModel = Context.GetSemanticModelAsync().Result;
        }

        public void SetSpan(TextSpan span)
        {
            this.span = span;
        }

        public Document Context { get; private set; }
        private static SemanticModel semanticModel;
    }
}
