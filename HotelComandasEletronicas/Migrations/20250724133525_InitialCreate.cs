using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelComandasEletronicas.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LOGS_SISTEMA",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CodigoUsuario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Acao = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Tabela = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegistroID = table.Column<int>(type: "int", nullable: true),
                    DetalhesAntes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetalhesDepois = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOGS_SISTEMA", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PRODUTOS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descricao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioCadastro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PRODUTOS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "REGISTROS_HOSPEDE",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroQuarto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NomeCliente = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TelefoneCliente = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValorGastoTotal = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UsuarioRegistro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REGISTROS_HOSPEDE", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "USUARIOS_SISTEMA",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Login = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodigoID = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Perfil = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Senha = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimoAcesso = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIOS_SISTEMA", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "LANCAMENTOS_CONSUMO",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataHoraLancamento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RegistroHospedeID = table.Column<int>(type: "int", nullable: false),
                    ProdutoID = table.Column<int>(type: "int", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CodigoUsuarioLancamento = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ObservacoesCancelamento = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DataCancelamento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioCancelamento = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LANCAMENTOS_CONSUMO", x => x.ID);
                    table.ForeignKey(
                        name: "FK_LANCAMENTOS_CONSUMO_PRODUTOS_ProdutoID",
                        column: x => x.ProdutoID,
                        principalTable: "PRODUTOS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LANCAMENTOS_CONSUMO_REGISTROS_HOSPEDE_RegistroHospedeID",
                        column: x => x.RegistroHospedeID,
                        principalTable: "REGISTROS_HOSPEDE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LANCAMENTOS_CONSUMO_CodigoUsuarioLancamento",
                table: "LANCAMENTOS_CONSUMO",
                column: "CodigoUsuarioLancamento");

            migrationBuilder.CreateIndex(
                name: "IX_LANCAMENTOS_CONSUMO_DataHoraLancamento",
                table: "LANCAMENTOS_CONSUMO",
                column: "DataHoraLancamento");

            migrationBuilder.CreateIndex(
                name: "IX_LANCAMENTOS_CONSUMO_ProdutoID",
                table: "LANCAMENTOS_CONSUMO",
                column: "ProdutoID");

            migrationBuilder.CreateIndex(
                name: "IX_LANCAMENTOS_CONSUMO_RegistroHospedeID",
                table: "LANCAMENTOS_CONSUMO",
                column: "RegistroHospedeID");

            migrationBuilder.CreateIndex(
                name: "IX_LANCAMENTOS_CONSUMO_Status",
                table: "LANCAMENTOS_CONSUMO",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LOGS_SISTEMA_Acao",
                table: "LOGS_SISTEMA",
                column: "Acao");

            migrationBuilder.CreateIndex(
                name: "IX_LOGS_SISTEMA_CodigoUsuario",
                table: "LOGS_SISTEMA",
                column: "CodigoUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_LOGS_SISTEMA_DataHora",
                table: "LOGS_SISTEMA",
                column: "DataHora");

            migrationBuilder.CreateIndex(
                name: "IX_PRODUTOS_Categoria",
                table: "PRODUTOS",
                column: "Categoria");

            migrationBuilder.CreateIndex(
                name: "IX_PRODUTOS_Descricao",
                table: "PRODUTOS",
                column: "Descricao");

            migrationBuilder.CreateIndex(
                name: "IX_REGISTROS_HOSPEDE_NumeroQuarto",
                table: "REGISTROS_HOSPEDE",
                column: "NumeroQuarto",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USUARIOS_SISTEMA_CodigoID",
                table: "USUARIOS_SISTEMA",
                column: "CodigoID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USUARIOS_SISTEMA_Login",
                table: "USUARIOS_SISTEMA",
                column: "Login",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LANCAMENTOS_CONSUMO");

            migrationBuilder.DropTable(
                name: "LOGS_SISTEMA");

            migrationBuilder.DropTable(
                name: "USUARIOS_SISTEMA");

            migrationBuilder.DropTable(
                name: "PRODUTOS");

            migrationBuilder.DropTable(
                name: "REGISTROS_HOSPEDE");
        }
    }
}
