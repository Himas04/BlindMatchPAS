using BlindMatchPAS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<ResearchArea> ResearchAreas { get; set; }
        public DbSet<ProjectProposal> ProjectProposals { get; set; }
        public DbSet<SupervisorExpertise> SupervisorExpertises { get; set; }
        public DbSet<Match> Matches { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ProjectProposal → Student (restrict delete)
            builder.Entity<ProjectProposal>()
                .HasOne(p => p.Student)
                .WithMany()
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Match → Supervisor (restrict delete)
            builder.Entity<Match>()
                .HasOne(m => m.Supervisor)
                .WithMany()
                .HasForeignKey(m => m.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Match → ProjectProposal (cascade)
            builder.Entity<Match>()
                .HasOne(m => m.ProjectProposal)
                .WithOne(p => p.Match)
                .HasForeignKey<Match>(m => m.ProjectProposalId)
                .OnDelete(DeleteBehavior.Cascade);

            // SupervisorExpertise → Supervisor (restrict delete)
            builder.Entity<SupervisorExpertise>()
                .HasOne(se => se.Supervisor)
                .WithMany()
                .HasForeignKey(se => se.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed research areas
            builder.Entity<ResearchArea>().HasData(
                new ResearchArea { Id = 1, Name = "Artificial Intelligence", Description = "Machine learning, deep learning, NLP", IsActive = true },
                new ResearchArea { Id = 2, Name = "Web Development", Description = "Frontend, backend, full-stack", IsActive = true },
                new ResearchArea { Id = 3, Name = "Cybersecurity", Description = "Network security, cryptography, ethical hacking", IsActive = true },
                new ResearchArea { Id = 4, Name = "Cloud Computing", Description = "AWS, Azure, GCP, distributed systems", IsActive = true },
                new ResearchArea { Id = 5, Name = "Mobile Development", Description = "iOS, Android, cross-platform", IsActive = true },
                new ResearchArea { Id = 6, Name = "Data Science", Description = "Data analysis, visualization, big data", IsActive = true }
            );
        }
    }
}
