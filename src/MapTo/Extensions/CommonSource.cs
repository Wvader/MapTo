using MapTo.Sources;
using static MapTo.Sources.Constants;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

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

            if (writeDebugInfo)
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

            if (model.IsJsonExtension) builder.WriteToJsonMethod(model);
            if (model.IsTypeUpdatable && model.TypeProperties.GetWritableMappedProperties().Length > 0) builder.GenerateUpdateMethod(model);
            if (model.IsTypeUpdatable && model.TypeFields.GetWritableMappedProperties().Length > 0) builder.GenerateUpdateMethod(model);

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

        private static bool IsMappedProperty(this System.Collections.Immutable.ImmutableArray<MappedMember> properties, MappedMember property)
        {

            foreach (var prop in properties)
            {
                if (prop.Name == property.Name) return true;
            }

            return false;
        }

        private static SourceBuilder WriteToJsonMethod(this SourceBuilder builder, MappingModel model)
        {
            builder
                .WriteLine($"public string ToJson()")
                .WriteOpeningBracket()
                .WriteLine("var stringBuilder = new System.Text.StringBuilder();")
                .WriteLine(GetStringBuilderAppendNoInterpolation("{"));

            foreach (var property in model.TypeProperties)
            {
                if (!property.isEnumerable)
                    HandlePropertyEnumerable(builder, property);
                else
                {
                    builder = WriteJsonField(builder, property);
                }
            }
            foreach (var property in model.TypeFields)
            {
                if (!property.isEnumerable)
                    HandleFieldEnumerable(builder, property);
                else
                {
                    builder.WriteLine(GetStringBuilderAppend($"\\\"{property.Name.ToCamelCase()}\\\" : [{GetJsonArrayValue(property, ref builder)}],"));
                }
            }

            builder.WriteLine(GetStringBuilderAppendNoInterpolation("}"));
            builder.WriteLine("return stringBuilder.ToString();");
            builder.WriteClosingBracket();
            return builder;
        }

        private static SourceBuilder WriteJsonField(SourceBuilder builder, MappedMember property)
        {
            builder.WriteLine(
                GetStringBuilderAppend(
                    $"\\\"{property.Name.ToCamelCase()}\\\" : [{GetJsonArrayValue(property, ref builder)}],"));
            return builder;
        }

        private static void HandleEnumerable(SourceBuilder builder, MappedMember property)
        {
            var symbol = property.ActualSymbol as IPropertySymbol;
            builder.WriteCommentArray(symbol.Parameters, nameof(symbol.Parameters));
            builder.WriteCommentArray(symbol.TypeCustomModifiers, nameof(symbol.TypeCustomModifiers));

            builder.WriteComment($"Is enumerable {(property.ActualSymbol as IPropertySymbol).Parameters}");
            builder.WriteLine(
                GetStringBuilderAppend($"\\\"{property.Name.ToCamelCase()}\\\" : {GetJsonValue(property, builder)},"));
        }

        
        private static void HandleFieldEnumerable(SourceBuilder builder, MappedMember property)
        {
            HandleEnumerable(builder, property);
        }

        private static void HandlePropertyEnumerable(SourceBuilder builder, MappedMember property)
        {
            HandleEnumerable(builder, property);
        }

        private static string GetJsonArrayValue(MappedMember member, ref SourceBuilder builder)
        {
            if (member.isEnumerable)
            {
                // get underlying type (check if is a json extension)

                builder.WriteLine("var arrStrBuilder = new StringBuilder();");
                
                foreach (var named in member.NamedTypeSymbol?.TypeArguments!)
                {
                    bool? containedTypeIsJsonEXtension = named?.HasAttribute(MappingContext.JsonExtensionAttributeSymbol);
                    if (!containedTypeIsJsonEXtension.HasValue) continue;
                    builder.WriteLine($"foreach (var v in {member.SourcePropertyName.ToString()})");
                    builder.WriteOpeningBracket();
                    builder.WriteLine("arrStrBuilder.Append(v.ToJson());");
                    builder.WriteLine("arrStrBuilder.Append(\", \");");
                    builder.WriteClosingBracket();
                }
                builder.WriteLine("arrStrBuilder.Remove(arrStrBuilder.Length -1, 1);"); 
            }

            return "{arrStrBuilder.ToString()}";
        }
        private static string GetJsonValue(MappedMember member, SourceBuilder builder)
        {

            if (member.FullyQualifiedType == "string") return $"\\\"{{{member.SourcePropertyName}}}\\\"";
            if (member.FullyQualifiedType is "int" or "double" or "float" or "long") return $"{{{member.SourcePropertyName}}}";

            return "";
        }

        private static string GetStringBuilderAppend(string stringToAppend)
        {
            return $"stringBuilder.Append($\"{stringToAppend}\");";
        }
        private static string GetStringBuilderAppendNoInterpolation(string stringToAppend)
        {
            return $"stringBuilder.Append(\"{stringToAppend}\");";
        }

        private static SourceBuilder WriteAssignmentMethod(this SourceBuilder builder, MappingModel model, System.Collections.Immutable.ImmutableArray<MappedMember>? otherProperties,
            string? sourceClassParameterName, string mappingContextParameterName, bool fromUpdate)
        {

            foreach (var property in model.SourceProperties)
            {
                if (property.isReadOnly && fromUpdate) continue;

                builder.WriteLine($"{property.Name} = {sourceClassParameterName}.{property.SourcePropertyName};");

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
        
        private static SourceBuilder GenerateEnumerableJsonSourceTypeExtensionMethod(this SourceBuilder builder, MappingModel model)
        {
            var sourceClassParameterName = model.SourceTypeIdentifierName.ToCamelCase();

            return builder
                .WriteLineIf(model.Options.SupportNullableStaticAnalysis, $"[return: NotNullIfNotNull(\"{sourceClassParameterName}\")]")
                .WriteLine($"{model.Options.GeneratedMethodsAccessModifier.ToLowercaseString()} static string ToJson(this IEnumerable<{model.SourceType}{model.Options.NullableReferenceSyntax}> {sourceClassParameterName}List)")
                .WriteOpeningBracket()
                .WriteLine($"return {sourceClassParameterName} == null ? null : new {model.TypeIdentifierName}({sourceClassParameterName});")
                .WriteClosingBracket();
        }
    }
}
