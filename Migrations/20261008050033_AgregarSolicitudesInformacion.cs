using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VigiaTrujillo.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSolicitudesInformacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitudesInformacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IncidenciaId = table.Column<int>(type: "int", nullable: false),
                    Pregunta = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SolicitadoPor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Respuesta = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RespondidoPor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FechaRespuesta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchivoRuta = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ArchivoNombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RespuestaVista = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesInformacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesInformacion_Incidencias_IncidenciaId",
                        column: x => x.IncidenciaId,
                        principalTable: "Incidencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesInformacion_Estado",
                table: "SolicitudesInformacion",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesInformacion_IncidenciaId",
                table: "SolicitudesInformacion",
                column: "IncidenciaId");

            // Algunos registros antiguos tenían el estado escrito sin tilde.
            // Se unifican con los nombres oficiales para que filtros y contadores funcionen igual.
            migrationBuilder.Sql("UPDATE Incidencias SET Estado = N'Pendiente de revisión' WHERE Estado = N'Pendiente de revision'");
            migrationBuilder.Sql("UPDATE Incidencias SET Estado = N'En revisión' WHERE Estado = N'En revision'");
            migrationBuilder.Sql("UPDATE Incidencias SET Estado = N'Información solicitada' WHERE Estado = N'Informacion solicitada'");
            migrationBuilder.Sql("UPDATE Incidencias SET Estado = N'En verificación' WHERE Estado = N'En verificacion'");
            migrationBuilder.Sql("UPDATE Obras SET Estado = N'En ejecución' WHERE Estado = N'En ejecucion'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesInformacion");
        }
    }
}
