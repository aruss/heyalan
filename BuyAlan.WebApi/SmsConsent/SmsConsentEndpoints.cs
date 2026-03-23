namespace BuyAlan.WebApi.SmsConsent;

using BuyAlan.Data;
using BuyAlan.Data.Entities;
using Microsoft.AspNetCore.Mvc;

public static class SmsConsentEndpoints
{
    private const string PrivacyPageConsentSource = "privacy-page";

    public static IEndpointRouteBuilder MapSmsConsentEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        RouteGroupBuilder routeGroup = routeBuilder
            .MapGroup("/sms")
            .WithTags("SmsConsent");

        routeGroup
            .MapPost("/subscribe", CreateSmsConsentAsync)
            .AllowAnonymous()
            .Produces<CreateSmsConsentResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return routeBuilder;
    }

    private static async Task<IResult> CreateSmsConsentAsync(
        [FromBody] CreateSmsConsentInput input,
        MainDataContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!IsPlausiblePhoneNumber(input.PhoneNumber))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid phone number",
                detail: "A valid phone number is required.");
        }

        SmsConsent subscription = new()
        {
            PhoneNumber = input.PhoneNumber!,
            TransactionalConsent = input.TransactionalConsent ?? false,
            MarketingConsent = input.MarketingConsent ?? false,
            ConsentSource = PrivacyPageConsentSource
        };

        dbContext.SmsConsents.Add(subscription);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new CreateSmsConsentResult(true));
    }

    private static bool IsPlausiblePhoneNumber(string? phoneNumber)
    {
        if (String.IsNullOrWhiteSpace(phoneNumber))
        {
            return false;
        }

        string trimmedPhoneNumber = phoneNumber.Trim();
        int digitCount = 0;

        foreach (char character in trimmedPhoneNumber)
        {
            if (Char.IsDigit(character))
            {
                digitCount++;
                continue;
            }

            if (character is '+' or '(' or ')' or '-' or '.' || Char.IsWhiteSpace(character))
            {
                continue;
            }

            return false;
        }

        return digitCount >= 7 && digitCount <= 20;
    }
}
