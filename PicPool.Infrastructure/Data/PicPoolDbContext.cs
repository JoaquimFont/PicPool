using Microsoft.EntityFrameworkCore;
using PicPool.Domain.Entities;

namespace PicPool.Infrastructure.Data;

public class PicPoolDbContext : DbContext
{
    /// <summary>
    /// Explicació: inicialitza el context d'Entity Framework Core de PicPool.
    /// Precondicions: les opcions del context han d'incloure la configuració de base de dades necessària.
    /// Postcondicions: el context queda preparat per consultar i persistir les entitats del domini.
    /// </summary>
    public PicPoolDbContext(DbContextOptions<PicPoolDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuari> Usuaris => Set<Usuari>();
    public DbSet<Pla> Plans => Set<Pla>();
    public DbSet<UsuariPla> UsuariPlans => Set<UsuariPla>();
    public DbSet<Sala> Sales => Set<Sala>();
    public DbSet<SalaUsuari> SalaUsuaris => Set<SalaUsuari>();
    public DbSet<Imatge> Imatges { get; set; }
    public DbSet<SalaImatge> SalaImatges { get; set; }

    public DbSet<SalaLinkCompartit> SalaLinksCompartits { get; set; }
    /// <summary>
    /// Explicació: configura el model relacional, claus, longituds, índexs i relacions entre entitats.
    /// Precondicions: Entity Framework ha de proporcionar un <see cref="ModelBuilder"/> vàlid durant la construcció del model.
    /// Postcondicions: el model queda configurat amb les restriccions i relacions que utilitzarà la base de dades.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuari>(entity =>
        {
            entity.HasKey(e => e.UsuariPK);

            entity.Property(e => e.UsuariPK)
                .IsRequired()
                .HasMaxLength(50)
                .ValueGeneratedNever();

            entity.Property(e => e.Nom)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);
        });

        modelBuilder.Entity<Pla>(entity =>
        {
            entity.HasKey(e => e.PlaPK);

            entity.Property(e => e.PlaPK)
                .IsRequired()
                .HasMaxLength(50)
                .ValueGeneratedNever();

            entity.Property(e => e.Nom)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Preu)
                .HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<UsuariPla>(entity =>
        {
            entity.HasKey(e => e.UsuariPlaPK);

            entity.Property(e => e.UsuariPlaPK)
                .IsRequired()
                .HasMaxLength(50)
                .ValueGeneratedNever();

            entity.Property(e => e.UsuariPK)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.PlaPK)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne<Usuari>()
                .WithMany(u => u.UsuariPlans)
                .HasForeignKey(e => e.UsuariPK)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Pla>()
                .WithMany()
                .HasForeignKey(e => e.PlaPK)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Sala>(entity =>
        {
            entity.HasKey(e => e.SalaPK);

            entity.Property(e => e.SalaPK)
                .IsRequired()
                .HasMaxLength(50)
                .ValueGeneratedNever();

            entity.Property(e => e.Nom)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.TokenAcces)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(e => e.TokenAcces)
                .IsUnique();

            entity.HasIndex(e => new { e.Activa, e.DataExpiracio });

            entity.Property(e => e.UsuariCreadorPK)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(e => e.Usuari)
                .WithMany(u => u.SalesCreades)
                .HasForeignKey(e => e.UsuariCreadorPK)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalaUsuari>(entity =>
        {
            entity.HasKey(e => e.SalaUsuariPK);

            entity.Property(e => e.SalaUsuariPK)
                .IsRequired()
                .HasMaxLength(50)
                .ValueGeneratedNever();

            entity.Property(e => e.SalaPK)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.UsuariPK)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Rol)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(e => e.Sala)
                .WithMany(s => s.UsuarisSala)
                .HasForeignKey(e => e.SalaPK)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Usuari)
                .WithMany(u => u.SalesUsuari)
                .HasForeignKey(e => e.UsuariPK)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.SalaPK, e.UsuariPK })
                .IsUnique();
        });

        modelBuilder.Entity<Imatge>(entity =>
        {
            entity.HasKey(e => e.ImatgePK);

            entity.Property(e => e.ImatgePK)
                .IsRequired()
                .HasMaxLength(50)
                .ValueGeneratedNever();

            entity.Property(e => e.NomOriginal)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.RutaStorage)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.SourceUrl)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(e => e.TipusMime)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.UsuariPujadorPK)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(e => e.Usuari)
                .WithMany(u => u.ImatgesPujades)
                .HasForeignKey(e => e.UsuariPujadorPK)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalaImatge>(entity =>
        {
            entity.HasKey(e => e.ImatgeSalaPK);

            entity.Property(e => e.ImatgeSalaPK)
                .IsRequired()
                .HasMaxLength(50)
                .ValueGeneratedNever();

            entity.Property(e => e.SalaPK)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.ImatgePK)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(e => e.Sala)
                .WithMany(s => s.ImatgesSala)
                .HasForeignKey(e => e.SalaPK)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Imatge)
                .WithMany()
                .HasForeignKey(e => e.ImatgePK)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.SalaPK, e.ImatgePK })
                .IsUnique();
        });

        modelBuilder.Entity<SalaLinkCompartit>(entity =>
        {
            entity.HasKey(e => e.SalaLinkCompartitPK);

            entity.Property(e => e.SalaLinkCompartitPK)
                .IsRequired()
                .HasMaxLength(50)
                .ValueGeneratedNever();

            entity.Property(e => e.SalaPK)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Nom)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.Rol)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.UsuariCreadorPK)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.Token)
                .IsUnique();

            entity.HasOne(e => e.Sala)
                .WithMany(s => s.LinksCompartits)
                .HasForeignKey(e => e.SalaPK)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.UsuariCreador)
                .WithMany()
                .HasForeignKey(e => e.UsuariCreadorPK)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
