using MapTo.Extensions;
using static MapTo.Sources.Constants;
using System.Collections.Generic;

namespace MapTo.Sources
{
    internal static class MapStructSource
    {
        internal static SourceCode Generate(MappingModel model)
        {
            using var builder = new SourceBuilder()
                .WriteLine(GeneratedFilesHeader)
                .WriteNullableContextOptionIf(model.Options.SupportNullableReferenceTypes)
                .WriteUsings(model.Usings)
                .WriteLine()

                // Namespace declaration
                .WriteLine($"namespace {model.Namespace}")
                .WriteOpeningBracket()

                // Class declaration
                .WriteLine($"partial struct {model.TypeIdentifierName}")
                .WriteOpeningBracket()
                .WriteLine()

            // Class body
                .GeneratePublicConstructor(model);

            if (!AllPropertiesAreReadOnly(model))
            {
                builder.GenerateUpdateMethod(model);
            }

            builder
                    .WriteLine()
                    // End class declaration
                    .WriteClosingBracket()
                    .WriteLine()

                    // End namespace declaration
                    .WriteClosingBracket();

            return new(builder.ToString(), $"{model.Namespace}.{model.TypeIdentifierName}.g.cs");
        }
        private static bool AllPropertiesAreReadOnly(MappingModel model)
        {
            foreach (var property in model.SourceProperties)
            {
                if (!property.isReadOnly) return false;
            }
            return true;

        }


        private static SourceBuilder GenerateSecondaryConstructor(this SourceBuilder builder, MappingModel model)
        {
            var sourceClassParameterName = model.SourceTypeIdentifierName.ToCamelCase();

            if (model.Options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine($"/// Initializes a new instance of the <see cref=\"{model.TypeIdentifierName}\"/> struct")
                    .WriteLine($"/// using the property values from the specified <paramref name=\"{sourceClassParameterName}\"/>.")
                    .WriteLine("/// </summary>")
                    .WriteLine($"/// <exception cref=\"ArgumentNullException\">{sourceClassParameterName} is null</exception>");
            }

            return builder
                .WriteLine($"{model.Options.ConstructorAccessModifier.ToLowercaseString()} {model.TypeIdentifierName}({model.SourceType} {sourceClassParameterName})")
                .WriteLine($"    : this(new {MappingContextSource.ClassName}(), {sourceClassParameterName}) {{ }}");
        }


        private static SourceBuilder GeneratePublicConstructor(this SourceBuilder builder, MappingModel model)
        {
            var sourceClassParameterName = model.SourceTypeIdentifierName.ToCamelCase();
            const string mappingContextParameterName = "context";

            var baseConstructor = /*model.HasMappedBaseClass ? $" : base({mappingContextParameterName}, {sourceClassParameterName})" :*/ string.Empty;

            var readOnlyProperties = model.TypeProperties.GetReadOnlyMappedProperties();

            var readOnlyFields = "";

            for (int i = 0; i < readOnlyProperties.Length; i++)
            {
                var property = readOnlyProperties[i];
                readOnlyFields += $"{property.Type} {property.SourcePropertyName.ToCamelCase()}";
                if (i != readOnlyProperties.Length - 1) readOnlyFields += " ,";
            }


            builder
                .WriteLine($"public {model.TypeIdentifierName}({model.SourceType} {sourceClassParameterName}{(string.IsNullOrEmpty(readOnlyFields) ? "" : $", {readOnlyFields}")}){baseConstructor}")
                .WriteOpeningBracket()
                .TryWriteProperties(model.SourceProperties, readOnlyProperties, sourceClassParameterName, mappingContextParameterName, false);

            // End constructor declaration
            return builder.WriteClosingBracket();
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