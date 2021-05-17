using Microsoft.EntityFrameworkCore;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Entities.Relations;
using MSDiskManagerData.Helpers;
using Newtonsoft.Json;
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
        public static string DriverName => CurrentDriver.DriverLetter;
        public DbSet<FileEntity> Files { get; set; }
        public DbSet<IgnoredFile> ignoredFiles { get; set; }
        public DbSet<DirectoryEntity> Directories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<FileTag> FileTags { get; set; }
        public DbSet<DirectoryTag> DirectoryTags { get; set; }
        public DbSet<ImageThumbnail> Thumbnails { get; set; }
        public DbSet<MSDriver> MSDrivers { get; set; }
        private static bool IsTest { get; set; }
        private static MSDriver CurrentDriver { get; set; }
        internal static long? currentDriverId => CurrentDriver.Id;
        
        public static void SetDriver(MSDriver driver) => CurrentDriver = driver;
     
       
        public static void SetConnectionString(string str)
        {
            connectionString = str;
        }
        private static string connectionString = "";

        public MSDM_DBContext(bool isTest = false)
        {
            if (IsTest != isTest)
            {
            }
            Database.EnsureCreated();
        }
        public static ConnectionState ConnectionState
        {
            get
            {
#if DEBUG
                return ConnectionState.Open;
#endif
                ConnectionState state = ConnectionState.Broken;
                if (Globals.IsNullOrEmpty(connectionString)) return state;
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
#if DEBUG
            optionsBuilder.UseSqlite(@"Data Source=.\msdmtest.db", x => x.UseNodaTime());
#else
            optionsBuilder.UseNpgsql(connectionString, o => o.UseNodaTime());
#endif

            optionsBuilder.UseSnakeCaseNamingConvention();
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<MSDriver>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

            });
            modelBuilder.Entity<FileEntity>(entity =>
             {
                 entity.HasKey(e => e.Id);
                 entity.Property(e => e.Id).ValueGeneratedOnAdd();
                 entity.HasOne(e => e.Parent).WithMany(e => e.Files).HasForeignKey(e => e.ParentId).OnDelete(DeleteBehavior.Cascade).IsRequired(false);
                 entity.HasOne<MSDriver>().WithMany(e => e.Files).HasForeignKey(e => e.DriverId).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
                 entity.HasMany(e => e.FileTags).WithOne(e => e.File).HasForeignKey(e => e.FileId).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
                 entity.HasIndex(e => new { e.DriverId, e.Path }).IsUnique(true);
                 entity.HasIndex(e => e.Name).IsUnique(false);
                 entity.Property(e => e.Extension).HasColumnName("ext");
                 entity.HasOne(e => e.Thumbnail).WithOne(e => e.File).HasForeignKey<ImageThumbnail>(e => e.FileId).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
                 entity.Ignore(e => e.OldPath);
#if DEBUG
                 entity.Property(e => e.AncestorIds).HasConversion(lst => JsonConvert.SerializeObject(lst), str => JsonConvert.DeserializeObject<List<long>>(str));
#endif
             });
            modelBuilder.Entity<DirectoryEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasOne(e => e.Parent).WithMany(e => e.Children).HasForeignKey(e => e.ParentId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<MSDriver>().WithMany(e => e.Directories).HasForeignKey(e => e.DriverId).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
                entity.HasMany(e => e.DirectoryTags).WithOne(e => e.Directory).HasForeignKey(e => e.DirectoryId).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
                entity.HasIndex(e => e.Name).IsUnique(false);
                entity.Ignore(e => e.OldPath);
                entity.HasIndex(e => new { e.DriverId, e.Path }).IsUnique(true);
                entity.Ignore(e => e.NumberOfFiles);
                entity.Ignore(e => e.NumberOfFilesRec);
                entity.Ignore(e => e.NumberOfDirectories);
                entity.Ignore(e => e.NumberOfDirectoriesRec);
                entity.Ignore(e => e.NumberOfItems);
                entity.Ignore(e => e.NumberOfItemsRec);
#if DEBUG
                entity.Property(e => e.AncestorIds).HasConversion(lst => JsonConvert.SerializeObject(lst), str => JsonConvert.DeserializeObject<List<long>>(str));
#endif
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

#if DEBUG
            
#endif
        }
    }
}
