using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsDomain.Models;

namespace TicketsPersistence.Configurations
{
    public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            // Primary Key
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .HasMaxLength(50)
                .ValueGeneratedNever();

            // Properties
            builder.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(t => t.Body)
                .IsRequired();

            builder.Property(t => t.GroupNumber)
                .IsRequired();

            builder.Property(t => t.Level)
                .IsRequired();

            builder.Property(t => t.Term)
                .IsRequired();

            builder.Property(t => t.Status)
                .IsRequired()
                .HasDefaultValue(TicketsShared.Enums.TicketStatus.New);

            builder.Property(t => t.CreatedAt)
                .IsRequired();

            // Relations
            builder.HasOne(t => t.Student)
                .WithMany()
                .HasForeignKey(t => t.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Doctor)
                .WithMany()
                .HasForeignKey(t => t.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Subject)
                .WithMany(s => s.Tickets)
                .HasForeignKey(t => t.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(t => t.Messages)
                .WithOne(m => m.Ticket)
                .HasForeignKey(m => m.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}