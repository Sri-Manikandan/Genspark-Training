using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusBooking.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCitiesRoutesAndPlatformFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "citext", maxLength: 120, nullable: false),
                    state = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_fee_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    value = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_admin_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_fee_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "routes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    distance_km = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routes", x => x.id);
                    table.ForeignKey(
                        name: "FK_routes_cities_destination_city_id",
                        column: x => x.destination_city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_routes_cities_source_city_id",
                        column: x => x.source_city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cities_name",
                table: "cities",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_fee_config_effective_from",
                table: "platform_fee_config",
                column: "effective_from");

            migrationBuilder.CreateIndex(
                name: "IX_routes_destination_city_id",
                table: "routes",
                column: "destination_city_id");

            migrationBuilder.CreateIndex(
                name: "IX_routes_source_city_id_destination_city_id",
                table: "routes",
                columns: new[] { "source_city_id", "destination_city_id" },
                unique: true);

            migrationBuilder.Sql(
                "CREATE INDEX ix_cities_name_trgm ON cities USING gin (name gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_cities_name_trgm;");

            migrationBuilder.DropTable(
                name: "platform_fee_config");

            migrationBuilder.DropTable(
                name: "routes");

            migrationBuilder.DropTable(
                name: "cities");
        }
    }
}
