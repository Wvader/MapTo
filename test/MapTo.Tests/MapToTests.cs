using System.Collections.Generic;
using System.Linq;
using MapTo.Sources;
using MapTo.Tests.Extensions;
using MapTo.Tests.Infrastructure;
using Shouldly;
using Xunit;
using static MapTo.Extensions.GeneratorExecutionContextExtensions;
using static MapTo.Tests.Common;

namespace MapTo.Tests
{
    public class MapToTests
    {
        private static readonly string ExpectedAttribute = $@"{Constants.GeneratedFilesHeader}
using System;

namespace MapTo
{{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class MapFromAttribute : Attribute
    {{
        public MapFromAttribute(Type sourceType)
        {{
            SourceType = sourceType;
        }}

        public Type SourceType {{ get; }}
    }}
}}";

        [Fact]
        public void VerifyMapToAttribute()
        {
            // Arrange
            const string source = "";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.ShouldContainSource(MapFromAttributeSource.AttributeClassName, ExpectedAttribute);
        }

        [Fact]
        public void When_FoundMatchingPropertyNameWithDifferentTypes_Should_ReportError()
        {
            // Arrange
            var source = GetSourceText(new SourceGeneratorOptions(
                true,
                PropertyBuilder: builder => { builder.WriteLine("public string Prop4 { get; set; }"); },
                SourcePropertyBuilder: builder => builder.WriteLine("public int Prop4 { get; set; }")));

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            var expectedError = DiagnosticProvider.NoMatchingPropertyTypeFoundError(GetSourcePropertySymbol("Prop4", compilation));

            diagnostics.ShouldBeUnsuccessful(expectedError);
        }

        [Fact]
        public void When_MappingsModifierOptionIsSetToInternal_Should_GenerateThoseMethodsWithInternalAccessModifier()
        {
            // Arrange
            var source = GetSourceText();
            var configOptions = new Dictionary<string, string>
            {
                [GetBuildPropertyName(nameof(SourceGenerationOptions.GeneratedMethodsAccessModifier))] = "Internal",
                [GetBuildPropertyName(nameof(SourceGenerationOptions.GenerateXmlDocument))] = "false"
            };

            var expectedExtension = @"    
    internal static partial class BazToFooExtensions
    {
        internal static Foo ToFoo(this Baz baz)
        {
            return baz == null ? null : new Foo(baz);
        }
    }".Trim();

            var expectedFactory = @"
        internal static Foo From(Baz baz)
        {
            return baz == null ? null : MappingContext.Create<Baz, Foo>(baz);
        }".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: configOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();

            var syntaxTree = compilation.SyntaxTrees.Last().ToString();
            syntaxTree.ShouldContain(expectedFactory);
            syntaxTree.ShouldContain(expectedExtension);
        }

        [Fact]
        public void When_MapToAttributeFound_Should_GenerateTheClass()
        {
            // Arrange
            const string source = @"
using MapTo;

namespace Test
{
    [MapFrom(typeof(Baz))]
    public partial class Foo
    {
        public int Prop1 { get; set; }
    }

    public class Baz 
    {
        public int Prop1 { get; set; }
    }
}
";

            const string expectedResult = @"
// <auto-generated />

using MapTo;
using System;

namespace Test
{
    partial class Foo
    {
        public Foo(Baz baz)
            : this(new MappingContext(), baz) { }

        private protected Foo(MappingContext context, Baz baz)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (baz == null) throw new ArgumentNullException(nameof(baz));

            context.Register(baz, this);

            Prop1 = baz.Prop1;
        }
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ToString().ShouldStartWith(expectedResult.Trim());
        }

        [Fact]
        public void When_MapToAttributeFoundWithoutMatchingProperties_Should_ReportError()
        {
            // Arrange
            const string source = @"
using MapTo;

namespace Test
{
    [MapFrom(typeof(Baz))]
    public partial class Foo { }

    public class Baz { public int Prop1 { get; set; } }
}
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source);

            // Assert
            var fooType = compilation.GetTypeByMetadataName("Test.Foo");
            fooType.ShouldNotBeNull();

            var bazType = compilation.GetTypeByMetadataName("Test.Baz");
            bazType.ShouldNotBeNull();

            var expectedDiagnostic = DiagnosticProvider.NoMatchingPropertyFoundError(fooType.Locations.Single(), fooType, bazType);
            var error = diagnostics.FirstOrDefault(d => d.Id == expectedDiagnostic.Id);
            error.ShouldNotBeNull();
        }

