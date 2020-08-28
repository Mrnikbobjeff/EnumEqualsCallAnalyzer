using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;

namespace EnumComparedByEqualsAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        [TestMethod]
        public void EmptySource_NoResults()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Nameof_NoResults()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public TypeName(string y)
            {
                var x = nameof(y);
            }
        }
    }";

            VerifyCSharpDiagnostic(test);
        }


        [TestMethod]
        public void MoreArguments_NoResults()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public TypeName()
            {
                var x = "".Equals("", StringComparison.Ordinal);
            }
        }
    }";

            VerifyCSharpDiagnostic(test);
        }

        public void CustomEquals_NoResults()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public TypeName()
            {
var y = new TypeName();
                var x = y.Equals(StringSplitOptions.None);
            }
        }
    }";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void SingleDiagnostic_AssignmentReplaceEqualsWithOpEq()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public TypeName()
            {
                var x = StringSplitOptions.None;
                var y = x.Equals(StringSplitOptions.RemoveEmptyEntries);
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "EnumComparedByEqualsAnalyzer",
                Message = String.Format("Replace '{0}' with '=='", "x.Equals"),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 11, 25)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void SingleDiagnostic_ComparisonReplaceEqualsWithOpEq()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public TypeName()
            {
                var x = StringSplitOptions.None;
                if(x.Equals(StringSplitOptions.RemoveEmptyEntries))
                    return;
            }
        }
    }";
            var fixtest = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public TypeName()
            {
                var x = StringSplitOptions.None;
                if(x == StringSplitOptions.RemoveEmptyEntries)
                    return;
            }
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void MultipleDiagnostics_ComparisonReplaceEqualsWithOpEq()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public TypeName()
            {
                var x = StringSplitOptions.None;
                if(x.Equals(StringSplitOptions.RemoveEmptyEntries))
                    return;
                if(x.Equals(StringSplitOptions.RemoveEmptyEntries))
                    return;
            }
        }
    }";
            var fixtest = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public TypeName()
            {
                var x = StringSplitOptions.None;
                if(x == StringSplitOptions.RemoveEmptyEntries)
                    return;
                if(x == StringSplitOptions.RemoveEmptyEntries)
                    return;
            }
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new EnumComparedByEqualsAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new EnumComparedByEqualsAnalyzerAnalyzer();
        }
    }
}
