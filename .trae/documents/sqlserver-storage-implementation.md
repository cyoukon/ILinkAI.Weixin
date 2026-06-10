# SQL Server 存储实现计划

## 概述

为 ILinkai.Weixin.Sdk 添加 SQL Server 存储实现，将账户数据从本地文件存储扩展到支持 SQL Server 数据库存储。

## 当前状态分析

### 现有架构

* **AccountStore.cs**: 基于本地文件系统的账户存储实现

  * 数据存储在 `~/.ilinkai/weixin/accounts/` 目录下的 JSON 文件中

  * 账户索引存储在 `accounts.json` 文件中

  * 主要方法: `ListAccountIds`, `LoadAccount`, `SaveAccount`, `ClearAccount`, `ResolveAccount`, `RegisterAccountId`

  * 静态方法: `NormalizeAccountId`, `DeriveRawAccountId`

### 数据模型

* **WeixinAccountData**: Token, SavedAt, BaseUrl, UserId

* **ResolvedWeixinAccount**: AccountId, BaseUrl, CdnBaseUrl, Token, Enabled, Configured, Name

### 使用场景

* CLI 项目通过 `CommandBase` 基类创建 `AccountStore` 实例

* Sample 项目直接实例化 `AccountStore`

* `MessageMonitorService` 使用同步缓冲区文件存储

## 提议变更

### 1. 在 ILinkai.Weixin.Sdk 中添加存储接口

**文件**: `ILinkai.Weixin.Sdk/Auth/IAccountStore.cs`

```csharp
namespace ILinkai.Weixin.Sdk.Auth;

/// <summary>
/// 账户存储接口
/// </summary>
public interface IAccountStore
{
    /// <summary>
    /// 列出所有已注册的账户ID
    /// </summary>
    List<string> ListAccountIds();

    /// <summary>
    /// 注册账户ID到索引
    /// </summary>
    void RegisterAccountId(string accountId);

    /// <summary>
    /// 加载账户数据
    /// </summary>
    WeixinAccountData? LoadAccount(string accountId);

    /// <summary>
    /// 保存账户数据
    /// </summary>
    void SaveAccount(string accountId, WeixinAccountData data);

    /// <summary>
    /// 清除账户数据
    /// </summary>
    void ClearAccount(string accountId);

    /// <summary>
    /// 解析账户信息
    /// </summary>
    ResolvedWeixinAccount ResolveAccount(string accountId);
}
```

### 2. 重构现有 AccountStore 实现接口

**文件**: `ILinkai.Weixin.Sdk/Auth/AccountStore.cs`

* 让 `AccountStore` 类实现 `IAccountStore` 接口

* 保持现有功能不变

* 保持 `NormalizeAccountId` 和 `DeriveRawAccountId` 为静态方法

### 3. 创建新项目 ILinkai.Weixin.Sdk.SqlServer

**项目结构**:

```
ILinkai.Weixin.Sdk.SqlServer/
├── ILinkai.Weixin.Sdk.SqlServer.csproj
├── SqlServerAccountStore.cs
├── ServiceCollectionExtensions.cs
└── Models/
    └── AccountEntity.cs
```

#### 3.1 项目文件

**文件**: `ILinkai.Weixin.Sdk.SqlServer.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <PackageId>ILinkai.Weixin.Sdk.SqlServer</PackageId>
    <Version>1.0.0</Version>
    <Authors>Tencent</Authors>
    <Description>SQL Server storage implementation for ILinkai.Weixin.Sdk</Description>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <RootNamespace>ILinkai.Weixin.Sdk.SqlServer</RootNamespace>
    <PackageTags>weixin;wechat;ilinkai;sdk;sqlserver;storage</PackageTags>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
    <RepositoryUrl>https://github.com/cyoukon/ILinkAI.Weixin</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <PackageReadmeFile>README.md</PackageReadmeFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.5" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.5" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ILinkai.Weixin.Sdk\ILinkai.Weixin.Sdk.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="..\README.md" Pack="true" PackagePath="\" />
  </ItemGroup>
</Project>
```

#### 3.2 实体模型

**文件**: `Models/AccountEntity.cs`

```csharp
namespace ILinkai.Weixin.Sdk.SqlServer.Models;

/// <summary>
/// 账户实体（EF Core 映射）
/// </summary>
public class AccountEntity
{
    public string AccountId { get; set; } = string.Empty;
    public string? Token { get; set; }
    public DateTime? SavedAt { get; set; }
    public string? BaseUrl { get; set; }
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### 3.3 DbContext

**文件**: `AccountDbContext.cs`

```csharp
using ILinkai.Weixin.Sdk.SqlServer.Models;
using Microsoft.EntityFrameworkCore;

namespace ILinkai.Weixin.Sdk.SqlServer;

public class AccountDbContext : DbContext
{
    public DbSet<AccountEntity> Accounts { get; set; }

    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntity>(entity =>
        {
            entity.HasKey(e => e.AccountId);
            entity.HasIndex(e => e.UserId);
        });
    }
}
```

#### 3.4 SQL Server 存储实现

**文件**: `SqlServerAccountStore.cs`

```csharp
using ILinkai.Weixin.Sdk.Auth;
using ILinkai.Weixin.Sdk.SqlServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ILinkai.Weixin.Sdk.SqlServer;

