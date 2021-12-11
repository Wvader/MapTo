using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MapTo.Extensions;
using MapTo.Sources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapTo
{
    internal static class MappingContextExtensions
    {
        internal static ImmutableArray<MappedProperty> GetReadOnlyMappedProperties(this ImmutableArray<MappedProperty> mappedProperties) => mappedProperties.Where(p => p.isReadOnly).ToImmutableArray()!;
        internal static ImmutableArray<MappedProperty> GetWritableMappedProperties(this ImmutableArray<MappedProperty> mappedProperties) => mappedProperties.Where(p => !p.isReadOnly).ToImmutableArray()!;
    }

    internal abstract class MappingContext
    {
        private readonly List<SymbolDisplayPart> _ignoredNamespaces;

        protected MappingContext(Compilation compilation, SourceGenerationOptions sourceGenerationOptions, TypeDeclarationSyntax typeSyntax)
        {
            _ignoredNamespaces = new();
            Diagnostics = ImmutableArray<Diagnostic>.Empty;
            Usings = ImmutableArray.Create("System", Constants.RootNamespace);
            SourceGenerationOptions = sourceGenerationOptions;
            TypeSyntax = typeSyntax;
            Compilation = compilation;

            IgnorePropertyAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(IgnorePropertyAttributeSource.FullyQualifiedName);
            MapTypeConverterAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(MapTypeConverterAttributeSource.FullyQualifiedName);
            TypeConverterInterfaceTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(ITypeConverterSource.FullyQualifiedName);
            MapPropertyAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(MapPropertyAttributeSource.FullyQualifiedName);
            MapFromAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(MapFromAttributeSource.FullyQualifiedName);
            UseUpdateAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(UseUpdateAttributeSource.FullyQualifiedName);
            JsonExtensionAttributeTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(UseUpdateAttributeSource.FullyQualifiedName);

            MappingContextTypeSymbol = compilation.GetTypeByMetadataNameOrThrow(MappingContextSource.FullyQualifiedName);

            AddUsingIfRequired(sourceGenerationOptions.SupportNullableStaticAnalysis, "System.Diagnostics.CodeAnalysis");
        }

        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }

        public MappingModel? Model { get; private set; }

        protected Compilation Compilation { get; }

        protected INamedTypeSymbol IgnorePropertyAttributeTypeSymbol { get; }

        protected INamedTypeSymbol MapFromAttributeTypeSymbol { get; }
        
        protected INamedTypeSymbol UseUpdateAttributeTypeSymbol { get; }

        protected INamedTypeSymbol JsonExtensionAttributeTypeSymbol { get; }

        protected INamedTypeSymbol MappingContextTypeSymbol { get; }

        protected INamedTypeSymbol MapPropertyAttributeTypeSymbol { get; }

        protected INamedTypeSymbol MapTypeConverterAttributeTypeSymbol { get; }

        protected SourceGenerationOptions SourceGenerationOptions { get; }

        protected INamedTypeSymbol TypeConverterInterfaceTypeSymbol { get; }

        protected TypeDeclarationSyntax TypeSyntax { get; }

        protected ImmutableArray<string> Usings { get; private set; }

        public static MappingContext Create(Compilation compilation, SourceGenerationOptions sourceGenerationOptions, TypeDeclarationSyntax typeSyntax)
        {
            MappingContext context = typeSyntax switch
            {
                StructDeclarationSyntax => new StructMappingContext(compilation, sourceGenerationOptions, typeSyntax),
                ClassDeclarationSyntax => new ClassMappingContext(compilation, sourceGenerationOptions, typeSyntax),
                RecordDeclarationSyntax => new RecordMappingContext(compilation, sourceGenerationOptions, typeSyntax),
                _ => throw new ArgumentOutOfRangeException()
            };

            context.Model = context.CreateMappingModel();

            return context;
        }

        protected void AddDiagnostic(Diagnostic diagnostic)
        {
            Diagnostics = Diagnostics.Add(diagnostic);
        }

        protected void AddUsingIfRequired(ISymbol? namedTypeSymbol) =>
            AddUsingIfRequired(namedTypeSymbol?.ContainingNamespace.IsGlobalNamespace == false, namedTypeSymbol?.ContainingNamespace);

        protected void AddUsingIfRequired(bool condition, INamespaceSymbol? ns) =>
            AddUsingIfRequired(condition && ns is not null && !_ignoredNamespaces.Contains(ns.ToDisplayParts().First()), ns?.ToDisplayString());

        protected void AddUsingIfRequired(bool condition, string? ns)
        {
            if (ns is not null && condition && ns != TypeSyntax.GetNamespace() && !Usings.Contains(ns))
            {
                Usings = Usings.Add(ns);
            }
        }

        protected ISymbol? FindSourceProperty(IEnumerable<ISymbol> sourceMembers, ISymbol member)
        {
            var propertyName = member
                .GetAttribute(MapPropertyAttributeTypeSymbol)
                ?.NamedArguments
                .SingleOrDefault(a => a.Key == MapPropertyAttributeSource.SourcePropertyNamePropertyName)
                .Value.Value as string ?? member.Name;
            foreach(var sourceProperty in sourceMembers)
            {
                if(sourceProperty is IPropertySymbol propertySymbol)
                {
                    if (propertySymbol.Name == propertyName) return sourceProperty;
                }
                if (sourceProperty is IFieldSymbol fieldSymbol)
                {
                    if (fieldSymbol.Name == propertyName) return sourceProperty;
                }

            }

            return sourceMembers.SingleOrDefault(p => p.Name == propertyName);
        }

        protected abstract ImmutableArray<MappedProperty> GetSourceMappedMembers(ITypeSymbol typeSymbol, ITypeSymbol sourceTypeSymbol, bool isInheritFromMappedBaseClass);
        protected abstract ImmutableArray<MappedProperty> GetTypeMappedMembers(ITypeSymbol typeSymbol, ITypeSymbol sourceTypeSymbol, bool isInheritFromMappedBaseClass);

        protected INamedTypeSymbol? GetSourceTypeSymbol(TypeDeclarationSyntax typeDeclarationSyntax, SemanticModel? semanticModel = null) =>
            GetSourceTypeSymbol(typeDeclarationSyntax.GetAttribute(MapFromAttributeSource.AttributeName), semanticModel);

        protected INamedTypeSymbol? GetSourceTypeSymbol(SyntaxNode? attributeSyntax, SemanticModel? semanticModel = null)
        {
            if (attributeSyntax is null)
            {
                return null;
            }

            semanticModel ??= Compilation.GetSemanticModel(attributeSyntax.SyntaxTree);
            var sourceTypeExpressionSyntax = attributeSyntax
                .DescendantNodes()
                .OfType<TypeOfExpressionSyntax>()
                .SingleOrDefault();

            return sourceTypeExpressionSyntax is not null ? semanticModel.GetTypeInfo(sourceTypeExpressionSyntax.Type).Type as INamedTypeSymbol : null;
        }

        protected bool IsTypeInheritFromMappedBaseClass(SemanticModel semanticModel)
        {
            return TypeSyntax.BaseList is not null && TypeSyntax.BaseList.Types
                .Select(t => semanticModel.GetTypeInfo(t.Type).Type)
                .Any(t => t?.GetAttribute(MapFromAttributeTypeSymbol) != null);
        }

        protected bool IsTypeUpdatable()
        {
            return TypeSyntax.GetAttribute("UseUpdate") != null;
        }

        protected bool IsJsonExtension()
        {
            return TypeSyntax.GetAttribute("JsonExtension") != null;
        }

        protected virtual MappedProperty? MapProperty(ISymbol sourceTypeSymbol, IReadOnlyCollection<ISymbol> sourceProperties, ISymbol property)
        {
            var sourceProperty = FindSourceProperty(sourceProperties, property);
            if (sourceProperty is null || !property.TryGetTypeSymbol(out var propertyType))
            {
                return null;
            }

           
            string? converterFullyQualifiedName = null;
            var converterParameters = ImmutableArray<string>.Empty;
            ITypeSymbol? mappedSourcePropertyType = null;
            ITypeSymbol? enumerableTypeArgumentType = null;

            if (!Compilation.HasCompatibleTypes(sourceProperty, property))
            {
                if (!TryGetMapTypeConverter(property, sourceProperty, out converterFullyQualifiedName, out converterParameters) &&
                    !TryGetNestedObjectMappings(property, out mappedSourcePropertyType, out enumerableTypeArgumentType))
                {
                    return null;
                }
            }

            AddUsingIfRequired(propertyType);
            AddUsingIfRequired(enumerableTypeArgumentType);
            AddUsingIfRequired(mappedSourcePropertyType);

            bool isReadOnly = false;

            if(property is IPropertySymbol pSymbol)
            {
                isReadOnly = pSymbol.IsReadOnly;
            }
            if (property is IFieldSymbol fSymbol)
            {
                isReadOnly = fSymbol.IsReadOnly;
            }

            return new MappedProperty(
                property.Name,
                property.GetTypeSymbol().ToString(),
                ToQualifiedDisplayName(propertyType) ?? propertyType.Name,
                converterFullyQualifiedName,
                converterParameters.ToImmutableArray(),
                sourceProperty.Name,
                ToQualifiedDisplayName(mappedSourcePropertyType),
                ToQualifiedDisplayName(enumerableTypeArgumentType),
               isReadOnly);
;
        }
        protected virtual MappedProperty? MapPropertySimple(ISymbol sourceTypeSymbol, ISymbol property)
        {
            if (!property.TryGetTypeSymbol(out var propertyType))
            {
                return null;
            }


            string? converterFullyQualifiedName = null;
            var converterParameters = ImmutableArray<string>.Empty;
            ITypeSymbol? mappedSourcePropertyType = null;
            ITypeSymbol? enumerableTypeArgumentType = null;


            AddUsingIfRequired(propertyType);
            AddUsingIfRequired(enumerableTypeArgumentType);
            AddUsingIfRequired(mappedSourcePropertyType);

            bool isReadOnly = false;

            if (property is IPropertySymbol pSymbol)
            {
                isReadOnly = pSymbol.IsReadOnly;
            }
            if (property is IFieldSymbol fSymbol)
            {
                isReadOnly = fSymbol.IsReadOnly;
            }
            return new MappedProperty(
                property.Name,
                property.GetTypeSymbol().ToString(),
                ToQualifiedDisplayName(propertyType) ?? propertyType.Name,
                converterFullyQualifiedName,
                converterParameters.ToImmutableArray(),
                property.Name,
                ToQualifiedDisplayName(mappedSourcePropertyType),
                ToQualifiedDisplayName(enumerableTypeArgumentType),
               isReadOnly);
            ;
        }
        protected bool TryGetMapTypeConverter(ISymbol property, ISymbol sourceMember, out string? converterFullyQualifiedName,
            out ImmutableArray<string> converterParameters)
        {
            converterFullyQualifiedName = null;
            converterParameters = ImmutableArray<string>.Empty;

            if (!Diagnostics.IsEmpty())
            {
                return false;
            }

            var typeConverterAttribute = property.GetAttribute(MapTypeConverterAttributeTypeSymbol);
            if (typeConverterAttribute?.ConstructorArguments.First().Value is not INamedTypeSymbol converterTypeSymbol)
            {
                return false;
            }

            var baseInterface = GetTypeConverterBaseInterface(converterTypeSymbol, property, sourceMember);
            if (baseInterface is null)
            {
                AddDiagnostic(DiagnosticsFactory.InvalidTypeConverterGenericTypesError(property, sourceMember));
                return false;
            }

            converterFullyQualifiedName = converterTypeSymbol.ToDisplayString();
            converterParameters = GetTypeConverterParameters(typeConverterAttribute);
            return true;
        }

        protected bool TryGetNestedObjectMappings(ISymbol property, out ITypeSymbol? mappedSourcePropertyType, out ITypeSymbol? enumerableTypeArgument)
        {
            mappedSourcePropertyType = null;
            enumerableTypeArgument = null;

            if (!Diagnostics.IsEmpty())
            {
                return false;
            }

            if (!property.TryGetTypeSymbol(out var propertyType))
            {
                AddDiagnostic(DiagnosticsFactory.NoMatchingPropertyTypeFoundError(property));
                return false;
            }

            var mapFromAttribute = propertyType.GetAttribute(MapFromAttributeTypeSymbol);
            if (mapFromAttribute is null &&
                propertyType is INamedTypeSymbol namedTypeSymbol &&
                !propertyType.IsPrimitiveType() &&
                (Compilation.IsGenericEnumerable(propertyType) || propertyType.AllInterfaces.Any(i => Compilation.IsGenericEnumerable(i))))
            {
                enumerableTypeArgument = namedTypeSymbol.TypeArguments.First();
                mapFromAttribute = enumerableTypeArgument.GetAttribute(MapFromAttributeTypeSymbol);
            }

            mappedSourcePropertyType = mapFromAttribute?.ConstructorArguments.First().Value as INamedTypeSymbol;

            if (mappedSourcePropertyType is null && enumerableTypeArgument is null)
            {
                AddDiagnostic(DiagnosticsFactory.NoMatchingPropertyTypeFoundError(property));
            }

            return Diagnostics.IsEmpty();
        }

        private static ImmutableArray<string> GetTypeConverterParameters(AttributeData typeConverterAttribute)
        {
            var converterParameter = typeConverterAttribute.ConstructorArguments.Skip(1).FirstOrDefault();
            return converterParameter.IsNull
                ? ImmutableArray<string>.Empty
                : converterParameter.Values.Where(v => v.Value is not null).Select(v => v.Value!.ToSourceCodeString()).ToImmutableArray();
        }

        private MappingModel? CreateMappingModel()
        {
            var semanticModel = Compilation.GetSemanticModel(TypeSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(TypeSyntax) is not INamedTypeSymbol typeSymbol)
            {
                AddDiagnostic(DiagnosticsFactory.TypeNotFoundError(TypeSyntax.GetLocation(), TypeSyntax.Identifier.ValueText));
                return null;
            }

            var sourceTypeSymbol = GetSourceTypeSymbol(TypeSyntax, semanticModel);
            if (sourceTypeSymbol is null)
            {
                AddDiagnostic(DiagnosticsFactory.MapFromAttributeNotFoundError(TypeSyntax.GetLocation()));
                return null;
            }

            _ignoredNamespaces.Add(sourceTypeSymbol.ContainingNamespace.ToDisplayParts().First());

            var typeIdentifierName = TypeSyntax.GetIdentifierName();
            var sourceTypeIdentifierName = sourceTypeSymbol.Name;
            var isTypeInheritFromMappedBaseClass = IsTypeInheritFromMappedBaseClass(semanticModel);
            var isTypeUpdatable = IsTypeUpdatable();
            var isJsonExtension = IsJsonExtension();
            var shouldGenerateSecondaryConstructor = ShouldGenerateSecondaryConstructor(semanticModel, sourceTypeSymbol);

            var mappedProperties = GetSourceMappedMembers(typeSymbol, sourceTypeSymbol, isTypeInheritFromMappedBaseClass);
          /*  if (!mappedProperties.Any())
            {
                AddDiagnostic(DiagnosticsFactory.NoMatchingPropertyFoundError(TypeSyntax.GetLocation(), typeSymbol, sourceTypeSymbol));
                return null;
            }*/

            AddUsingIfRequired(mappedProperties.Any(p => p.IsEnumerable), "System.Linq");

            var allProperties = GetTypeMappedMembers(sourceTypeSymbol, typeSymbol , isTypeInheritFromMappedBaseClass);

            return new MappingModel(
                SourceGenerationOptions,
                TypeSyntax.GetNamespace(),
                TypeSyntax.Modifiers,
                TypeSyntax.Keyword.Text,
                typeIdentifierName,
                sourceTypeSymbol.ContainingNamespace.ToDisplayString(),
                sourceTypeIdentifierName,
                sourceTypeSymbol.ToDisplayString(),
                isTypeUpdatable,
                isJsonExtension,
                mappedProperties,
                allProperties,
                isTypeInheritFromMappedBaseClass,
                Usings,
                shouldGenerateSecondaryConstructor);
        }

       

        private INamedTypeSymbol? GetTypeConverterBaseInterface(ITypeSymbol converterTypeSymbol, ISymbol property, ISymbol sourceMember)
        {
            if (!property.TryGetTypeSymbol(out var propertyType))
            {
                return null;
            }
            ISymbol? type = null;

            if(sourceMember is IPropertySymbol propertySymbol)
            {
                type = propertySymbol.Type;
            }

            if (sourceMember is IFieldSymbol fieldSymbol)
            {
                type = fieldSymbol.Type;
            }

            if (type == null) return null;

            return converterTypeSymbol.AllInterfaces
                .SingleOrDefault(i =>
                    i.TypeArguments.Length == 2 &&
                    SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, TypeConverterInterfaceTypeSymbol) &&
                    SymbolEqualityComparer.Default.Equals(type, i.TypeArguments[0]) &&
                    SymbolEqualityComparer.Default.Equals(propertyType, i.TypeArguments[1]));
        }

        private bool ShouldGenerateSecondaryConstructor(SemanticModel semanticModel, ISymbol sourceTypeSymbol)
        {
            var constructorSyntax = TypeSyntax.DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .SingleOrDefault(c =>
                    c.ParameterList.Parameters.Count == 1 &&
                    SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(c.ParameterList.Parameters.Single().Type!).ConvertedType, sourceTypeSymbol));

            if (constructorSyntax is null)
            {
                // Secondary constructor is not defined.
                return true;
            }

            if (constructorSyntax.Initializer?.ArgumentList.Arguments is not { Count: 2 } arguments ||
                !SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(arguments[0].Expression).ConvertedType, MappingContextTypeSymbol) ||
                !SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(arguments[1].Expression).ConvertedType, sourceTypeSymbol))
            {
                AddDiagnostic(DiagnosticsFactory.MissingConstructorArgument(constructorSyntax));
            }

            return false;
        }

        private string? ToQualifiedDisplayName(ISymbol? symbol)
        {
            if (symbol is null)
            {
                return null;
            }

            var containingNamespace = TypeSyntax.GetNamespace();
            var symbolNamespace = symbol.ContainingNamespace.ToDisplayString();
            return  containingNamespace != symbolNamespace && _ignoredNamespaces.Contains(symbol.ContainingNamespace.ToDisplayParts().First())
                ? symbol.ToDisplayString()
                : symbol.Name;
        }
    }
}