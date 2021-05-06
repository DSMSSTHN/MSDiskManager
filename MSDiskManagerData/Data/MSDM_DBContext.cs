using Microsoft.EntityFrameworkCore;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Entities.Relations;
using Nito.AsyncEx;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManagerData.Data
{
    public class MSDM_DBContext : DbContext
    {
        internal static int ActiveConnections = 0;
        internal static PauseTokenSource PauseSource = new PauseTokenSource();
        public static string DriverName = "D:" + '\\';
        public DbSet<FileEntity> Files { get; set; }
        public DbSet<IgnoredFile> ignoredFiles { get; set; }
        public DbSet<DirectoryEntity> Directories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<FileTag> FileTags { get; set; }
        public DbSet<DirectoryTag> DirectoryTags { get; set; }
        public DbSet<ImageThumbnail> Thumbnails { get; set; }
        private static bool IsTest { get; set; }
        private static string connectionString = "host=localhost;port=5432;database=msdmdb_test;username=smsthn;password=19131920814;timeout=0;";

        public MSDM_DBContext(bool isTest = false)
        {
            if (IsTest != isTest)
            {
                IsTest = isTest;
                connectionString = !IsTest ? "host=localhost;port=5432;database=msdmdb_test;username=smsthn;password=19131920814;timeout=0;"
                    : "host=localhost;port=5432;database=msdmdb;username=smsthn;password=19131920814;timeout=0;";
            }
        }
        public static ConnectionState ConnectionState
        {
            get
            {
                ConnectionState state = ConnectionState.Broken;
                NpgsqlConnection conn = new NpgsqlConnection(connectionString);
                try
                {
                    conn.Open();
                    state = conn.State;
                    conn.Close();
                }
                catch (Exception e)
                {
                    state = ConnectionState.Broken;
                }
                return state;
            }
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(connectionString, o => o.UseNodaTime());
            optionsBuilder.UseSnakeCaseNamingConvention();
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<FileEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasOne(e => e.Parent).WithMany(e => e.Files).HasForeignKey(e => e.ParentId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(e => e.FileTags).WithOne(e => e.File).HasForeignKey(e => e.FileId).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
                entity.HasIndex(e => e.Name).IsUnique(false);
                entity.HasOne(e => e.Thumbnail).WithOne(e => e.File).HasForeignKey<ImageThumbnail>(e => e.FileId).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
                entity.Ignore(e => e.OldPath);
            });
            modelBuilder.Entity<DirectoryEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasOne(e => e.Parent).WithMany(e => e.Children).HasForeignKey(e => e.ParentId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(e => e.DirectoryTags).WithOne(e => e.Directory).HasForeignKey(e => e.DirectoryId).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
                entity.HasMany(e => e.Files).WithOne(e => e.Parent).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
                entity.HasIndex(e => e.Name).IsUnique(false);
                entity.Ignore(e => e.OldPath);
            });
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasMany(e => e.DirectoryTags).WithOne(e => e.Tag).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
                entity.HasMany(e => e.FileTags).WithOne(e => e.Tag).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
                entity.HasIndex(e => e.Name).IsUnique(true);
            });
            modelBuilder.Entity<FileTag>(entity =>
            {
                entity.HasKey(e => new { e.FileId, e.TagId });
            });
            modelBuilder.Entity<DirectoryTag>(entity =>
            {
                entity.HasKey(e => new { e.DirectoryId, e.TagId });
            });
            modelBuilder.Entity<ImageThumbnail>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }
}
