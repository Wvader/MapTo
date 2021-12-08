using System.Collections.Immutable;
using System.Linq;
using MapTo.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo
{
    internal class StructMappingContext : MappingContext
    {
        internal StructMappingContext(Compilation compilation, SourceGenerationOptions sourceGenerationOptions, TypeDeclarationSyntax typeSyntax)
            : base(compilation, sourceGenerationOptions, typeSyntax) { }

        protected override ImmutableArray<MappedProperty> GetSourceMappedProperties(ITypeSymbol typeSymbol, ITypeSymbol sourceTypeSymbol, bool hasInheritedClass)
        {
            var sourceProperties = sourceTypeSymbol.GetAllMembers().OfType<IPropertySymbol>().ToArray();

            return typeSymbol
                .GetAllMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.HasAttribute(IgnorePropertyAttributeTypeSymbol))
                .Select(property => MapProperty(sourceTypeSymbol, sourceProperties, property))
                .Where(mappedProperty => mappedProperty is not null)
                .ToImmutableArray()!;
        }
        protected override ImmutableArray<MappedProperty> GetTypeMappedProperties(ITypeSymbol typeSymbol, ITypeSymbol sourceTypeSymbol, bool hasInheritedClass)
        {
            var sourceProperties = sourceTypeSymbol.GetAllMembers().OfType<IPropertySymbol>().ToArray();

            return sourceTypeSymbol
                .GetAllMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.HasAttribute(IgnorePropertyAttributeTypeSymbol))
                .Select(property => MapPropertySimple(typeSymbol, property))
                .Where(mappedProperty => mappedProperty is not null)
                .ToImmutableArray()!;
        }

    }
}