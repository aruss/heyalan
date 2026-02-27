namespace SquareBuddy.WebApi.Infrastructure;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

public class CamelCaseQueryParameterOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (operation.Parameters is null || operation.Parameters.Count == 0)
        {
            return Task.CompletedTask;
        }

        foreach (OpenApiParameter parameter in operation.Parameters)
        {
            if (parameter.In != ParameterLocation.Query || string.IsNullOrEmpty(parameter.Name))
            {
                continue;
            }

            string camelCaseName = ToCamelCase(parameter.Name);
            parameter.Name = camelCaseName;

            if (IsIntegerStringUnion(parameter.Schema))
            {
                NormalizeIntegerQuerySchema(parameter);
            }
        }

        return Task.CompletedTask;
    }

    private static string ToCamelCase(string value)
    {
        if (value.Length <= 1)
        {
            return value.ToLowerInvariant();
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    private static void NormalizeIntegerQuerySchema(OpenApiParameter parameter)
    {
        if (parameter.Schema is null)
        {
            return;
        }

        IOpenApiSchema currentSchema = parameter.Schema;
        OpenApiSchema normalizedSchema = new()
        {
            Type = JsonSchemaType.Integer,
            Format = currentSchema.Format,
            Minimum = currentSchema.Minimum,
            Maximum = currentSchema.Maximum,
            Default = currentSchema.Default,
            Pattern = null
        };

        parameter.Schema = normalizedSchema;
    }

    private static bool IsIntegerStringUnion(IOpenApiSchema? schema)
    {
        if (schema is null)
        {
            return false;
        }

        return (schema.Type & JsonSchemaType.Integer) == JsonSchemaType.Integer
            && (schema.Type & JsonSchemaType.String) == JsonSchemaType.String;
    }
}
