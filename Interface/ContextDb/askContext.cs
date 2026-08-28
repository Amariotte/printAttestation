using ask.Model;
using Microsoft.EntityFrameworkCore;

namespace ask.ContextDb
{
    /// <summary>
    /// Contexte de base de données Entity Framework pour l'application ASK
    /// </summary>
    public class askContext : DbContext
    {
        public askContext(DbContextOptions<askContext> option) : base(option)
        {
        }

        // DbSets
        public DbSet<t_user> t_user { get; set; } = null!;
        public DbSet<t_histo_sms> t_histo_sms { get; set; } = null!;
        public DbSet<t_histo_email> t_histo_email { get; set; } = null!;
        public DbSet<t_modele> t_modele { get; set; } = null!;
        public DbSet<t_refresh_token> t_refresh_token { get; set; } = null!;
        public DbSet<t_session> t_session { get; set; } = null!;
        public DbSet<t_site> t_site { get; set; } = null!;
        public DbSet<t_trace_action> t_trace_action { get; set; } = null!;
        public DbSet<t_trace_connexion> t_trace_connexion { get; set; } = null!;
        public DbSet<t_job> t_job { get; set; } = null!;
        public DbSet<t_job_details> t_job_details { get; set; } = null!;
        public DbSet<t_demande_annulation> t_demande_annulation { get; set; } = null!;
        public DbSet<t_motif_annulation> t_motif_annulation { get; set; } = null!;
        public DbSet<t_role> t_role { get; set; } = null!;
        public DbSet<t_scope> t_scope { get; set; } = null!;

        public DbSet<t_user_role> t_user_role { get; set; } = null!;
        public DbSet<t_user_scope> t_user_scope { get; set; } = null!;
        public DbSet<t_role_scope> t_role_scope { get; set; } = null!;
      
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration des relations t_user
            modelBuilder.Entity<t_user>(entity =>
            {
                entity.HasMany(u => u.r_refresh_tokens)
                      .WithOne(rt => rt.r_user)
                      .HasForeignKey(rt => rt.r_user_id_fk)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.r_sessions)
                      .WithOne(s => s.r_user)
                      .HasForeignKey(s => s.r_user_id_fk)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.r_jobs)
                      .WithOne(j => j.r_user)
                      .HasForeignKey(j => j.r_user_id_fk)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(u => u.r_site)
                      .WithMany(s => s.r_users)
                      .HasForeignKey(u => u.r_site_id_fk)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuration t_refresh_token
            modelBuilder.Entity<t_refresh_token>(entity =>
            {
                // Index composite pour recherche de tokens actifs par utilisateur
                entity.HasIndex(rt => new { rt.r_user_id_fk, rt.r_is_revoked, rt.r_expires_at })
                      .HasDatabaseName("IX_RefreshToken_UserId_IsRevoked_ExpiresAt");

                // Index pour nettoyage des tokens expirés
                entity.HasIndex(rt => new { rt.r_expires_at, rt.r_is_delete })
                      .HasDatabaseName("IX_RefreshToken_ExpiresAt_IsDelete");
            });

            // Configuration t_session
            modelBuilder.Entity<t_session>(entity =>
            {
                // Index composite pour sessions actives par utilisateur
                entity.HasIndex(s => new { s.r_user_id_fk, s.r_is_active, s.r_login_at })
                      .HasDatabaseName("IX_Session_UserId_IsActive_LoginAt");
            });

         

            // Query filters globaux pour soft delete
            modelBuilder.Entity<t_user>().HasQueryFilter(e => !e.r_is_delete);
            modelBuilder.Entity<t_refresh_token>().HasQueryFilter(e => !e.r_is_delete);
            modelBuilder.Entity<t_job>().HasQueryFilter(e => !e.r_is_delete);
            modelBuilder.Entity<t_session>().HasQueryFilter(e => !e.r_is_delete);
            modelBuilder.Entity<t_histo_sms>().HasQueryFilter(e => !e.r_is_delete);
            modelBuilder.Entity<t_histo_email>().HasQueryFilter(e => !e.r_is_delete);
            modelBuilder.Entity<t_site>().HasQueryFilter(e => !e.r_is_delete);
            modelBuilder.Entity<t_job_details>().HasQueryFilter(e => !e.r_is_delete);
            modelBuilder.Entity<t_demande_annulation>().HasQueryFilter(e => !e.r_is_delete);
            modelBuilder.Entity<t_motif_annulation>().HasQueryFilter(e => !e.r_is_delete);