        [Fact]
        public void When_MapToAttributeWithNamespaceFound_Should_GenerateTheClass()
        {
            // Arrange
            const string source = @"
namespace Test
{
    [MapTo.MapFrom(typeof(Baz))]
    public partial class Foo { public int Prop1 { get; set; } }

    public class Baz { public int Prop1 { get; set; } }
}
";

            const string expectedResult = @"
// <auto-generated />

using MapTo;
using System;

namespace Test
{
    partial class Foo
    {
        public Foo(Baz baz)
            : this(new MappingContext(), baz) { }

        private protected Foo(MappingContext context, Baz baz)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (baz == null) throw new ArgumentNullException(nameof(baz));

            context.Register(baz, this);

            Prop1 = baz.Prop1;
        }
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ToString().ShouldStartWith(expectedResult.Trim());
        }

        [Fact]
        public void When_NoMapToAttributeFound_Should_GenerateOnlyTheAttribute()
        {
            // Arrange
            const string source = "";
            var expectedTypes = new[]
            {
                IgnorePropertyAttributeSource.AttributeName,
                MapFromAttributeSource.AttributeName,
                ITypeConverterSource.InterfaceName,
                MapPropertyAttributeSource.AttributeName
            };

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees
                .Select(s => s.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s.ToString()))
                .All(s => expectedTypes.Any(s.Contains))
                .ShouldBeTrue();
        }

        [Fact]
        public void When_SourceTypeHasDifferentNamespace_Should_NotAddToUsings()
        {
            // Arrange
            var source = GetSourceText(new SourceGeneratorOptions(SourceClassNamespace: "Bazaar"));

            const string expectedResult = @"
// <auto-generated />

using Bazaar;
using MapTo;
using System;

namespace Test
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ToString().ShouldStartWith(expectedResult.Trim());
        }

        [Fact]
        public void When_SourceTypeHasMatchingProperties_Should_CreateConstructorAndAssignSrcToDest()
        {
            // Arrange
            var source = GetSourceText();

            const string expectedResult = @"
    partial class Foo
    {
        public Foo(Baz baz)
            : this(new MappingContext(), baz) { }

        private protected Foo(MappingContext context, Baz baz)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (baz == null) throw new ArgumentNullException(nameof(baz));

            context.Register(baz, this);

            Prop1 = baz.Prop1;
            Prop2 = baz.Prop2;
            Prop3 = baz.Prop3;
        }
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ShouldContainPartialSource(expectedResult.Trim());
        }

        [Fact]
        public void When_SourceTypeHasMatchingProperties_Should_CreateFromStaticMethod()
        {
            // Arrange
            var source = GetSourceText();

            const string expectedResult = @"
        public static Foo From(Baz baz)
        {
            return baz == null ? null : MappingContext.Create<Baz, Foo>(baz);
        }
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ShouldContainPartialSource(expectedResult.Trim());
        }

        [Fact]
        public void When_SourceTypeHasMatchingProperties_Should_GenerateToExtensionMethodOnSourceType()
        {
            // Arrange
            var source = GetSourceText();

            const string expectedResult = @"
    public static partial class BazToFooExtensions
    {
        public static Foo ToFoo(this Baz baz)
        {
            return baz == null ? null : new Foo(baz);
        }
    }
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ShouldContainPartialSource(expectedResult.Trim());
        }

        [Fact]
        public void When_HasNestedObjectPropertyTypeHasMapFromAttribute_Should_UseContinueToMap()
        {
            // Arrange
            var source = GetSourceText(new SourceGeneratorOptions(
                SourceClassNamespace: "Test",
                PropertyBuilder: b => b.WriteLine("public B InnerProp1 { get; }"),
                SourcePropertyBuilder: b => b.WriteLine("public A InnerProp1 { get; }")));

            source += @"
namespace Test
{
    public class A { public int Prop1 { get; } }

    [MapTo.MapFrom(typeof(A))]
    public partial class B { public int Prop1 { get; }}
}
".Trim();
            
            var expectedResult = @"
    partial class Foo
    {
        public Foo(Baz baz)
            : this(new MappingContext(), baz) { }

        private protected Foo(MappingContext context, Baz baz)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (baz == null) throw new ArgumentNullException(nameof(baz));

            context.Register(baz, this);

            Prop1 = baz.Prop1;
            Prop2 = baz.Prop2;
            Prop3 = baz.Prop3;
            InnerProp1 = context.MapFromWithContext<A, B>(baz.InnerProp1);
        }
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.ToArray()[^2].ShouldContainPartialSource(expectedResult);
        }
        
        [Fact]
        public void When_HasNestedObjectPropertyTypeDoesNotHaveMapFromAttribute_Should_ReportError()
        {
            // Arrange
            var source = GetSourceText(new SourceGeneratorOptions(
                SourceClassNamespace: "Test",
                PropertyBuilder: b => b.WriteLine("public FooInner1 InnerProp1 { get; }"),
                SourcePropertyBuilder: b => b.WriteLine("public BazInner1 InnerProp1 { get; }")));

            source += @"
namespace Test
{
    public class FooInner1 { public int Prop1 { get; } }
    
    public partial class BazInner1 { public int Prop1 { get; }}
}
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            var expectedError = DiagnosticProvider.NoMatchingPropertyTypeFoundError(GetSourcePropertySymbol("InnerProp1", compilation));
            diagnostics.ShouldBeUnsuccessful(expectedError);
        }
        
