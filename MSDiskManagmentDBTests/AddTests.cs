using MSDiskManagerData.Data;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using MSDiskManagerData.Helpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MSDiskManagmentDBTests
{
    public class AddTests
    {
        [SetUp]
        public void Setup()
        {
            var db = new MSDM_DBContext(true);
            MSDM_DBContext.DriverName = "D:/";
            db.Database.EnsureCreated();
        }

        [Test]
        [Order(1)]
        public async Task AddTags()
        {
            var rep = new TagRepository(true);
            for (int i = 0; i < 200; i++)
            {
                var tag = new Tag
                {
                    Name = $"MSDMTag{i + 1}",
                    Color = new Random().Next(10),
                };
                tag = await rep.AddTag(tag);
                Assert.NotNull(tag.Id);
            }
        }

        [Test]
        [Order(2)]
        public async Task AddDirectory_1()
        {
            var rep = new DirectoryRepository(true);
            var dir = new DirectoryEntity
            {
                OnDeskName = "MSDMTest",
                Description = "Directory to test the folder writing",
                IsHidden = false,
                Name = "RootDirectory",
                Path = "MSDMTest/",
            };
            dir = await rep.CreateDirectory(dir);
            Assert.NotNull(dir.Id, $"Failure while adding directory: root");
            Assert.True(Directory.Exists(dir.FullPath), "root Directory did not exist");
            File.WriteAllText("root_id", dir.Id.ToString());
        }
        [Test]
        [Order(3)]
        public async Task AddDirectories()
        {
            var rep = new DirectoryRepository(true);
            var id = long.Parse(File.ReadAllText("root_id"));
            Assert.NotNull(id, "root id was null");
            Assert.True(id >= 0, "root id was smaller than 0");
            var tags = await new TagRepository(true).GetTags();
            Assert.True(Globals.IsNotNullNorEmpty(tags), "tags where null or empty");
            for (int i = 0; i < 10; i++)
            {
                var parent = await rep.GetDirectory(id);
                Assert.NotNull(parent, $"get parent {id} was null");
                var dir = await rep.CreateDirectory(id, $"Directory{i + 1}");
                Assert.NotNull(dir.Id, $"Failure while adding directory:{i}");
                Assert.True(Directory.Exists(dir.FullPath), $"Directory:{dir.Name} did not exist");
                id = (long)dir.Id;
                var rand = new Random().Next(10);
                var added = new List<long>();
                for (int i2 = 0; i2 < rand; i2++)
                {
                    var tid = (long)tags[new Random().Next(tags.Count)].Id;
                    if (added.Contains(tid)) continue;
                    Assert.True(await rep.AddTag(id, tid), $"couldn't add tags[{tags}] to directory{dir.Name}");
                    added.Add(tid);
                }
            }
        }
        [Test]
        [Order(4)]
        public async Task AddDirectoriesDuplicate()
        {
            var rep = new DirectoryRepository(true);
            var id = long.Parse(File.ReadAllText("root_id"));
            Assert.NotNull(id);
            Assert.True(id >= 0);
            var tags = await new TagRepository(true).GetTags();
            Assert.True(Globals.IsNotNullNorEmpty(tags), "tags where null or empty");
            var child = await rep.GetDirectory(id);
            for (int i = 0; i < 10; i++)
            {
                child = (await rep.GetChildren(id))[0];
                
                Assert.NotNull(child);
                var dir = await rep.CreateDirectory(id, child.Name);
                Assert.NotNull(dir.Id, $"Failure while adding directory:{i}");
                Assert.True(Directory.Exists(dir.FullPath), $"Directory:{dir.Name} did not exist");
                Assert.True(dir.OnDeskName.Contains("_"));
                id = (long)child.Id;
                var rand = new Random().Next(10);
                var added = new List<long>();
                for (int i2 = 0; i2 < rand; i2++)
                {
                    var tid = (long)tags[new Random().Next(tags.Count)].Id;
                    if (added.Contains(tid)) continue;
                    Assert.True(await rep.AddTag(id, tid));
                    added.Add(tid);
                }
            }
        }
        [Test]
        [Order(5)]
        public async Task MoveDirectories()
        {
            var rep = new DirectoryRepository(true);
            var id = long.Parse(File.ReadAllText("root_id"));
            Assert.NotNull(id);
            Assert.True(id >= 0);
            for (int i = 0; i < 10; i++)
            {
                var children = await rep.GetChildren(id);
                Assert.True(Globals.IsNotNullNorEmpty(children), "Children were null or empty");
                var dir1 = children.FirstOrDefault(c => !c.OnDeskName.Contains("_"));
                Assert.NotNull(dir1, "dir1 was null");
                var dir2 = children.FirstOrDefault(c => c.OnDeskName.Contains($"{i + 1}_"));
                Assert.NotNull(dir2, "dir1 was null");
                var d = await rep.Move((long)dir2.Id, dir1);
                Assert.True(d.success, "move was not succesful");
                id = (long)dir1.Id;
            }
        }
        [Test]
        [Order(6)]
        public async Task AddFile()
        {
            var rep = new FileRepository(true);
            var str = "ksdasldaslkdsalkda";
            var path = "D:/dt/random_file.txt";
            File.WriteAllText(path, str);
            var file = new FileEntity { Name = "random file", IsHidden = false, Path = "random_file.txt", Extension = "txt", FileType = FileType.Text };
            file = await rep.AddFile(file, path);
            Assert.NotNull(file.Id, "File id was null after add");
            Assert.True(File.Exists(file.FullPath), "File was not added");
        }
        [Test]
        [Order(7)]
        public async Task AddDuplicateFile()
        {
            var rep = new FileRepository(true);
            var str = "ksdasldaslkdsalkda";
            var path = "D:/dt/random_file.txt";
            File.WriteAllText(path, str);
            var file = new FileEntity { Name = "random file", IsHidden = false, Path = "random_file.txt", Extension = "txt", FileType = FileType.Text };
            file = await rep.AddFile(file, path);
            Assert.NotNull(file.Id, "File id was null after add");
            Assert.True(file.OnDeskName.Contains("_"));
            Assert.True(File.Exists(file.FullPath), "File was not added");
        }
        [Test]
        [Order(8)]
        public async Task MoveFile()
        {
            var rep = new FileRepository(true);
            var files = await rep.FilterFiles(null);
            var directories = await new DirectoryRepository(true).FilterDirectories(null);
            Assert.True(Globals.IsNotNullNorEmpty(files), "files were null or empty");
            Assert.True(Globals.IsNotNullNorEmpty(directories), "directories were null or empty");
            var file = files[0];
            var f = await rep.Move((long)file.Id, directories[0]);
            Assert.True(f.success, f.message);
        }
    }
}