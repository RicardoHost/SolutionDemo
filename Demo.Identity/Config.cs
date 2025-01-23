using Demo.Common.Constants.IdentityServerConstants;
using IdentityServer4.Models;
using IdentityServer4.Test;
using System.Collections.Generic;
using System.Security.Claims;

namespace Demo.Identity
{
    public class Config
    {
        public static IEnumerable<Client> GetClients()
        {
            return new List<Client>
                {
                    new Client
                    {
                        Enabled = true,
                        ClientId =  IdentityServerClientId.DEMO_WEBAPI,//客户端标识
                        ClientSecrets = { new Secret(IdentityServerClientSecret.DEMO_WEBAPI.Sha256()) },//客户端密码
                        RequireClientSecret = true,
                        RequirePkce = true,
                        AllowPlainTextPkce=false,
                        AllowedGrantTypes = GrantTypes.ClientCredentials,//客户端模式
                        AccessTokenLifetime=100000000,//token过期时间
                        AllowedScopes = { IdentityServerResourceName.DEMO_WEBAPI },
                        Properties = new Dictionary<string, string>()
                    },
                };
        }
        public static IEnumerable<ApiResource> GetApis()
        {
            return new List<ApiResource>
            {
                new ApiResource(IdentityServerResourceName.DEMO_WEBAPI, IdentityServerResourceDisplayName.DEMO_WEBAPI)
                {
                    ApiSecrets = { new Secret(IdentityServerClientSecret.DEMO_WEBAPI.Sha256()) }
                }
            };
        }
    }
}
