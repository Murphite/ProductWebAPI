namespace Products.Api.Authentication;

public static class ProductScopes
{
    public const string ClaimType = "scope";
    public const string Read = "products.read";
    public const string Write = "products.write";
}

public static class Policies
{
    public const string ReadProducts = nameof(ReadProducts);
    public const string WriteProducts = nameof(WriteProducts);
}
