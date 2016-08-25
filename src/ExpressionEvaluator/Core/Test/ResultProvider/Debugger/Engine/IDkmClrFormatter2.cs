// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// C:\Users\kevinh\.nuget\packages\Microsoft.VisualStudio.Debugger.Engine\14.3.25422\lib\net20\Microsoft.VisualStudio.Debugger.Engine.dll
#endregion

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.VisualStudio.Debugger.ComponentInterfaces
{
    public interface IDkmClrFormatter2
    {
        string GetEditableValueString(DkmClrValue clrValue, DkmInspectionContext inspectionContext, DkmClrCustomTypeInfo customTypeInfo);
        string GetValueString(DkmClrValue clrValue, DkmClrCustomTypeInfo customTypeInfo, DkmInspectionContext inspectionContext, ReadOnlyCollection<string> formatSpecifiers);
    }
}