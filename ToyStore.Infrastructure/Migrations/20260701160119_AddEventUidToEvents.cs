using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToyStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventUidToEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "System");

            migrationBuilder.EnsureSchema(
                name: "Notification");

            migrationBuilder.EnsureSchema(
                name: "Interaction");

            migrationBuilder.EnsureSchema(
                name: "Recommendation");

            migrationBuilder.CreateTable(
                name: "Ages",
                columns: table => new
                {
                    AgeID = table.Column<byte>(type: "tinyint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgeRange = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Ages__875454C22C2A4528", x => x.AgeID);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundJobs",
                schema: "System",
                columns: table => new
                {
                    JobID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    CronExpression = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastRunTime = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    NextRunTime = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    LastRunStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    LastRunMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Backgrou__056690E24B19863C", x => x.JobID);
                });

            migrationBuilder.CreateTable(
                name: "BlogCategories",
                columns: table => new
                {
                    BlogCategoryID = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BlogCategoriesName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__BlogCate__6BD2DA61E9AA8F5E", x => x.BlogCategoryID);
                });

            migrationBuilder.CreateTable(
                name: "BlogCommentBanReasons",
                columns: table => new
                {
                    BanReasonID = table.Column<byte>(type: "tinyint", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogCommentBanReasons", x => x.BanReasonID);
                });

            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    BrandID = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrandName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Brands__DAD4F3BE26CDE783", x => x.BrandID);
                });

            migrationBuilder.CreateTable(
                name: "DomainEventOutbox",
                schema: "System",
                columns: table => new
                {
                    EventID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    AggregateId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: "{}"),
                    OccurredOn = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    ProcessingLockId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProcessingAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    Attempts = table.Column<byte>(type: "tinyint", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DomainEv__7944C87080D07E3D", x => x.EventID);
                });

            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    MaterialID = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialName = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Material__C5061317E87CD884", x => x.MaterialID);
                });

            migrationBuilder.CreateTable(
                name: "OrderRefundReasons",
                columns: table => new
                {
                    RefundReasonID = table.Column<byte>(type: "tinyint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ResponsibleParty = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__OrderRef__9A229525BB82DCC0", x => x.RefundReasonID);
                });

            migrationBuilder.CreateTable(
                name: "Origins",
                columns: table => new
                {
                    OriginID = table.Column<byte>(type: "tinyint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Origins__171FA2C65CE9E745", x => x.OriginID);
                });

            migrationBuilder.CreateTable(
                name: "PriceRanges",
                columns: table => new
                {
                    PriceRangeID = table.Column<byte>(type: "tinyint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PriceRangeMin = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    PriceRangeMax = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PriceRan__B8A301FFFA15956C", x => x.PriceRangeID);
                });

            migrationBuilder.CreateTable(
                name: "Provinces",
                columns: table => new
                {
                    ProvinceId = table.Column<int>(type: "int", nullable: false),
                    ProvinceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProvinceCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Province__FD0A6F83F155A109", x => x.ProvinceId);
                });

            migrationBuilder.CreateTable(
                name: "ReactionTypes",
                columns: table => new
                {
                    ReactionTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Reaction__01E625C0806F96F1", x => x.ReactionTypeID);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<byte>(type: "tinyint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Roles__8AFACE3ACBF2EEFE", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "Sexes",
                columns: table => new
                {
                    SexID = table.Column<byte>(type: "tinyint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SexName = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Sexes__75622DB6312F4F3E", x => x.SexID);
                });

            migrationBuilder.CreateTable(
                name: "ShiftTemplates",
                columns: table => new
                {
                    ShiftTemplateID = table.Column<byte>(type: "tinyint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time(0)", precision: 0, nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time(0)", precision: 0, nullable: false),
                    MaxOrdersPerShift = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)20),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftTemplates", x => x.ShiftTemplateID);
                });

            migrationBuilder.CreateTable(
                name: "StatusOrders",
                columns: table => new
                {
                    StatusID = table.Column<byte>(type: "tinyint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusName = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StatusOr__C8EE20436DF30AC3", x => x.StatusID);
                });

            migrationBuilder.CreateTable(
                name: "StatusRefunds",
                columns: table => new
                {
                    StatusID = table.Column<byte>(type: "tinyint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusName = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusRefunds", x => x.StatusID);
                });

            migrationBuilder.CreateTable(
                name: "SuperCategories",
                columns: table => new
                {
                    SuperCategoryID = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SuperCategoryName = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SuperCat__CEB990D38D215BB2", x => x.SuperCategoryID);
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                schema: "Notification",
                columns: table => new
                {
                    TemplateID = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    UsageScope = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false, defaultValue: "ADMIN"),
                    TitleTemplate = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MessageTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Template__F87ADD07CB394BD6", x => x.TemplateID);
                    table.UniqueConstraint("AK_Templates_TemplateCode", x => x.TemplateCode);
                });

            migrationBuilder.CreateTable(
                name: "Widgets",
                schema: "Recommendation",
                columns: table => new
                {
                    WidgetID = table.Column<byte>(type: "tinyint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WidgetCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    WidgetName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Algorithm = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    MaxItems = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)10),
                    FallbackAlgo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Config = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Widgets__ADFD3072E41ABF05", x => x.WidgetID);
                });

            migrationBuilder.CreateTable(
                name: "AiPromptTemplates",
                columns: table => new
                {
                    TemplateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PromptStructure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultTone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DefaultCategoryID = table.Column<short>(type: "smallint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiPromptTemplates", x => x.TemplateID);
                    table.ForeignKey(
                        name: "FK_AIPromptTemplates_BlogCategories",
                        column: x => x.DefaultCategoryID,
                        principalTable: "BlogCategories",
                        principalColumn: "BlogCategoryID");
                });

            migrationBuilder.CreateTable(
                name: "Districts",
                columns: table => new
                {
                    DistrictId = table.Column<int>(type: "int", nullable: false),
                    ProvinceId = table.Column<int>(type: "int", nullable: false),
                    DistrictName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__District__85FDA4C63161ED48", x => x.DistrictId);
                    table.ForeignKey(
                        name: "FK_Districts_Provinces",
                        column: x => x.ProvinceId,
                        principalTable: "Provinces",
                        principalColumn: "ProvinceId");
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleID = table.Column<byte>(type: "tinyint", nullable: false),
                    SexID = table.Column<byte>(type: "tinyint", nullable: true),
                    DOB = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmployeeCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    AccountName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    Email = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ImageURL = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    PasswordHash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    HasPassword = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Provider = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Accounts__349DA58684B564C1", x => x.AccountID);
                    table.ForeignKey(
                        name: "FK_Accounts_Roles",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID");
                    table.ForeignKey(
                        name: "FK_Accounts_Sexes",
                        column: x => x.SexID,
                        principalTable: "Sexes",
                        principalColumn: "SexID");
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryID = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SuperCategoryID = table.Column<short>(type: "smallint", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Categori__19093A2B8978D605", x => x.CategoryID);
                    table.ForeignKey(
                        name: "FK_Categories_SuperCategories",
                        column: x => x.SuperCategoryID,
                        principalTable: "SuperCategories",
                        principalColumn: "SuperCategoryID");
                });

            migrationBuilder.CreateTable(
                name: "Wards",
                columns: table => new
                {
                    WardCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    DistrictId = table.Column<int>(type: "int", nullable: false),
                    WardName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Wards__1A7FBFF15512F9D4", x => x.WardCode);
                    table.ForeignKey(
                        name: "FK_Wards_Districts",
                        column: x => x.DistrictId,
                        principalTable: "Districts",
                        principalColumn: "DistrictId");
                });

            migrationBuilder.CreateTable(
                name: "BlogCommentViolationCount",
                columns: table => new
                {
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    ViolationCount = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    LastViolatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    IsCommentBanned = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    BannedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    BanExpiresAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    UnbannedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    UnbannedBy = table.Column<int>(type: "int", nullable: true),
                    RateCount = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    RateWindowAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogCommentViolationCount", x => x.AccountID);
                    table.ForeignKey(
                        name: "FK_BCVC_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_BCVC_UnbannedBy",
                        column: x => x.UnbannedBy,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                });

            migrationBuilder.CreateTable(
                name: "BlogPosts",
                columns: table => new
                {
                    BlogPostID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    BlogCategoryID = table.Column<short>(type: "smallint", nullable: false),
                    BlogTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BlogContent = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    BlogThumbnail = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    BlogAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__BlogPost__3217414947E21B0D", x => x.BlogPostID);
                    table.ForeignKey(
                        name: "FK_BlogPosts_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_BlogPosts_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_BlogPosts_BlogCategories",
                        column: x => x.BlogCategoryID,
                        principalTable: "BlogCategories",
                        principalColumn: "BlogCategoryID");
                });

            migrationBuilder.CreateTable(
                name: "Campaigns",
                schema: "Notification",
                columns: table => new
                {
                    CampaignID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TemplateCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    TitleOverride = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MessageOverride = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReferenceType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    ReferenceID = table.Column<int>(type: "int", nullable: true),
                    SourceType = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false, defaultValue: "ADMIN"),
                    TargetType = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false, defaultValue: "ALL"),
                    SubmittedByAccountID = table.Column<int>(type: "int", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ReviewedByAccountID = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Draft"),
                    EventKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ActionTarget = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ApprovedExpireAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    RescheduleCount = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    MaxRescheduleCount = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)3),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByAccountID = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.CampaignID);
                    table.CheckConstraint("CK_Campaigns_AdminRequiresCreator", "[SourceType] = 'SYSTEM' OR [CreatedByAccountID] IS NOT NULL");
                    table.CheckConstraint("CK_Campaigns_ApprovedExpireConsistency", "[ApprovedExpireAt] IS NULL OR [ReviewedAt] IS NULL OR [ApprovedExpireAt] > [ReviewedAt]");
                    table.CheckConstraint("CK_Campaigns_ApprovedExpireOnlyAfterApprove", "[ApprovedExpireAt] IS NULL OR [Status] IN ('Approved', 'Scheduled', 'Sending', 'Sent', 'Cancelled', 'Failed')");
                    table.CheckConstraint("CK_Campaigns_MustHaveContent", "[TemplateCode] IS NOT NULL OR ([TitleOverride] IS NOT NULL AND [MessageOverride] IS NOT NULL)");
                    table.CheckConstraint("CK_Campaigns_ReferenceConsistency", "([ReferenceType] IS NULL AND [ReferenceID] IS NULL) OR ([ReferenceType] IS NOT NULL AND [ReferenceID] IS NOT NULL)");
                    table.CheckConstraint("CK_Campaigns_ReferenceType", "[ReferenceType] IN ('VOUCHER', 'PRODUCT', 'BLOG', 'SALE', 'OTHER')");
                    table.CheckConstraint("CK_Campaigns_RejectedNeedsNote", "[Status] <> 'Rejected' OR [ReviewNote] IS NOT NULL");
                    table.CheckConstraint("CK_Campaigns_RescheduleNotExceedMax", "[RescheduleCount] <= [MaxRescheduleCount]");
                    table.CheckConstraint("CK_Campaigns_ScheduledAtConsistency", "[ScheduledAt] IS NULL OR [Status] IN ('Scheduled', 'Sending', 'Sent', 'Cancelled', 'Failed')");
                    table.CheckConstraint("CK_Campaigns_ScheduledAtInRange", "[ScheduledAt] IS NULL OR [ValidFrom] IS NULL OR [ValidTo] IS NULL OR ([ScheduledAt] >= [ValidFrom] AND [ScheduledAt] <= [ValidTo])");
                    table.CheckConstraint("CK_Campaigns_SourceType", "[SourceType] IN ('ADMIN', 'SYSTEM')");
                    table.CheckConstraint("CK_Campaigns_Status", "[Status] IN ('Draft', 'PendingApproval', 'Approved', 'Rejected', 'Scheduled', 'Sending', 'Sent', 'Cancelled', 'Failed')");
                    table.CheckConstraint("CK_Campaigns_TargetType", "[TargetType] IN ('ALL', 'INDIVIDUAL', 'ROLE')");
                    table.CheckConstraint("CK_Campaigns_ValidRange", "[ValidFrom] IS NULL OR [ValidTo] IS NULL OR [ValidFrom] < [ValidTo]");
                    table.ForeignKey(
                        name: "FK_Campaigns_Accounts",
                        column: x => x.CreatedByAccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_Campaigns_ReviewedBy",
                        column: x => x.ReviewedByAccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_Campaigns_SubmittedBy",
                        column: x => x.SubmittedByAccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_Campaigns_Templates",
                        column: x => x.TemplateCode,
                        principalSchema: "Notification",
                        principalTable: "Templates",
                        principalColumn: "TemplateCode");
                });

            migrationBuilder.CreateTable(
                name: "Cart",
                columns: table => new
                {
                    CartID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Cart__51BCD797AD6E6049", x => x.CartID);
                    table.ForeignKey(
                        name: "FK_Cart_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                });

            migrationBuilder.CreateTable(
                name: "CustomerChildren",
                columns: table => new
                {
                    ChildID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NickName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DOB = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SexID = table.Column<byte>(type: "tinyint", nullable: true),
                    BirthdayNotifiedYear = table.Column<short>(type: "smallint", nullable: true),
                    EditCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CustomerChildren__1234", x => x.ChildID);
                    table.ForeignKey(
                        name: "FK_CustomerChildren_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_CustomerChildren_Sexes",
                        column: x => x.SexID,
                        principalTable: "Sexes",
                        principalColumn: "SexID");
                });

            migrationBuilder.CreateTable(
                name: "CustomerDeliveryAbuseCases",
                columns: table => new
                {
                    CaseID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    WarningLevel = table.Column<byte>(type: "tinyint", nullable: false),
                    SuspiciousOrderCount = table.Column<int>(type: "int", nullable: false),
                    CountingFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastGHNFailCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    LastSuspiciousOrderDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CodRestrictedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewRequestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BlockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BlockedBy = table.Column<int>(type: "int", nullable: true),
                    AppealReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppealReviewedBy = table.Column<int>(type: "int", nullable: true),
                    AppealDecision = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    StrictPeriodUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PermanentBlockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PermanentBlockReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerDeliveryAbuseCases", x => x.CaseID);
                    table.ForeignKey(
                        name: "FK_CustomerDeliveryAbuseCases_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                });

            migrationBuilder.CreateTable(
                name: "Events",
                schema: "Interaction",
                columns: table => new
                {
                    EventID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventUid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountID = table.Column<int>(type: "int", nullable: true),
                    SessionID = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    EntityID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Source = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    Referrer = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    DeviceType = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: true),
                    ScrollDepth = table.Column<byte>(type: "tinyint", nullable: true),
                    ClickPosition = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Events__7944C870E12C15C1", x => x.EventID);
                    table.ForeignKey(
                        name: "FK_Events_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    StatusID = table.Column<byte>(type: "tinyint", nullable: false),
                    AssignedToStaffID = table.Column<int>(type: "int", nullable: true),
                    AssignedToMerchID = table.Column<int>(type: "int", nullable: true),
                    OrderCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ShippingOrderCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ShippingName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShippingPhone = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    ShippingAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ShippingWardCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ShippingWardName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShippingDistrictId = table.Column<int>(type: "int", nullable: false),
                    ShippingDistrictName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShippingProvinceId = table.Column<int>(type: "int", nullable: false),
                    ShippingProvinceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    PaymentMethod = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "SHIP_COD"),
                    PaymentStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    PaymentCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    SubTotal = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    VoucherDiscountAmount = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    EstimatedShippingFee = table.Column<decimal>(type: "decimal(10,0)", nullable: false),
                    ActualShippingFee = table.Column<decimal>(type: "decimal(10,0)", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    CancelReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancelledBy = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    FailedDeliveryAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ReturnedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    LastGHNFailCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    DeliveryFailCount = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Orders__C3905BAF5F588397", x => x.OrderID);
                    table.ForeignKey(
                        name: "FK_Orders_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_Orders_AssignedMerch",
                        column: x => x.AssignedToMerchID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_Orders_AssignedStaff",
                        column: x => x.AssignedToStaffID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_Orders_CancelledBy",
                        column: x => x.CancelledBy,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_Orders_StatusOrders",
                        column: x => x.StatusID,
                        principalTable: "StatusOrders",
                        principalColumn: "StatusID");
                });

            migrationBuilder.CreateTable(
                name: "Promotions",
                columns: table => new
                {
                    PromotionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    PromotionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PromotionType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Scheduled"),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Promotio__52C42F2F0105A9F7", x => x.PromotionID);
                    table.ForeignKey(
                        name: "FK_Promotions_Accounts",
                        column: x => x.CreatedBy,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                });

            migrationBuilder.CreateTable(
                name: "SavedBankAccounts",
                columns: table => new
                {
                    SavedBankAccountID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    BankBin = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BankShortName = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    BankCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    AccountNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedBankAccounts", x => x.SavedBankAccountID);
                    table.ForeignKey(
                        name: "FK_SavedBankAccounts_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                schema: "Notification",
                columns: table => new
                {
                    PreferenceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    EmailOptIn = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    WebPushOptIn = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    OrderUpdates = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Promotions = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    StockAlerts = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    BlogAlerts = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserPreferences__1234", x => x.PreferenceID);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                });

            migrationBuilder.CreateTable(
                name: "Vouchers",
                columns: table => new
                {
                    VoucherID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    VoucherCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    VoucherName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    VoucherDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DiscountType = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    MaxDiscountCap = table.Column<decimal>(type: "decimal(12,0)", nullable: true),
                    DiscountTarget = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(12,0)", nullable: true),
                    TotalQuantity = table.Column<int>(type: "int", nullable: true),
                    UsedQuantity = table.Column<int>(type: "int", nullable: false),
                    MaxUsagePerUser = table.Column<short>(type: "smallint", nullable: true, defaultValue: (short)1),
                    StartDate = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    Status = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Vouchers__3AEE79C105B9C507", x => x.VoucherID);
                    table.ForeignKey(
                        name: "FK_Vouchers_Accounts",
                        column: x => x.CreatedBy,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                });

            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    WalletID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false, defaultValue: "VND"),
                    Balance = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    Status = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false, defaultValue: "Active"),
                    LastTransactionAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    LockedBalance = table.Column<decimal>(type: "decimal(12,0)", nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Wallets__84D4F92E5D7899BD", x => x.WalletID);
                    table.ForeignKey(
                        name: "FK_Wallets_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                });

            migrationBuilder.CreateTable(
                name: "WorkSchedules",
                columns: table => new
                {
                    ScheduleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    ShiftTemplateID = table.Column<byte>(type: "tinyint", nullable: false),
                    WorkDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Scheduled"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkSchedules", x => x.ScheduleID);
                    table.ForeignKey(
                        name: "FK_WorkSchedules_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_WorkSchedules_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_WorkSchedules_ShiftTemplates",
                        column: x => x.ShiftTemplateID,
                        principalTable: "ShiftTemplates",
                        principalColumn: "ShiftTemplateID");
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryID = table.Column<short>(type: "smallint", nullable: false),
                    BrandID = table.Column<short>(type: "smallint", nullable: true),
                    PriceRangeID = table.Column<byte>(type: "tinyint", nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ProductStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    LaunchDate = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    StockThreshold = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)10),
                    LowStockNotificationEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastLowStockNotifiedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Products__B40CC6ED1F57C0B1", x => x.ProductID);
                    table.ForeignKey(
                        name: "FK_Products_Brands",
                        column: x => x.BrandID,
                        principalTable: "Brands",
                        principalColumn: "BrandID");
                    table.ForeignKey(
                        name: "FK_Products_Categories",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "CategoryID");
                    table.ForeignKey(
                        name: "FK_Products_PriceRanges",
                        column: x => x.PriceRangeID,
                        principalTable: "PriceRanges",
                        principalColumn: "PriceRangeID");
                });

            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    AddressID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AddressLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    WardCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    DistrictId = table.Column<int>(type: "int", nullable: true),
                    ProvinceId = table.Column<int>(type: "int", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Addresse__091C2A1B80DFE817", x => x.AddressID);
                    table.ForeignKey(
                        name: "FK_Addresses_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_Addresses_Districts",
                        column: x => x.DistrictId,
                        principalTable: "Districts",
                        principalColumn: "DistrictId");
                    table.ForeignKey(
                        name: "FK_Addresses_Provinces",
                        column: x => x.ProvinceId,
                        principalTable: "Provinces",
                        principalColumn: "ProvinceId");
                    table.ForeignKey(
                        name: "FK_Addresses_Wards",
                        column: x => x.WardCode,
                        principalTable: "Wards",
                        principalColumn: "WardCode");
                });

            migrationBuilder.CreateTable(
                name: "AIBlogQueue",
                columns: table => new
                {
                    QueueID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BlogPostID = table.Column<int>(type: "int", nullable: false),
                    StaffID = table.Column<int>(type: "int", nullable: false),
                    TemplateID = table.Column<int>(type: "int", nullable: true),
                    PromptData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Pending"),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIBlogQueue", x => x.QueueID);
                    table.ForeignKey(
                        name: "FK_AIBlogQueue_BlogPosts",
                        column: x => x.BlogPostID,
                        principalTable: "BlogPosts",
                        principalColumn: "BlogPostID");
                    table.ForeignKey(
                        name: "FK_AIBlogQueue_Staff",
                        column: x => x.StaffID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_AIBlogQueue_Template",
                        column: x => x.TemplateID,
                        principalTable: "AiPromptTemplates",
                        principalColumn: "TemplateID");
                });

            migrationBuilder.CreateTable(
                name: "BlogPostReactions",
                columns: table => new
                {
                    ReactionPostID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BlogPostID = table.Column<int>(type: "int", nullable: false),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    ReactionTypeID = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogPostReactions", x => x.ReactionPostID);
                    table.ForeignKey(
                        name: "FK_BlogPostReactions_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_BlogPostReactions_BlogPosts",
                        column: x => x.BlogPostID,
                        principalTable: "BlogPosts",
                        principalColumn: "BlogPostID");
                    table.ForeignKey(
                        name: "FK_BlogPostReactions_ReactionTypes",
                        column: x => x.ReactionTypeID,
                        principalTable: "ReactionTypes",
                        principalColumn: "ReactionTypeID");
                });

            migrationBuilder.CreateTable(
                name: "BlogPostStats",
                columns: table => new
                {
                    BlogPostID = table.Column<int>(type: "int", nullable: false),
                    LikeCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CommentCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__BlogPost__32174149A2A95B6D", x => x.BlogPostID);
                    table.ForeignKey(
                        name: "FK_BlogPostStats_BlogPosts",
                        column: x => x.BlogPostID,
                        principalTable: "BlogPosts",
                        principalColumn: "BlogPostID");
                });

            migrationBuilder.CreateTable(
                name: "ReviewBlogs",
                columns: table => new
                {
                    ReviewBlogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BlogPostID = table.Column<int>(type: "int", nullable: false),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ModerationStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ManualReviewDeadline = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    RetryCount = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    LastRetryAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HiddenBy = table.Column<int>(type: "int", nullable: true),
                    HiddenAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ReviewBl__A19536C07ECC639B", x => x.ReviewBlogID);
                    table.ForeignKey(
                        name: "FK_ReviewBlogs_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_ReviewBlogs_BlogPosts",
                        column: x => x.BlogPostID,
                        principalTable: "BlogPosts",
                        principalColumn: "BlogPostID");
                    table.ForeignKey(
                        name: "FK_ReviewBlogs_HiddenBy",
                        column: x => x.HiddenBy,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                });

            migrationBuilder.CreateTable(
                name: "CampaignApprovalLogs",
                schema: "Notification",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignID = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ActorID = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignApprovalLogs", x => x.LogID);
                    table.CheckConstraint("CK_CAL_Action", "[Action] IN ('Submitted', 'Approved', 'Rejected', 'Recalled', 'Scheduled', 'Rescheduled', 'Cancelled', 'Overridden')");
                    table.CheckConstraint("CK_CAL_RejectedNeedsNote", "[Action] <> 'Rejected' OR [Note] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_CAL_Actor",
                        column: x => x.ActorID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_CAL_Campaigns",
                        column: x => x.CampaignID,
                        principalSchema: "Notification",
                        principalTable: "Campaigns",
                        principalColumn: "CampaignID");
                });

            migrationBuilder.CreateTable(
                name: "CampaignReferenceSnapshots",
                schema: "Notification",
                columns: table => new
                {
                    SnapshotID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignID = table.Column<int>(type: "int", nullable: false),
                    ReferenceType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ReferenceID = table.Column<int>(type: "int", nullable: false),
                    EntityStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    EntityStartDate = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    EntityEndDate = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    IsStale = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    StaleReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StaleDetectedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    SnapshotAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignReferenceSnapshots", x => x.SnapshotID);
                    table.CheckConstraint("CK_CRS_DateRange", "[EntityStartDate] IS NULL OR [EntityEndDate] > [EntityStartDate]");
                    table.CheckConstraint("CK_CRS_ReferenceType", "[ReferenceType] IN ('VOUCHER', 'PRODUCT', 'BLOG', 'SALE', 'OTHER')");
                    table.CheckConstraint("CK_CRS_StaleConsistency", "[IsStale] = 0 OR ([StaleReason] IS NOT NULL AND [StaleDetectedAt] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_CRS_Campaigns",
                        column: x => x.CampaignID,
                        principalSchema: "Notification",
                        principalTable: "Campaigns",
                        principalColumn: "CampaignID");
                });

            migrationBuilder.CreateTable(
                name: "CampaignScheduleLogs",
                schema: "Notification",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignID = table.Column<int>(type: "int", nullable: false),
                    ActorID = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    PreviousScheduledAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    NewScheduledAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignScheduleLogs", x => x.LogID);
                    table.CheckConstraint("CK_CSL_Action", "[Action] IN ('Scheduled', 'Rescheduled')");
                    table.CheckConstraint("CK_CSL_ActionConsistency", "([Action] = 'Scheduled' AND [PreviousScheduledAt] IS NULL) OR ([Action] = 'Rescheduled' AND [PreviousScheduledAt] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_CSL_Actor",
                        column: x => x.ActorID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_CSL_Campaigns",
                        column: x => x.CampaignID,
                        principalSchema: "Notification",
                        principalTable: "Campaigns",
                        principalColumn: "CampaignID");
                });

            migrationBuilder.CreateTable(
                name: "CampaignSchedules",
                schema: "Notification",
                columns: table => new
                {
                    ScheduleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignID = table.Column<int>(type: "int", nullable: false),
                    ScheduledBy = table.Column<int>(type: "int", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    LockedByJobID = table.Column<int>(type: "int", nullable: true),
                    LockedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ExecutionStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Waiting"),
                    AttemptCount = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    MaxAttemptCount = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)3),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignSchedules", x => x.ScheduleID);
                    table.CheckConstraint("CK_CS_AttemptNotExceedMax", "[AttemptCount] <= [MaxAttemptCount]");
                    table.CheckConstraint("CK_CS_ExecutedAtConsistency", "[ExecutionStatus] IN ('Done', 'Failed') OR [ExecutedAt] IS NULL");
                    table.CheckConstraint("CK_CS_ExecutionStatus", "[ExecutionStatus] IN ('Waiting', 'Dispatched', 'Done', 'Failed', 'Cancelled')");
                    table.CheckConstraint("CK_CS_LockConsistency", "([LockedByJobID] IS NULL) = ([LockedAt] IS NULL)");
                    table.ForeignKey(
                        name: "FK_CS_Campaigns",
                        column: x => x.CampaignID,
                        principalSchema: "Notification",
                        principalTable: "Campaigns",
                        principalColumn: "CampaignID");
                    table.ForeignKey(
                        name: "FK_CS_Jobs",
                        column: x => x.LockedByJobID,
                        principalSchema: "System",
                        principalTable: "BackgroundJobs",
                        principalColumn: "JobID");
                    table.ForeignKey(
                        name: "FK_CS_ScheduledBy",
                        column: x => x.ScheduledBy,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                });

            migrationBuilder.CreateTable(
                name: "CampaignStats",
                schema: "Notification",
                columns: table => new
                {
                    StatID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignID = table.Column<int>(type: "int", nullable: false),
                    TotalSent = table.Column<int>(type: "int", nullable: false),
                    TotalRead = table.Column<int>(type: "int", nullable: false),
                    TotalClicked = table.Column<int>(type: "int", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Campaign__3A162D1EA29FD2D5", x => x.StatID);
                    table.ForeignKey(
                        name: "FK_CampaignStats_Campaigns",
                        column: x => x.CampaignID,
                        principalSchema: "Notification",
                        principalTable: "Campaigns",
                        principalColumn: "CampaignID");
                });

            migrationBuilder.CreateTable(
                name: "CampaignTargets",
                schema: "Notification",
                columns: table => new
                {
                    CampaignTargetID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignID = table.Column<int>(type: "int", nullable: false),
                    TargetType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACCOUNT_ID"),
                    TargetValue = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Campaign__C1C43FA75E1078F4", x => x.CampaignTargetID);
                    table.ForeignKey(
                        name: "FK_CampaignTargets_Campaigns",
                        column: x => x.CampaignID,
                        principalSchema: "Notification",
                        principalTable: "Campaigns",
                        principalColumn: "CampaignID");
                });

            migrationBuilder.CreateTable(
                name: "Deliveries",
                schema: "Notification",
                columns: table => new
                {
                    DeliveryID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    CreatedByJobID = table.Column<int>(type: "int", nullable: true),
                    CampaignID = table.Column<int>(type: "int", nullable: true),
                    TemplateCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    RecipientType = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false, defaultValue: "CUSTOMER"),
                    Channel = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "WEB_BELL"),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NotificationType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "SYSTEM"),
                    ActionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ActionTarget = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false, defaultValue: "{}"),
                    Status = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false, defaultValue: "Unread"),
                    ReadAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    EmailStatus = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    PushStatus = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Deliveri__626D8FEE3F04E48B", x => x.DeliveryID);
                    table.ForeignKey(
                        name: "FK_Deliveries_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_Deliveries_Campaigns",
                        column: x => x.CampaignID,
                        principalSchema: "Notification",
                        principalTable: "Campaigns",
                        principalColumn: "CampaignID");
                    table.ForeignKey(
                        name: "FK_Deliveries_Jobs",
                        column: x => x.CreatedByJobID,
                        principalSchema: "System",
                        principalTable: "BackgroundJobs",
                        principalColumn: "JobID");
                    table.ForeignKey(
                        name: "FK_Deliveries_Templates",
                        column: x => x.TemplateCode,
                        principalSchema: "Notification",
                        principalTable: "Templates",
                        principalColumn: "TemplateCode");
                });

            migrationBuilder.CreateTable(
                name: "OrderQueue",
                columns: table => new
                {
                    QueueID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    QueuedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    Reason = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    AssignedBy = table.Column<int>(type: "int", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderQueue", x => x.QueueID);
                    table.ForeignKey(
                        name: "FK_OQ_AssignedBy",
                        column: x => x.AssignedBy,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_OQ_Orders",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                });

            migrationBuilder.CreateTable(
                name: "OrderStatusHistory",
                columns: table => new
                {
                    HistoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    StatusID = table.Column<byte>(type: "tinyint", nullable: false),
                    ChangedBy = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__OrderSta__4D7B4ADDB5579E5E", x => x.HistoryID);
                    table.ForeignKey(
                        name: "FK_OrderStatusHistory_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_OrderStatusHistory_Orders",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                    table.ForeignKey(
                        name: "FK_OrderStatusHistory_Status",
                        column: x => x.StatusID,
                        principalTable: "StatusOrders",
                        principalColumn: "StatusID");
                });

            migrationBuilder.CreateTable(
                name: "ShippingProviderTransactions",
                columns: table => new
                {
                    ShippingTransactionID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ProviderOrderCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    TrackingNumber = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ServiceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ShippingFee = table.Column<decimal>(type: "decimal(12,0)", nullable: true),
                    CodAmount = table.Column<decimal>(type: "decimal(12,0)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedDelivery = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ActualDelivery = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    LastPolledAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Shipping__F215F69363919D74", x => x.ShippingTransactionID);
                    table.ForeignKey(
                        name: "FK_ShippingTxn_Orders",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                });

            migrationBuilder.CreateTable(
                name: "PromotionTimeSlots",
                columns: table => new
                {
                    TimeSlotID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromotionID = table.Column<int>(type: "int", nullable: false),
                    StartAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    EndAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Scheduled"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Promotio__41CC1F52F08BECC5", x => x.TimeSlotID);
                    table.ForeignKey(
                        name: "FK_PromotionTimeSlots_Promotions",
                        column: x => x.PromotionID,
                        principalTable: "Promotions",
                        principalColumn: "PromotionID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderVouchers",
                columns: table => new
                {
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    VoucherID = table.Column<int>(type: "int", nullable: false),
                    DiscountAmountApplied = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    VoucherTarget = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderVouchers", x => new { x.OrderID, x.VoucherID });
                    table.ForeignKey(
                        name: "FK_OrderVouchers_Orders",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                    table.ForeignKey(
                        name: "FK_OrderVouchers_Vouchers",
                        column: x => x.VoucherID,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherID");
                });

            migrationBuilder.CreateTable(
                name: "VoucherUsageLogs",
                columns: table => new
                {
                    UsageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherID = table.Column<int>(type: "int", nullable: false),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__VoucherU__29B197C0D357122E", x => x.UsageID);
                    table.ForeignKey(
                        name: "FK_VoucherUsageLogs_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_VoucherUsageLogs_Orders",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                    table.ForeignKey(
                        name: "FK_VoucherUsageLogs_Vouchers",
                        column: x => x.VoucherID,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherID");
                });

            migrationBuilder.CreateTable(
                name: "WalletPinAttempts",
                columns: table => new
                {
                    AttemptID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WalletID = table.Column<int>(type: "int", nullable: false),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletPinAttempts", x => x.AttemptID);
                    table.ForeignKey(
                        name: "FK_WalletPinAttempts_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_WalletPinAttempts_Wallets",
                        column: x => x.WalletID,
                        principalTable: "Wallets",
                        principalColumn: "WalletID");
                });

            migrationBuilder.CreateTable(
                name: "WalletPins",
                columns: table => new
                {
                    WalletPinID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WalletID = table.Column<int>(type: "int", nullable: false),
                    PinHash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FailedAttempts = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    TotalFailedAttempts = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    LockedUntil = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    LastChangedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletPins", x => x.WalletPinID);
                    table.ForeignKey(
                        name: "FK_WalletPins_Wallets",
                        column: x => x.WalletID,
                        principalTable: "Wallets",
                        principalColumn: "WalletID");
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    WalletTransactionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WalletID = table.Column<int>(type: "int", nullable: false),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    RelatedOrderID = table.Column<int>(type: "int", nullable: true),
                    TxnType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Direction = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    Method = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    ExternalRef = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false, defaultValue: "Pending"),
                    Reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__WalletTr__7184AECF832C4B4D", x => x.WalletTransactionID);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_WalletTransactions_Orders",
                        column: x => x.RelatedOrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                    table.ForeignKey(
                        name: "FK_WalletTransactions_Wallets",
                        column: x => x.WalletID,
                        principalTable: "Wallets",
                        principalColumn: "WalletID");
                });

            migrationBuilder.CreateTable(
                name: "OrderAssignments",
                columns: table => new
                {
                    AssignmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    ScheduleID = table.Column<int>(type: "int", nullable: false),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<byte>(type: "tinyint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    AssignedBy = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAssignments", x => x.AssignmentID);
                    table.ForeignKey(
                        name: "FK_OA_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_OA_AssignedBy",
                        column: x => x.AssignedBy,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_OA_Orders",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                    table.ForeignKey(
                        name: "FK_OA_Roles",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleID");
                    table.ForeignKey(
                        name: "FK_OA_Schedules",
                        column: x => x.ScheduleID,
                        principalTable: "WorkSchedules",
                        principalColumn: "ScheduleID");
                });

            migrationBuilder.CreateTable(
                name: "StaffShiftCapacity",
                columns: table => new
                {
                    CapacityID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleID = table.Column<int>(type: "int", nullable: false),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    CurrentLoad = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    MaxLoad = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)20),
                    ShiftFullNotifiedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffShiftCapacity", x => x.CapacityID);
                    table.ForeignKey(
                        name: "FK_SSC_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_SSC_Schedules",
                        column: x => x.ScheduleID,
                        principalTable: "WorkSchedules",
                        principalColumn: "ScheduleID");
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    CartItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CartID = table.Column<int>(type: "int", nullable: false),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<short>(type: "smallint", nullable: false),
                    PriceAtThatTime = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CartItem__488B0B2A728313E0", x => x.CartItemID);
                    table.ForeignKey(
                        name: "FK_CartItems_Cart",
                        column: x => x.CartID,
                        principalTable: "Cart",
                        principalColumn: "CartID");
                    table.ForeignKey(
                        name: "FK_CartItems_Products",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "ItemSimilarities",
                schema: "Recommendation",
                columns: table => new
                {
                    SimilarityID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceProductID = table.Column<int>(type: "int", nullable: false),
                    SimilarProductID = table.Column<int>(type: "int", nullable: false),
                    SimilarityScore = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    AlgorithmType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "cf"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ItemSimi__64D0C10E684FF281", x => x.SimilarityID);
                    table.ForeignKey(
                        name: "FK_ItemSimilarities_SimilarProduct",
                        column: x => x.SimilarProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                    table.ForeignKey(
                        name: "FK_ItemSimilarities_SourceProduct",
                        column: x => x.SourceProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "ProductDetails",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    MaterialID = table.Column<short>(type: "smallint", nullable: true),
                    AgeID = table.Column<byte>(type: "tinyint", nullable: true),
                    SexID = table.Column<byte>(type: "tinyint", nullable: true),
                    OriginID = table.Column<byte>(type: "tinyint", nullable: true),
                    WeightGram = table.Column<int>(type: "int", nullable: false),
                    LengthCm = table.Column<int>(type: "int", nullable: false),
                    WidthCm = table.Column<int>(type: "int", nullable: false),
                    HeightCm = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ProductD__B40CC6EDB04D8C10", x => x.ProductID);
                    table.ForeignKey(
                        name: "FK_ProductDetails_Ages",
                        column: x => x.AgeID,
                        principalTable: "Ages",
                        principalColumn: "AgeID");
                    table.ForeignKey(
                        name: "FK_ProductDetails_Materials",
                        column: x => x.MaterialID,
                        principalTable: "Materials",
                        principalColumn: "MaterialID");
                    table.ForeignKey(
                        name: "FK_ProductDetails_Origins",
                        column: x => x.OriginID,
                        principalTable: "Origins",
                        principalColumn: "OriginID");
                    table.ForeignKey(
                        name: "FK_ProductDetails_Products",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                    table.ForeignKey(
                        name: "FK_ProductDetails_Sexes",
                        column: x => x.SexID,
                        principalTable: "Sexes",
                        principalColumn: "SexID");
                });

            migrationBuilder.CreateTable(
                name: "ProductFollowers",
                columns: table => new
                {
                    FollowerID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ProductF__E85940F983D3C9F5", x => x.FollowerID);
                    table.ForeignKey(
                        name: "FK_ProductFollowers_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_ProductFollowers_Products",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    ImageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    IsMain = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ProductI__7516F4EC6261C86C", x => x.ImageID);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "ProductPromotions",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    PromotionID = table.Column<int>(type: "int", nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPromotions", x => new { x.ProductID, x.PromotionID });
                    table.ForeignKey(
                        name: "FK_ProductPromotions_Products",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                    table.ForeignKey(
                        name: "FK_ProductPromotions_Promotions",
                        column: x => x.PromotionID,
                        principalTable: "Promotions",
                        principalColumn: "PromotionID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewProducts",
                columns: table => new
                {
                    ReviewID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<byte>(type: "tinyint", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ModerationStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Pending"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsEdited = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ReviewPr__74BC79AE912C0FED", x => x.ReviewID);
                    table.ForeignKey(
                        name: "FK_Reviews_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_Reviews_Orders",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                    table.ForeignKey(
                        name: "FK_Reviews_Products",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "TrendingProducts",
                schema: "Recommendation",
                columns: table => new
                {
                    TrendingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "global"),
                    Score = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    PurchaseCount = table.Column<int>(type: "int", nullable: false),
                    Rank = table.Column<short>(type: "smallint", nullable: false),
                    WindowHours = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)24),
                    ComputedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Trending__8938F5282894286C", x => x.TrendingID);
                    table.ForeignKey(
                        name: "FK_Trending_Products",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "UserProductScores",
                schema: "Recommendation",
                columns: table => new
                {
                    ScoreID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    ViewCount = table.Column<short>(type: "smallint", nullable: false),
                    CartCount = table.Column<byte>(type: "tinyint", nullable: false),
                    PurchaseCount = table.Column<byte>(type: "tinyint", nullable: false),
                    WishlistCount = table.Column<byte>(type: "tinyint", nullable: false),
                    LastInteractedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserProd__7DD229F1BA602B69", x => x.ScoreID);
                    table.ForeignKey(
                        name: "FK_UPS_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_UPS_Products",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "Wishlists",
                columns: table => new
                {
                    WishlistID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Wishlist__233189CBFF107F0A", x => x.WishlistID);
                    table.ForeignKey(
                        name: "FK_Wishlists_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_Wishlists_Products",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "ReviewBlogReactions",
                columns: table => new
                {
                    ReactionBlogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewBlogID = table.Column<int>(type: "int", nullable: false),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    ReactionTypeID = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ReviewBl__6A8A0D2701E53414", x => x.ReactionBlogID);
                    table.ForeignKey(
                        name: "FK_ReviewBlogReactions_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_ReviewBlogReactions_ReactionTypes",
                        column: x => x.ReactionTypeID,
                        principalTable: "ReactionTypes",
                        principalColumn: "ReactionTypeID");
                    table.ForeignKey(
                        name: "FK_ReviewBlogReactions_ReviewBlogs",
                        column: x => x.ReviewBlogID,
                        principalTable: "ReviewBlogs",
                        principalColumn: "ReviewBlogID");
                });

            migrationBuilder.CreateTable(
                name: "ReviewBlogReplies",
                columns: table => new
                {
                    ReplyBlogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewBlogID = table.Column<int>(type: "int", nullable: false),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    ParentReplyID = table.Column<int>(type: "int", nullable: true),
                    ReplyToAccountID = table.Column<int>(type: "int", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ModerationStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ManualReviewDeadline = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    RetryCount = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    LastRetryAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HiddenBy = table.Column<int>(type: "int", nullable: true),
                    HiddenAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ReviewBl__5996364139D7A15C", x => x.ReplyBlogID);
                    table.ForeignKey(
                        name: "FK_ReviewBlogReplies_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_ReviewBlogReplies_HiddenBy",
                        column: x => x.HiddenBy,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_ReviewBlogReplies_Parent",
                        column: x => x.ParentReplyID,
                        principalTable: "ReviewBlogReplies",
                        principalColumn: "ReplyBlogID");
                    table.ForeignKey(
                        name: "FK_ReviewBlogReplies_ReplyTo",
                        column: x => x.ReplyToAccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_ReviewBlogReplies_ReviewBlogs",
                        column: x => x.ReviewBlogID,
                        principalTable: "ReviewBlogs",
                        principalColumn: "ReviewBlogID");
                });

            migrationBuilder.CreateTable(
                name: "DeliveryActions",
                schema: "Notification",
                columns: table => new
                {
                    ActionID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeliveryID = table.Column<long>(type: "bigint", nullable: false),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    ActionTarget = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Delivery__FFE3F4B9CCE7F330", x => x.ActionID);
                    table.ForeignKey(
                        name: "FK_DeliveryActions_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_DeliveryActions_Deliveries",
                        column: x => x.DeliveryID,
                        principalSchema: "Notification",
                        principalTable: "Deliveries",
                        principalColumn: "DeliveryID");
                });

            migrationBuilder.CreateTable(
                name: "ShippingStatusHistories",
                columns: table => new
                {
                    HistoryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShippingTxId = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NewStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Shipping__4D7B4ABDF6123545", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK__ShippingS__Order__1C5231C2",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                    table.ForeignKey(
                        name: "FK__ShippingS__Shipp__1B5E0D89",
                        column: x => x.ShippingTxId,
                        principalTable: "ShippingProviderTransactions",
                        principalColumn: "ShippingTransactionID");
                });

            migrationBuilder.CreateTable(
                name: "PromotionProductSlots",
                columns: table => new
                {
                    SlotProductID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimeSlotID = table.Column<int>(type: "int", nullable: false),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    SaleQuantity = table.Column<int>(type: "int", nullable: false),
                    SoldQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ReservedQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionProductSlots", x => x.SlotProductID);
                    table.ForeignKey(
                        name: "FK_PromotionProductSlots_Products",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                    table.ForeignKey(
                        name: "FK_PromotionProductSlots_TimeSlot",
                        column: x => x.TimeSlotID,
                        principalTable: "PromotionTimeSlots",
                        principalColumn: "TimeSlotID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderRefunds",
                columns: table => new
                {
                    RefundID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    RefundReasonID = table.Column<byte>(type: "tinyint", nullable: true),
                    CustomerID = table.Column<int>(type: "int", nullable: false),
                    RequestedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    WalletTransactionID = table.Column<int>(type: "int", nullable: true),
                    ReasonDetails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RefundSource = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefundType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ReturnAndRefund"),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    RefundCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ShippingOrderCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ReturnShippingOrderCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    InspectionNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InspectionPassed = table.Column<bool>(type: "bit", nullable: true),
                    ShippingFee = table.Column<decimal>(type: "decimal(10,0)", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(12,0)", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(12,0)", nullable: true),
                    AdminNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    StatusID = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ReturnShippingFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReturnShippingFeeBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReturnShippingFeeNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FinalRefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DamageResponsibility = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerShippingPaid = table.Column<decimal>(type: "decimal(12,0)", nullable: false, defaultValue: 0m),
                    IncludeShippingInRefund = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__OrderRef__725AB9001E9FBB52", x => x.RefundID);
                    table.ForeignKey(
                        name: "FK_OrderRefunds_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_OrderRefunds_Customer",
                        column: x => x.CustomerID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_OrderRefunds_Orders",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                    table.ForeignKey(
                        name: "FK_OrderRefunds_RefundReasons",
                        column: x => x.RefundReasonID,
                        principalTable: "OrderRefundReasons",
                        principalColumn: "RefundReasonID");
                    table.ForeignKey(
                        name: "FK_OrderRefunds_RequestedBy",
                        column: x => x.RequestedBy,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_OrderRefunds_StatusRefunds",
                        column: x => x.StatusID,
                        principalTable: "StatusRefunds",
                        principalColumn: "StatusID");
                    table.ForeignKey(
                        name: "FK_OrderRefunds_WalletTransactions",
                        column: x => x.WalletTransactionID,
                        principalTable: "WalletTransactions",
                        principalColumn: "WalletTransactionID");
                });

            migrationBuilder.CreateTable(
                name: "PaymentHistory",
                columns: table => new
                {
                    PaymentHistoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    WalletTransactionID = table.Column<int>(type: "int", nullable: true),
                    PaymentStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    PaymentMethod = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    TransactionCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PaymentH__F3B93391666E61EF", x => x.PaymentHistoryID);
                    table.ForeignKey(
                        name: "FK_PaymentHistory_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_PaymentHistory_Orders",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                    table.ForeignKey(
                        name: "FK_PaymentHistory_WalletTransactions",
                        column: x => x.WalletTransactionID,
                        principalTable: "WalletTransactions",
                        principalColumn: "WalletTransactionID");
                });

            migrationBuilder.CreateTable(
                name: "WithdrawalRequests",
                columns: table => new
                {
                    WithdrawalID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WalletID = table.Column<int>(type: "int", nullable: false),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    WalletTransactionID = table.Column<int>(type: "int", nullable: true),
                    ReferenceId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    ToBankBin = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    ToBankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ToAccountNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ToAccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PayosPayoutId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    PayosTransactionId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    PayosRawResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    FailReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RetryCount = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    ProcessingAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WithdrawalRequests", x => x.WithdrawalID);
                    table.ForeignKey(
                        name: "FK_WithdrawalRequests_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_WithdrawalRequests_WalletTransactions",
                        column: x => x.WalletTransactionID,
                        principalTable: "WalletTransactions",
                        principalColumn: "WalletTransactionID");
                    table.ForeignKey(
                        name: "FK_WithdrawalRequests_Wallets",
                        column: x => x.WalletID,
                        principalTable: "Wallets",
                        principalColumn: "WalletID");
                });

            migrationBuilder.CreateTable(
                name: "ReviewProductImages",
                columns: table => new
                {
                    ReviewProductImageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewProductID = table.Column<int>(type: "int", nullable: false),
                    ImageURL = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    ModerationStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Pending"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ReviewPr__013E0F1E09405F43", x => x.ReviewProductImageID);
                    table.ForeignKey(
                        name: "FK_ReviewProductImages_ReviewProducts",
                        column: x => x.ReviewProductID,
                        principalTable: "ReviewProducts",
                        principalColumn: "ReviewID");
                });

            migrationBuilder.CreateTable(
                name: "ReviewProductReactions",
                columns: table => new
                {
                    ReactionProductID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewProductID = table.Column<int>(type: "int", nullable: false),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    ReactionTypeID = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ReviewPr__B56FCAF15CBA344B", x => x.ReactionProductID);
                    table.ForeignKey(
                        name: "FK_ReviewProductReactions_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_ReviewProductReactions_ReactionTypes",
                        column: x => x.ReactionTypeID,
                        principalTable: "ReactionTypes",
                        principalColumn: "ReactionTypeID");
                    table.ForeignKey(
                        name: "FK_ReviewProductReactions_ReviewProducts",
                        column: x => x.ReviewProductID,
                        principalTable: "ReviewProducts",
                        principalColumn: "ReviewID");
                });

            migrationBuilder.CreateTable(
                name: "StaffReviewProductReplies",
                columns: table => new
                {
                    ReplyProductID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewProductID = table.Column<int>(type: "int", nullable: false),
                    StaffID = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StaffRev__2DCE233CE3243EE5", x => x.ReplyProductID);
                    table.ForeignKey(
                        name: "FK_ReviewProductReplies_Accounts",
                        column: x => x.StaffID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_ReviewProductReplies_ReviewProducts",
                        column: x => x.ReviewProductID,
                        principalTable: "ReviewProducts",
                        principalColumn: "ReviewID");
                });

            migrationBuilder.CreateTable(
                name: "BlogCommentModerationLogs",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TargetType = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    CommentID = table.Column<int>(type: "int", nullable: true),
                    ReplyID = table.Column<int>(type: "int", nullable: true),
                    ModeratorType = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    ModeratedBy = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    BanReasonID = table.Column<byte>(type: "tinyint", nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    ModerationResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogCommentModerationLogs", x => x.LogID);
                    table.ForeignKey(
                        name: "FK_BCML_Accounts",
                        column: x => x.ModeratedBy,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_BCML_BanReasons",
                        column: x => x.BanReasonID,
                        principalTable: "BlogCommentBanReasons",
                        principalColumn: "BanReasonID");
                    table.ForeignKey(
                        name: "FK_BCML_ReviewBlogReplies",
                        column: x => x.ReplyID,
                        principalTable: "ReviewBlogReplies",
                        principalColumn: "ReplyBlogID");
                    table.ForeignKey(
                        name: "FK_BCML_ReviewBlogs",
                        column: x => x.CommentID,
                        principalTable: "ReviewBlogs",
                        principalColumn: "ReviewBlogID");
                });

            migrationBuilder.CreateTable(
                name: "ReviewBlogReplyReactions",
                columns: table => new
                {
                    ReactionReplyBlogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReplyBlogID = table.Column<int>(type: "int", nullable: false),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    ReactionTypeID = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewBlogReplyReactions", x => x.ReactionReplyBlogID);
                    table.ForeignKey(
                        name: "FK_ReviewBlogReplyReactions_Accounts",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_ReviewBlogReplyReactions_ReactionTypes",
                        column: x => x.ReactionTypeID,
                        principalTable: "ReactionTypes",
                        principalColumn: "ReactionTypeID");
                    table.ForeignKey(
                        name: "FK_ReviewBlogReplyReactions_ReviewBlogReplies",
                        column: x => x.ReplyBlogID,
                        principalTable: "ReviewBlogReplies",
                        principalColumn: "ReplyBlogID");
                });

            migrationBuilder.CreateTable(
                name: "OrderDetails",
                columns: table => new
                {
                    OrderDetailID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProductImage = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    Quantity = table.Column<short>(type: "smallint", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(19,0)", nullable: true, computedColumnSql: "(case when ([Quantity]*[UnitPrice]-[DiscountAmount])<(0) then (0) else [Quantity]*[UnitPrice]-[DiscountAmount] end)", stored: true),
                    PromotionID = table.Column<int>(type: "int", nullable: true),
                    SlotProductID = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__OrderDet__D3B9D30C69BA0900", x => x.OrderDetailID);
                    table.ForeignKey(
                        name: "FK_OrderDetails_Orders",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                    table.ForeignKey(
                        name: "FK_OrderDetails_Products",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                    table.ForeignKey(
                        name: "FK_OrderDetails_PromotionProductSlots",
                        column: x => x.SlotProductID,
                        principalTable: "PromotionProductSlots",
                        principalColumn: "SlotProductID");
                    table.ForeignKey(
                        name: "FK_OrderDetails_Promotions",
                        column: x => x.PromotionID,
                        principalTable: "Promotions",
                        principalColumn: "PromotionID");
                });

            migrationBuilder.CreateTable(
                name: "RefundDetails",
                columns: table => new
                {
                    RefundDetailID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RefundID = table.Column<int>(type: "int", nullable: false),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<short>(type: "smallint", nullable: false),
                    RestorableQuantity = table.Column<short>(type: "smallint", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundDetails", x => x.RefundDetailID);
                    table.ForeignKey(
                        name: "FK_RefundDetails_OrderRefunds",
                        column: x => x.RefundID,
                        principalTable: "OrderRefunds",
                        principalColumn: "RefundID");
                    table.ForeignKey(
                        name: "FK_RefundDetails_Products",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "RefundImages",
                columns: table => new
                {
                    RefundImageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RefundID = table.Column<int>(type: "int", nullable: false),
                    ImageURL = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RefundIm__1234567890ABCDEF", x => x.RefundImageID);
                    table.ForeignKey(
                        name: "FK_RefundImages_OrderRefunds",
                        column: x => x.RefundID,
                        principalTable: "OrderRefunds",
                        principalColumn: "RefundID");
                });

            migrationBuilder.CreateTable(
                name: "RefundStatusHistory",
                columns: table => new
                {
                    HistoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RefundID = table.Column<int>(type: "int", nullable: false),
                    StatusID = table.Column<byte>(type: "tinyint", nullable: false),
                    ChangedBy = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundStatusHistory", x => x.HistoryID);
                    table.ForeignKey(
                        name: "FK_RefundStatusHistory_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_RefundStatusHistory_OrderRefunds",
                        column: x => x.RefundID,
                        principalTable: "OrderRefunds",
                        principalColumn: "RefundID");
                    table.ForeignKey(
                        name: "FK_RefundStatusHistory_StatusRefunds",
                        column: x => x.StatusID,
                        principalTable: "StatusRefunds",
                        principalColumn: "StatusID");
                });

            migrationBuilder.CreateTable(
                name: "PaymentGatewayTransactions",
                columns: table => new
                {
                    PaymentGatewayTxnID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    PaymentHistoryID = table.Column<int>(type: "int", nullable: true),
                    Provider = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    RequestID = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    TransactionNo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    ResponseCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    ResponseMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    RawCallback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentGatewayTransactions", x => x.PaymentGatewayTxnID);
                    table.ForeignKey(
                        name: "FK_PayGwTxn_Orders",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                    table.ForeignKey(
                        name: "FK_PayGwTxn_PaymentHistory",
                        column: x => x.PaymentHistoryID,
                        principalTable: "PaymentHistory",
                        principalColumn: "PaymentHistoryID");
                });

            migrationBuilder.CreateTable(
                name: "WithdrawalStatusHistory",
                columns: table => new
                {
                    HistoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WithdrawalID = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    ToStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WithdrawalStatusHistory", x => x.HistoryID);
                    table.ForeignKey(
                        name: "FK_WithdrawalStatusHistory_WithdrawalRequests",
                        column: x => x.WithdrawalID,
                        principalTable: "WithdrawalRequests",
                        principalColumn: "WithdrawalID");
                });

            migrationBuilder.CreateTable(
                name: "ReviewModerationLogs",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TargetType = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    ReviewID = table.Column<int>(type: "int", nullable: false),
                    ImageID = table.Column<int>(type: "int", nullable: true),
                    ModeratorType = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    ModeratedBy = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    AIModelVersion = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ModerationResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ReviewMo__5E5499A8", x => x.LogID);
                    table.ForeignKey(
                        name: "FK_ModerationLogs_Accounts",
                        column: x => x.ModeratedBy,
                        principalTable: "Accounts",
                        principalColumn: "AccountID");
                    table.ForeignKey(
                        name: "FK_ModerationLogs_ReviewProductImages",
                        column: x => x.ImageID,
                        principalTable: "ReviewProductImages",
                        principalColumn: "ReviewProductImageID");
                    table.ForeignKey(
                        name: "FK_ModerationLogs_ReviewProducts",
                        column: x => x.ReviewID,
                        principalTable: "ReviewProducts",
                        principalColumn: "ReviewID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_EmployeeCode",
                table: "Accounts",
                column: "EmployeeCode",
                unique: true,
                filter: "([EmployeeCode] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Login",
                table: "Accounts",
                columns: new[] { "Email", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_RoleID",
                table: "Accounts",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_SexID",
                table: "Accounts",
                column: "SexID");

            migrationBuilder.CreateIndex(
                name: "UQ__Accounts__A9D105341DE5C280",
                table: "Accounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_DistrictId",
                table: "Addresses",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_OneDefaultPerUser",
                table: "Addresses",
                column: "AccountID",
                unique: true,
                filter: "([IsDefault]=(1) AND [IsDeleted]=(0))");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_ProvinceId",
                table: "Addresses",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_WardCode",
                table: "Addresses",
                column: "WardCode");

            migrationBuilder.CreateIndex(
                name: "UQ__Ages__E0EBEE38F4878BA4",
                table: "Ages",
                column: "AgeRange",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AIBlogQueue_BlogPost_RequestedAt",
                table: "AIBlogQueue",
                columns: new[] { "BlogPostID", "RequestedAt", "QueueID" });

            migrationBuilder.CreateIndex(
                name: "IX_AIBlogQueue_StaffID",
                table: "AIBlogQueue",
                column: "StaffID");

            migrationBuilder.CreateIndex(
                name: "IX_AIBlogQueue_Status",
                table: "AIBlogQueue",
                columns: new[] { "Status", "Priority", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AIBlogQueue_TemplateID",
                table: "AIBlogQueue",
                column: "TemplateID");

            migrationBuilder.CreateIndex(
                name: "IX_AiPromptTemplates_DefaultCategoryID",
                table: "AiPromptTemplates",
                column: "DefaultCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_AiPromptTemplates_TemplateName",
                table: "AiPromptTemplates",
                column: "TemplateName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Backgrou__F1AC1A95DC882CD8",
                schema: "System",
                table: "BackgroundJobs",
                column: "JobName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__BlogCate__CD921A00A50628DA",
                table: "BlogCategories",
                column: "BlogCategoriesName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BCML_Comment",
                table: "BlogCommentModerationLogs",
                columns: new[] { "CommentID", "CreatedAt" },
                filter: "([CommentID] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_BCML_ManualReview",
                table: "BlogCommentModerationLogs",
                columns: new[] { "Action", "CreatedAt" },
                filter: "([Action]='ManualReview' OR [Action]='Failed')");

            migrationBuilder.CreateIndex(
                name: "IX_BCML_Reply",
                table: "BlogCommentModerationLogs",
                columns: new[] { "ReplyID", "CreatedAt" },
                filter: "([ReplyID] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_BlogCommentModerationLogs_BanReasonID",
                table: "BlogCommentModerationLogs",
                column: "BanReasonID");

            migrationBuilder.CreateIndex(
                name: "IX_BlogCommentModerationLogs_ModeratedBy",
                table: "BlogCommentModerationLogs",
                column: "ModeratedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BCVC_Banned",
                table: "BlogCommentViolationCount",
                columns: new[] { "IsCommentBanned", "BanExpiresAt" },
                filter: "([IsCommentBanned]=(1))");

            migrationBuilder.CreateIndex(
                name: "IX_BlogCommentViolationCount_UnbannedBy",
                table: "BlogCommentViolationCount",
                column: "UnbannedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPostReactions_ReactionTypeID",
                table: "BlogPostReactions",
                column: "ReactionTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPostReactions_Stats",
                table: "BlogPostReactions",
                columns: new[] { "BlogPostID", "ReactionTypeID" });

            migrationBuilder.CreateIndex(
                name: "UQ_BlogPostReactions_AccountPost",
                table: "BlogPostReactions",
                columns: new[] { "AccountID", "BlogPostID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_AccountID",
                table: "BlogPosts",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_ApprovedBy",
                table: "BlogPosts",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_Category",
                table: "BlogPosts",
                column: "BlogCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_Status_Date",
                table: "BlogPosts",
                columns: new[] { "Status", "IsDeleted", "BlogAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_BlogPostStats_Score",
                table: "BlogPostStats",
                columns: new[] { "LikeCount", "CommentCount" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "UQ__Brands__2206CE9B151E23F4",
                table: "Brands",
                column: "BrandName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CAL_Campaign",
                schema: "Notification",
                table: "CampaignApprovalLogs",
                columns: new[] { "CampaignID", "CreatedAt" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "Action", "ActorID" });

            migrationBuilder.CreateIndex(
                name: "IX_CAL_PendingSubmissions",
                schema: "Notification",
                table: "CampaignApprovalLogs",
                columns: new[] { "Action", "CreatedAt" },
                filter: "[Action] = 'Submitted'")
                .Annotation("SqlServer:Include", new[] { "CampaignID", "ActorID" });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignApprovalLogs_ActorID",
                schema: "Notification",
                table: "CampaignApprovalLogs",
                column: "ActorID");

            migrationBuilder.CreateIndex(
                name: "IX_CRS_RevalidationJob",
                schema: "Notification",
                table: "CampaignReferenceSnapshots",
                columns: new[] { "IsStale", "EntityEndDate" },
                filter: "[IsStale] = 0")
                .Annotation("SqlServer:Include", new[] { "CampaignID", "ReferenceType", "ReferenceID", "EntityStatus" });

            migrationBuilder.CreateIndex(
                name: "UQ_CRS_OneLiveSnapshotPerCampaign",
                schema: "Notification",
                table: "CampaignReferenceSnapshots",
                column: "CampaignID",
                unique: true,
                filter: "[IsStale] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_ApprovedExpired",
                schema: "Notification",
                table: "Campaigns",
                columns: new[] { "Status", "ApprovedExpireAt" },
                filter: "[Status] = 'Approved' AND [IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "CampaignID", "SubmittedByAccountID", "CreatedByAccountID" });

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_CreatedByAccountID",
                schema: "Notification",
                table: "Campaigns",
                column: "CreatedByAccountID");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_PendingApproval",
                schema: "Notification",
                table: "Campaigns",
                columns: new[] { "Status", "SubmittedAt" },
                filter: "[Status] = 'PendingApproval' AND [IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "CampaignID", "CampaignName", "SubmittedByAccountID" });

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_ReviewedByAccountID",
                schema: "Notification",
                table: "Campaigns",
                column: "ReviewedByAccountID");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_ScheduledWithRef",
                schema: "Notification",
                table: "Campaigns",
                columns: new[] { "Status", "ReferenceType", "ScheduledAt" },
                filter: "[Status] = 'Scheduled' AND [ReferenceType] IS NOT NULL AND [IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "CampaignID", "ReferenceID", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_Status",
                schema: "Notification",
                table: "Campaigns",
                columns: new[] { "Status", "CreatedAt" },
                descending: new[] { false, true },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_SubmittedByAccountID",
                schema: "Notification",
                table: "Campaigns",
                column: "SubmittedByAccountID");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_TemplateCode",
                schema: "Notification",
                table: "Campaigns",
                column: "TemplateCode");

            migrationBuilder.CreateIndex(
                name: "UQ_Campaigns_EventKey_Active",
                schema: "Notification",
                table: "Campaigns",
                column: "EventKey",
                unique: true,
                filter: "[EventKey] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignScheduleLogs_ActorID",
                schema: "Notification",
                table: "CampaignScheduleLogs",
                column: "ActorID");

            migrationBuilder.CreateIndex(
                name: "IX_CSL_Campaign",
                schema: "Notification",
                table: "CampaignScheduleLogs",
                columns: new[] { "CampaignID", "CreatedAt" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "ActorID", "Action", "NewScheduledAt", "PreviousScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignSchedules_LockedByJobID",
                schema: "Notification",
                table: "CampaignSchedules",
                column: "LockedByJobID");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignSchedules_ScheduledBy",
                schema: "Notification",
                table: "CampaignSchedules",
                column: "ScheduledBy");

            migrationBuilder.CreateIndex(
                name: "IX_CS_StaleLock",
                schema: "Notification",
                table: "CampaignSchedules",
                columns: new[] { "ExecutionStatus", "LockedAt" },
                filter: "[ExecutionStatus] = 'Dispatched'")
                .Annotation("SqlServer:Include", new[] { "CampaignID", "LockedByJobID" });

            migrationBuilder.CreateIndex(
                name: "IX_CS_Waiting",
                schema: "Notification",
                table: "CampaignSchedules",
                columns: new[] { "ExecutionStatus", "ScheduledAt" },
                filter: "[ExecutionStatus] = 'Waiting'")
                .Annotation("SqlServer:Include", new[] { "CampaignID", "AttemptCount", "MaxAttemptCount" });

            migrationBuilder.CreateIndex(
                name: "UQ_CampaignSchedules_CampaignID",
                schema: "Notification",
                table: "CampaignSchedules",
                column: "CampaignID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_CampaignStats_CampaignID",
                schema: "Notification",
                table: "CampaignStats",
                column: "CampaignID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignTargets_CampaignID",
                schema: "Notification",
                table: "CampaignTargets",
                column: "CampaignID");

            migrationBuilder.CreateIndex(
                name: "UQ__Cart__349DA58776E0F0FB",
                table: "Cart",
                column: "AccountID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ActiveCart",
                table: "CartItems",
                columns: new[] { "CartID", "RemovedAt" },
                filter: "([RemovedAt] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductID",
                table: "CartItems",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "UQ_CartItems_CartProduct",
                table: "CartItems",
                columns: new[] { "CartID", "ProductID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_SuperCategoryID",
                table: "Categories",
                column: "SuperCategoryID");

            migrationBuilder.CreateIndex(
                name: "UQ__Categori__8517B2E01B631F3E",
                table: "Categories",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerChildren_AccountID",
                table: "CustomerChildren",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerChildren_DOB",
                table: "CustomerChildren",
                column: "DOB",
                filter: "([IsDeleted]=(0))");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerChildren_SexID",
                table: "CustomerChildren",
                column: "SexID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDeliveryAbuseCases_Status_StrictUntil",
                table: "CustomerDeliveryAbuseCases",
                columns: new[] { "Status", "StrictPeriodUntil" });

            migrationBuilder.CreateIndex(
                name: "UQ_CustomerDeliveryAbuseCases_Account",
                table: "CustomerDeliveryAbuseCases",
                column: "AccountID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_CampaignID",
                schema: "Notification",
                table: "Deliveries",
                column: "CampaignID");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_CreatedByJobID",
                schema: "Notification",
                table: "Deliveries",
                column: "CreatedByJobID");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_NotificationType",
                schema: "Notification",
                table: "Deliveries",
                columns: new[] { "AccountID", "NotificationType", "Status" },
                filter: "([Status]<>'Deleted')");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_TemplateCode",
                schema: "Notification",
                table: "Deliveries",
                column: "TemplateCode");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_Admin",
                schema: "Notification",
                table: "Deliveries",
                columns: new[] { "RecipientType", "Status", "CreatedAt" },
                descending: new[] { false, false, true },
                filter: "([RecipientType]<>'CUSTOMER')");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_User",
                schema: "Notification",
                table: "Deliveries",
                columns: new[] { "AccountID", "RecipientType", "Status" },
                filter: "([Status]='Unread' AND [RecipientType]='CUSTOMER')");

            migrationBuilder.CreateIndex(
                name: "UQ_Deliveries_IdempotencyKey",
                schema: "Notification",
                table: "Deliveries",
                column: "IdempotencyKey",
                unique: true,
                filter: "([IdempotencyKey] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryActions_AccountID",
                schema: "Notification",
                table: "DeliveryActions",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryActions_DeliveryID",
                schema: "Notification",
                table: "DeliveryActions",
                column: "DeliveryID");

            migrationBuilder.CreateIndex(
                name: "IX_Districts_ProvinceId",
                table: "Districts",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEventOutbox_Pending",
                schema: "System",
                table: "DomainEventOutbox",
                column: "OccurredOn",
                filter: "([ProcessedOn] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_InteractionEvents_UserBehavior",
                schema: "Interaction",
                table: "Events",
                columns: new[] { "AccountID", "EventType", "CreatedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "UQ_Events_EventUid",
                schema: "Interaction",
                table: "Events",
                column: "EventUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemSimilarities_Score",
                schema: "Recommendation",
                table: "ItemSimilarities",
                columns: new[] { "SourceProductID", "SimilarityScore" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ItemSimilarities_SimilarProductID",
                schema: "Recommendation",
                table: "ItemSimilarities",
                column: "SimilarProductID");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSimilarities_Source",
                schema: "Recommendation",
                table: "ItemSimilarities",
                column: "SourceProductID");

            migrationBuilder.CreateIndex(
                name: "UQ__Material__9C87053C5302146A",
                table: "Materials",
                column: "MaterialName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OA_AccountID_Date",
                table: "OrderAssignments",
                columns: new[] { "AccountID", "AssignedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_OA_OrderID",
                table: "OrderAssignments",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_OA_ScheduleID",
                table: "OrderAssignments",
                column: "ScheduleID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAssignments_AssignedBy",
                table: "OrderAssignments",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAssignments_RoleId",
                table: "OrderAssignments",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "UQ_OA_ActiveOrderRole",
                table: "OrderAssignments",
                columns: new[] { "OrderID", "RoleId" },
                unique: true,
                filter: "([IsActive]=(1))");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_ProductSales",
                table: "OrderDetails",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_PromotionID",
                table: "OrderDetails",
                column: "PromotionID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_SlotProductID",
                table: "OrderDetails",
                column: "SlotProductID");

            migrationBuilder.CreateIndex(
                name: "UQ_OrderDetails_OrderProduct",
                table: "OrderDetails",
                columns: new[] { "OrderID", "ProductID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OQ_Unresolved",
                table: "OrderQueue",
                columns: new[] { "IsResolved", "QueuedAt" },
                filter: "([IsResolved]=(0))");

            migrationBuilder.CreateIndex(
                name: "IX_OrderQueue_AssignedBy",
                table: "OrderQueue",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "UQ_OQ_OrderID_Unresolved",
                table: "OrderQueue",
                column: "OrderID",
                unique: true,
                filter: "([IsResolved]=(0))");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRefunds_ApprovedBy",
                table: "OrderRefunds",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRefunds_CustomerID",
                table: "OrderRefunds",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRefunds_Order",
                table: "OrderRefunds",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRefunds_RefundReasonID",
                table: "OrderRefunds",
                column: "RefundReasonID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRefunds_RequestedBy",
                table: "OrderRefunds",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRefunds_ShippingOrderCode",
                table: "OrderRefunds",
                column: "ShippingOrderCode",
                filter: "([ShippingOrderCode] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRefunds_StatusID",
                table: "OrderRefunds",
                column: "StatusID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRefunds_WalletTransactionID",
                table: "OrderRefunds",
                column: "WalletTransactionID");

            migrationBuilder.CreateIndex(
                name: "UQ_OrderRefunds_RefundCode",
                table: "OrderRefunds",
                column: "RefundCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AssignedToMerchID",
                table: "Orders",
                column: "AssignedToMerchID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AssignedToStaffID",
                table: "Orders",
                column: "AssignedToStaffID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CancelledBy",
                table: "Orders",
                column: "CancelledBy");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentCode",
                table: "Orders",
                column: "PaymentCode");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ReportByDate",
                table: "Orders",
                columns: new[] { "OrderDate", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StatusID",
                table: "Orders",
                column: "StatusID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StatusTracking",
                table: "Orders",
                columns: new[] { "OrderCode", "PaymentStatus", "StatusID" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserHistory",
                table: "Orders",
                columns: new[] { "AccountID", "OrderDate" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "UQ__Orders__999B52290487CF36",
                table: "Orders",
                column: "OrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistory_ChangedBy",
                table: "OrderStatusHistory",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistory_OrderID",
                table: "OrderStatusHistory",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistory_StatusID",
                table: "OrderStatusHistory",
                column: "StatusID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderVouchers_VoucherID",
                table: "OrderVouchers",
                column: "VoucherID");

            migrationBuilder.CreateIndex(
                name: "UQ__Origins__636F5CFDD97DCEC5",
                table: "Origins",
                column: "OriginName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayGwTxn_OrderID",
                table: "PaymentGatewayTransactions",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_PayGwTxn_PaymentHistoryID",
                table: "PaymentGatewayTransactions",
                column: "PaymentHistoryID",
                filter: "([PaymentHistoryID] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_PayGwTxn_Provider_Status",
                table: "PaymentGatewayTransactions",
                columns: new[] { "Provider", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ__PaymentG__33A8519B13DA0886",
                table: "PaymentGatewayTransactions",
                column: "RequestID",
                unique: true,
                filter: "[RequestID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentHistory_AccountID",
                table: "PaymentHistory",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentHistory_Order",
                table: "PaymentHistory",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentHistory_WalletTransactionID",
                table: "PaymentHistory",
                column: "WalletTransactionID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDetails_AgeID",
                table: "ProductDetails",
                column: "AgeID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDetails_MaterialID",
                table: "ProductDetails",
                column: "MaterialID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDetails_OriginID",
                table: "ProductDetails",
                column: "OriginID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDetails_SexID",
                table: "ProductDetails",
                column: "SexID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFollowers_Account",
                table: "ProductFollowers",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFollowers_Pending",
                table: "ProductFollowers",
                columns: new[] { "ProductID", "NotifiedAt" },
                filter: "([NotifiedAt] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "UQ_ProductFollowers_ProductAccount",
                table: "ProductFollowers",
                columns: new[] { "ProductID", "AccountID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_ProductImages_OneMain",
                table: "ProductImages",
                column: "ProductID",
                unique: true,
                filter: "([IsMain]=(1))");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPromotions_ProductID_Active",
                table: "ProductPromotions",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPromotions_PromotionID",
                table: "ProductPromotions",
                column: "PromotionID");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Brand_Status",
                table: "Products",
                columns: new[] { "BrandID", "ProductStatus" },
                filter: "([IsDeleted]=(0) AND [BrandID] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category_Status",
                table: "Products",
                columns: new[] { "CategoryID", "ProductStatus" },
                filter: "([IsDeleted]=(0))");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ComingSoon_Launch",
                table: "Products",
                columns: new[] { "ProductStatus", "LaunchDate" },
                filter: "([ProductStatus]='ComingSoon' AND [IsDeleted]=(0))");

            migrationBuilder.CreateIndex(
                name: "IX_Products_FilterSort",
                table: "Products",
                columns: new[] { "CategoryID", "BrandID", "Price", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_LowStock_V2",
                table: "Products",
                columns: new[] { "ProductStatus", "IsDeleted" },
                filter: "([Quantity]<=(10) AND [IsDeleted]=(0) AND [ProductStatus]='Active')");

            migrationBuilder.CreateIndex(
                name: "IX_Products_PriceRange_Status",
                table: "Products",
                columns: new[] { "PriceRangeID", "ProductStatus" },
                filter: "([IsDeleted]=(0) AND [ProductStatus]='Active')");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Search",
                table: "Products",
                columns: new[] { "ProductName", "CategoryID", "BrandID" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionProductSlots_Product",
                table: "PromotionProductSlots",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionProductSlots_Slot_Active",
                table: "PromotionProductSlots",
                column: "TimeSlotID");

            migrationBuilder.CreateIndex(
                name: "UQ_PromotionProductSlots_SlotProduct",
                table: "PromotionProductSlots",
                columns: new[] { "TimeSlotID", "ProductID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_CreatedBy",
                table: "Promotions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_Priority",
                table: "Promotions",
                column: "Priority",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_Status_Time",
                table: "Promotions",
                columns: new[] { "Status", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_Worker",
                table: "Promotions",
                columns: new[] { "Status", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionTimeSlots_Active",
                table: "PromotionTimeSlots",
                columns: new[] { "Status", "StartAt", "EndAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionTimeSlots_Promotion",
                table: "PromotionTimeSlots",
                columns: new[] { "PromotionID", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_PromotionTimeSlots_UniqueSlot",
                table: "PromotionTimeSlots",
                columns: new[] { "PromotionID", "StartAt", "EndAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Reaction__A25C5AA7C65C7840",
                table: "ReactionTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefundDetails_ProductID",
                table: "RefundDetails",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_RefundDetails_RefundID",
                table: "RefundDetails",
                column: "RefundID");

            migrationBuilder.CreateIndex(
                name: "IX_RefundImages_RefundID",
                table: "RefundImages",
                column: "RefundID",
                filter: "([IsDeleted]=(0))");

            migrationBuilder.CreateIndex(
                name: "IX_RefundStatusHistory_ChangedBy",
                table: "RefundStatusHistory",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RefundStatusHistory_RefundID",
                table: "RefundStatusHistory",
                column: "RefundID");

            migrationBuilder.CreateIndex(
                name: "IX_RefundStatusHistory_StatusID",
                table: "RefundStatusHistory",
                column: "StatusID");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewBlogReactions_ReactionTypeID",
                table: "ReviewBlogReactions",
                column: "ReactionTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewBlogReactions_Stats",
                table: "ReviewBlogReactions",
                columns: new[] { "ReviewBlogID", "ReactionTypeID" });

            migrationBuilder.CreateIndex(
                name: "UQ_ReviewBlogReactions_AccountReview",
                table: "ReviewBlogReactions",
                columns: new[] { "AccountID", "ReviewBlogID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewBlogReplies_AccountID",
                table: "ReviewBlogReplies",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewBlogReplies_Comment_Status",
                table: "ReviewBlogReplies",
                columns: new[] { "ReviewBlogID", "ModerationStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewBlogReplies_Failed",
                table: "ReviewBlogReplies",
                columns: new[] { "ModerationStatus", "CreatedAt" },
                filter: "([ModerationStatus]='Failed' OR [ModerationStatus]='Pending')");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewBlogReplies_HiddenBy",
                table: "ReviewBlogReplies",
                column: "HiddenBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewBlogReplies_ParentReplyID",
                table: "ReviewBlogReplies",
                column: "ParentReplyID");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewBlogReplies_ReplyToAccountID",
                table: "ReviewBlogReplies",
                column: "ReplyToAccountID");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewBlogReplyReactions_ReactionTypeID",
                table: "ReviewBlogReplyReactions",
                column: "ReactionTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewBlogReplyReactions_Stats",
                table: "ReviewBlogReplyReactions",
                columns: new[] { "ReplyBlogID", "ReactionTypeID" });

            migrationBuilder.CreateIndex(
                name: "UQ_ReviewBlogReplyReactions_AccountReply",
                table: "ReviewBlogReplyReactions",
                columns: new[] { "AccountID", "ReplyBlogID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewBlogs_AccountID",
                table: "ReviewBlogs",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewBlogs_BlogPost_Status",
                table: "ReviewBlogs",
                columns: new[] { "BlogPostID", "ModerationStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewBlogs_Failed",
                table: "ReviewBlogs",
                columns: new[] { "ModerationStatus", "CreatedAt" },
                filter: "([ModerationStatus]='Failed' OR [ModerationStatus]='Pending')");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewBlogs_HiddenBy",
                table: "ReviewBlogs",
                column: "HiddenBy");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationLogs_AIPerformance",
                table: "ReviewModerationLogs",
                columns: new[] { "ModeratorType", "AIModelVersion", "Action" },
                filter: "([ModeratorType]='AI')");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationLogs_ManualReview",
                table: "ReviewModerationLogs",
                columns: new[] { "Action", "CreatedAt" },
                filter: "([Action]='ManualReview')");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationLogs_Review",
                table: "ReviewModerationLogs",
                columns: new[] { "ReviewID", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewModerationLogs_ImageID",
                table: "ReviewModerationLogs",
                column: "ImageID");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewModerationLogs_ModeratedBy",
                table: "ReviewModerationLogs",
                column: "ModeratedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewProductImages_ModerationPending",
                table: "ReviewProductImages",
                columns: new[] { "ModerationStatus", "CreatedAt" },
                filter: "([ModerationStatus]='Pending' AND [IsDeleted]=(0))");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewProductImages_ReviewProductID",
                table: "ReviewProductImages",
                column: "ReviewProductID");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewProductReactions_ReactionTypeID",
                table: "ReviewProductReactions",
                column: "ReactionTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewProductReactions_ReviewProductID",
                table: "ReviewProductReactions",
                column: "ReviewProductID");

            migrationBuilder.CreateIndex(
                name: "UQ_ReviewProductReactions_AccountReview",
                table: "ReviewProductReactions",
                columns: new[] { "AccountID", "ReviewProductID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewProducts_ManualReview",
                table: "ReviewProducts",
                columns: new[] { "ModerationStatus", "CreatedAt" },
                filter: "([ModerationStatus]='ManualReview' AND [IsDeleted]=(0))");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewProducts_ModerationPending",
                table: "ReviewProducts",
                columns: new[] { "ModerationStatus", "CreatedAt" },
                filter: "([ModerationStatus]='Pending' AND [IsDeleted]=(0))");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewProducts_OrderID",
                table: "ReviewProducts",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewProducts_Product",
                table: "ReviewProducts",
                columns: new[] { "ProductID", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "UQ_Review_Account_Order_Product",
                table: "ReviewProducts",
                columns: new[] { "AccountID", "OrderID", "ProductID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Roles__8A2B61608A369932",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_SavedBankAccounts_AccountBin",
                table: "SavedBankAccounts",
                columns: new[] { "AccountID", "BankBin", "AccountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_SavedBankAccounts_OneDefault",
                table: "SavedBankAccounts",
                column: "AccountID",
                unique: true,
                filter: "([IsDefault]=(1) AND [IsDeleted]=(0))");

            migrationBuilder.CreateIndex(
                name: "UQ__Sexes__BA354290E01BBEF1",
                table: "Sexes",
                column: "SexName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_ShiftTemplates_ShiftName",
                table: "ShiftTemplates",
                column: "ShiftName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShippingProviderTransactions_Polling",
                table: "ShippingProviderTransactions",
                columns: new[] { "Provider", "Status", "OrderID" },
                filter: "([Status]<>'delivered' AND [Status]<>'returned' AND [Status]<>'return_fail' AND [Status]<>'exception' AND [Status]<>'damage' AND [Status]<>'lost' AND [Status]<>'cancel')");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingTxn_OrderID",
                table: "ShippingProviderTransactions",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingTxn_Provider_Status",
                table: "ShippingProviderTransactions",
                columns: new[] { "Provider", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingStatusHistories_OrderId",
                table: "ShippingStatusHistories",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingStatusHistories_ShippingTxId",
                table: "ShippingStatusHistories",
                column: "ShippingTxId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffReviewProductReplies_ReviewProductID",
                table: "StaffReviewProductReplies",
                column: "ReviewProductID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffReviewProductReplies_StaffID",
                table: "StaffReviewProductReplies",
                column: "StaffID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffShiftCapacity_AccountID",
                table: "StaffShiftCapacity",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "UQ_SSC_ScheduleID",
                table: "StaffShiftCapacity",
                column: "ScheduleID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__StatusOr__05E7698A8B998D32",
                table: "StatusOrders",
                column: "StatusName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__SuperCat__3FA779DF65E3CBC6",
                table: "SuperCategories",
                column: "SuperCategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Template__0FDB5081E141D516",
                schema: "Notification",
                table: "Templates",
                column: "TemplateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trending_Scope_Rank",
                schema: "Recommendation",
                table: "TrendingProducts",
                columns: new[] { "Scope", "WindowHours", "Rank" });

            migrationBuilder.CreateIndex(
                name: "UQ_Trending_Product_Scope_Window",
                schema: "Recommendation",
                table: "TrendingProducts",
                columns: new[] { "ProductID", "Scope", "WindowHours" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_UserPreferences_AccountID",
                schema: "Notification",
                table: "UserPreferences",
                column: "AccountID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UPS_User",
                schema: "Recommendation",
                table: "UserProductScores",
                columns: new[] { "AccountID", "Score" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_UserProductScores_ProductID",
                schema: "Recommendation",
                table: "UserProductScores",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "UQ_UserProductScores",
                schema: "Recommendation",
                table: "UserProductScores",
                columns: new[] { "AccountID", "ProductID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_CreatedBy",
                table: "Vouchers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_Worker",
                table: "Vouchers",
                columns: new[] { "Status", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "UQ_Vouchers_Code_Active",
                table: "Vouchers",
                column: "VoucherCode",
                unique: true,
                filter: "([IsDeleted]=(0))");

            migrationBuilder.CreateIndex(
                name: "IX_VoucherUsageLogs_AccountID",
                table: "VoucherUsageLogs",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_VoucherUsageLogs_Analytics",
                table: "VoucherUsageLogs",
                columns: new[] { "VoucherID", "UsedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VoucherUsageLogs_OrderID",
                table: "VoucherUsageLogs",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_WalletPinAttempts_AccountID",
                table: "WalletPinAttempts",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_WalletPinAttempts_Wallet",
                table: "WalletPinAttempts",
                columns: new[] { "WalletID", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "UQ_WalletPins_WalletID",
                table: "WalletPins",
                column: "WalletID",
                unique: true,
                filter: "([IsActive]=(1))");

            migrationBuilder.CreateIndex(
                name: "UQ__Wallets__349DA587207D60C0",
                table: "Wallets",
                column: "AccountID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_Account",
                table: "WalletTransactions",
                columns: new[] { "AccountID", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_Order",
                table: "WalletTransactions",
                column: "RelatedOrderID",
                filter: "([RelatedOrderID] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_Wallet",
                table: "WalletTransactions",
                columns: new[] { "WalletID", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "UQ_WalletTransactions_IdempotencyKey",
                table: "WalletTransactions",
                column: "IdempotencyKey",
                unique: true,
                filter: "([IdempotencyKey] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Wards_DistrictId",
                table: "Wards",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "UQ__Widgets__C77DBD58FBAAB47D",
                schema: "Recommendation",
                table: "Widgets",
                column: "WidgetCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wishlists_ProductID",
                table: "Wishlists",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlists_User",
                table: "Wishlists",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "UQ_Wishlists_AccountProduct",
                table: "Wishlists",
                columns: new[] { "AccountID", "ProductID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_Account",
                table: "WithdrawalRequests",
                columns: new[] { "AccountID", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_PayosPayoutId",
                table: "WithdrawalRequests",
                column: "PayosPayoutId",
                filter: "([PayosPayoutId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_Pending",
                table: "WithdrawalRequests",
                columns: new[] { "Status", "CreatedAt" },
                filter: "([Status]='PENDING' OR [Status]='PROCESSING')");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_WalletID",
                table: "WithdrawalRequests",
                column: "WalletID");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_WalletTransactionID",
                table: "WithdrawalRequests",
                column: "WalletTransactionID");

            migrationBuilder.CreateIndex(
                name: "UQ_WithdrawalRequests_ReferenceId",
                table: "WithdrawalRequests",
                column: "ReferenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalStatusHistory_Withdrawal",
                table: "WithdrawalStatusHistory",
                columns: new[] { "WithdrawalID", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_CreatedBy",
                table: "WorkSchedules",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_Date_Status",
                table: "WorkSchedules",
                columns: new[] { "WorkDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_ShiftTemplateID",
                table: "WorkSchedules",
                column: "ShiftTemplateID");

            migrationBuilder.CreateIndex(
                name: "UQ_WorkSchedules_StaffShiftDay",
                table: "WorkSchedules",
                columns: new[] { "AccountID", "WorkDate", "ShiftTemplateID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "AIBlogQueue");

            migrationBuilder.DropTable(
                name: "BlogCommentModerationLogs");

            migrationBuilder.DropTable(
                name: "BlogCommentViolationCount");

            migrationBuilder.DropTable(
                name: "BlogPostReactions");

            migrationBuilder.DropTable(
                name: "BlogPostStats");

            migrationBuilder.DropTable(
                name: "CampaignApprovalLogs",
                schema: "Notification");

            migrationBuilder.DropTable(
                name: "CampaignReferenceSnapshots",
                schema: "Notification");

            migrationBuilder.DropTable(
                name: "CampaignScheduleLogs",
                schema: "Notification");

            migrationBuilder.DropTable(
                name: "CampaignSchedules",
                schema: "Notification");

            migrationBuilder.DropTable(
                name: "CampaignStats",
                schema: "Notification");

            migrationBuilder.DropTable(
                name: "CampaignTargets",
                schema: "Notification");

            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "CustomerChildren");

            migrationBuilder.DropTable(
                name: "CustomerDeliveryAbuseCases");

            migrationBuilder.DropTable(
                name: "DeliveryActions",
                schema: "Notification");

            migrationBuilder.DropTable(
                name: "DomainEventOutbox",
                schema: "System");

            migrationBuilder.DropTable(
                name: "Events",
                schema: "Interaction");

            migrationBuilder.DropTable(
                name: "ItemSimilarities",
                schema: "Recommendation");

            migrationBuilder.DropTable(
                name: "OrderAssignments");

            migrationBuilder.DropTable(
                name: "OrderDetails");

            migrationBuilder.DropTable(
                name: "OrderQueue");

            migrationBuilder.DropTable(
                name: "OrderStatusHistory");

            migrationBuilder.DropTable(
                name: "OrderVouchers");

            migrationBuilder.DropTable(
                name: "PaymentGatewayTransactions");

            migrationBuilder.DropTable(
                name: "ProductDetails");

            migrationBuilder.DropTable(
                name: "ProductFollowers");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "ProductPromotions");

            migrationBuilder.DropTable(
                name: "RefundDetails");

            migrationBuilder.DropTable(
                name: "RefundImages");

            migrationBuilder.DropTable(
                name: "RefundStatusHistory");

            migrationBuilder.DropTable(
                name: "ReviewBlogReactions");

            migrationBuilder.DropTable(
                name: "ReviewBlogReplyReactions");

            migrationBuilder.DropTable(
                name: "ReviewModerationLogs");

            migrationBuilder.DropTable(
                name: "ReviewProductReactions");

            migrationBuilder.DropTable(
                name: "SavedBankAccounts");

            migrationBuilder.DropTable(
                name: "ShippingStatusHistories");

            migrationBuilder.DropTable(
                name: "StaffReviewProductReplies");

            migrationBuilder.DropTable(
                name: "StaffShiftCapacity");

            migrationBuilder.DropTable(
                name: "TrendingProducts",
                schema: "Recommendation");

            migrationBuilder.DropTable(
                name: "UserPreferences",
                schema: "Notification");

            migrationBuilder.DropTable(
                name: "UserProductScores",
                schema: "Recommendation");

            migrationBuilder.DropTable(
                name: "VoucherUsageLogs");

            migrationBuilder.DropTable(
                name: "WalletPinAttempts");

            migrationBuilder.DropTable(
                name: "WalletPins");

            migrationBuilder.DropTable(
                name: "Widgets",
                schema: "Recommendation");

            migrationBuilder.DropTable(
                name: "Wishlists");

            migrationBuilder.DropTable(
                name: "WithdrawalStatusHistory");

            migrationBuilder.DropTable(
                name: "Wards");

            migrationBuilder.DropTable(
                name: "AiPromptTemplates");

            migrationBuilder.DropTable(
                name: "BlogCommentBanReasons");

            migrationBuilder.DropTable(
                name: "Cart");

            migrationBuilder.DropTable(
                name: "Deliveries",
                schema: "Notification");

            migrationBuilder.DropTable(
                name: "PromotionProductSlots");

            migrationBuilder.DropTable(
                name: "PaymentHistory");

            migrationBuilder.DropTable(
                name: "Ages");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "Origins");

            migrationBuilder.DropTable(
                name: "OrderRefunds");

            migrationBuilder.DropTable(
                name: "ReviewBlogReplies");

            migrationBuilder.DropTable(
                name: "ReviewProductImages");

            migrationBuilder.DropTable(
                name: "ReactionTypes");

            migrationBuilder.DropTable(
                name: "ShippingProviderTransactions");

            migrationBuilder.DropTable(
                name: "WorkSchedules");

            migrationBuilder.DropTable(
                name: "Vouchers");

            migrationBuilder.DropTable(
                name: "WithdrawalRequests");

            migrationBuilder.DropTable(
                name: "Districts");

            migrationBuilder.DropTable(
                name: "Campaigns",
                schema: "Notification");

            migrationBuilder.DropTable(
                name: "BackgroundJobs",
                schema: "System");

            migrationBuilder.DropTable(
                name: "PromotionTimeSlots");

            migrationBuilder.DropTable(
                name: "OrderRefundReasons");

            migrationBuilder.DropTable(
                name: "StatusRefunds");

            migrationBuilder.DropTable(
                name: "ReviewBlogs");

            migrationBuilder.DropTable(
                name: "ReviewProducts");

            migrationBuilder.DropTable(
                name: "ShiftTemplates");

            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "Provinces");

            migrationBuilder.DropTable(
                name: "Templates",
                schema: "Notification");

            migrationBuilder.DropTable(
                name: "Promotions");

            migrationBuilder.DropTable(
                name: "BlogPosts");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Wallets");

            migrationBuilder.DropTable(
                name: "BlogCategories");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "PriceRanges");

            migrationBuilder.DropTable(
                name: "StatusOrders");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "SuperCategories");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Sexes");
        }
    }
}
