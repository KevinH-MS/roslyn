// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Debugging;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Debugging
{
    internal static class DataTipInfoGetter
    {
        internal static async Task<DebugDataTipInfo> GetInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var root = await document.GetCSharpSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);

            var expression = token.Parent as ExpressionSyntax;
            if (expression == null)
            {
                return token.IsKind(SyntaxKind.IdentifierToken)
                    ? new DebugDataTipInfo(token.Span, text: null)
                    : default(DebugDataTipInfo);
            }

            if (expression.IsAnyLiteralExpression())
            {
                // If the user hovers over a literal, give them a DataTip for the type of the
                // literal they're hovering over.
                // Partial semantics should always be sufficient because the (unconverted) type
                // of a literal can always easily be determined.
                var partialDocument = await document.WithFrozenPartialSemanticsAsync(cancellationToken).ConfigureAwait(false);
                var semanticModel = await partialDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var type = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
                return type == null
                    ? default(DebugDataTipInfo)
                    : new DebugDataTipInfo(expression.Span, type.ToNameDisplayString());
            }

            if (expression.IsRightSideOfDotOrArrow())
            {
                expression = (ExpressionSyntax)expression.Parent;
            }

            // NOTE(cyrusn): This behavior is to mimic what we did in Dev10, i'm not sure if it's
            // necessary or not.
            if (expression.IsKind(SyntaxKind.InvocationExpression))
            {
                expression = ((InvocationExpressionSyntax)expression).Expression;
            }

            string textOpt = null;
            var typeSyntax = expression as TypeSyntax;
            if (typeSyntax != null && typeSyntax.IsVar)
            {
                // If the user is hovering over 'var', then pass back the full type name that 'var'
                // binds to.
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var type = semanticModel.GetTypeInfo(typeSyntax, cancellationToken).Type;
                if (type != null)
                {
                    textOpt = type.ToNameDisplayString();
                }
            }

            return new DebugDataTipInfo(expression.Span, textOpt);
        }
    }
}
