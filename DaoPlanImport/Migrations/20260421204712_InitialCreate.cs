using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaoPlanImport.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobPolygons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiomNr = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    JobNr = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Polygon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LocationCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPolygons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ligas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ImportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DATO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DARTID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DISTNR = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DIOMNR = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JOBSID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JOBNR = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RAEKKEFOELGE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LIGASORTNR = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VEJBEMAERK = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ADRBEMAERK = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GADENAVN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HUSNR = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LITRA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ETAGE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SIDELEJLIGHED = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ABONNR = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ABONNAVN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CONAVN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AFLOTEKST = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ETAGELEVERING = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SUPPADRESSE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PRODUKTNR = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PRODUKTKORT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PRODUKTANTAL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ADRESSERET = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NOEGLEBUNDTHUL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    REKLAMATION = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TILGANG = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FORDNR = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    POSTNR = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    POSTDISTRIKT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    STEDBETEGNELSE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GADESORT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HUSNRSORT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LABELSLEVERING = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    STANGNR = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    STANGSUFFIX = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JOBADRNR = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HUSN_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SOURCE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LONG = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LAT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RECEIPT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LIGA_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BARCODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PHOTO_URL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SORT_NO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JOSTNR = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PRIORITET = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DOERKODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PAKKE_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LABELLESS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FULD_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HOMEBOX_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FOTO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LOCATION_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KONTO_NO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HOEJDE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BREDDE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LAENGDE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VAEGT = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ligas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobPolygons_CreatedDate",
                table: "JobPolygons",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_JobPolygons_DiomNr_JobNr",
                table: "JobPolygons",
                columns: new[] { "DiomNr", "JobNr" },
                unique: true,
                filter: "[DiomNr] IS NOT NULL AND [JobNr] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Ligas_FileName",
                table: "Ligas",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_Ligas_FileName_ImportDate",
                table: "Ligas",
                columns: new[] { "FileName", "ImportDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Ligas_ImportDate",
                table: "Ligas",
                column: "ImportDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobPolygons");

            migrationBuilder.DropTable(
                name: "Ligas");
        }
    }
}
