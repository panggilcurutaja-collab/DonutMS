using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonutMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase2Integrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecipeVersionIngredients_RecipeVersionId",
                table: "RecipeVersionIngredients");

            migrationBuilder.DropIndex(
                name: "IX_RecipeIngredients_RecipeId",
                table: "RecipeIngredients");

            migrationBuilder.DropIndex(
                name: "IX_InventoryStocks_IngredientId",
                table: "InventoryStocks");

            migrationBuilder.DropIndex(
                name: "IX_BatchIngredients_BatchId",
                table: "BatchIngredients");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeVersionIngredients_RecipeVersionId_IngredientId",
                table: "RecipeVersionIngredients",
                columns: new[] { "RecipeVersionId", "IngredientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeId_IngredientId",
                table: "RecipeIngredients",
                columns: new[] { "RecipeId", "IngredientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStocks_IngredientId",
                table: "InventoryStocks",
                column: "IngredientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BatchIngredients_BatchId_IngredientId",
                table: "BatchIngredients",
                columns: new[] { "BatchId", "IngredientId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecipeVersionIngredients_RecipeVersionId_IngredientId",
                table: "RecipeVersionIngredients");

            migrationBuilder.DropIndex(
                name: "IX_RecipeIngredients_RecipeId_IngredientId",
                table: "RecipeIngredients");

            migrationBuilder.DropIndex(
                name: "IX_InventoryStocks_IngredientId",
                table: "InventoryStocks");

            migrationBuilder.DropIndex(
                name: "IX_BatchIngredients_BatchId_IngredientId",
                table: "BatchIngredients");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeVersionIngredients_RecipeVersionId",
                table: "RecipeVersionIngredients",
                column: "RecipeVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeId",
                table: "RecipeIngredients",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStocks_IngredientId",
                table: "InventoryStocks",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchIngredients_BatchId",
                table: "BatchIngredients",
                column: "BatchId");
        }
    }
}
