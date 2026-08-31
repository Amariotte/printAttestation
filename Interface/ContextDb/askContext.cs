using print_attestation.Model;
using Microsoft.EntityFrameworkCore;
using print_attestation.Security;

namespace print_attestation.ContextDb
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




            modelBuilder.Entity<t_scope>().HasData(
                

                // Attestations
                new t_scope
                {
                    r_code = Scopes.AttestationsRead,
                    r_nom = "Lecture des attestations",
                    r_description = "Permet de consulter les attestations",
               
                },

                 new t_scope
                 {
                     r_code = Scopes.AttestationsReadAll,
                     r_nom = "Lecture des attestations de tous les intermediaires",
                     r_description = "Permet de consulter les attestations de tous les intermediaires",
              
                 },


                // demandes d'annulation
                new t_scope
                {
                    r_code = Scopes.DemandesAnnulationsRead,
                    r_nom = "Lecture des demandes d'annulation",
                    r_description = "Permet de consulter les demandes d'annulation",
               
                },

                 new t_scope
                 {
                     r_code = Scopes.DemandesAnnulationsReadSite,
                     r_nom = "Lecture des demandes d'annulation de mon intérmediaire",
                     r_description = "Permet de consulter les demandes d'annulation de tous les intermediaires",
                    
                 },

                  new t_scope
                  {
                      r_code = Scopes.DemandesAnnulationsReadAll,
                      r_nom = "Lecture des demandes d'annulation de tous les intermediaires",
                      r_description = "Permet de consulter les demandes d'annulation de tous les intermediaires",
                 
                  },

                  // Taches
                  new t_scope
                 {
                      r_code = Scopes.TachesRead,
                      r_nom = "Lecture des taches",
                      r_description = "Permet de consulter les taches",
                 
                 },
                   new t_scope
                   {
                       r_code = Scopes.TachesReadSite,
                       r_nom = "Lecture des taches de mon site",
                       r_description = "Permet de consulter les taches de mon site",
                  
                   },
                     new t_scope
                     {
                         r_code = Scopes.TachesReadAll,
                         r_nom = "Lecture des taches de tous les utilisateurs",
                         r_description = "Permet de consulter les taches de tous les utilisateurs",
                
                     },
                new t_scope
                {
                    r_code = Scopes.TachesUpdate,
                    r_nom = "Annulation des taches",
                    r_description = "Permet d'annuler les taches",
                 
                },
                new t_scope
                {
                    r_code = Scopes.TachesUpdateSite,
                    r_nom = "Annulation des taches de mon site",
                    r_description = "Permet d'annuler les taches de mon site",
               
                },
                new t_scope
                {
                    r_code = Scopes.TachesUpdateAll,
                    r_nom = "Annulation des taches de tous les utilisateurs",
                    r_description = "Permet d'annuler les taches de tous les utilisateurs",
               
                },
                
                // Audits
                new t_scope
                {
                    r_code = Scopes.AuditsActionsRead,
                    r_nom = "Lecture des audits actions",
                    r_description = "Permet de consulter les audits liés aux actions",
             
                },
                 new t_scope
                 {
                     r_code = Scopes.AuditsActionsReadSite,
                     r_nom = "Lecture des audits actions de mon site",
                     r_description = "Permet de consulter les audits liés aux actions de mon site",
                  
                 },
                    new t_scope
                    {
                        r_code = Scopes.AuditsActionsReadAll,
                        r_nom = "Lecture des audits actions de tous les utilisateurs",
                        r_description = "Permet de consulter les audits liés aux actions de tous les utilisateurs",
                    
                    },
                new t_scope
                {
                    r_code = Scopes.AuditsAccesRead,
                    r_nom = "Lecture des audits connexions",
                    r_description = "Permet de consulter les audits liés aux connexions",
                 
                },
                 new t_scope
                 {
                     r_code = Scopes.AuditsAccesReadSite,
                     r_nom = "Lecture des audits connexions de mon site",
                     r_description = "Permet de consulter les audits liés aux connexions de mon site",
                  
                 },
                 new t_scope
                 {
                     r_code = Scopes.AuditsAccesReadAll,
                     r_nom = "Lecture des audits connexions de tous les utilisateurs",
                     r_description = "Permet de consulter les audits liés aux connexions de tous les utilisateurs",
                
                 },
                new t_scope
                {
                    r_code = Scopes.UsersRead,
                    r_nom = "Lecture des utilisateurs",
                    r_description = "Permet de consulter les utilisateurs",
                
                },
                new t_scope
                {
                    r_code = Scopes.SitesCreate,
                    r_nom = "Création des intermédiaires",
                    r_description = "Permet de créer des intermédiaires",
                

                },
                new t_scope
                {
                    r_code = Scopes.SitesUpdate,
                    r_nom = "Modification des intermédiaires",
                    r_description = "Permet de modifier les intermédiaires",
               
                },
                new t_scope
                {
                    r_code = Scopes.SitesDelete,
                    r_nom = "Suppression des intermédiaires",
                    r_description = "Permet de supprimer les intermédiaires",
              
                },
                new t_scope
                {
                    r_code = Scopes.SitesRead,
                    r_nom = "Lecture des intermédiaires",
                    r_description = "Permet de consulter les intermédiaires",

                },
                 new t_scope
                 {
                     r_code = Scopes.SitesUpload,
                     r_nom = "Téléversement des intermédiaires",
                     r_description = "Permet de téléverser les intermédiaires",

                 },
                new t_scope
                {
                    r_code = Scopes.UsersCreate,
                    r_nom = "Création des utilisateurs",
                    r_description = "Permet de créer des utilisateurs",


                },
                new t_scope
                {
                    r_code = Scopes.UsersUpdate,
                    r_nom = "Modification des utilisateurs",
                    r_description = "Permet de modifier les utilisateurs",

                },
                new t_scope
                {
                    r_code = Scopes.UsersDelete,
                    r_nom = "Suppression des utilisateurs",
                    r_description = "Permet de supprimer les utilisateurs",

                }
                ,

                new t_scope
                {
                    r_code = Scopes.RolesRead,
                    r_nom = "Lecture des profils d'utilisateurs",
                    r_description = "Permet de consulter les profils d'utilisateurs",

                },
                new t_scope
                {
                    r_code = Scopes.RolesCreate,
                    r_nom = "Création des profils d'utilisateurs",
                    r_description = "Permet de créer les profils d'utilisateurs",
                },
                new t_scope
                {
                    r_code = Scopes.RolesUpdate,
                    r_nom = "Modification des profils d'utilisateurs",
                    r_description = "Permet de modifier les profils d'utilisateurs",
                },
                new t_scope
                {
                    r_code = Scopes.RolesDelete,
                    r_nom = "Suppression des profils d'utilisateurs",
                    r_description = "Permet de supprimer les profils d'utilisateurs",
                },

                new t_scope
                {
                    r_code = Scopes.MotifAnnulationRead,
                    r_nom = "Lecture des motifs d'annulation",
                    r_description = "Permet de consulter les motifs d'annulation",

                },
                new t_scope
                {
                    r_code = Scopes.MotifAnnulationCreate,
                    r_nom = "Création des motifs d'annulation",
                    r_description = "Permet de créer les motifs d'annulation",
                },
                new t_scope
                {
                    r_code = Scopes.MotifAnnulationUpdate,
                    r_nom = "Modification des motifs d'annulation",
                    r_description = "Permet de modifier les motifs d'annulation",
                },
                new t_scope
                {
                    r_code = Scopes.MotifAnnulationDelete,
                    r_nom = "Suppression des motifs d'annulation",
                    r_description = "Permet de supprimer les motifs d'annulation",
                }

            );


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
