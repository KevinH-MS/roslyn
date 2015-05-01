using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SemanticModelWorkspaceService;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense2
{
    internal abstract class AbstractDebuggerSemanticModelLanguageService : ISemanticModelService, IWorkspaceServiceFactory, IDebuggerSemanticModelLanguageService
    {
        private TextSpan contextSpan;
        private Document contextDocument = null;

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return this;
        }

        private async Task<SyntaxNode> GetOriginalNode()
        {
            var root = await contextDocument.GetSyntaxRootAsync().ConfigureAwait(false);
            return root.FindNode(contextSpan);
        }

        protected virtual SyntaxNode GetOriginalNode(SyntaxNode originalNode)
        {
            return originalNode;
        }

        internal abstract bool TryGetSpeculativeModel(SyntaxNode originalNode, SyntaxNode parentStatement, SemanticModel parentSemanticModel, out SemanticModel speculativeModel);
        internal abstract SyntaxNode GetStatementForSpeculation(SyntaxNode node);

        public async Task<SemanticModel> GetSemanticModelForNodeAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(contextDocument);

            SyntaxNode originalNode = GetOriginalNode(await GetOriginalNode().ConfigureAwait(false));
            SyntaxNode statement = GetStatementForSpeculation(node);
            var parentSemanticModel = await contextDocument.GetSemanticModelAsync().ConfigureAwait(false);
            SemanticModel speculativeModel = null;
            if (document.Project.Solution.Workspace.Kind != WorkspaceKind.Debugger ||
                !TryGetSpeculativeModel(originalNode, statement, parentSemanticModel, out speculativeModel))
            {
                return await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            }

            return speculativeModel;
        }

        public void UpdateContext(Document document, TextSpan textSpan)
        {
            contextDocument = document;
            contextSpan = textSpan;
        }
    }
}
