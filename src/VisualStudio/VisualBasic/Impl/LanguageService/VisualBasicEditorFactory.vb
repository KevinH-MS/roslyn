' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.Implementation

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic
    <Guid(Guids.VisualBasicEditorFactoryIdString)>
    Friend Class VisualBasicEditorFactory
        Inherits AbstractEditorFactory

        Public Sub New(package As VisualBasicPackage)
            MyBase.New(package)
        End Sub

        Protected Overrides Function GetContentTypeNameFromFileExtension(fileExtension As String) As String
            Return If(fileExtension = ".vbx", ContentTypeNames.VisualBasicScriptContentType, ContentTypeNames.VisualBasicContentType)
        End Function

        Protected Overrides Function GetFormattedTextChanges(workspace As VisualStudioWorkspace, filePath As String, text As SourceText, cancellationToken As CancellationToken) As IList(Of TextChange)
            Dim root = SyntaxFactory.ParseSyntaxTree(text, path:=filePath, cancellationToken:=cancellationToken).GetRoot(cancellationToken)
            Return Formatter.GetFormattedTextChanges(root, workspace, cancellationToken:=cancellationToken)
        End Function
    End Class
End Namespace
