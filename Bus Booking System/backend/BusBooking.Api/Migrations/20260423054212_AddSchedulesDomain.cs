using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusBooking.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulesDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bus_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bus_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    departure_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    arrival_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    fare_per_seat = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: false),
                    days_of_week = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bus_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_bus_schedules_buses_bus_id",
                        column: x => x.bus_id,
                        principalTable: "buses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bus_schedules_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bus_trips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    cancel_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bus_trips", x => x.id);
                    table.ForeignKey(
                        name: "FK_bus_trips_bus_schedules_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "bus_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bus_schedules_bus_id",
                table: "bus_schedules",
                column: "bus_id");

            migrationBuilder.CreateIndex(
                name: "IX_bus_schedules_route_id_is_active",
                table: "bus_schedules",
                columns: new[] { "route_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_bus_trips_schedule_id_trip_date",
                table: "bus_trips",
                columns: new[] { "schedule_id", "trip_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bus_trips");

            migrationBuilder.DropTable(
                name: "bus_schedules");
        }
    }
}
