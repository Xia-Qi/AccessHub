## 集成事件和领域事件的区别？

## Ids4如何结合User实现基于角色的权限控制？

## ef migration
dotnet ef migrations add InitialCreate
dotnet ef database update

mysql -u appuser -p
DROP DATABASE accesshub;
CREATE DATABASE accesshub CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

## swagger
手动添加swagger时，代码配置好后，运行报错：Could not find file 'E:\Code\AccessHub\AccessHub.API\bin\Debug\net8.0\AccessHub.API.xml
解决办法：右键项目属性，找到 生成->输出->文档文件，勾选“生成包含API文档的文件”

## ef 多对多该怎么配置？是否需要相互包含一个List
使用中间表时，不能相互包含，应同时包含中间表List
eg, User和Role多对多关系，具体看注释

## 泛型类型不变性（invariance）
思考：这段代码，为什么运行时，builder as EntityTypeBuilder<IAggregateRoot> 为null
void IEntityTypeConfiguration<T>.Configure(EntityTypeBuilder<T> builder) 
{ 
if(typeof(IAggregateRoot).IsAssignableFrom(typeof(T))) 
{ EntityBuilderExtensions.AggregateRootPropertyBuild(builder as EntityTypeBuilder<IAggregateRoot>); }
} 
这是 因为泛型类型不变性（invariance）导致的正常现象，
✅ 关键原因：EntityTypeBuilder<T> 与 EntityTypeBuilder<IAggregateRoot> 无继承关系

即使 T : IAggregateRoot，以下类型也 没有任何关系：

EntityTypeBuilder<T>  ❌→  EntityTypeBuilder<IAggregateRoot>


类似于：

List<Dog> 不能 cast 成 List<Animal>


.NET 的泛型 默认是不可变（invariant），所以 cast 失败返回 null 完全正常。

❌ 为什么 builder as EntityTypeBuilder<IAggregateRoot> 是 null？

因为：

你的 builder 实际类型是 EntityTypeBuilder<User> 或 EntityTypeBuilder<Order>

即使 User : IAggregateRoot，也无法转换成 EntityTypeBuilder<IAggregateRoot>

因此 null。

## 架构
AccessHub.API : 
提供统一的登录页面 /account/login?=returnUrl=xxx
提供

## TODO


## openiddict 服务端配置和客户端配置方式