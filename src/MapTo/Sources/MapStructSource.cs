using MapTo.Extensions;
using static MapTo.Sources.Constants;

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
                .WriteOpeningBracket();

                // Class body
            
                builder
                    .GeneratePrivateConstructor(model)
                    .WriteLine()
                    // End class declaration
                    .WriteClosingBracket()
                    .WriteLine()

                    // End namespace declaration
                    .WriteClosingBracket();
            
            return new(builder.ToString(), $"{model.Namespace}.{model.TypeIdentifierName}.g.cs");
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
        
        private static SourceBuilder GeneratePrivateConstructor(this SourceBuilder builder, MappingModel model)
        {
            var sourceClassParameterName = model.SourceTypeIdentifierName.ToCamelCase();
            const string mappingContextParameterName = "context";

            var baseConstructor = model.HasMappedBaseClass ? $" : base({mappingContextParameterName}, {sourceClassParameterName})" : string.Empty;

            builder
                .WriteLine($"private protected {model.TypeIdentifierName}({MappingContextSource.ClassName} {mappingContextParameterName}, {model.SourceType} {sourceClassParameterName}){baseConstructor}")
                .WriteOpeningBracket()
                .WriteLine()
                .WriteLine($"{mappingContextParameterName}.{MappingContextSource.RegisterMethodName}({sourceClassParameterName}, this);")
                .WriteLine().

            WriteProperties( model, sourceClassParameterName, mappingContextParameterName);

            // End constructor declaration
            return builder.WriteClosingBracket();
        }

        private static SourceBuilder WriteProperties(this SourceBuilder builder, MappingModel model,
            string? sourceClassParameterName, string mappingContextParameterName)
        {
            foreach (var property in model.MappedProperties)
            {
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
                            : $"{property.Name} = {mappingContextParameterName}.{MappingContextSource.MapMethodName}<{property.MappedSourcePropertyTypeName}, {property.Type}>({sourceClassParameterName}.{property.SourcePropertyName});");
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