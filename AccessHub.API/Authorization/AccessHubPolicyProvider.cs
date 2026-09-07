using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace AccessHub.API.Authorization
{
    /// <summary>
    /// 动态授权策略提供器:scope 与 permission 均按命名约定动态解析,无需启动期 AddPolicy 注册。
    ///   ahb.&lt;模块&gt;   → 校验 token 的 scope claim(client 被授权访问的 API 模块)
    ///   perm.&lt;资源&gt;.&lt;动作&gt; → 校验 token 的 permission claim(user 被授权的动作),
    ///                    支持 *.*(顶级通配,超管) 与 &lt;模块&gt;.all(模块通配)短路
    /// 其他命名 → 委托默认 provider(查 AddPolicy 注册的命名策略,如未来可能加的 AdminOnly)
    /// 新增 API 资源只需在 Controller 写 [Authorize(Policy="ahb.xxx"/"perm.xxx")],不再碰 Program.cs。
    /// </summary>
    public class AccessHubPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public AccessHubPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // 1. 命名策略走默认(启动期 AddPolicy 注册的)
            var policy = await base.GetPolicyAsync(policyName);
            if (policy is not null) return policy;

            // 2. scope 策略:ahb.<模块> → token 必须含该 scope(OpenIddict HasScope 自动 Split)
            if (policyName.StartsWith("ahb.", StringComparison.OrdinalIgnoreCase))
            {
                return new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .RequireAssertion(ctx => ctx.User.HasScope(policyName))
                    .Build();
            }

            // 3. permission 策略:perm.<资源>.<动作> → 支持顶级/模块通配短路
            if (policyName.StartsWith("perm.", StringComparison.OrdinalIgnoreCase))
            {
                var permission = policyName["perm.".Length..]; // "perm.user.read" → "user.read"
                var module = permission.Split('.').FirstOrDefault() ?? string.Empty; // "user.read" → "user"
                return new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .RequireAssertion(ctx =>
                        ctx.User.HasClaim("permission", "*.*") ||             // 顶级通配(超管)
                        ctx.User.HasClaim("permission", module + ".all") ||   // 模块通配
                        ctx.User.HasClaim("permission", permission))          // 具体权限
                    .Build();
            }

            return null;
        }
    }
}
