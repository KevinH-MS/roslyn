using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    [ExportCompletionProvider("ClassDesignerCompletionProvider", LanguageNames.CSharp)]
    internal class ClassDesignerCompletionProvider : SymbolCompletionProvider
    {
        [ImportingConstructor]
        public ClassDesignerCompletionProvider()
        {
        }

        protected override async Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            return document.Project.Solution.Workspace.Kind == WorkspaceKind.Debugger
                ? await base.GetItemsWorkerAsync(document, position, triggerInfo, cancellationToken).ConfigureAwait(false)
                : null;
        }

        protected override async Task<IEnumerable<ISymbol>> GetSymbolsWorker(AbstractSyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            if (context.SyntaxTree.IsTypeDeclarationContext(context.Position, context.LeftToken, cancellationToken) ||
                context.SyntaxTree.IsMemberDeclarationContext(context.Position, context.LeftToken, cancellationToken))
            {
                var symbols = await base.GetSymbolsWorker(context, position, options, cancellationToken).ConfigureAwait(false);
                return symbols.Where(s => s.MatchesKind(SymbolKind.NamedType, SymbolKind.Namespace));
            }

            return null;
        }

        protected override async Task<bool> IsExclusiveAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var leftToken = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            return tree.IsTypeDeclarationContext(position, leftToken, cancellationToken) ||
                    tree.IsMemberDeclarationContext(position, leftToken, cancellationToken);
        }
    }
}
