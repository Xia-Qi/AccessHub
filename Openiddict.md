# OpenIddict 授权模式（Grant Types）对照表

| Grant Type（参数值） | 别名 / 其它叫法 | 是否需要浏览器 | 是否需要用户参与 | 典型场景 | 推荐程度 |
|----------------------|------------------|----------------|------------------|----------|---------|
| **授权码模式<br>authorization_code** | <ul><li>标准授权码流程</li><li>三方认证流程</li> <li>Web服务器流程</li><li>Code Flow</li></ul> | ✔️ | ✔️ | Web 应用、第三方登录（标准 OIDC 登录） | ⭐⭐⭐⭐⭐ 强烈推荐 |
| **授权码+PKCE<br>authorization_code + PKCE** | <ul><li>增强授权码模式</li><li>授权码 + PKCE（用于 SPA/Native）</li><li>PKCE流程</li><li>公共客户端流程</li></ul> | ✔️ | ✔️ | SPA / Mobile（无后端密钥） | ⭐⭐⭐⭐⭐ 强烈推荐 |
| **客户端凭证模式<br>client_credentials** | <ul><li>客户端模式</li><li>服务端凭证模式</li><li>M2M(机器对机器模式)</li><li>Service-to-Service</li><li>2-legged OAuth</li></ul> | ❌ | ❌ | 微服务、后台任务、守护进程 | ⭐⭐⭐⭐ 推荐 |
| **刷新令牌<br>refresh_token** | <ul><li>令牌刷新流程</li><li>Silent Renew</li><li>重新认证流程</li></ul> | ❌ | ❌ | 延长会话，无感刷新 access token | ⭐⭐⭐⭐ 推荐 |
| **设备授权模式<br>device_code** |  <ul><li>设备流程（Device Flow）</li> <li>设备代码流程</li><li>设备码模式（OIDC Device Authorization）</li><li>电视认证流程</li></ul> | ✔️（在另一设备上授权） | ✔️ | TV、IoT、无法输入账号密码的设备 | ⭐⭐⭐⭐ 推荐（特定场景） |
| **密码模式<br>password** | <ul><li>ROPC</li><li>Resource Owner Password Credentials</li><li>密码凭证模式</li><li>直接密码模式</li></ul> | ❌（但客户端收集） | ✔️（用户输入用户名/密码） | 旧系统 / 内部系统（不推荐） | ⭐⭐ 不推荐（受控环境） |
| **隐式模式<br>implicit** | <ul><li>简化流程</li><li>前端令牌流程</li><li>片段令牌流程</li><li>Implicit Flow（OAuth2/旧 OIDC）</li></ul> | ✔️ | ✔️ | 老式 SPA（已弃用） | ⭐ 已弃用 |


引用自 [OpenIddict 授权模式（Grant Types）对照表](https://chatgpt.com/c/69398878-1e64-8332-be04-390a01fd0f67)