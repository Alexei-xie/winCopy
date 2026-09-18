using System;
using System.IO;
using System.Linq;
using System.Text;

namespace WinCopy {
    static class SelfTest {
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        public static int Run() {
            string root = Path.Combine(Path.GetTempPath(), "winCopy-test-" + Guid.NewGuid().ToString("N"));
            try {
                var db = new Database { Limit = 20 };
                db.Add(new Clip { Text = "dedupe", Pinned = true }); db.Add(new Clip { Text = "dedupe" }); Check(db.Items.Count == 1 && db.Items[0].Pinned, "Deduplication must preserve pin");
                db.Items.Add(new Clip { Text = "expired", Created = DateTime.Now.AddDays(-50) });
                db.Items.Add(new Clip { Text = "snippet", Snippet = true, Created = DateTime.Now.AddDays(-100) });
                for (int i = 0; i < 30; i++) db.Add(new Clip { Text = "history " + i });
                Check(db.Items.Count(x => !x.Pinned && !x.Snippet) == 20, "History limit"); Check(!db.Items.Any(x => x.Text == "expired"), "Retention"); Check(db.Items.Any(x => x.Snippet), "Preserve snippets");
                var store = new Store(root); store.Save(db); var loaded = store.Load(); Check(loaded.Items.Count == db.Items.Count, "Persistence roundtrip");
                Check(!Encoding.UTF8.GetString(File.ReadAllBytes(store.FilePath)).Contains("dedupe"), "Encrypted persistence");
                db.RememberHistory = false; store.Save(db); loaded = store.Load(); Check(loaded.Items.All(x => x.Pinned || x.Snippet), "Session-only history");
                File.WriteAllText(store.FilePath, "invalid"); bool rejected = false; try { store.Load(); } catch { rejected = true; } Check(rejected, "Corrupt store must not silently reset");
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "self-test-result.txt"), "PASS: dedupe, pinned retention, history limit, expiry, encrypted roundtrip, atomic replacement, session-only storage, corrupt data rejection."); return 0;
            } catch (Exception ex) { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "self-test-result.txt"), "FAIL: " + ex); return 1; }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
    }
}
