using System.Linq;
using System.Text;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace MapToTests
{
    public class Tests
    {
        private readonly ITestOutputHelper _output;
        private const string ExpectedAttribute = @"
using System;

namespace MapTo
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class MapFromAttribute : Attribute
    {
        public MapFromAttribute(Type sourceType)
        {
            SourceType = sourceType;
        }

        public Type SourceType { get; }
    }
}
";
        
        public Tests(ITestOutputHelper output)
        {
            _output = output;
        }
        
        [Fact]
        public void VerifyMapToAttribute()
        {
            // Arrange
            const string source = "";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source);
            
            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.ShouldContain(c => c.ToString() == ExpectedAttribute);
        }

        [Fact]
        public void When_NoMapToAttributeFound_Should_GenerateOnlyTheAttribute()
        {
            // Arrange
            const string source = "";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source);
            
            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.ShouldContain(s => s.ToString() == ExpectedAttribute);
            compilation.SyntaxTrees.Select(s => s.ToString()).Where(s => s != string.Empty && s != ExpectedAttribute).ShouldBeEmpty();
        }

        [Fact]
        public void When_MapToAttributeFound_Should_GenerateTheClass()
        {
            // Arrange
            const string source = @"
namespace Test
{
    [MapFrom(typeof(Baz)]
    public partial class Foo
    {
        
    }

    public class Baz 
    {
        public int Prop1 { get; set; }
        public int Prop2 { get;  }
        public int Prop3 { get; set; }
    }
}
";
            
            const string expectedResult = @"
// <auto-generated />
using System;

namespace Test
{
    public partial class Foo
    {
        public Foo(Baz baz)
        {
            if (baz == null) throw new ArgumentNullException(nameof(baz));
        }
";
            
            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source);
            
            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Count().ShouldBe(3);
            compilation.SyntaxTrees.Last().ToString().ShouldStartWith(expectedResult.Trim());
        }
        
        [Fact]
        public void When_MapToAttributeWithNamespaceFound_Should_GenerateTheClass()
        {
            // Arrange
            const string source = @"
namespace Test
{
    [MapTo.MapFrom(typeof(Baz)]
    public partial class Foo
    {
        
    }

    public class Baz 
    {
        
    }
}
";
            
            const string expectedResult = @"
// <auto-generated />
using System;

namespace Test
{
    public partial class Foo
    {
        public Foo(Baz baz)
        {
            if (baz == null) throw new ArgumentNullException(nameof(baz));
        }
";
            
            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source);
            
            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Count().ShouldBe(3);
            compilation.SyntaxTrees.Last().ToString().ShouldStartWith(expectedResult.Trim());
        }

        [Fact]
        public void When_SourceTypeHasMatchingProperties_Should_CreateConstructorAndAssignSrcToDest()
        {
            // Arrange
            var source = GetSourceText();
            
            const string expectedResult = @"
    public partial class Foo
    {
        public Foo(Baz baz)
        {
            if (baz == null) throw new ArgumentNullException(nameof(baz));
            Prop1 = baz.Prop1;
            Prop2 = baz.Prop2;
            Prop3 = baz.Prop3;
        }
";
            
            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source);
            
            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Count().ShouldBe(3);
            compilation.SyntaxTrees.Last().ToString().ShouldContain(expectedResult.Trim());
        }
        
        [Fact]
        public void When_SourceTypeHasMatchingProperties_Should_CreateFromStaticMethod()
        {
            // Arrange
            var source = GetSourceText();
            
            const string expectedResult = @"
        public static Foo From(Baz baz)
        {
            return baz == null ? null : new Foo(baz);
        }
";
            
            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source);
            
            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Count().ShouldBe(3);
            compilation.SyntaxTrees.Last().ToString().ShouldContain(expectedResult.Trim());
        }
        
        [Fact]
        public void When_SourceTypeHasDifferentNamespace_Should_AddToUsings()
        {
            // Arrange
            var source = GetSourceText(sourceClassNamespace: "Bazaar");
            
            const string expectedResult = @"
// <auto-generated />
using System;
using Bazaar;
";
            
            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source);
            
            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Count().ShouldBe(3);
            compilation.SyntaxTrees.Last().ToString().ShouldStartWith(expectedResult.Trim());
        }

        private static string GetSourceText(bool includeAttributeNamespace = false, string sourceClassNamespace = "Test")
        {
            var builder = new StringBuilder();
            builder.AppendLine($@"
namespace Test
{{
    {(sourceClassNamespace != "Test" && !includeAttributeNamespace ? $"using {sourceClassNamespace};": string.Empty)}

    {(includeAttributeNamespace ? "[MapTo.MapFrom(typeof(Baz))]" : "[MapFrom(typeof(Baz))]")}
    public partial class Foo
    {{
        public int Prop1 {{ get; set; }}
        public int Prop2 {{ get; }}
        public int Prop3 {{ get; }}
    }}
}}
");

            builder.AppendLine($@"
namespace {sourceClassNamespace}
{{
    public class Baz 
    {{
        public int Prop1 {{ get; set; }}
        public int Prop2 {{ get;  }}
        public int Prop3 {{ get; set; }}
    }}
}}
");

            return builder.ToString();
        }
    }
}
