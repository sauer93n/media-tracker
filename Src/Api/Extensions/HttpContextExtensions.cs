using Domain.Entity;

namespace Api.Extensions;

public static class HttpContextExtensions
{
    private const string DomainUserKey = "DomainUser";

    /// <summary>
    /// Gets the domain User model from the current HttpContext
    /// </summary>
    public static User? GetDomainUser(this HttpContext context)
    {
        if (context.Items.TryGetValue(DomainUserKey, out var user) && user is User domainUser) return domainUser;

        return null;
    }

    /// <summary>
    /// Gets the domain User model from the current HttpContext, throwing if not found
    /// </summary>
    public static User GetDomainUserOrThrow(this HttpContext context)
    {
        var user = context.GetDomainUser();
        if (user == null)
            throw new InvalidOperationException("Domain user not found in HttpContext. Ensure the request is authenticated.");
            
        return user;
    }
}
