using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketsDomain.Models;
using TicketsPersistence.Configurations;
using TicketsPerstince.Data.Configuration;

namespace TicketsPerstince.Data.Configuration
{
    internal class DoctorSubjectConfiguration
  : IEntityTypeConfiguration<DoctorSubject>
        {
            public void Configure(EntityTypeBuilder<DoctorSubject> builder)
            {
                // Composite Primary Key
                builder.HasKey(ds => new { ds.DoctorId, ds.SubjectId });

                // Relations
                builder.HasOne(ds => ds.Doctor)
                    .WithMany(u => u.DoctorSubjects)
                    .HasForeignKey(ds => ds.DoctorId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasOne(ds => ds.Subject)
                    .WithMany(s => s.DoctorSubjects)
                    .HasForeignKey(ds => ds.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            }
        }
    }