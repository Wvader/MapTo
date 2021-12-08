using MapTo.Sources;
using static MapTo.Sources.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapTo.Extensions
{
    internal static class CommonSource
    {
        internal static SourceCode GenerateStructOrClass(this MappingModel model, string structOrClass)
        {
            const bool writeDebugInfo = true;

            using var builder = new SourceBuilder()
                .WriteLine(GeneratedFilesHeader)
                .WriteNullableContextOptionIf(model.Options.SupportNullableReferenceTypes)
                .WriteUsings(model.Usings)
                .WriteLine()

                // Namespace declaration
                .WriteLine($"namespace {model.Namespace}")
                .WriteOpeningBracket();

            if(writeDebugInfo)
            builder
                .WriteModelInfo(model)
                .WriteLine()
                .WriteComment("Type properties")
                .WriteComment()
                .WriteMappedProperties(model.TypeProperties)
                .WriteLine()
                .WriteComment("Source properties")
                .WriteMappedProperties(model.SourceProperties)
                .WriteLine();

            builder
                // Class declaration
                .WriteLine($"partial {structOrClass} {model.TypeIdentifierName}")
                .WriteOpeningBracket()
                .WriteLine()
                // Class body
                .GeneratePublicConstructor(model);

            if (model.IsTypeUpdatable && model.TypeProperties.GetWritableMappedProperties().Length > 0) builder.GenerateUpdateMethod(model);

            builder
                .WriteLine()
                // End class declaration
                .WriteClosingBracket()
                .WriteLine()
                // End namespace declaration
                .WriteClosingBracket();

            return new(builder.ToString(), $"{model.Namespace}.{model.TypeIdentifierName}.g.cs");
        }

        private static SourceBuilder GeneratePublicConstructor(this SourceBuilder builder, MappingModel model)
        {
            var sourceClassParameterName = model.SourceTypeIdentifierName.ToCamelCase();
            const string mappingContextParameterName = "context";

            var baseConstructor = /*model.HasMappedBaseClass ? $" : base({mappingContextParameterName}, {sourceClassParameterName})" :*/ string.Empty;

            var readOnlyProperties = model.TypeProperties.GetReadOnlyMappedProperties();

            var stringBuilder = new StringBuilder();

            for (int i = 0; i < readOnlyProperties.Length; i++)
            {
                var property = readOnlyProperties[i];
                if(!model.SourceProperties.IsMappedProperty(property))
                {
                    stringBuilder.Append(", ");
                    stringBuilder.Append($"{property.FullyQualifiedType} {property.SourcePropertyName.ToCamelCase()}");
                }
                
            }

            var readOnlyPropertiesArguments = stringBuilder.ToString();

            builder
                .WriteLine($"public {model.TypeIdentifierName}({model.SourceType} {sourceClassParameterName}{readOnlyPropertiesArguments}){baseConstructor}")
                .WriteOpeningBracket()
                .TryWriteProperties(model.SourceProperties, readOnlyProperties, sourceClassParameterName, mappingContextParameterName, false);

            // End constructor declaration
            return builder.WriteClosingBracket();
        }

        private static bool IsMappedProperty(this System.Collections.Immutable.ImmutableArray<MappedProperty> properties, MappedProperty property) {

            foreach(var prop in properties)
            {
                if (prop.FullyQualifiedType == property.FullyQualifiedType) return true;
            }

            return false;
        }

        private static SourceBuilder TryWriteProperties(this SourceBuilder builder, System.Collections.Immutable.ImmutableArray<MappedProperty> properties, System.Collections.Immutable.ImmutableArray<MappedProperty>? otherProperties,
            string? sourceClassParameterName, string mappingContextParameterName, bool fromUpdate)
        {
            if (fromUpdate)
            {
                properties = properties.GetWritableMappedProperties();
            }

            foreach (var property in properties)
            {
                if (property.isReadOnly && fromUpdate) continue;

                if (property.TypeConverter is null)
                {
                    if (property.IsEnumerable)
                    {
                        builder.WriteLine(
                            $"{property.Name} = {sourceClassParameterName}.{property.SourcePropertyName}.Select({mappingContextParameterName}.{MappingContextSource.MapMethodName}<{property.MappedSourcePropertyTypeName}, {property.EnumerableTypeArgument}>).ToList();");
                    }
                    else
                    {
                        builder.WriteLine(property.MappedSourcePropertyTypeName is null
                            ? $"{property.Name} = {sourceClassParameterName}.{property.SourcePropertyName};"
                            : "");
                    }
                }
                else
                {
                    var parameters = property.TypeConverterParameters.IsEmpty
                        ? "null"
                        : $"new object[] {{ {string.Join(", ", property.TypeConverterParameters)} }}";

                    builder.WriteLine(
                        $"{property.Name} = new {property.TypeConverter}().Convert({sourceClassParameterName}.{property.SourcePropertyName}, {parameters});");
                }

            }


            if (otherProperties == null) return builder;

            foreach (var property in otherProperties)
            {
                if(!properties.IsMappedProperty(property))
                    builder.WriteLine(property.MappedSourcePropertyTypeName is null
                    ? $"{property.Name} = {property.SourcePropertyName.ToCamelCase()};"
                    : "");

            }

            return builder;

        }


        private static SourceBuilder GenerateUpdateMethod(this SourceBuilder builder, MappingModel model)
        {
            var sourceClassParameterName = model.SourceTypeIdentifierName.ToCamelCase();

            builder
               .GenerateUpdaterMethodsXmlDocs(model, sourceClassParameterName)
               .WriteLine($"public void Update({model.SourceType} {sourceClassParameterName})")
               .WriteOpeningBracket()
               .TryWriteProperties(model.SourceProperties, null, sourceClassParameterName, "context", true)
               .WriteClosingBracket();

            return builder;
        }

        private static SourceBuilder GenerateUpdaterMethodsXmlDocs(this SourceBuilder builder, MappingModel model, string sourceClassParameterName)
        {
            if (!model.Options.GenerateXmlDocument)
            {
                return builder;
            }

            return builder
                .WriteLine("/// <summary>")
                .WriteLine($"/// Updates <see cref=\"{model.TypeIdentifierName}\"/> and sets its participating properties")
                .WriteLine($"/// using the property values from <paramref name=\"{sourceClassParameterName}\"/>.")
                .WriteLine("/// </summary>")
                .WriteLine($"/// <param name=\"{sourceClassParameterName}\">The instance of <see cref=\"{model.SourceType}\"/> to use as source.</param>");
        }
    }
}
