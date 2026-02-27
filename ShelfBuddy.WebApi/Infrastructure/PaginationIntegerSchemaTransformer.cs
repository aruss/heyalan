namespace ShelfBuddy.WebApi.Infrastructure;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

public class PaginationIntegerSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (!IsIntegerStringUnion(schema))
        {
            return Task.CompletedTask;
        }

        schema.Type = JsonSchemaType.Integer;
        schema.Pattern = null;

        return Task.CompletedTask;
    }

    private static bool IsIntegerStringUnion(OpenApiSchema schema)
    {
        return (schema.Type & JsonSchemaType.Integer) == JsonSchemaType.Integer
            && (schema.Type & JsonSchemaType.String) == JsonSchemaType.String;
    }
}
