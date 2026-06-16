using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddConsensusEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Consensuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ParticipantSensorCount = table.Column<int>(type: "integer", nullable: false),
                    ExcludedSensorCount = table.Column<int>(type: "integer", nullable: false),
                    ParticipantSensorIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    ExcludedSensorIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    StandardDeviation = table.Column<double>(type: "double precision", nullable: false),
                    Median = table.Column<double>(type: "double precision", nullable: false),
                    WindowStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WindowEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consensuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Heartbeats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SensorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Heartbeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Heartbeats_Sensors_SensorId",
                        column: x => x.SensorId,
                        principalTable: "Sensors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Heartbeats_SensorId",
                table: "Heartbeats",
                column: "SensorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Consensuses");

            migrationBuilder.DropTable(
                name: "Heartbeats");
        }
    }
}
