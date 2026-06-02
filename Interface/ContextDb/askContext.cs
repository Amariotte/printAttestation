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
        public DbSet<t_otp> t_otp { get; set; } = null!;
        public DbSet<t_scope> t_scope { get; set; } = null!;
        public DbSet<t_user_scopes> t_user_scopes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration des relations t_user
            modelBuilder.Entity<t_user>(entity =>
            {
                entity.HasMany(u => u.r_refresh_tokens)
                      .WithOne(rt => rt.r_userTab)
                      .HasForeignKey(rt => rt.r_user_id_fk)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.r_sessions)
                      .WithOne(s => s.r_userTab)
                      .HasForeignKey(s => s.r_user_id_fk)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.r_user_scopes)
                      .WithOne()
                      .OnDelete(DeleteBehavior.Cascade);
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

            // Configuration t_otp
            modelBuilder.Entity<t_otp>(entity =>
            {
                entity.HasOne(o => o.r_userTab)
                      .WithMany()
                      .HasForeignKey(o => o.r_user_id_fk)
                      .OnDelete(DeleteBehavior.SetNull);

                // Index composite pour recherche OTP valides
                entity.HasIndex(o => new { o.r_challenge_id, o.r_type, o.r_created_at })
                      .HasDatabaseName("IX_OTP_ChallengeId_Type_CreatedAt");
            });

            // Configuration t_scope
            modelBuilder.Entity<t_scope>(entity =>
            {
                entity.HasMany(s => s.r_user_scopes)
                      .WithOne()
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuration t_user_scopes (table de jointure)
            modelBuilder.Entity<t_user_scopes>(entity =>
            {
                // Index composite pour recherche de scopes par utilisateur
                entity.HasIndex(us => new { us.r_user_id_fk, us.r_scope_id_fk })
                      .IsUnique()
                      .HasDatabaseName("IX_UserScopes_UserId_ScopeId");
            });

            // Query filters globaux pour soft delete
            modelBuilder.Entity<t_user>().HasQueryFilter(e => !e.r_is_delete);
            modelBuilder.Entity<t_refresh_token>().HasQueryFilter(e => !e.r_is_delete);
            modelBuilder.Entity<t_session>().HasQueryFilter(e => !e.r_is_delete);
            modelBuilder.Entity<t_otp>().HasQueryFilter(e => !e.r_is_delete);
            modelBuilder.Entity<t_scope>().HasQueryFilter(e => !e.r_is_delete);
            modelBuilder.Entity<t_user_scopes>().HasQueryFilter(e => !e.r_is_delete);
            modelBuilder.Entity<t_histo_sms>().HasQueryFilter(e => !e.r_is_delete);
            modelBuilder.Entity<t_histo_email>().HasQueryFilter(e => !e.r_is_delete);
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
                .Where(e => e.Entity is t_base && (e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                if (entry.Entity is t_base entity)
                {
                    entity.r_updated_at = DateTime.UtcNow;
                }
            }
        }
    }
}
