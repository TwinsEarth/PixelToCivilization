using System.IO;
using NUnit.Framework;
using PxC.Core.Persistence;
using PxC.Infra.Save;

namespace PxC.Tests.EditMode
{
    public sealed class SaveSystemTests
    {
        [Test]
        public void WriteThenRead_RoundTrips()
        {
            using var tmp = new TempDir();
            var saves = new SaveSystem(tmp.Path);
            var data = new SaveData();
            data.SetSegment(new PluginSegmentBlob
            {
                PluginId = "p",
                SegmentVersion = 1,
                Json = "{}"
            });
            Assert.IsTrue(saves.Write(0, data));
            Assert.IsTrue(saves.HasSave(0));

            var read = saves.Read(0);
            Assert.IsNotNull(read);
            Assert.AreEqual("p", read.GetSegment("p").PluginId);
        }

        [Test]
        public void Write_CreatesBakOnSecondWrite()
        {
            using var tmp = new TempDir();
            var saves = new SaveSystem(tmp.Path);
            saves.Write(0, new SaveData());
            saves.Write(0, new SaveData());
            Assert.IsTrue(File.Exists(Path.Combine(tmp.Path, "slot_0.json.bak")));
            Assert.IsTrue(File.Exists(Path.Combine(tmp.Path, "slot_0.cksum")));
            Assert.IsTrue(File.Exists(Path.Combine(tmp.Path, "slot_0.cksum.bak")));
        }

        [Test]
        public void Migration_UpgradesOldVersion()
        {
            using var tmp = new TempDir();
            var saves = new SaveSystem(tmp.Path);
            saves.AddMigration(new FakeMigration(0, 1));
            saves.AddMigration(new FakeMigration(1, 2));
            saves.Write(0, new SaveData { Version = 0 });
            var read = saves.Read(0);
            Assert.AreEqual(2, read.Version);
        }

        [Test]
        public void Read_CorruptedFile_FallsBackToBak()
        {
            using var tmp = new TempDir();
            var saves = new SaveSystem(tmp.Path);
            saves.Write(0, new SaveData { Version = 0 });
            saves.Write(0, new SaveData { Version = 0 }); // 生成 bak
            // 破坏正式文件
            File.WriteAllText(Path.Combine(tmp.Path, "slot_0.json"), "not-valid-json");
            File.WriteAllText(Path.Combine(tmp.Path, "slot_0.cksum"), "0000000000000000");
            // 应从 bak 恢复
            var read = saves.Read(0);
            Assert.IsNotNull(read);
        }

        [Test]
        public void Rollback_RestoresBak()
        {
            using var tmp = new TempDir();
            var saves = new SaveSystem(tmp.Path);
            saves.Write(0, new SaveData());
            saves.Write(0, new SaveData());
            Assert.IsTrue(saves.Rollback(0));
            Assert.IsTrue(saves.Verify(0, out _));
        }

        [Test]
        public void Delete_RemovesAllFiles()
        {
            using var tmp = new TempDir();
            var saves = new SaveSystem(tmp.Path);
            saves.Write(0, new SaveData());
            saves.Delete(0);
            Assert.IsFalse(saves.HasSave(0));
        }

        [Test]
        public void Verify_ValidSave_ReturnsTrue()
        {
            using var tmp = new TempDir();
            var saves = new SaveSystem(tmp.Path);
            saves.Write(0, new SaveData());
            Assert.IsTrue(saves.Verify(0, out string reason));
            Assert.AreEqual("OK", reason);
        }
    }
}
