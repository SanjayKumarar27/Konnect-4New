using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Konnect_4New.Models;

public partial class Konnect4Context : DbContext
{
    public Konnect4Context()
    {
    }

    public Konnect4Context(DbContextOptions<Konnect4Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<Follower> Followers { get; set; }

    public virtual DbSet<Like> Likes { get; set; }

    public virtual DbSet<Post> Posts { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Conversation> Conversations { get; set; }

    public virtual DbSet<ConversationParticipant> ConversationParticipants { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<MessageReadStatus> MessageReadStatus { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:mycon");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.ProfileImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsPrivate).HasDefaultValue(false);
            entity.Property(e => e.Role).HasMaxLength(20).HasDefaultValue("User");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<Follower>(entity =>
        {
            entity.HasKey(e => e.FollowerId);

            entity.HasIndex(e => new { e.UserId, e.FollowerUserId }, "UQ_Follow").IsUnique();

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(d => d.User)
                .WithMany(p => p.FollowerUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.FollowerUser)
                .WithMany(p => p.FollowerFollowerUsers)
                .HasForeignKey(d => d.FollowerUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.PostId);

            entity.Property(e => e.Content);
            entity.Property(e => e.PostImageUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Posts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.CommentId);

            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(d => d.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.User)
                .WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(d => d.ParentComment)
                .WithMany(p => p.InverseParentComment)
                .HasForeignKey(d => d.ParentCommentId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Like>(entity =>
        {
            entity.HasKey(e => e.LikeId);

            entity.HasIndex(e => new { e.PostId, e.UserId }, "UQ_Like").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(d => d.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.User)
                .WithMany(p => p.Likes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId);

            entity.Property(e => e.LastMessageAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.HasKey(e => e.ParticipantId);

            entity.HasIndex(e => new { e.ConversationId, e.UserId }, "UQ_ConversationUser").IsUnique();

            entity.Property(e => e.JoinedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(d => d.Conversation)
                .WithMany(p => p.ConversationParticipants)
                .HasForeignKey(d => d.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.User)
                .WithMany(p => p.ConversationParticipants)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId);

            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.MessageType)
                .HasMaxLength(20)
                .HasDefaultValue("Text");
            entity.Property(e => e.FileUrl).HasMaxLength(500);
            entity.Property(e => e.SentAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.IsEdited).HasDefaultValue(false);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasOne(d => d.Conversation)
                .WithMany(p => p.Messages)
                .HasForeignKey(d => d.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Sender)
                .WithMany(p => p.MessagesSent)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MessageReadStatus>(entity =>
        {
            entity.HasKey(e => e.ReadStatusId);

            entity.HasIndex(e => new { e.MessageId, e.UserId }, "UQ_MessageRead").IsUnique();

            entity.Property(e => e.ReadAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(d => d.Message)
                .WithMany(p => p.MessageReadStatuses)
                .HasForeignKey(d => d.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.User)
                .WithMany(p => p.MessageReadStatuses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        OnModelCreatingPartial(modelBuilder);
    }
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}