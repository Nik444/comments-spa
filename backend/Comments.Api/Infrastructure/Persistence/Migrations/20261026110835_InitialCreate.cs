using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comments.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    RootId = table.Column<long>(type: "bigint", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    HomePage = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttachmentKind = table.Column<int>(type: "int", nullable: true),
                    AttachmentKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AttachmentName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AttachmentSize = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.CheckConstraint("CK_Comments_ParentId_RootId", "([ParentId] IS NULL AND [RootId] IS NULL) OR ([ParentId] IS NOT NULL AND [RootId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Comments_Comments_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Comments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Comments_Comments_RootId",
                        column: x => x.RootId,
                        principalTable: "Comments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ParentId",
                table: "Comments",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_RootId_CreatedAt",
                table: "Comments",
                columns: new[] { "RootId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_Roots_CreatedAt",
                table: "Comments",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0],
                filter: "[ParentId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_Roots_Email",
                table: "Comments",
                columns: new[] { "Email", "Id" },
                filter: "[ParentId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_Roots_UserName",
                table: "Comments",
                columns: new[] { "UserName", "Id" },
                filter: "[ParentId] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comments");
        }
    }
}
