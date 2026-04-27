using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusBooking.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatorDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "buses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    operator_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_number = table.Column<string>(type: "citext", maxLength: 32, nullable: false),
                    bus_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    bus_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    approval_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    operational_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reject_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buses", x => x.id);
                    table.ForeignKey(
                        name: "FK_buses_users_operator_user_id",
                        column: x => x.operator_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operator_offices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    operator_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_line = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operator_offices", x => x.id);
                    table.ForeignKey(
                        name: "FK_operator_offices_cities_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operator_offices_users_operator_user_id",
                        column: x => x.operator_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "operator_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    company_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reject_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operator_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_operator_requests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seat_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bus_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seat_number = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    row_index = table.Column<int>(type: "integer", nullable: false),
                    column_index = table.Column<int>(type: "integer", nullable: false),
                    seat_category = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seat_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_seat_definitions_buses_bus_id",
                        column: x => x.bus_id,
                        principalTable: "buses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_target_type_target_id",
                table: "audit_log",
                columns: new[] { "target_type", "target_id" });

            migrationBuilder.CreateIndex(
                name: "IX_buses_operator_user_id_approval_status",
                table: "buses",
                columns: new[] { "operator_user_id", "approval_status" });

            migrationBuilder.CreateIndex(
                name: "IX_buses_registration_number",
                table: "buses",
                column: "registration_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_operator_offices_city_id",
                table: "operator_offices",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "IX_operator_offices_operator_user_id_city_id",
                table: "operator_offices",
                columns: new[] { "operator_user_id", "city_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_operator_requests_user_id",
                table: "operator_requests",
                column: "user_id",
                unique: true,
                filter: "status = 'pending'");

            migrationBuilder.CreateIndex(
                name: "IX_seat_definitions_bus_id_seat_number",
                table: "seat_definitions",
                columns: new[] { "bus_id", "seat_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "operator_offices");

            migrationBuilder.DropTable(
                name: "operator_requests");

            migrationBuilder.DropTable(
                name: "seat_definitions");

            migrationBuilder.DropTable(
                name: "buses");
        }
    }
}
