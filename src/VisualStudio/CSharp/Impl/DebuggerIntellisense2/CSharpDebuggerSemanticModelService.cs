using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SemanticModelWorkspaceService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense2
{
    [ExportLanguageService(typeof(IDebuggerSemanticModelLanguageService), LanguageNames.CSharp, ServiceLayer.Host), System.Composition.Shared]
    internal class CSharpDebuggerSemanticModelService : AbstractDebuggerSemanticModelLanguageService
    {
        internal override SyntaxNode GetStatementForSpeculation(SyntaxNode node)
        {
            return node.GetAncestor<StatementSyntax>() ?? node;
        }

        internal override bool TryGetSpeculativeModel(SyntaxNode originalNode, SyntaxNode parentStatement, SemanticModel parentSemanticModel, out SemanticModel speculativeModel)
        {
            if (SpeculationAnalyzer.CanSpeculateOnNode(parentStatement))
            {
                speculativeModel = SpeculationAnalyzer.CreateSpeculativeSemanticModelForNode(originalNode, parentStatement, parentSemanticModel);
                if (speculativeModel == null)
                {
                }

                return true;
            }

            speculativeModel = null;
            return false;
        }
    }
}
