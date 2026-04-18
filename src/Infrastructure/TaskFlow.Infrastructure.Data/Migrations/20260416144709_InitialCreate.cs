using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "taskflow");

            migrationBuilder.CreateTable(
                name: "Attachment",
                schema: "taskflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageUri = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OwnerType = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachment", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateTable(
                name: "Category",
                schema: "taskflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                    table.ForeignKey(
                        name: "FK_Category_Category_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalSchema: "taskflow",
                        principalTable: "Category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tag",
                schema: "taskflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tag", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                });

            migrationBuilder.CreateTable(
                name: "TaskItem",
                schema: "taskflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Features = table.Column<int>(type: "int", nullable: false),
                    EstimatedEffort = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 2, nullable: true),
                    ActualEffort = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 2, nullable: true),
                    CompletedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentTaskItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DueDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RecurrenceInterval = table.Column<int>(type: "int", nullable: true),
                    RecurrenceFrequency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RecurrenceEndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskItem", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                    table.ForeignKey(
                        name: "FK_TaskItem_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "taskflow",
                        principalTable: "Category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TaskItem_TaskItem_ParentTaskItemId",
                        column: x => x.ParentTaskItemId,
                        principalSchema: "taskflow",
                        principalTable: "TaskItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChecklistItem",
                schema: "taskflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CompletedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TaskItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistItem", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                    table.ForeignKey(
                        name: "FK_ChecklistItem_TaskItem_TaskItemId",
                        column: x => x.TaskItemId,
                        principalSchema: "taskflow",
                        principalTable: "TaskItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comment",
                schema: "taskflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TaskItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comment", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                    table.ForeignKey(
                        name: "FK_Comment_TaskItem_TaskItemId",
                        column: x => x.TaskItemId,
                        principalSchema: "taskflow",
                        principalTable: "TaskItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskItemTag",
                schema: "taskflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskItemTag", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                    table.ForeignKey(
                        name: "FK_TaskItemTag_Tag_TagId",
                        column: x => x.TagId,
                        principalSchema: "taskflow",
                        principalTable: "Tag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskItemTag_TaskItem_TaskItemId",
                        column: x => x.TaskItemId,
                        principalSchema: "taskflow",
                        principalTable: "TaskItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_OwnerType_OwnerId",
                schema: "taskflow",
                table: "Attachment",
                columns: new[] { "OwnerType", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_TenantId_Id",
                schema: "taskflow",
                table: "Attachment",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Category_ParentCategoryId",
                schema: "taskflow",
                table: "Category",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Category_TenantId",
                schema: "taskflow",
                table: "Category",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Category_TenantId_Name",
                schema: "taskflow",
                table: "Category",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "CIX_ChecklistItem_TenantId_TaskItemId",
                schema: "taskflow",
                table: "ChecklistItem",
                columns: new[] { "TenantId", "TaskItemId" })
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistItem_TaskItemId",
                schema: "taskflow",
                table: "ChecklistItem",
                column: "TaskItemId");

            migrationBuilder.CreateIndex(
                name: "CIX_Comment_TenantId_TaskItemId",
                schema: "taskflow",
                table: "Comment",
                columns: new[] { "TenantId", "TaskItemId" })
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_Comment_TaskItemId",
                schema: "taskflow",
                table: "Comment",
                column: "TaskItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_TenantId",
                schema: "taskflow",
                table: "Tag",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_TenantId_Name",
                schema: "taskflow",
                table: "Tag",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "CIX_TaskItem_TenantId_Id",
                schema: "taskflow",
                table: "TaskItem",
                columns: new[] { "TenantId", "Id" },
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskItem_CategoryId",
                schema: "taskflow",
                table: "TaskItem",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItem_ParentTaskItemId",
                schema: "taskflow",
                table: "TaskItem",
                column: "ParentTaskItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItem_Priority",
                schema: "taskflow",
                table: "TaskItem",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItem_Status",
                schema: "taskflow",
                table: "TaskItem",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "CIX_TaskItemTag_TenantId_TaskItemId",
                schema: "taskflow",
                table: "TaskItemTag",
                columns: new[] { "TenantId", "TaskItemId" })
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskItemTag_TagId",
                schema: "taskflow",
                table: "TaskItemTag",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItemTag_TaskItemId_TagId",
                schema: "taskflow",
                table: "TaskItemTag",
                columns: new[] { "TaskItemId", "TagId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attachment",
                schema: "taskflow");

            migrationBuilder.DropTable(
                name: "ChecklistItem",
                schema: "taskflow");

            migrationBuilder.DropTable(
                name: "Comment",
                schema: "taskflow");

            migrationBuilder.DropTable(
                name: "TaskItemTag",
                schema: "taskflow");

            migrationBuilder.DropTable(
                name: "Tag",
                schema: "taskflow");

            migrationBuilder.DropTable(
                name: "TaskItem",
                schema: "taskflow");

            migrationBuilder.DropTable(
                name: "Category",
                schema: "taskflow");
        }
    }
}
