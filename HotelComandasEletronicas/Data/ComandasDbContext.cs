using Microsoft.EntityFrameworkCore;
using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Data
{
    public class ComandasDbContext : DbContext
    {
        public ComandasDbContext(DbContextOptions<ComandasDbContext> options) : base(options)
        {
        }

        // DbSets das entidades (SEM LogsSistema)
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<RegistroHospede> RegistrosHospede { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<LancamentoConsumo> LancamentosConsumo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurações da entidade Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasIndex(e => e.Login).IsUnique();
                entity.HasIndex(e => e.CodigoID).IsUnique();
                entity.Property(e => e.Nome).HasMaxLength(100);
                entity.Property(e => e.Login).HasMaxLength(50);
                entity.Property(e => e.CodigoID).HasMaxLength(2);
                entity.Property(e => e.Perfil).HasMaxLength(20);
            });

            // Configurações da entidade RegistroHospede
            modelBuilder.Entity<RegistroHospede>(entity =>
            {
                entity.HasIndex(e => e.NumeroQuarto).IsUnique();
                entity.Property(e => e.NumeroQuarto).HasMaxLength(20);
                entity.Property(e => e.NomeCliente).HasMaxLength(100);
                entity.Property(e => e.TelefoneCliente).HasMaxLength(20);
                entity.Property(e => e.ValorGastoTotal).HasColumnType("decimal(10,2)");
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.UsuarioRegistro).HasMaxLength(50);
            });

            // Configurações da entidade Produto
            modelBuilder.Entity<Produto>(entity =>
            {
                entity.Property(e => e.Descricao).HasMaxLength(100);
                entity.Property(e => e.Valor).HasColumnType("decimal(10,2)");
                entity.Property(e => e.Categoria).HasMaxLength(30);
                entity.Property(e => e.UsuarioCadastro).HasMaxLength(50);
                entity.HasIndex(e => e.Descricao);
                entity.HasIndex(e => e.Categoria);
            });

            // Configurações da entidade LancamentoConsumo
            modelBuilder.Entity<LancamentoConsumo>(entity =>
            {
                entity.Property(e => e.Quantidade).HasColumnType("decimal(8,2)");
                entity.Property(e => e.ValorUnitario).HasColumnType("decimal(10,2)");
                entity.Property(e => e.ValorTotal).HasColumnType("decimal(10,2)");
                entity.Property(e => e.CodigoUsuarioLancamento).HasMaxLength(2);
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.ObservacoesCancelamento).HasMaxLength(200);
                entity.Property(e => e.UsuarioCancelamento).HasMaxLength(2);

                // Relacionamentos
                entity.HasOne(e => e.RegistroHospede)
                    .WithMany(e => e.Lancamentos)
                    .HasForeignKey(e => e.RegistroHospedeID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Produto)
                    .WithMany(e => e.Lancamentos)
                    .HasForeignKey(e => e.ProdutoID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Índices para performance
                entity.HasIndex(e => e.DataHoraLancamento);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CodigoUsuarioLancamento);
            });

            // REMOVIDO: Configurações da entidade LogSistema
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=HotelComandasDB;Trusted_Connection=true;TrustServerCertificate=true;");
            }
        }

        // Método para popular dados iniciais (SEM LOGS)
        public void PopularDadosIniciais()
        {
            try
            {
                Console.WriteLine(" Iniciando população de dados iniciais...");

                // ===================================
                //  CRIAR USUÁRIOS INICIAIS
                // ===================================
                if (!Usuarios.Any())
                {
                    Console.WriteLine(" Criando usuários iniciais...");

                    var usuariosIniciais = new List<Usuario>
                    {
                        new Usuario
                        {
                            Nome = "Administrador do Sistema",
                            Login = "admin",
                            CodigoID = "00",
                            Perfil = "Supervisor",
                            Senha = BCrypt.Net.BCrypt.HashPassword("admin123"),
                            Status = true,
                            DataCadastro = DateTime.Now
                        },
                        new Usuario
                        {
                            Nome = "Maria Silva",
                            Login = "mariasilva01",
                            CodigoID = "01",
                            Perfil = "Supervisor",
                            Senha = BCrypt.Net.BCrypt.HashPassword("123456"),
                            Status = true,
                            DataCadastro = DateTime.Now
                        },
                        new Usuario
                        {
                            Nome = "Ana Clara Santos",
                            Login = "anacclara01",
                            CodigoID = "03",
                            Perfil = "Recepção",
                            Senha = BCrypt.Net.BCrypt.HashPassword("123456"),
                            Status = true,
                            DataCadastro = DateTime.Now
                        },
                        new Usuario
                        {
                            Nome = "João Santos",
                            Login = "joaosantos18",
                            CodigoID = "18",
                            Perfil = "Garçom",
                            Senha = "",
                            Status = true,
                            DataCadastro = DateTime.Now
                        }
                    };

                    Usuarios.AddRange(usuariosIniciais);
                    SaveChanges();
                    Console.WriteLine($" {usuariosIniciais.Count} usuários criados!");
                }

                // ===================================
                //  CRIAR PRODUTOS INICIAIS
                // ===================================
                if (!Produtos.Any())
                {
                    Console.WriteLine(" Criando produtos iniciais...");

                    var produtosIniciais = new List<Produto>
                    {
                        // BEBIDAS
                        new Produto { Descricao = "Água Mineral 500ml", Valor = 3.50m, Categoria = "Bebidas", UsuarioCadastro = "admin", DataCadastro = DateTime.Now, Status = true },
                        new Produto { Descricao = "Refrigerante Lata 350ml", Valor = 5.00m, Categoria = "Bebidas", UsuarioCadastro = "admin", DataCadastro = DateTime.Now, Status = true },
                        new Produto { Descricao = "Cerveja Long Neck", Valor = 8.00m, Categoria = "Bebidas", UsuarioCadastro = "admin", DataCadastro = DateTime.Now, Status = true },
                        new Produto { Descricao = "Suco Natural 300ml", Valor = 6.50m, Categoria = "Bebidas", UsuarioCadastro = "admin", DataCadastro = DateTime.Now, Status = true },
                        new Produto { Descricao = "Caipirinha", Valor = 12.00m, Categoria = "Bebidas", UsuarioCadastro = "admin", DataCadastro = DateTime.Now, Status = true },

                        // COMIDAS
                        new Produto { Descricao = "Sanduíche Natural", Valor = 15.00m, Categoria = "Comidas", UsuarioCadastro = "admin", DataCadastro = DateTime.Now, Status = true },
                        new Produto { Descricao = "Porção de Batata Frita", Valor = 18.00m, Categoria = "Comidas", UsuarioCadastro = "admin", DataCadastro = DateTime.Now, Status = true },
                        new Produto { Descricao = "Hambúrguer Artesanal", Valor = 25.00m, Categoria = "Comidas", UsuarioCadastro = "admin", DataCadastro = DateTime.Now, Status = true },
                        new Produto { Descricao = "Porção de Pastel", Valor = 20.00m, Categoria = "Comidas", UsuarioCadastro = "admin", DataCadastro = DateTime.Now, Status = true },

                        // SERVIÇOS
                        new Produto { Descricao = "Toalha Extra", Valor = 10.00m, Categoria = "Serviços", UsuarioCadastro = "admin", DataCadastro = DateTime.Now, Status = true },
                        new Produto { Descricao = "Serviço de Quarto", Valor = 30.00m, Categoria = "Serviços", UsuarioCadastro = "admin", DataCadastro = DateTime.Now, Status = true },
                        new Produto { Descricao = "Lavanderia Express", Valor = 25.00m, Categoria = "Serviços", UsuarioCadastro = "admin", DataCadastro = DateTime.Now, Status = true }
                    };

                    Produtos.AddRange(produtosIniciais);
                    SaveChanges();
                    Console.WriteLine($" {produtosIniciais.Count} produtos criados!");
                }

                // ===================================
                //  CRIAR HÓSPEDES DE TESTE
                // ===================================
                if (!RegistrosHospede.Any())
                {
                    Console.WriteLine(" Criando hóspedes de teste...");

                    var hospedesTest = new List<RegistroHospede>
                    {
                        new RegistroHospede
                        {
                            NumeroQuarto = "101",
                            NomeCliente = "João Silva",
                            TelefoneCliente = "(95) 99999-1234",
                            DataRegistro = DateTime.Now.AddDays(-2),
                            ValorGastoTotal = 0.00m,
                            Status = "Ativo",
                            UsuarioRegistro = "anacclara01"
                        },
                        new RegistroHospede
                        {
                            NumeroQuarto = "205",
                            NomeCliente = "Maria Santos",
                            TelefoneCliente = "(95) 99888-5678",
                            DataRegistro = DateTime.Now.AddDays(-1),
                            ValorGastoTotal = 0.00m,
                            Status = "Ativo",
                            UsuarioRegistro = "anacclara01"
                        }
                    };

                    RegistrosHospede.AddRange(hospedesTest);
                    SaveChanges();
                    Console.WriteLine($" {hospedesTest.Count} hóspedes de teste criados!");
                }

                // REMOVIDO: Criação de LogSistema

                // ===================================
                //  ESTATÍSTICAS FINAIS (SEM LOGS)
                // ===================================
                var stats = new
                {
                    TotalUsuarios = Usuarios.Count(),
                    TotalProdutos = Produtos.Count(),
                    TotalHospedes = RegistrosHospede.Count()
                    // REMOVIDO: TotalLogs
                };

                Console.WriteLine("====================================");
                Console.WriteLine(" DADOS INICIAIS CONFIGURADOS:");
                Console.WriteLine($"     Usuários: {stats.TotalUsuarios}");
                Console.WriteLine($"     Produtos: {stats.TotalProdutos}");
                Console.WriteLine($"     Hóspedes: {stats.TotalHospedes}");
                Console.WriteLine("====================================");
                Console.WriteLine(" Sistema pronto para uso!");
                Console.WriteLine(" Login Admin: admin / admin123");
                Console.WriteLine(" Login Maria: mariasilva01 / 123456");
                Console.WriteLine(" Login Ana: anacclara01 / 123456");
                Console.WriteLine(" Código João: 18 (Garçom)");
                Console.WriteLine("====================================");

            }
            catch (Exception ex)
            {
                Console.WriteLine($" Erro ao popular dados iniciais: {ex.Message}");
                throw;
            }
        }
    }
}