using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense2
{
    internal interface IDebuggerSemanticModelLanguageService : ILanguageService
    {
        void UpdateContext(Document document, TextSpan textSpan);
        Task<SemanticModel> GetSemanticModelForNodeAsync(Document document, SyntaxNode node, CancellationToken cancellationToken);
    }
}