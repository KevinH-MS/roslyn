Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense2

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic
    <ExportLanguageService(GetType(IDebuggerSemanticModelLanguageService), LanguageNames.VisualBasic, ServiceLayer.Host), System.Composition.Shared>
    Friend Class VisualBasicDebuggerSemanticModelService
        Inherits AbstractDebuggerSemanticModelLanguageService

        Friend Overrides Function GetStatementForSpeculation(node As SyntaxNode) As SyntaxNode
            Return If(node.GetAncestor(Of StatementSyntax), node)
        End Function

        Protected Overrides Function GetOriginalNode(originalNode As SyntaxNode) As SyntaxNode
            ' A method/property/etc block begin doesn't actually count as within the block, 
            ' so we can't get a speculative SemanticModel there. Get one from the end instead
            If TypeOf originalNode Is MethodBaseSyntax AndAlso TypeOf originalNode.Parent Is MethodBlockBaseSyntax Then
                Dim parent = DirectCast(originalNode.Parent, MethodBlockBaseSyntax)
                Return parent.End
            End If

            If TypeOf originalNode Is SingleLineLambdaExpressionSyntax Then
                Return DirectCast(originalNode, SingleLineLambdaExpressionSyntax).Body
            End If

            If originalNode.IsKind(CodeAnalysis.VisualBasic.SyntaxKind.FunctionLambdaHeader) OrElse
                    originalNode.IsKind(CodeAnalysis.VisualBasic.SyntaxKind.SubLambdaHeader) Then
                originalNode = originalNode.Parent
            End If

            If TypeOf originalNode Is MultiLineLambdaExpressionSyntax Then
                Return DirectCast(originalNode, MultiLineLambdaExpressionSyntax).Statements.First()
            End If

            Return MyBase.GetOriginalNode(originalNode)
        End Function

        Friend Overrides Function TryGetSpeculativeModel(originalNode As SyntaxNode, parentStatement As SyntaxNode, parentSemanticModel As SemanticModel, ByRef speculativeModel As SemanticModel) As Boolean
            If SpeculationAnalyzer.CanSpeculateOnNode(parentStatement) Then
                'speculativeModel = SpeculationAnalyzer.CreateSpeculativeSemanticModelForNode(parentStatement, parentSemanticModel, originalNode.SpanStart, False)
                speculativeModel = SpeculationAnalyzer.CreateSpeculativeSemanticModelForNode(originalNode, parentStatement, parentSemanticModel)
                Contract.ThrowIfNull(speculativeModel)
                Return True
            End If

            speculativeModel = Nothing
            Return False
        End Function
    End Class
End Namespace