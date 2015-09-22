// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    public partial class Solution
    {
        private partial class CompilationTracker
        {
            private class MetadataReferenceComparer : IEqualityComparer<MetadataReference>
            {
                public readonly static MetadataReferenceComparer Instance = new MetadataReferenceComparer();

                private MetadataReferenceComparer()
                {
                }

                bool IEqualityComparer<MetadataReference>.Equals(MetadataReference x, MetadataReference y)
                {
                    var compilationReference1 = x as CompilationReference;
                    if (compilationReference1 != null)
                    {
                        var compilationReference2 = y as CompilationReference;
                        if (compilationReference2 != null)
                        {
                            return compilationReference1.Compilation == compilationReference2.Compilation;
                        }
                    }

                    return x == y;
                }

                int IEqualityComparer<MetadataReference>.GetHashCode(MetadataReference obj)
                {
                    var compilationReference = obj as CompilationReference;
                    return compilationReference?.Compilation.GetHashCode() ?? obj.GetHashCode();
                }
            }
        }
    }
}