public class SqlServerAccountStore : IAccountStore
{
    private readonly AccountDbContext _dbContext;
    private readonly ILogger? _logger;

    public SqlServerAccountStore(AccountDbContext dbContext, ILogger? logger = null)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public List<string> ListAccountIds()
    {
        return _dbContext.Accounts
            .Select(a => a.AccountId)
            .ToList();
    }

    public void RegisterAccountId(string accountId)
    {
        // SQL Server 实现中，账户在 SaveAccount 时自动创建
        // 此方法保留以保持接口兼容性
    }

    public WeixinAccountData? LoadAccount(string accountId)
    {
        var normalizedId = AccountStore.NormalizeAccountId(accountId);
        var entity = _dbContext.Accounts.Find(normalizedId);

        if (entity == null) return null;

        return new WeixinAccountData
        {
            Token = entity.Token,
            SavedAt = entity.SavedAt,
            BaseUrl = entity.BaseUrl,
            UserId = entity.UserId
        };
    }

    public void SaveAccount(string accountId, WeixinAccountData data)
    {
        var normalizedId = AccountStore.NormalizeAccountId(accountId);
        var entity = _dbContext.Accounts.Find(normalizedId);

        if (entity == null)
        {
            entity = new AccountEntity
            {
                AccountId = normalizedId,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Accounts.Add(entity);
        }

        entity.Token = data.Token?.Trim() ?? entity.Token;
        entity.BaseUrl = data.BaseUrl?.Trim() ?? entity.BaseUrl;
        entity.UserId = data.UserId ?? entity.UserId;
        entity.SavedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        _dbContext.SaveChanges();
    }

    public void ClearAccount(string accountId)
    {
        var normalizedId = AccountStore.NormalizeAccountId(accountId);
        var entity = _dbContext.Accounts.Find(normalizedId);

        if (entity != null)
        {
            _dbContext.Accounts.Remove(entity);
            _dbContext.SaveChanges();
        }
    }

    public ResolvedWeixinAccount ResolveAccount(string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("accountId is required", nameof(accountId));
        }

        var id = AccountStore.NormalizeAccountId(accountId);
        var accountData = LoadAccount(id);

        return new ResolvedWeixinAccount
        {
            AccountId = id,
            BaseUrl = accountData?.BaseUrl?.Trim() ?? WeixinApiClient.DefaultBaseUrl,
            CdnBaseUrl = WeixinApiClient.DefaultCdnBaseUrl,
            Token = accountData?.Token?.Trim(),
            Enabled = true,
            Configured = !string.IsNullOrWhiteSpace(accountData?.Token)
        };
    }
}
```

### 3.5 SDK 依赖注入扩展（文件存储默认）

**文件**: `ILinkai.Weixin.Sdk/Auth/ServiceCollectionExtensions.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace ILinkai.Weixin.Sdk.Auth;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加微信账户存储服务（默认使用文件存储）
    /// </summary>
    public static IServiceCollection AddWeixinFileStorage(
        this IServiceCollection services,
        string? stateDir = null)
    {
        services.AddSingleton<IAccountStore>(sp =>
        {
            var logger = sp.GetService<ILogger<AccountStore>>();
            return new AccountStore(stateDir, logger);
        });

        return services;
    }

    /// <summary>
    /// 添加微信账户存储服务（默认使用文件存储）
    /// </summary>
    public static IServiceCollection AddWeixinStorage(
        this IServiceCollection services,
        string? stateDir = null)
    {
        return services.AddWeixinFileStorage(stateDir);
    }
}
```

### 3.6 SQL Server 依赖注入扩展

**文件**: `ILinkai.Weixin.Sdk.SqlServer/ServiceCollectionExtensions.cs`

```csharp
using ILinkai.Weixin.Sdk.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ILinkai.Weixin.Sdk.SqlServer;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加微信账户存储服务（使用 SQL Server）
    /// 此方法会替换默认的文件存储实现
    /// </summary>
    public static IServiceCollection AddWeixinSqlServerStorage(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AccountDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IAccountStore, SqlServerAccountStore>();

        return services;
    }

    /// <summary>
    /// 添加微信账户存储服务（使用 SQL Server，自定义配置）
    /// </summary>
    public static IServiceCollection AddWeixinSqlServerStorage(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
    {
        services.AddDbContext<AccountDbContext>(optionsAction);
        services.AddScoped<IAccountStore, SqlServerAccountStore>();

        return services;
    }
}
```

**注意**: 调用 `AddWeixinSqlServerStorage` 会替换之前注册的 `IAccountStore` 实现（文件存储），因为 ASP.NET Core DI 容器中后注册的服务会覆盖之前的注册。

### 4. 更新解决方案文件

**文件**: `ILinkai.Weixin.slnx`

添加新项目引用。

### 5. 更新 CLI 项目支持依赖注入（可选增强）

如果需要让 CLI 支持可配置的存储后端，可以：

* 修改 `CommandBase` 接受 `IAccountStore` 参数

* 在 `Program.cs` 中配置依赖注入

## 假设与决策

1. **EF Core 版本**: 使用与 SDK 相同的 .NET 10.0 版本对应的 EF Core 10.0.5
2. **数据库表设计**: 简单的单表设计，AccountId 为主键
3. **向后兼容**: 保持 `AccountStore` 类名不变，仅添加接口实现
4. **静态方法**: `NormalizeAccountId` 和 `DeriveRawAccountId` 保持为静态方法，便于各实现共用

## 验证步骤

1. 编译解决方案确保无错误
2. 创建单元测试项目验证 SQL Server 实现
3. 测试 NuGet 包打包：`dotnet pack ILinkai.Weixin.Sdk.SqlServer`
4. 验证依赖注入配置正常工作

## README 使用示例

需要在 README.md 中添加以下内容：

### 安装 SQL Server 存储包

```bash
# 从 NuGet 安装
dotnet add package ILinkai.Weixin.Sdk.SqlServer

