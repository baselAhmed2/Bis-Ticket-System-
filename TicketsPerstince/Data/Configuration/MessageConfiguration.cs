using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsDomain.Models;

namespace TicketsPerstince.Data.Configuration

{
    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            // Primary Key
            builder.HasKey(m => m.Id);

            // Properties
            builder.Property(m => m.Body)
                .IsRequired();

            builder.Property(m => m.SentAt)
                .IsRequired();

            // Relations
            builder.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}