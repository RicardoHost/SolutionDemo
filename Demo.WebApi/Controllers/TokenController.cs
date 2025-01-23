using Demo.Common.Const;
using IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace Demo.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        /// <summary>
        /// 身份认真请求主机地址
        /// </summary>
        private static readonly string _identityServerAddr = "http://localhost:63571";

        [HttpGet]
        public async Task<IActionResult> Token()
        {
            var client = new HttpClient();
            //通过获取发现文档查询是否配置了IdnetityServerAddr服务
            var disco = await client.GetDiscoveryDocumentAsync(_identityServerAddr);
            if (disco.IsError)
            {
                return Content("获取发现文档失败。error：" + disco.Error);
            }
            var token = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest()
            {
                Address = disco.TokenEndpoint,
                ClientId = IdentityServerClientId.DEMO_WEBAPI,
                ClientSecret = IdentityServerClientSecret.DEMO_WEBAPI,
                Scope = IdentityServerResourceName.DEMO_WEBAPI
            });
            if (token.IsError)
            {
                return Content("获取 AccessToken 失败。error：" + disco.Error);
            }
            return Content("获取 AccessToken 成功。Token:" + token.AccessToken);
        }
    }
}
