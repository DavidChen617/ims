namespace Api.Endpoints.WellKnown;

// Ordering/Inventory validate JWTs via `options.Authority = ...`, which makes ASP.NET Core's
// JwtBearer handler fetch {Authority}/.well-known/openid-configuration to discover the JWKS
// endpoint, then fetch that to get signing keys — Organization has to actually serve both for
// that mechanism to work at all.
public static class WellKnownEndpoints
{
    extension(WebApplication app)
    {
        public WebApplication MapWellKnownEndpoints()
        {
            app.MapGet("/.well-known/openid-configuration", (IConfiguration configuration) =>
            {
                var issuer = configuration["JwtSetting:Issuer"];

                return Results.Ok(new
                {
                    issuer,
                    jwks_uri = $"{issuer}/.well-known/jwks.json",
                    id_token_signing_alg_values_supported = new[] { "RS256" }
                });
            });

            app.MapGet("/.well-known/jwks.json", (RsaSecurityKey key) =>
            {
                // 不暴露私鑰
                var parameters = key.Rsa!.ExportParameters(includePrivateParameters: false);

                var jwk = new
                {
                    kty = "RSA",
                    use = "sig",
                    alg = SecurityAlgorithms.RsaSha256,
                    kid = key.KeyId,
                    n = Base64UrlEncoder.Encode(parameters.Modulus),
                    e = Base64UrlEncoder.Encode(parameters.Exponent)
                };

                return Results.Ok(new { keys = new[] { jwk } });
            });

            return app;
        }
    }
}
