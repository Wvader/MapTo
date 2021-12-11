using System.Collections.Immutable;
using System.Linq;
using MapTo.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo
{
    internal class ClassMappingContext : MappingContext
    {
        internal ClassMappingContext(Compilation compilation, SourceGenerationOptions sourceGenerationOptions, TypeDeclarationSyntax typeSyntax)
            : base(compilation, sourceGenerationOptions, typeSyntax) { }

        protected override ImmutableArray<MappedProperty> GetSourceMappedMembers(ITypeSymbol typeSymbol, ITypeSymbol sourceTypeSymbol, bool isInheritFromMappedBaseClass)
        {
            var sourceProperties = sourceTypeSymbol.GetAllMembers().ToArray();

            return typeSymbol
                .GetAllMembers(!isInheritFromMappedBaseClass)
                .Where(p => !p.HasAttribute(IgnorePropertyAttributeTypeSymbol))
                .Select(property => MapProperty(sourceTypeSymbol, sourceProperties, property))
                .Where(mappedProperty => mappedProperty is not null)
                .ToImmutableArray()!;
        }

        protected override ImmutableArray<MappedProperty> GetTypeMappedMembers(ITypeSymbol typeSymbol, ITypeSymbol sourceTypeSymbol, bool isInheritFromMappedBaseClass)
        {
            var sourceProperties = sourceTypeSymbol.GetAllMembers().ToArray();
           

            return typeSymbol
                .GetAllMembers()
                .Where(p => !p.HasAttribute(IgnorePropertyAttributeTypeSymbol))
                .Select(property => MapProperty(typeSymbol, sourceProperties, property))
                .Where(mappedProperty => mappedProperty is not null)
                .ToImmutableArray()!;
        }
    }
}