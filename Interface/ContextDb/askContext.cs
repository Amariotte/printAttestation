using ask.Model;
using Microsoft.EntityFrameworkCore;

namespace ask.ContextDb
{
    public class askContext : DbContext
    {
        public askContext(DbContextOptions<askContext> option) : base(option)
        {

        }

        public DbSet<t_user> t_user { get; set; }

        public DbSet<t_employe> t_employe { get; set; }
        public DbSet<t_demande> t_demande { get; set; }
        public DbSet<t_demande_ligne> t_demande_ligne { get; set; }
        public DbSet<t_histo_sms> t_histo_sms { get; set; }
        public DbSet<t_histo_email> t_histo_email { get; set; }
        public DbSet<t_modele> t_modele { get; set; }
        public DbSet<t_direction> t_direction { get; set; }
        public DbSet<t_fonction> t_fonction { get; set; }

        public DbSet<t_route_scope> t_route_scope { get; set; }
        public DbSet<t_entite> t_entite { get; set; }
        public DbSet<t_refresh_token> t_refresh_token { get; set; }
        public DbSet<t_session> t_session { get; set; }
        public DbSet<t_otp> t_otp { get; set; }
        public DbSet<t_role> t_role { get; set; }
        public DbSet<t_user_roles> t_user_roles { get; set; }
        public DbSet<t_role_scopes> t_role_scopes { get; set; }
        public DbSet<t_scope> t_scope { get; set; }


        public DbSet<t_parametre_systeme> t_parametre_systeme { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //modelBuilder.Entity<t_acces>()
            //.HasOne(u => u.r_t_client)
            //.WithOne(up => up.r_t_acces)
            //.HasForeignKey<t_client>(up => up.r_acces_id_fk);



            modelBuilder.Entity<t_demande_ligne>()
                .HasOne(dl => dl.r_demandeTab)
                .WithMany(dm => dm.r_t_demandeligne)
                .HasForeignKey(dl => dl.r_demande_FK);

            modelBuilder.Entity<t_refresh_token>()
                .HasOne(rt => rt.r_userTab)
                .WithMany()
                .HasForeignKey(rt => rt.r_user_id_fk);

            modelBuilder.Entity<t_session>()
                .HasOne(s => s.r_userTab)
                .WithMany()
                .HasForeignKey(s => s.r_user_id_fk);

            modelBuilder.Entity<t_user_roles>()
                .HasOne(ur => ur.r_userTab)
                .WithMany(u => u.r_user_roles)
                .HasForeignKey(ur => ur.r_user_id_fk);

            modelBuilder.Entity<t_user_roles>()
                .HasOne(ur => ur.r_roleTab)
                .WithMany(r => r.r_user_roles)
                .HasForeignKey(ur => ur.r_role_id_fk);

            modelBuilder.Entity<t_role_scopes>()
                .HasOne(rs => rs.r_roleTab)
                .WithMany(r => r.r_role_scopes)
                .HasForeignKey(rs => rs.r_role_id_fk);

            modelBuilder.Entity<t_role_scopes>()
                .HasOne(rs => rs.r_scopeTab)
                .WithMany()
                .HasForeignKey(rs => rs.r_scope_id_fk);

            //modelBuilder.Entity<t_alias>()
            //   .HasOne(dl => dl.t_client_tab)
            //   .WithMany(dm => dm.r_t_alias)
            //   .HasForeignKey(dl => dl.r_client_id_fk);
        }
    }
}
