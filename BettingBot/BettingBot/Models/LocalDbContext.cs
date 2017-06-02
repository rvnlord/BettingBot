using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using BettingBot.Models;

namespace BettingBot.Models
{
    public class LocalDbContext : DbContext
    {
        public LocalDbContext() : base("name=DBCS")
        {
            Database.SetInitializer<LocalDbContext>(null);
        }

        public virtual DbSet<Bet> Bets { get; set; }
        public virtual DbSet<Option> Options { get; set; }
        public virtual DbSet<Tipster> Tipsters { get; set; }
        public virtual DbSet<User> Logins { get; set; }
        public virtual DbSet<Pick> Picks { get; set; }
        public virtual DbSet<Website> Websites { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Bets

            modelBuilder.Entity<Bet>()
                .HasKey(e => e.Id)
                .ToTable("tblBets");

            modelBuilder.Entity<Bet>()
                .Property(e => e.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            
            modelBuilder.Entity<Bet>()
                .Property(e => e.Date)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("UQ_Bets", 1)));

            modelBuilder.Entity<Bet>()
                .Property(e => e.Match)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("UQ_Bets", 2)));

            modelBuilder.Entity<Bet>()
                .Property(e => e.TipsterId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("UQ_Bets", 3)));

            // Tipsters

            modelBuilder.Entity<Tipster>()
                .HasKey(e => e.Id)
                .ToTable("tblTipsters");

            modelBuilder.Entity<Tipster>()
                .Property(e => e.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<Tipster>()
                .HasMany(e => e.Bets)
                .WithRequired(e => e.Tipster)
                .HasForeignKey(e => e.TipsterId);

            // Picks

            modelBuilder.Entity<Pick>()
                .HasKey(e => e.Id)
                .ToTable("tblPicks");

            modelBuilder.Entity<Pick>()
                .Property(e => e.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<Pick>()
                .HasMany(e => e.Bets)
                .WithRequired(e => e.Pick)
                .HasForeignKey(e => e.PickId);
            
            // Logins

            modelBuilder.Entity<User>()
                .HasKey(e => e.Id)
                .ToTable("tblLogins");

            modelBuilder.Entity<User>()
                .Property(e => e.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<User>()
                .HasMany(e => e.Websites)
                .WithOptional(e => e.Login)
                .HasForeignKey(e => e.LoginId);

            modelBuilder.Entity<User>()
                .Property(e => e.Name)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("UQ_Logins", 1)));

            modelBuilder.Entity<User>()
                .Property(e => e.Password)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("UQ_Logins", 2)));

            // Websites

            modelBuilder.Entity<Website>()
                .HasKey(e => e.Id)
                .ToTable("tblWebsites");

            modelBuilder.Entity<Website>()
                .Property(e => e.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<Website>()
                .HasMany(e => e.Tipsters)
                .WithOptional(e => e.Website)
                .HasForeignKey(e => e.WebsiteId);

            // Options

            modelBuilder.Entity<Option>()
                .HasKey(e => e.Key)
                .ToTable("tblOptions");
        }
    }
}