            // Configuration des tables de traçabilité
            modelBuilder.Entity<t_trace_action>(entity =>
            {
                // Index pour recherche par utilisateur
                entity.HasIndex(e => e.r_user_id);

                // Index pour recherche par type d'action
                entity.HasIndex(e => e.r_type_action);

                // Index pour recherche par date
                entity.HasIndex(e => e.r_created_at);

                // Index composite pour recherche par utilisateur et date
                entity.HasIndex(e => new { e.r_user_id, e.r_created_at });

            
            });

            modelBuilder.Entity<t_trace_connexion>(entity =>
            {
                // Index pour recherche par utilisateur
                entity.HasIndex(e => e.r_user_id);

                // Index pour recherche par email
                entity.HasIndex(e => e.r_email);

                // Index pour recherche par IP
                entity.HasIndex(e => e.r_ip_address);

                // Index pour recherche par date
                entity.HasIndex(e => e.r_created_at);

                // Index composite pour recherche par utilisateur et date
                entity.HasIndex(e => new { e.r_user_id, e.r_created_at });

                // Index pour recherche échecs
                entity.HasIndex(e => new { e.r_succes, e.r_created_at });
            });

            modelBuilder.Entity<t_job>(entity =>
            {
                entity.HasMany(u => u.r_job_details)
                    .WithOne(j => j.r_job)
                    .HasForeignKey(j => j.r_job_id_fk)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.r_job_id);
                entity.HasIndex(e => e.r_user_id_fk);
                entity.HasIndex(e => e.r_created_at);
            });

            modelBuilder.Entity<t_job_details>(entity =>
            {
                entity.HasIndex(e => e.r_job_id_fk);
                entity.HasIndex(e => e.r_created_at);
            });

            modelBuilder.Entity<t_motif_annulation>(entity =>
            {
                entity.HasIndex(e => e.r_libelle);
            });

            modelBuilder.Entity<t_demande_annulation>(entity =>
            {
                entity.HasIndex(e => e.r_user_id_fk);
                entity.HasIndex(e => e.r_site_id_fk);
                entity.HasIndex(e => e.r_motif_annulation_id_fk);
                entity.HasIndex(e => e.r_status);
                entity.HasIndex(e => e.r_created_at);

                entity.HasOne(e => e.r_user)
                    .WithMany()
                    .HasForeignKey(e => e.r_user_id_fk)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.r_site)
                    .WithMany()
                    .HasForeignKey(e => e.r_site_id_fk)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.r_motif_annulation)
                    .WithMany(m => m.r_users)
                    .HasForeignKey(e => e.r_motif_annulation_id_fk)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        /// <summary>
        /// Override SaveChanges pour mettre à jour automatiquement r_updated_at
        /// </summary>
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        /// <summary>
        /// Override SaveChangesAsync pour mettre à jour automatiquement r_updated_at
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Met à jour automatiquement les timestamps lors de la modification
        /// </summary>
        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is t_base && (e.State == EntityState.Modified || e.State == EntityState.Added));

            foreach (var entry in entries)
            {
                if (entry.Entity is t_base entity)
                {
                    // Convertir tous les DateTime en UTC
                    ConvertDateTimesToUtc(entry);

                    if (entry.State == EntityState.Modified)
                    {
                        entity.r_updated_at = DateTime.UtcNow;
                    }
                }
            }
        }

        /// <summary>
        /// Convertit tous les DateTime d'une entité en UTC pour PostgreSQL
        /// </summary>
        private void ConvertDateTimesToUtc(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            foreach (var property in entry.Properties)
            {
                if (property.Metadata.ClrType == typeof(DateTime) || property.Metadata.ClrType == typeof(DateTime?))
                {
                    if (property.CurrentValue != null && property.CurrentValue is DateTime dateTime)
                    {
                        if (dateTime.Kind == DateTimeKind.Local)
                        {
                            property.CurrentValue = dateTime.ToUniversalTime();
                        }
                        else if (dateTime.Kind == DateTimeKind.Unspecified)
                        {
                            property.CurrentValue = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                        }
                    }
                }
            }
        }
    }
}
