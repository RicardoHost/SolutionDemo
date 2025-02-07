// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Demo.Common.Constants.IdentityServerConstants;
using IdentityServer4.Models;
using System.Collections.Generic;

namespace Demo.IdentityServer
{
    public static class Config
    {
        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[]
            {
                new ApiScope()
                {
                    Name = IdentityServerResourceName.DEMO_WEBAPI,
                    DisplayName=IdentityServerResourceName.DEMO_WEBAPI,
                }
            };

        public static IEnumerable<ApiResource> ApiResources =>
            new ApiResource[]
            {
                new ApiResource()
                {
                    Name = IdentityServerResourceName.DEMO_WEBAPI,
                    DisplayName=IdentityServerResourceName.DEMO_WEBAPI,
                    Scopes = { IdentityServerResourceName.DEMO_WEBAPI }
                }
            };

        public static IEnumerable<Client> Clients =>
            new Client[]
            { 
                new Client()
                {
                    ClientId = IdentityServerClientId.DEMO_MAIN,
                    ClientSecrets = { new Secret(IdentityServerClientSecret.DEMO_MAIN.Sha256()) },
                    AccessTokenLifetime = 60,
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowOfflineAccess = false,
                    AllowedScopes={ IdentityServerResourceName.DEMO_WEBAPI }
                }
            };
    }
}