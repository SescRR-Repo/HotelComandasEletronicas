using Microsoft.EntityFrameworkCore;
using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Data
{
    public class ComandasDbContext : DbContext
    {
        public ComandasDbContext(DbContextOptions<ComandasDbContext> options) : base(options)
        {
        }

        // DbSets das entidades
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<RegistroHospede> RegistrosHospede { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<LancamentoConsumo> LancamentosConsumo { get; set; }
        public DbSet<LogSistema> LogsSistema { get; set; }

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

            // Configurações da entidade LogSistema
            modelBuilder.Entity<LogSistema>(entity =>
            {
                entity.Property(e => e.CodigoUsuario).HasMaxLength(50);
                entity.Property(e => e.Acao).HasMaxLength(50);
                entity.Property(e => e.Tabela).HasMaxLength(50);
                entity.Property(e => e.IPAddress).HasMaxLength(45);
                entity.Property(e => e.DetalhesAntes).HasColumnType("nvarchar(max)");
                entity.Property(e => e.DetalhesDepois).HasColumnType("nvarchar(max)");

                // Índices para consultas de auditoria
                entity.HasIndex(e => e.DataHora);
                entity.HasIndex(e => e.CodigoUsuario);
                entity.HasIndex(e => e.Acao);
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Esta configuração só será usada se não estiver configurado no Program.cs
                optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=HotelComandasDB;Trusted_Connection=true;TrustServerCertificate=true;");
            }
        }

        // Método para popular dados iniciais
        public void PopularDadosIniciais()
        {
            // Verificar se já existem dados
            if (!Usuarios.Any())
            {
                // Criar usuário supervisor inicial
                var supervisorInicial = new Usuario
                {
                    Nome = "Maria Silva",
                    Login = "mariasilva01",
                    CodigoID = "01",
                    Perfil = "Supervisor",
                    Senha = BCrypt.Net.BCrypt.HashPassword("123456"), // Senha temporária
                    Status = true,
                    DataCadastro = DateTime.Now
                };

                // Criar usuário recepção inicial
                var recepcaoInicial = new Usuario
                {
                    Nome = "Ana Clara",
                    Login = "anacclara01",
                    CodigoID = "03",
                    Perfil = "Recepção",
                    Senha = BCrypt.Net.BCrypt.HashPassword("123456"), // Senha temporária
                    Status = true,
                    DataCadastro = DateTime.Now
                };

                // Criar garçom inicial
                var garcomInicial = new Usuario
                {
                    Nome = "João Santos",
                    Login = "joaosantos18",
                    CodigoID = "18",
                    Perfil = "Garçom",
                    Senha = "", // Garçom não usa senha, apenas código
                    Status = true,
                    DataCadastro = DateTime.Now
                };

                Usuarios.AddRange(supervisorInicial, recepcaoInicial, garcomInicial);
            }

            // Verificar se já existem produtos
            if (!Produtos.Any())
            {
                var produtosIniciais = new List<Produto>
                {
                    new Produto { Descricao = "Água Mineral", Valor = 3.50m, Categoria = "Bebidas", UsuarioCadastro = "mariasilva01" },
                    new Produto { Descricao = "Refrigerante Lata", Valor = 5.00m, Categoria = "Bebidas", UsuarioCadastro = "mariasilva01" },
                    new Produto { Descricao = "Cerveja", Valor = 8.00m, Categoria = "Bebidas", UsuarioCadastro = "mariasilva01" },
                    new Produto { Descricao = "Suco Natural", Valor = 6.50m, Categoria = "Bebidas", UsuarioCadastro = "mariasilva01" },
                    new Produto { Descricao = "Sanduíche Natural", Valor = 12.00m, Categoria = "Comidas", UsuarioCadastro = "mariasilva01" },
                    new Produto { Descricao = "Porção de Batata", Valor = 15.00m, Categoria = "Comidas", UsuarioCadastro = "mariasilva01" },
                    new Produto { Descricao = "Hambúrguer", Valor = 18.00m, Categoria = "Comidas", UsuarioCadastro = "mariasilva01" },
                    new Produto { Descricao = "Toalha Extra", Valor = 10.00m, Categoria = "Serviços", UsuarioCadastro = "mariasilva01" },
                    new Produto { Descricao = "Serviço de Quarto", Valor = 25.00m, Categoria = "Serviços", UsuarioCadastro = "mariasilva01" }
                };

                Produtos.AddRange(produtosIniciais);
            }

            SaveChanges();
        }
    }
}