using MapTo.Sources;
using static MapTo.Sources.Constants;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Immutable;

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
                .WriteLine()
                .WriteComment("Type fields")
                .WriteComment()
                .WriteMappedProperties(model.TypeFields)
                .WriteLine()
                .WriteComment("Source fields")
                .WriteMappedProperties(model.SourceFields)
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

            var stringBuilder = new StringBuilder();

            var otherProperties = new List<MappedMember>();

            foreach (var property in model.TypeProperties)
            {
                if (!model.SourceProperties.IsMappedProperty(property))
                {
                    stringBuilder.Append(", ");
                    stringBuilder.Append($"{property.FullyQualifiedType} {property.SourcePropertyName.ToCamelCase()}");
                    otherProperties.Add(property);
                }
            }

            foreach (var property in model.TypeFields)
            {
                if (!model.SourceFields.IsMappedProperty(property))
                {
                    stringBuilder.Append(", ");
                    stringBuilder.Append($"{property.FullyQualifiedType} {property.SourcePropertyName.ToCamelCase()}");
                    otherProperties.Add(property);
                }
            }


            var readOnlyPropertiesArguments = stringBuilder.ToString();

            builder
                .WriteLine($"public {model.TypeIdentifierName}({model.SourceType} {sourceClassParameterName}{readOnlyPropertiesArguments}){baseConstructor}")
                .WriteOpeningBracket()
                .WriteAssignmentMethod(model, otherProperties.ToArray().ToImmutableArray(), sourceClassParameterName, mappingContextParameterName, false);

            // End constructor declaration
            return builder.WriteClosingBracket();
        }

        private static bool IsMappedProperty(this System.Collections.Immutable.ImmutableArray<MappedMember> properties, MappedMember property) {

            foreach(var prop in properties)
            {
                if (prop.Name == property.Name) return true;
            }

            return false;
        }

        private static SourceBuilder WriteAssignmentMethod(this SourceBuilder builder, MappingModel model, System.Collections.Immutable.ImmutableArray<MappedMember>? otherProperties,
            string? sourceClassParameterName, string mappingContextParameterName, bool fromUpdate)
        {

            foreach (var property in model.SourceProperties)
            {
                if (property.isReadOnly && fromUpdate) continue;

                builder.WriteLine( $"{property.Name} = {sourceClassParameterName}.{property.SourcePropertyName};");

            }

            foreach (var property in model.SourceFields)
            {
                if (property.isReadOnly && fromUpdate) continue;

                builder.WriteLine($"{property.Name} = {sourceClassParameterName}.{property.SourcePropertyName};");

            }

            if (otherProperties == null) return builder;

            foreach (var property in otherProperties)
            {
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
               .WriteAssignmentMethod(model, null, sourceClassParameterName, "context", true)
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
