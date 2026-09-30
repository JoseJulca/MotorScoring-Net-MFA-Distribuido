using Microsoft.EntityFrameworkCore;
using MotorScoring.Adapters.Outbound.Persistence.Models;
namespace MotorScoring.Adapters.Outbound.Persistence.Context;

public sealed class MotorScoringDbContext(DbContextOptions<MotorScoringDbContext> options) : DbContext(options)
{
    public DbSet<SolicitanteModel> Solicitantes => Set<SolicitanteModel>();
    public DbSet<SolicitudCreditoModel> Solicitudes => Set<SolicitudCreditoModel>();
    public DbSet<ProductoCrediticioModel> Productos => Set<ProductoCrediticioModel>();
    public DbSet<ModeloScoringModel> Modelos => Set<ModeloScoringModel>();
    public DbSet<VersionModeloModel> Versiones => Set<VersionModeloModel>();
    public DbSet<FactorScoringModel> Factores => Set<FactorScoringModel>();
    public DbSet<ReglaEvaluacionModel> Reglas => Set<ReglaEvaluacionModel>();
    public DbSet<EvaluacionCrediticiaModel> Evaluaciones => Set<EvaluacionCrediticiaModel>();
    public DbSet<ResultadoFactorModel> ResultadosFactor => Set<ResultadoFactorModel>();
    public DbSet<ResultadoScoringModel> ResultadosScoring => Set<ResultadoScoringModel>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<SolicitanteModel>(e => {
            e.ToTable("solicitantes");
            e.HasKey(x => x.IdSolicitante);
            e.HasIndex(x => new {
                x.TipoDocumento,
                x.NumeroDocumento
            }).IsUnique();
            e.Property(x => x.IngresosMensuales).HasPrecision(18, 2);
            e.Property(x => x.GastosMensuales).HasPrecision(18, 2);
            e.Property(x => x.ObligacionesFinancieras).HasPrecision(18, 2);
        });
        b.Entity<SolicitudCreditoModel>(e => {
            e.ToTable("solicitudes_credito");
            e.HasKey(x => x.IdSolicitud);
            e.HasIndex(x => x.IdentificadorExterno).IsUnique();
            e.Property(x => x.MontoSolicitado).HasPrecision(18, 2);
        });
        b.Entity<ProductoCrediticioModel>(e => {
            e.ToTable("productos_crediticios");
            e.HasKey(x => x.IdProducto);
            e.HasIndex(x => x.Codigo).IsUnique();
            e.Property(x => x.MontoMinimo).HasPrecision(18, 2);
            e.Property(x => x.MontoMaximo).HasPrecision(18, 2);
        });
        b.Entity<ModeloScoringModel>(e => {
            e.ToTable("modelos_scoring");
            e.HasKey(x => x.IdModelo);
            e.HasIndex(x => x.Codigo).IsUnique();
            e.HasMany(x => x.Versiones).WithOne().HasForeignKey(x => x.IdModelo);
        });
        b.Entity<VersionModeloModel>(e => {
            e.ToTable("versiones_modelo");
            e.HasKey(x => x.IdVersionModelo);
            e.HasIndex(x => new {
                x.IdModelo,
                x.NumeroVersion
            }).IsUnique();
            e.HasMany(x => x.Factores).WithOne().HasForeignKey(x => x.IdVersionModelo);
        });
        b.Entity<FactorScoringModel>(e => {
            e.ToTable("factores_scoring");
            e.HasKey(x => x.IdFactor);
            e.Property(x => x.Peso).HasPrecision(5, 2);
            e.HasIndex(x => new {
                x.IdVersionModelo,
                x.Codigo
            }).IsUnique();
            e.HasMany(x => x.Reglas).WithOne().HasForeignKey(x => x.IdFactor);
        });
        b.Entity<ReglaEvaluacionModel>(e => {
            e.ToTable("reglas_evaluacion");
            e.HasKey(x => x.IdRegla);
            e.Property(x => x.ValorMinimo).HasPrecision(18, 4);
            e.Property(x => x.ValorMaximo).HasPrecision(18, 4);
            e.HasIndex(x => new {
                x.IdFactor,
                x.Codigo
            }).IsUnique();
        });
        b.Entity<EvaluacionCrediticiaModel>(e => {
            e.ToTable("evaluaciones_crediticias");
            e.HasKey(x => x.IdEvaluacion);
            e.HasIndex(x => new {
                x.IdSolicitud,
                x.IdVersionModelo
            }).IsUnique();
            e.HasMany(x => x.ResultadosFactor).WithOne().HasForeignKey(x => x.IdEvaluacion);
            e.HasOne(x => x.ResultadoScoring).WithOne().HasForeignKey<ResultadoScoringModel>(x => x.IdEvaluacion);
        });
        b.Entity<ResultadoFactorModel>(e => {
            e.ToTable("resultados_factor");
            e.HasKey(x => x.IdResultadoFactor);
            e.Property(x => x.ValorEvaluado).HasPrecision(18, 4);
            e.Property(x => x.PesoAplicado).HasPrecision(5, 2);
        });
        b.Entity<ResultadoScoringModel>(e => {
            e.ToTable("resultados_scoring");
            e.HasKey(x => x.IdResultadoScoring);
            e.HasIndex(x => x.IdEvaluacion).IsUnique();
        });
    }
}