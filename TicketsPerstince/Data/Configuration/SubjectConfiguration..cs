using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketsDomain.Models;

namespace TicketsPerstince.Data.Configuration
{
    public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
    {
        public void Configure(EntityTypeBuilder<Subject> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                .HasMaxLength(20);    // "BIS123" / "HU12"

            // Properties
            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.Level)
                .IsRequired();
        }
    }
}
