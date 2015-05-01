Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense
Imports Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense2
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic
    Friend Class VisualBasicDebuggerWorkspace
        Inherits DebuggerWorkspace

        Private context As Document
        Private hostServices As Object
        Private textBuffer As Object

        Public Sub New(context As Document, textView As IWpfTextView, textBuffer As ITextBuffer, hostServices As HostServices)
            MyBase.New(context, textView, textBuffer, context, hostServices)
        End Sub

        Friend Shared Function Create(textView As DebuggerTextView, textBuffer As ITextBuffer, context As Document) As DebuggerWorkspace
            Dim workspace = New VisualBasicDebuggerWorkspace(context, textView, textBuffer, context.Project.Solution.Workspace.Services.HostServices)

            Dim Solution = New Solution(workspace, SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()))
            Dim Project = Solution.AddProject("project", "project.dll", LanguageNames.VisualBasic)
            Dim Document = Project.AddDocument("document.vb", textBuffer.AsTextContainer().CurrentText)

            workspace.SetCurrentSolution(Document.Project.Solution)
            workspace.OnDocumentOpened(Document.Id, textBuffer.AsTextContainer())
            Return workspace
        End Function

        ' for testing
        Friend Shared Function Create(context As Document, hostServices As HostServices, bufferFactory As ITextBufferFactoryService, editorFactory As ITextEditorFactoryService) As DebuggerWorkspace
            Dim contentType = context.GetTextAsync().Result.Container.GetTextBuffer().ContentType
            Dim textBuffer = bufferFactory.CreateTextBuffer(contentType)
            Dim textView = editorFactory.CreateTextView(textBuffer)

            Dim workspace = New VisualBasicDebuggerWorkspace(context, textView, textBuffer, hostServices)

            Dim solution = New Solution(workspace, SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()))
            Dim project = solution.AddProject("project", "project.dll", "Visual Basic")
            Dim document = project.AddDocument("document.vb", textBuffer.AsTextContainer().CurrentText)

            workspace.SetCurrentSolution(document.Project.Solution)
            workspace.OnDocumentOpened(document.Id, textBuffer.AsTextContainer())
            Return workspace
        End Function

        Public Overrides Function TryApplyChanges(newSolution As Solution) As Boolean
            Return False
        End Function
    End Class
End Namespace