        [Fact]
        public void When_HasNestedObjectPropertyTypeHasMapFromAttributeToDifferentType_Should_ReportError()
        {
            // Arrange
            var source = GetSourceText(new SourceGeneratorOptions(
                SourceClassNamespace: "Test",
                PropertyBuilder: b => b.WriteLine("public FooInner1 InnerProp1 { get; }"),
                SourcePropertyBuilder: b => b.WriteLine("public BazInner1 InnerProp1 { get; }")));

            source += @"
namespace Test
{
    public class FooInner1 { public int Prop1 { get; } }

    public class FooInner2 { public int Prop1 { get; } }

    [MapTo.MapFrom(typeof(FooInner2))]
    public partial class BazInner1 { public int Prop1 { get; }}
}
".Trim();

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            var expectedError = DiagnosticProvider.NoMatchingPropertyTypeFoundError(GetSourcePropertySymbol("InnerProp1", compilation));
            diagnostics.ShouldBeUnsuccessful(expectedError);
        }
        
        [Fact]
        public void When_SourceTypeEnumerableProperties_Should_CreateConstructorAndAssignSrcToDest()
        {
            // Arrange
            var source = GetSourceText(new SourceGeneratorOptions(
                Usings: new[] { "System.Collections.Generic"}, 
                PropertyBuilder: builder => builder.WriteLine("public IEnumerable<int> Prop4 { get; }"),
                SourcePropertyBuilder: builder => builder.WriteLine("public IEnumerable<int> Prop4 { get; }")));

            const string expectedResult = @"
    partial class Foo
    {
        public Foo(Baz baz)
            : this(new MappingContext(), baz) { }

        private protected Foo(MappingContext context, Baz baz)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (baz == null) throw new ArgumentNullException(nameof(baz));

            context.Register(baz, this);

            Prop1 = baz.Prop1;
            Prop2 = baz.Prop2;
            Prop3 = baz.Prop3;
            Prop4 = baz.Prop4;
        }
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(source, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ShouldContainPartialSource(expectedResult.Trim());
        }
        
        [Fact]
        public void When_DestinationTypeHasBaseClass_Should_CallBaseConstructor()
        {
            // Arrange
            var sources = GetEmployeeManagerSourceText();

            const string expectedResult = @"
private protected ManagerViewModel(MappingContext context, Manager manager) : base(context, manager)
{
    if (context == null) throw new ArgumentNullException(nameof(context));
    if (manager == null) throw new ArgumentNullException(nameof(manager));

    context.Register(manager, this);

    Level = manager.Level;
    Employees = manager.Employees.Select(context.MapFromWithContext<Employee, EmployeeViewModel>).ToList();
}
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(sources, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ShouldContainPartialSource(expectedResult);
        }
        
        [Fact]
        public void When_SourceTypeHasEnumerablePropertiesWithMapFromAttribute_Should_CreateANewEnumerableWithMappedObjects()
        {
            // Arrange
            var sources = GetEmployeeManagerSourceText();

            const string expectedResult = @"
using System;
using System.Collections.Generic;
using System.Linq;
using Test.Data.Models;

namespace Test.ViewModels
{
    partial class ManagerViewModel
    {
        public ManagerViewModel(Manager manager)
            : this(new MappingContext(), manager) { }

        private protected ManagerViewModel(MappingContext context, Manager manager) : base(context, manager)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (manager == null) throw new ArgumentNullException(nameof(manager));

            context.Register(manager, this);

            Level = manager.Level;
            Employees = manager.Employees.Select(context.MapFromWithContext<Employee, EmployeeViewModel>).ToList();
        }
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(sources, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ShouldContainPartialSource(expectedResult);
        }
        
        [Fact]
        public void When_SourceTypeHasEnumerablePropertiesWithMapFromAttributeInDifferentNamespaces_Should_CreateANewEnumerableWithMappedObjectsAndImportNamespace()
        {
            // Arrange
            var sources = GetEmployeeManagerSourceText(useDifferentViewModelNamespace: true);

            const string expectedResult = @"
using System;
using System.Collections.Generic;
using System.Linq;
using Test.Data.Models;
using Test.ViewModels;

namespace Test.ViewModels2
{
    partial class ManagerViewModel
    {
       public ManagerViewModel(Manager manager)
            : this(new MappingContext(), manager) { }

        private protected ManagerViewModel(MappingContext context, Manager manager) : base(context, manager)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (manager == null) throw new ArgumentNullException(nameof(manager));

            context.Register(manager, this);

            Level = manager.Level;
            Employees = manager.Employees.Select(context.MapFromWithContext<Employee, EmployeeViewModel>).ToList();
        }
";

            // Act
            var (compilation, diagnostics) = CSharpGenerator.GetOutputCompilation(sources, analyzerConfigOptions: DefaultAnalyzerOptions);

            // Assert
            diagnostics.ShouldBeSuccessful();
            compilation.SyntaxTrees.Last().ShouldContainPartialSource(expectedResult);
        }
    }
}