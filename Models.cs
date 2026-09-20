using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace WinCopy {
    public class Clip {
        public string Id = Guid.NewGuid().ToString("N");
        public string Kind = "文本";
        public string Text = "";
        public string Html = "";
        public string Rtf = "";
        public byte[] Image;
        public string[] Files;
        public DateTime Created = DateTime.Now;
        public string Source = "";
        public bool Pinned;
        public bool Snippet;
        public string Title = "";
        public string Group = "常用";
        public string Preview { get { return Snippet ? Title : Kind == "图片" ? "图片 · " + Text : Text; } }
        public string Fingerprint() {
            using (var sha = SHA256.Create()) {
                var data = Image ?? Encoding.UTF8.GetBytes(Kind + "\0" + Text + "\0" + Html + "\0" + Rtf);
                return Convert.ToBase64String(sha.ComputeHash(data));
            }
        }
    }
    public class Database {
        public List<Clip> Items = new List<Clip>();
        public List<string> GroupOrder = new List<string>(); public bool PopupPositionSet; public int PopupX, PopupY; public bool MenuPositionSet; public int MenuX, MenuY;
        public int Limit = 200;
        public int RetentionDays = 30;
        public bool AutoPaste = true;
        public bool CaptureImages = true;
        public string Hotkey = "V";
        public string ExcludedApps = "KeePass,KeePassXC,1Password,Bitwarden";
        public bool RememberHistory = true;
        public void Add(Clip clip) {
            var hash = clip.Fingerprint();
            var old = Items.FirstOrDefault(x => !x.Snippet && x.Fingerprint() == hash);
            if (old != null) { clip.Pinned = old.Pinned; Items.Remove(old); }
            Items.Insert(0, clip); Prune();
        }
        public int ClearHistory() { return Items.RemoveAll(x => !x.Snippet && !x.Pinned); }
        public string[] GetGroups() {
            var available=Items.Where(x=>x.Snippet).Select(x=>x.Group).Distinct().ToList();
            return GroupOrder.Where(available.Contains).Concat(available.Where(x=>!GroupOrder.Contains(x)).OrderBy(x=>x)).Distinct().ToArray();
        }
        public void RemoveEmptyGroups() {
            var available=Items.Where(x=>x.Snippet).Select(x=>x.Group).Distinct().ToList();
            GroupOrder=GroupOrder.Where(available.Contains).Distinct().ToList();
        }
        public void Prune() {
            Limit=Math.Max(20,Math.Min(2000,Limit)); RetentionDays=Math.Max(1,Math.Min(365,RetentionDays));
            RemoveEmptyGroups();
            Items.RemoveAll(x => !x.Snippet && !x.Pinned && x.Created < DateTime.Now.AddDays(-Math.Max(1, RetentionDays)));
            var excess = Items.Where(x => !x.Snippet && !x.Pinned).Skip(Math.Max(20, Math.Min(2000, Limit))).ToList();
            foreach (var x in excess) Items.Remove(x);
            // Keep the persisted payload bounded even when large images are copied repeatedly.
            long bytes = 0;
            foreach (var x in Items.ToList()) {
                bytes += x.Image == null ? Encoding.UTF8.GetByteCount(x.Text ?? "") : x.Image.Length;
                if (bytes > 64L * 1024 * 1024 && !x.Pinned && !x.Snippet) Items.Remove(x);
            }
        }
    }
    public class Store {
        public readonly string DirectoryPath;
        public string FilePath { get { return Path.Combine(DirectoryPath, "history.dat"); } }
        public Store(string path) { DirectoryPath = path; }
        static readonly XmlSerializer Serializer = new XmlSerializer(typeof(Database));
        public Database Load() {
            if (!File.Exists(FilePath)) return new Database();
            var clear = ProtectedData.Unprotect(File.ReadAllBytes(FilePath), null, DataProtectionScope.CurrentUser);
            using (var ms = new MemoryStream(clear)) {
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 150000000 };
                using (var reader = XmlReader.Create(ms, settings)) {
                    var db = (Database)Serializer.Deserialize(reader); db.Prune(); return db;
                }
            }
        }
        public void Save(Database db) {
            Directory.CreateDirectory(DirectoryPath);
            byte[] clear;
            using (var ms = new MemoryStream()) {
                var persisted = new Database { Items = db.Items.Where(x => db.RememberHistory || x.Pinned || x.Snippet).ToList(), GroupOrder = db.GroupOrder, PopupPositionSet = db.PopupPositionSet, PopupX = db.PopupX, PopupY = db.PopupY, MenuPositionSet = db.MenuPositionSet, MenuX = db.MenuX, MenuY = db.MenuY, Limit = db.Limit, RetentionDays = db.RetentionDays, AutoPaste = db.AutoPaste, CaptureImages = db.CaptureImages, Hotkey = db.Hotkey, ExcludedApps = db.ExcludedApps, RememberHistory = db.RememberHistory };
                Serializer.Serialize(ms, persisted); clear = ms.ToArray();
            }
            var temp = FilePath + ".tmp";
            File.WriteAllBytes(temp, ProtectedData.Protect(clear, null, DataProtectionScope.CurrentUser));
            if (File.Exists(FilePath)) File.Replace(temp, FilePath, null); else File.Move(temp, FilePath);
        }
    }
}
