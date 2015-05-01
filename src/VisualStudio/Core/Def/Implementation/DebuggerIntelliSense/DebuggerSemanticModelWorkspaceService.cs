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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense2;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense
{
    [ExportWorkspaceServiceFactory(typeof(ISemanticModelService), "Debugger"), System.Composition.Shared]
    internal class DebuggerSemanticModelWorkspaceService : ISemanticModelService, IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return this;
        }

        public Task<SemanticModel> GetSemanticModelForNodeAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            return document.GetLanguageService<IDebuggerSemanticModelLanguageService>().GetSemanticModelForNodeAsync(document, node, cancellationToken);
        }
    }
}
