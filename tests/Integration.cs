using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using WinCopy;

class Integration {
    static BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Pump(int ms) { var end = DateTime.UtcNow.AddMilliseconds(ms); while (DateTime.UtcNow < end) { Application.DoEvents(); Thread.Sleep(10); } }
    static void Check(bool condition, string text) { if (!condition) throw new Exception(text); }
    [STAThread] static int Main() {
        Application.EnableVisualStyles();
        IDataObject backup = Clipboard.GetDataObject();
        MainWindow main = null; Form destination = null; Database original = null;
        var results = new System.Collections.Generic.List<string>();
        try {
            main = new MainWindow(false);
            original = (Database)typeof(MainWindow).GetField("db", Flags).GetValue(main);
            // Use an isolated in-memory database and forbid persistence during integration tests.
            typeof(MainWindow).GetField("storageBlocked", Flags).SetValue(main, true);
            var db = new Database(); typeof(MainWindow).GetField("db", Flags).SetValue(main, db);
            main.Show(); Pump(150);
            string token = "winCopy integration 中文 " + Guid.NewGuid().ToString("N");
            Clipboard.SetText(token); Pump(450);
            Check(db.Items.Any(x => x.Text == token), "Clipboard text capture failed"); results.Add("PASS: WM_CLIPBOARDUPDATE text capture");
            using (var bmp = new Bitmap(32, 24)) { using (var g = Graphics.FromImage(bmp)) g.Clear(Color.CornflowerBlue); Clipboard.SetImage(bmp); Pump(450); }
            Check(db.Items.Any(x => x.Image != null && x.Text == "32 × 24"), "Image capture failed"); results.Add("PASS: image capture");
            typeof(MainWindow).GetField("paused", Flags).SetValue(main, true);
            Clipboard.SetText("paused unique marker"); Pump(300); Check(!db.Items.Any(x => x.Text == "paused unique marker"), "Pause failed"); results.Add("PASS: paused capture");
            typeof(MainWindow).GetField("paused", Flags).SetValue(main, false);
            var list = (ListBox)typeof(MainWindow).GetField("list", Flags).GetValue(main);
            list.SelectedItem = db.Items.First(x => x.Text == token);
            typeof(MainWindow).GetMethod("UseSelected", Flags).Invoke(main, new object[] { false, true }); Pump(200);
            Check(Clipboard.GetText() == token, "History copy failed"); results.Add("PASS: history restore to clipboard");
            Check(db.Items.Count(x => x.Text == token) == 1, "Self copy duplicated history"); results.Add("PASS: self-copy suppression");
            destination = new Form { Text = "winCopy integration paste target", Size = new Size(400, 200) };
            var box = new TextBox { Multiline = true, Dock = DockStyle.Fill }; destination.Controls.Add(box); destination.Show(); destination.Activate(); box.Focus(); Pump(350);
            typeof(MainWindow).GetField("target", Flags).SetValue(main, destination.Handle);
            typeof(MainWindow).GetMethod("OpenPanel", Flags).Invoke(main, null); Pump(150);
            list.SelectedItem = db.Items.First(x => x.Text == token);
            typeof(MainWindow).GetMethod("UseSelected", Flags).Invoke(main, new object[] { false, false }); Pump(700);
            results.Add("DEBUG: target=" + destination.Handle + ", foreground=" + typeof(MainWindow).Assembly.GetType("WinCopy.Native").GetMethod("GetForegroundWindow").Invoke(null, null) + ", focus=" + box.Focused + ", chars=" + box.Text.Length + ", visible=" + main.Visible); Check(box.Text == token, "Automatic paste failed in this desktop session"); results.Add("PASS: foreground restoration and SendInput paste");
            return 0;
        } catch (Exception ex) { results.Add("FAIL: " + ex); return 1; }
        finally {
            if (main != null) { typeof(MainWindow).GetField("quitting", Flags).SetValue(main, true); main.Close(); main.Dispose(); }
            if (destination != null) destination.Dispose();
            try { if (backup != null) Clipboard.SetDataObject(backup, true, 5, 50); } catch { results.Add("NOTE: Original clipboard could not be restored."); }
            File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "integration-result.txt"), results);
        }
    }
}
