﻿using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Net;


namespace Mango.Services.OrderAPI
{
    public class TokenConfig
    {
        public static TokenValidationParameters GetCognitoTokenValidationParams()
        {
            try
            {
                var cognitoIssuer = "https://cognito-idp.ap-south-1.amazonaws.com/ap-south-1_uOpPoSOMn";

                var jwtKeySetUrl = $"{cognitoIssuer}/.well-known/jwks.json";

                var cognitoAudience = "4giebmk031hhrfpbcv76e1vqbs";

                return new TokenValidationParameters
                {
                    IssuerSigningKeyResolver = (s, securityToken, identifier, parameters) =>
                    {
                        // get JsonWebKeySet from AWS 
                        var json = new WebClient().DownloadString(jwtKeySetUrl);

                        // serialize the result 
                        var keys = JsonConvert.DeserializeObject<JsonWebKeySet>(json).Keys;

                        // cast the result to be the type expected by IssuerSigningKeyResolver 
                        return (IEnumerable<SecurityKey>)keys;
                    },
                    ValidIssuer = cognitoIssuer,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidAudience = cognitoAudience
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}