using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIlian.EfCore.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class DefineConversationParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ConversationTurn_From",
                table: "ConversationTurn",
                column: "From");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConversationTurn_From",
                table: "ConversationTurn");
        }
    }
}