# 或从本地包安装
dotnet pack ILinkai.Weixin.Sdk.SqlServer -c Release -o ./nupkg
dotnet add package ILinkai.Weixin.Sdk.SqlServer --source ./nupkg
```

### 使用依赖注入配置存储

```csharp
using ILinkai.Weixin.Sdk.Auth;
using Microsoft.Extensions.DependencyInjection;

// 创建服务集合
var services = new ServiceCollection();

// 默认使用文件存储
services.AddWeixinStorage();

// 或指定自定义状态目录
services.AddWeixinStorage("/custom/state/dir");

// 构建服务提供者
var serviceProvider = services.BuildServiceProvider();

// 获取账户存储实例
var accountStore = serviceProvider.GetRequiredService<IAccountStore>();

// 使用账户存储
var accounts = accountStore.ListAccountIds();
var account = accountStore.ResolveAccount("account-id");
accountStore.SaveAccount("account-id", new WeixinAccountData
{
    Token = "bot-token",
    BaseUrl = "https://ilinkai.weixin.qq.com"
});
```

### 切换到 SQL Server 存储

```csharp
using ILinkai.Weixin.Sdk.Auth;
using ILinkai.Weixin.Sdk.SqlServer;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// 先添加默认文件存储（可选）
services.AddWeixinStorage();

// 切换到 SQL Server 存储（会替换文件存储）
services.AddWeixinSqlServerStorage("Server=localhost;Database=WeixinAccounts;Trusted_Connection=True;");

var serviceProvider = services.BuildServiceProvider();
var accountStore = serviceProvider.GetRequiredService<IAccountStore>();
// accountStore 现在是 SqlServerAccountStore 实例
```

### 使用自定义 DbContext 配置

```csharp
services.AddWeixinSqlServerStorage(options =>
{
    options.UseSqlServer("YourConnectionString", sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
    });
});
```

### 数据库表结构

SDK 会自动创建以下表结构：

```sql
CREATE TABLE Accounts (
    AccountId NVARCHAR(256) PRIMARY KEY,
    Token NVARCHAR(MAX),
    SavedAt DATETIME2,
    BaseUrl NVARCHAR(512),
    UserId NVARCHAR(256),
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);

CREATE INDEX IX_Accounts_UserId ON Accounts(UserId);
```

### 初始化数据库

```csharp
using ILinkai.Weixin.Sdk.SqlServer;
using Microsoft.EntityFrameworkCore;

// 创建 DbContext
var options = new DbContextOptionsBuilder<AccountDbContext>()
    .UseSqlServer("YourConnectionString")
    .Options;

using var context = new AccountDbContext(options);

// 创建数据库和表
context.Database.EnsureCreated();

// 或使用迁移（推荐生产环境）
// dotnet ef migrations add InitialCreate
// dotnet ef database update
```

### 切换存储实现

```csharp
// 方式1：直接实例化（不使用依赖注入）
var fileStore = new AccountStore();  // 文件存储

// 方式2：使用依赖注入（推荐）
var services = new ServiceCollection();

// 默认文件存储
services.AddWeixinStorage();

// 切换到 SQL Server 存储（调用此方法会替换文件存储）
services.AddWeixinSqlServerStorage("YourConnectionString");

var serviceProvider = services.BuildServiceProvider();
var store = serviceProvider.GetRequiredService<IAccountStore>();
```

### 项目结构更新

```
ILinkai.Weixin.Sdk/
├── Auth/
│   ├── IAccountStore.cs           # 存储接口（新增）
│   ├── AccountStore.cs            # 文件存储实现（修改：实现接口）
│   ├── ServiceCollectionExtensions.cs  # DI扩展（新增）
│   ├── QRCodeLoginService.cs
│   └── SessionGuard.cs
│   └── ...

ILinkai.Weixin.Sdk.SqlServer/
├── ILinkai.Weixin.Sdk.SqlServer.csproj
├── SqlServerAccountStore.cs       # SQL Server存储实现
├── AccountDbContext.cs            # EF Core DbContext
├── ServiceCollectionExtensions.cs # DI扩展
└── Models/
    └── AccountEntity.cs           # 实体模型
```

