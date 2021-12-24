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

        protected override ImmutableArray<MappedMember> GetSourceMappedFields(ITypeSymbol typeSymbol, ITypeSymbol sourceTypeSymbol, bool isInheritFromMappedBaseClass)
        {
            var sourceProperties = sourceTypeSymbol.GetAllMembers().OfType<IFieldSymbol>().ToArray();

            return typeSymbol
                .GetAllMembers()
                .OfType<IFieldSymbol>()
                .Where(p => !p.HasAttribute(IgnoreMemberAttributeTypeSymbol))
                .Select(property => MapField(sourceTypeSymbol, sourceProperties, property))
                .Where(mappedProperty => mappedProperty is not null)
                .ToImmutableArray()!;
        }

        protected override ImmutableArray<MappedMember> GetSourceMappedProperties(ITypeSymbol typeSymbol, ITypeSymbol sourceTypeSymbol, bool hasInheritedClass)
        {
            var sourceProperties = sourceTypeSymbol.GetAllMembers().OfType<IPropertySymbol>().ToArray();

            return typeSymbol
                .GetAllMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.HasAttribute(IgnoreMemberAttributeTypeSymbol))
                .Select(property => MapProperty(sourceTypeSymbol, sourceProperties, property))
                .Where(mappedProperty => mappedProperty is not null)
                .ToImmutableArray()!;
        }

        protected override ImmutableArray<MappedMember> GetTypeMappedFields(ITypeSymbol typeSymbol, ITypeSymbol sourceTypeSymbol, bool isInheritFromMappedBaseClass)
        {
            var sourceProperties = sourceTypeSymbol.GetAllMembers().OfType<IFieldSymbol>().ToArray();

            return sourceTypeSymbol
                .GetAllMembers()
                .OfType<IFieldSymbol>()
                .Where(p => !p.HasAttribute(IgnoreMemberAttributeTypeSymbol))
                .Select(property => MapFieldSimple(typeSymbol, property))
                .Where(mappedProperty => mappedProperty is not null)
                .ToImmutableArray()!;
        }

        protected override ImmutableArray<MappedMember> GetTypeMappedProperties(ITypeSymbol typeSymbol, ITypeSymbol sourceTypeSymbol, bool hasInheritedClass)
        {
            var sourceProperties = sourceTypeSymbol.GetAllMembers().OfType<IPropertySymbol>().ToArray();

            return sourceTypeSymbol
                .GetAllMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.HasAttribute(IgnoreMemberAttributeTypeSymbol))
                .Select(property => MapPropertySimple(typeSymbol, property))
                .Where(mappedProperty => mappedProperty is not null)
                .ToImmutableArray()!;
        }

    }
}