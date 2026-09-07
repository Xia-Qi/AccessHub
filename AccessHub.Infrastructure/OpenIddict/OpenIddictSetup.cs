using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using System.Text.Json;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AccessHub.Infrastructure.OpenIddict
{
    /// <summary>
    /// OpenIddict 初始化:从 ContentRootPath/seeds/*.json 读取客户端种子并 upsert。
    /// 客户端配置(URI/权限)外置到 JSON,改配置即演进,无需改代码。
    /// 系统引导客户端 userManageWebClient 由 seeds/userManageWebClient.json 提供;
    /// 业务客户端由 ClientController 运行时 API 管理。
    /// </summary>
    public static class OpenIddictSetup
    {
        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// 从 seeds/*.json 读取客户端种子并 upsert,再确保 scope 种子存在。
        /// </summary>
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var provider = scope.ServiceProvider;
            var appManager = provider.GetRequiredService<IOpenIddictApplicationManager>();
            var env = provider.GetRequiredService<IHostEnvironment>();

            // 1. 客户端种子:从 ContentRootPath/seeds/*.json 读取并 upsert
            var seedsDir = Path.Combine(env.ContentRootPath, "seeds");
            if (Directory.Exists(seedsDir))
            {
                foreach (var file in Directory.GetFiles(seedsDir, "*.json"))
                {
                    var json = await File.ReadAllTextAsync(file);
                    var record = JsonSerializer.Deserialize<ClientSeedRecord>(json, JsonOpts);
                    if (record == null || string.IsNullOrEmpty(record.ClientId)) continue;
                    await UpsertClientAsync(appManager, record);
                }
            }

            // 2. Scope 种子(系统资源定义,非客户端管理范畴)
            await EnsureScopesAsync(provider);
        }

        /// <summary>
        /// upsert 客户端:库中无则创建,有则用 JSON 配置覆盖可变字段。
        /// public 客户端不设 ClientSecret;upsert 路径 PopulateAsync 会拷入库里旧 secret,
        /// 故 ApplyRecord 显式置 null,避免 OpenIddict 校验「public 客户端不得带 secret」拒绝 Update。
        /// </summary>
        private static async Task UpsertClientAsync(IOpenIddictApplicationManager manager, ClientSeedRecord record)
        {
            var existing = await manager.FindByClientIdAsync(record.ClientId);
            var descriptor = new OpenIddictApplicationDescriptor();

            if (existing is null)
            {
                ApplyRecord(descriptor, record);
                await manager.CreateAsync(descriptor);
            }
            else
            {
                // 先加载现有实体到 descriptor(PopulateAsync 会拷入库里 secret),
                // 再用 JSON 覆盖可变字段,最后 UpdateAsync(existing, descriptor) 写回。
                await manager.PopulateAsync(descriptor, existing);
                ApplyRecord(descriptor, record);
                await manager.UpdateAsync(existing, descriptor);
            }
        }

        /// <summary>
        /// 把 JSON 种子配置应用到 descriptor(create 与 update 共用,保证幂等)。
        /// </summary>
        private static void ApplyRecord(OpenIddictApplicationDescriptor descriptor, ClientSeedRecord record)
        {
            descriptor.ClientId = record.ClientId;
            descriptor.DisplayName = record.DisplayName;
            descriptor.ClientType = record.ClientType;
            descriptor.ClientSecret = null; // public 客户端不校验 secret,靠 PKCE 防御

            descriptor.RedirectUris.Clear();
            foreach (var uri in record.RedirectUris)
                descriptor.RedirectUris.Add(new Uri(uri));

            descriptor.PostLogoutRedirectUris.Clear();
            foreach (var uri in record.PostLogoutRedirectUris)
                descriptor.PostLogoutRedirectUris.Add(new Uri(uri));

            descriptor.Permissions.Clear();
            foreach (var p in record.Permissions)
                descriptor.Permissions.Add(p);

            descriptor.Requirements.Clear();
            foreach (var r in record.Requirements)
                descriptor.Requirements.Add(r);
        }

        private static async Task EnsureScopesAsync(IServiceProvider provider)
        {
            var scopeManager = provider.GetRequiredService<IOpenIddictScopeManager>();

            // ahb.<模块> 业务 scope(模块级,客户端被授权访问的 API 模块)
            // 细粒度动作校验由 permission claim 完成,scope 只做客户端层面的模块授权。
            var businessScopes = new (string Name, string Description)[]
            {
                ("ahb.usermgmt", "User management module (users/roles/permissions)"),
                ("ahb.clientmgmt", "Client management module")
            };
            foreach (var (name, description) in businessScopes)
            {
                if (await scopeManager.FindByNameAsync(name) == null)
                {
                    await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                    {
                        Name = name,
                        Description = description,
                        Resources = { "ahbapi" }
                    });
                }
            }

            // 默认 OIDC scope
            var defaultScopes = new[] { Scopes.OpenId, Scopes.Email, Scopes.Profile, Scopes.Roles, Scopes.OfflineAccess };
            foreach (var scope in defaultScopes)
            {
                if (await scopeManager.FindByNameAsync(scope) == null)
                {
                    await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                    {
                        Name = scope,
                        Resources = { "AccessHub" }
                    });
                }
            }
        }

        /// <summary>
        /// 客户端种子 JSON 反序列化模型。
        /// permissions 用 OpenIddict 前缀字符串值:ept:/gt:/rst:/scp:
        /// requirements 用 ft:pkce
        /// </summary>
        private sealed class ClientSeedRecord
        {
            public string ClientId { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string ClientType { get; set; } = "Public";
            public List<string> RedirectUris { get; set; } = new();
            public List<string> PostLogoutRedirectUris { get; set; } = new();
            public List<string> Permissions { get; set; } = new();
            public List<string> Requirements { get; set; } = new();
        }
    }
}
