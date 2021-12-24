using System.Collections.Immutable;
using System.Linq;
using MapTo.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo
{
    internal class RecordMappingContext : MappingContext
    {
        internal RecordMappingContext(Compilation compilation, SourceGenerationOptions sourceGenerationOptions, TypeDeclarationSyntax typeSyntax)
            : base(compilation, sourceGenerationOptions, typeSyntax) { }

        protected override ImmutableArray<MappedMember> GetSourceMappedFields(ITypeSymbol typeSymbol, ITypeSymbol sourceTypeSymbol, bool isInheritFromMappedBaseClass)
        {
            throw new System.NotImplementedException();
        }

        protected override ImmutableArray<MappedMember> GetSourceMappedProperties(ITypeSymbol typeSymbol, ITypeSymbol sourceTypeSymbol, bool isInheritFromMappedBaseClass)
        {
            var sourceProperties = sourceTypeSymbol.GetAllMembers().OfType<IPropertySymbol>().ToArray();
            return typeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .OrderByDescending(s => s.Parameters.Length)
                .First(s => s.Name == ".ctor")
                .Parameters
                .Where(p => !p.HasAttribute(IgnoreMemberAttributeTypeSymbol))
                .Select(property => MapProperty(sourceTypeSymbol, sourceProperties, property))
                .Where(mappedProperty => mappedProperty is not null)
                .ToImmutableArray()!;
        }

        protected override ImmutableArray<MappedMember> GetTypeMappedFields(ITypeSymbol typeSymbol, ITypeSymbol sourceTypeSymbol, bool isInheritFromMappedBaseClass)
        {
            throw new System.NotImplementedException();
        }

        protected override ImmutableArray<MappedMember> GetTypeMappedProperties(ITypeSymbol typeSymbol, ITypeSymbol sourceTypeSymbol, bool isInheritFromMappedBaseClass)
        {
            var sourceProperties = sourceTypeSymbol.GetAllMembers().OfType<IPropertySymbol>().ToArray();
            return typeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .OrderByDescending(s => s.Parameters.Length)
                .First(s => s.Name == ".ctor")
                .Parameters
                .Where(p => !p.HasAttribute(IgnoreMemberAttributeTypeSymbol))
                .Select(property => MapProperty(typeSymbol, sourceProperties, property))
                .Where(mappedProperty => mappedProperty is not null)
                .ToImmutableArray()!;
        }
    }
}