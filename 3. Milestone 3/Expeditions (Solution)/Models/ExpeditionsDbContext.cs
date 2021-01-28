using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Expeditions.Models
{
    public partial class ExpeditionsDbContext : DbContext
    {
        public ExpeditionsDbContext()
        {
        }

        public ExpeditionsDbContext(DbContextOptions<ExpeditionsDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Expedition> Expeditions { get; set; }
        public virtual DbSet<NewsArticle> NewsArticles { get; set; }
        public virtual DbSet<Peak> Peaks { get; set; }
        public virtual DbSet<TrekkingAgency> TrekkingAgencies { get; set; }
        public virtual DbSet<TeamMember> TeamMembers { get; set; }
        public virtual DbSet<Nation> Nations { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Name=ExpeditionConnectionAzure");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Expedition>(entity =>
            {
                entity.HasOne(d => d.Peak)
                    .WithMany(p => p.Expeditions)
                    .HasForeignKey(d => d.PeakId)
                    .HasConstraintName("Expedition_FK_Peak");

                entity.HasOne(d => d.TrekkingAgency)
                    .WithMany(p => p.Expeditions)
                    .HasForeignKey(d => d.TrekkingAgencyId)
                    .HasConstraintName("Expedition_FK_TrekkingAgency");
            });


            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
