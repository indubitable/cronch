using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cronch.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Execution",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartReason = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CompletedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExitCode = table.Column<int>(type: "INTEGER", nullable: true),
                    StopReason = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Execution", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Execution");
        }
    }
}
