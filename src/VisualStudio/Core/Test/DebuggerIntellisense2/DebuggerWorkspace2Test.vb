Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.CSharp.Completion.Providers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.DebuggerIntelliSense2
    Public Class DebuggerWorkspace2Test
        <Fact>
        Public Sub Test1()
            Dim context = <Workspace>
                              <Project Language="C#" CommonReferences="true">
                                  <Document>$$</Document>
                                  <Document><![CDATA[using System;

namespace ConsoleApplication15
{
    class Program
    {
        static void Main(string[] args)
        {
            int m = 3;
            Func<int, int> z = (q) => [|q + 1|];
            z(2);
        }
    }
}
]]></Document>
                              </Project>
                          </Workspace>

            Dim TestState = Microsoft.VisualStudio.LanguageServices.UnitTests.DebuggerIntelliSense2.TestState.CreateCSharpTestState(context, isImmediateWindow:=False)
            Dim workspace = TestState.context
            Dim document = workspace.CurrentSolution.Projects.First().Documents.First()

            Dim text = <a>
class C
{
    void foo() 
    {
        [|var __o = |]
    }
}</a>.Value.NormalizeLineEndings()

            Dim output As String = Nothing
            Dim textspan As TextSpan = Nothing
            MarkupTestFile.GetSpan(text, output, textspan)

            TestState.TextView.TextBuffer.Insert(0, output)
            TestState.TextView.Caret.MoveTo(New SnapshotPoint(TestState.TextView.TextBuffer.CurrentSnapshot, textspan.End))

            TestState.SendInvokeCompletionList()
            TestState.AssertCompletionSession()
            TestState.AssertCompletionItemsContainAll("System", "Program", "m")
        End Sub

        <Fact>
        Public Sub Test2()
            Dim context = <Workspace>
                              <Project Language="C#" CommonReferences="true">
                                  <Document>$$</Document>
                                  <Document><![CDATA[using System;

namespace ConsoleApplication15
{
    class Program
    {
        static void Main(string[] args)
        {
            Func<int, int> z = (q) => [|q + 1|];
            z(2);
        }
    }
}
]]></Document>
                              </Project>
                          </Workspace>

            Dim TestState = Microsoft.VisualStudio.LanguageServices.UnitTests.DebuggerIntelliSense2.TestState.CreateCSharpTestState(context, isImmediateWindow:=False)

            Dim workspace = TestState.context
            Dim document = workspace.CurrentSolution.Projects.First().Documents.First()

            Dim text = <a>
class C
{
    void foo() 
    {
        [|var x = z.|]
    }
}</a>.Value.NormalizeLineEndings()

            Dim output As String = Nothing
            Dim textspan As TextSpan = Nothing
            MarkupTestFile.GetSpan(text, output, textspan)

            ' workspace.SetSpan(textspan)

            TestState.TextView.TextBuffer.Insert(0, output)
            TestState.TextView.Caret.MoveTo(New SnapshotPoint(TestState.TextView.TextBuffer.CurrentSnapshot, textspan.End))

            TestState.SendInvokeCompletionList()
            TestState.AssertCompletionItemsContainAll("BeginInvoke")
        End Sub

        <Fact>
        Public Sub Test3()
            Dim context = <Workspace>
                              <Project Language="C#" CommonReferences="true">
                                  <Document>$$</Document>
                                  <Document><![CDATA[using System;

namespace ConsoleApplication15
{
    class Program
    {
        static void Main(string[] args)
        {
            Func<int, int> z = (q) => [|q + 1|];
            z(2);
        }
    }
}
]]></Document>
                              </Project>
                          </Workspace>

            Dim TestState = Microsoft.VisualStudio.LanguageServices.UnitTests.DebuggerIntelliSense2.TestState.CreateCSharpTestState(context, isImmediateWindow:=False)

            Dim workspace = TestState.context
            Dim document = workspace.CurrentSolution.Projects.First().Documents.First()

            Dim text = <a>
class C
{
    void foo() 
    {
        [|var x = |]
    }
}</a>.Value.NormalizeLineEndings()

            Dim output As String = Nothing
            Dim textspan As TextSpan = Nothing
            MarkupTestFile.GetSpan(text, output, textspan)

            ' workspace.SetSpan(textspan)

            TestState.TextView.TextBuffer.Insert(0, output)
            TestState.TextView.Caret.MoveTo(New SnapshotPoint(TestState.TextView.TextBuffer.CurrentSnapshot, textspan.End))

            TestState.SendInvokeCompletionList()
            TestState.AssertCompletionItemsContainAll("q")
        End Sub

        <Fact>
        Public Sub TestVB()
            Dim context = <Workspace>
                              <Project Language="Visual Basic" CommonReferences="true">
                                  <Document>$$</Document>
                                  <Document><![CDATA[using System;

Module Program
    Sub Main(args As String())
        Dim z = 4
    [|End Sub|]
End Module
]]></Document>
                              </Project>
                          </Workspace>

            Dim TestState = Microsoft.VisualStudio.LanguageServices.UnitTests.DebuggerIntelliSense2.TestState.CreateVisualBasicTestState(context, isImmediateWindow:=False)

            Dim workspace = TestState.context
            Dim document = workspace.CurrentSolution.Projects.First().Documents.First()

            Dim text = <a>
Class C
    Sub foo() 
        [|Dim __o = |]
    End Sub
End Class</a>.Value.NormalizeLineEndings()

            Dim output As String = Nothing
            Dim textspan As TextSpan = Nothing
            MarkupTestFile.GetSpan(text, output, textspan)

            ' workspace.SetSpan(textspan)

            TestState.TextView.TextBuffer.Insert(0, output)
            TestState.TextView.Caret.MoveTo(New SnapshotPoint(TestState.TextView.TextBuffer.CurrentSnapshot, textspan.End))

            TestState.SendInvokeCompletionList()
            TestState.AssertCompletionItemsContainAll("q")
        End Sub
    End Class
End Namespace
