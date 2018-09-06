using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using BettingBot.Source.DbContext.Models;

namespace BettingBot.Source.DbContext
{
    public class LocalDbContext : System.Data.Entity.DbContext
    {
        public LocalDbContext() : base("name=DBCS")
        {
            Database.SetInitializer<LocalDbContext>(null);
        }

        public virtual DbSet<DbBet> Bets { get; set; }
        public virtual DbSet<DbOption> Options { get; set; }
        public virtual DbSet<DbTipster> Tipsters { get; set; }
        public virtual DbSet<DbLogin> Logins { get; set; }
        public virtual DbSet<DbPick> Picks { get; set; }
        public virtual DbSet<DbWebsite> Websites { get; set; }
        public virtual DbSet<DbMatch> Matches { get; set; }
        public virtual DbSet<DbTeam> Teams { get; set; }
        public virtual DbSet<DbTeamAlternateName> TeamAlternateNames { get; set; }
        public virtual DbSet<DbLeague> Leagues { get; set; }
        public virtual DbSet<DbLeagueAlternateName> LeagueAlternateNames { get; set; }
        public virtual DbSet<DbDiscipline> Disciplines { get; set; }
        public virtual DbSet<DbLocalizedString> LocalizedStrings { get; set; }
        
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Bets

            modelBuilder.Entity<DbBet>()
                .HasKey(e => e.Id)
                .ToTable("tblBets");

