using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tests.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "AnimalsDb");

            migrationBuilder.CreateSequence(
                name: "PersonSequence",
                schema: "AnimalsDb");

            migrationBuilder.CreateSequence(
                name: "PetSequence",
                schema: "AnimalsDb");

            migrationBuilder.CreateTable(
                name: "People",
                schema: "AnimalsDb",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('\"AnimalsDb\".\"PersonSequence\"')"),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Age = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cats",
                schema: "AnimalsDb",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('\"AnimalsDb\".\"PetSequence\"')"),
                    Age = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    LovesSleeping = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cats_People_OwnerId",
                        column: x => x.OwnerId,
                        principalSchema: "AnimalsDb",
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dogs",
                schema: "AnimalsDb",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('\"AnimalsDb\".\"PetSequence\"')"),
                    Age = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    LovesChasingSticks = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dogs_People_OwnerId",
                        column: x => x.OwnerId,
                        principalSchema: "AnimalsDb",
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cats_OwnerId",
                schema: "AnimalsDb",
                table: "Cats",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Dogs_OwnerId",
                schema: "AnimalsDb",
                table: "Dogs",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cats",
                schema: "AnimalsDb");

            migrationBuilder.DropTable(
                name: "Dogs",
                schema: "AnimalsDb");

            migrationBuilder.DropTable(
                name: "People",
                schema: "AnimalsDb");

            migrationBuilder.DropSequence(
                name: "PersonSequence",
                schema: "AnimalsDb");

            migrationBuilder.DropSequence(
                name: "PetSequence",
                schema: "AnimalsDb");
        }
    }
}