            modelBuilder.Entity<DbBet>()
                .Property(e => e.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<DbBet>()
                .Property(e => e.OriginalHomeName)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("uq_bets", 1)));
            modelBuilder.Entity<DbBet>()
                .Property(e => e.OriginalAwayName)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("uq_bets", 2)));
            modelBuilder.Entity<DbBet>()
                .Property(e => e.OriginalDate)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("uq_bets", 3)));
            modelBuilder.Entity<DbBet>()
                .Property(e => e.TipsterId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("uq_bets", 4)));
            modelBuilder.Entity<DbBet>()
                .Property(e => e.OriginalPickString)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("uq_bets", 5)));
            modelBuilder.Entity<DbBet>()
                .Property(e => e.OriginalDiscipline)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("uq_bets", 6)));

            modelBuilder.Entity<DbBet>()
                .HasRequired(e => e.Tipster)
                .WithMany(e => e.Bets)
                .HasForeignKey(e => e.TipsterId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DbBet>()
                .HasRequired(e => e.Pick)
                .WithMany(e => e.Bets)
                .HasForeignKey(e => e.PickId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DbBet>()
                .HasOptional(e => e.Match)
                .WithMany(e => e.Bets)
                .HasForeignKey(e => e.MatchId)
                .WillCascadeOnDelete(false);

            // Match

            modelBuilder.Entity<DbMatch>()
                .HasKey(e => e.Id)
                .ToTable("tblMatches");

            modelBuilder.Entity<DbMatch>()
                .Property(e => e.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<DbMatch>()
                .Property(e => e.Date)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("uq_matches", 1)));
            modelBuilder.Entity<DbMatch>()
                .Property(e => e.HomeId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("uq_matches", 2)));
            modelBuilder.Entity<DbMatch>()
                .Property(e => e.AwayId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("uq_matches", 3)));
            modelBuilder.Entity<DbMatch>()
                .Property(e => e.LeagueId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("uq_matches", 4)));

            modelBuilder.Entity<DbMatch>()
                .HasRequired(e => e.Home)
                .WithMany(e => e.HomeMatches)
                .HasForeignKey(e => e.HomeId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DbMatch>()
                .HasRequired(e => e.Away)
                .WithMany(e => e.AwayMatches)
                .HasForeignKey(e => e.AwayId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DbMatch>()
                .HasRequired(e => e.League)
                .WithMany(e => e.Matches)
                .HasForeignKey(e => e.LeagueId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DbMatch>()
                .HasMany(e => e.Bets)
                .WithOptional(e => e.Match)
                .HasForeignKey(e => e.MatchId)
                .WillCascadeOnDelete(false);

            // Teams

            modelBuilder.Entity<DbTeam>()
                .HasKey(e => e.Id)
                .ToTable("tblTeams");

            modelBuilder.Entity<DbTeam>()
                .Property(e => e.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<DbTeam>()
                .Property(e => e.Name)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("uq_teams", 1)));

            modelBuilder.Entity<DbTeam>()
                .HasMany(e => e.HomeMatches)
                .WithRequired(e => e.Home)
                .HasForeignKey(e => e.HomeId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DbTeam>()
                .HasMany(e => e.AwayMatches)
                .WithRequired(e => e.Away)
                .HasForeignKey(e => e.AwayId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DbTeam>()
                .HasMany(e => e.TeamAlternateNames)
                .WithRequired(e => e.Team)
                .HasForeignKey(e => e.TeamId)
                .WillCascadeOnDelete(false);

            // TeamAlternateNames

            modelBuilder.Entity<DbTeamAlternateName>()
                .HasKey(e => e.AlternateName)
                .ToTable("tblTeamAlternateNames");

            modelBuilder.Entity<DbTeamAlternateName>()
                .Property(e => e.AlternateName)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("uq_teamsalternatenames_alternatenameteamid", 0)));
            modelBuilder.Entity<DbTeamAlternateName>()
                .Property(e => e.TeamId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("uq_teamsalternatenames_alternatenameteamid", 1)));

            modelBuilder.Entity<DbTeamAlternateName>()
                .Property(e => e.AlternateName)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<DbTeamAlternateName>()
                .HasRequired(e => e.Team)
                .WithMany(e => e.TeamAlternateNames)
                .HasForeignKey(e => e.TeamId)
                .WillCascadeOnDelete(false);

            // Leagues

            modelBuilder.Entity<DbLeague>()
                .HasKey(e => e.Id)
                .ToTable("tblLeagues");

            modelBuilder.Entity<DbLeague>()
                .Property(e => e.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<DbLeague>()
                .Property(e => e.Name)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("uq_leagues", 1)));
            modelBuilder.Entity<DbLeague>()
                .Property(e => e.Season)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("uq_leagues", 2)));
            modelBuilder.Entity<DbLeague>()
                .Property(e => e.DisciplineId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("uq_leagues", 3)));

            modelBuilder.Entity<DbLeague>()
                .HasOptional(e => e.Discipline)
                .WithMany(e => e.Leagues)
                .HasForeignKey(e => e.DisciplineId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DbLeague>()
                .HasMany(e => e.LeagueAlternateNames)
                .WithRequired(e => e.League)
                .HasForeignKey(e => e.LeagueId)
                .WillCascadeOnDelete(false);

            // LeagueAlternateNames

            modelBuilder.Entity<DbLeagueAlternateName>()
                .HasKey(e => new { e.AlternateName, e.LeagueId })
                .ToTable("tblLeagueAlternateNames");

            modelBuilder.Entity<DbLeagueAlternateName>()
                .Property(e => e.AlternateName)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<DbLeagueAlternateName>()
                .HasRequired(e => e.League)
                .WithMany(e => e.LeagueAlternateNames)
                .HasForeignKey(e => e.LeagueId)
                .WillCascadeOnDelete(false);

            // Disciplines

            modelBuilder.Entity<DbDiscipline>()
                .HasKey(e => e.Id)
                .ToTable("tblDisciplines");

            modelBuilder.Entity<DbDiscipline>()
                .Property(e => e.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<DbDiscipline>()
                .HasMany(e => e.Leagues)
                .WithOptional(e => e.Discipline)
                .HasForeignKey(e => e.DisciplineId)
                .WillCascadeOnDelete(false);

            // Tipsters

            modelBuilder.Entity<DbTipster>()
                .HasKey(e => e.Id)
                .ToTable("tblTipsters");

            modelBuilder.Entity<DbTipster>()
                .Property(e => e.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<DbTipster>()
                .HasMany(e => e.Bets)
                .WithRequired(e => e.Tipster)
                .HasForeignKey(e => e.TipsterId)
                .WillCascadeOnDelete(false);

            // Picks

            modelBuilder.Entity<DbPick>()
                .HasKey(e => e.Id)
                .ToTable("tblPicks");

            modelBuilder.Entity<DbPick>()
                .Property(e => e.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<DbPick>()
                .HasMany(e => e.Bets)
                .WithRequired(e => e.Pick)
                .HasForeignKey(e => e.PickId)
                .WillCascadeOnDelete(false);
            
            // Logins

            modelBuilder.Entity<DbLogin>()
                .HasKey(e => e.Id)
                .ToTable("tblLogins");

            modelBuilder.Entity<DbLogin>()
                .Property(e => e.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<DbLogin>()
                .HasMany(e => e.Websites)
                .WithOptional(e => e.Login)
                .HasForeignKey(e => e.LoginId);

            modelBuilder.Entity<DbLogin>()
                .Property(e => e.Name)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("UQ_Logins", 1)));
            modelBuilder.Entity<DbLogin>()
                .Property(e => e.Password)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("UQ_Logins", 2)));

            // Websites

            modelBuilder.Entity<DbWebsite>()
                .HasKey(e => e.Id)
                .ToTable("tblWebsites");

            modelBuilder.Entity<DbWebsite>()
                .Property(e => e.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<DbWebsite>()
                .HasMany(e => e.Tipsters)
                .WithOptional(e => e.Website)
                .HasForeignKey(e => e.WebsiteId)
                .WillCascadeOnDelete(false);

            // Options

            modelBuilder.Entity<DbOption>()
                .HasKey(e => e.Key)
                .ToTable("tblOptions");

            // Options

            modelBuilder.Entity<DbLocalizedString>()
                .HasKey(e => e.Key)
                .ToTable("tblLocalizedStrings");
        }
    }
}
