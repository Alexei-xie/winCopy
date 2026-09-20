using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Win32;

namespace WinCopy {
    static class Program {
        [STAThread] static void Main(string[] args) {
            if (args.Contains("--self-test")) { Environment.Exit(SelfTest.Run()); return; }
            bool created;
            using (var mutex = new Mutex(true, "Local\\winCopy.Desktop", out created)) {
                if (!created) { MessageBox.Show("winCopy 已在运行。请点击系统托盘图标打开。", "winCopy"); return; }
                Native.SetProcessDPIAware(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainWindow(args.Contains("--background"), true));
            }
        }
    }
    public partial class MainWindow : Form {
        readonly Color Ink = Design.Ink, Muted = Design.Muted, Accent = Design.Accent;
        Database db;
        Store store = new Store(StorageLocation.DefaultDirectory);
        readonly TextBox search = new TextBox();
        readonly ListBox list = new ListBox();
        readonly Label status = new Label(), detailTitle = new Label(), detailMeta = new Label();
        readonly TextBox preview = new TextBox();
        readonly PictureBox picture = new PictureBox();
        readonly ComboBox groups = new ComboBox();
        readonly System.Windows.Forms.Timer capture = new System.Windows.Forms.Timer(), foreground = new System.Windows.Forms.Timer(), save = new System.Windows.Forms.Timer();
        readonly NotifyIcon tray = new NotifyIcon();
        readonly List<Button> nav = new List<Button>();
        string view = "全部历史";
        bool paused, quitting, loading, startupHidden, storageBlocked;
        uint ownSequence, pendingSequence;
        int attempts;
        IntPtr target;
        public MainWindow(bool background) : this(background, false) { }
        public MainWindow(bool background, bool menuOnStart) {
            startupHidden = background;
            try { store = new StorageLocation(StorageLocation.DefaultDirectory).Resolve(); db = store.Load(); } catch (Exception ex) {
                db = new Database(); storageBlocked = true;
                MessageBox.Show("无法读取本地数据，原文件已保留。本次运行不会覆盖它。\n" + ex.Message, "winCopy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            Text = "winCopy · 剪贴板工作台"; Size = new Size(1120, 730); MinimumSize = new Size(960, 600);
            StartPosition = FormStartPosition.CenterScreen; Font = new Font("Microsoft YaHei UI", 10); BackColor = Color.FromArgb(246, 248, 252); ForeColor = Ink; Icon = MakeIcon();
            KeyPreview = true; SetupPopup(); BuildUi();
            capture.Interval = 90; capture.Tick += CaptureTick;
            foreground.Interval = 250; foreground.Tick += delegate { var h = Native.GetForegroundWindow(); if (Native.IsPasteTarget(h)) target = h; }; foreground.Start();
            save.Interval = 650; save.Tick += delegate { save.Stop(); SaveNow(); };
            tray.Icon = Icon; tray.Text = "winCopy · Ctrl + Alt + " + db.Hotkey; tray.Visible = true;
            tray.MouseUp += delegate(object sender, MouseEventArgs e) { if(e.Button==MouseButtons.Left||e.Button==MouseButtons.Right)OpenQuickMenu(); };
            Shown += delegate { if(startupHidden)Hide(); else if(menuOnStart)BeginInvoke((Action)OpenQuickMenu); else search.Focus(); };
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (!quitting && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); } else { SaveNow(); if(quickMenu!=null)quickMenu.Dispose(); tray.Visible = false; tray.Dispose(); capture.Dispose(); foreground.Dispose(); save.Dispose(); } };
            KeyDown += HandleKeys;
            RefreshItems();
        }
        Icon MakeIcon() { return Brand.LoadIcon(); }
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wparam, string text);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
        Button Button(string text, Action action, bool primary = false) {
            var b = new RoundedButton { Text = text, AutoSize = true, Height = 36, MinimumSize = new Size(80, 36), FlatStyle = FlatStyle.Flat, BackColor = primary ? Accent : Color.White, ForeColor = primary ? Color.White : Ink, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 8), Padding = new Padding(8, 0, 8, 0) };
            b.FlatAppearance.BorderColor = Color.FromArgb(224, 229, 239); b.Click += delegate { action(); }; return b;
        }
        Clip Selected { get { return list.SelectedItem as Clip; } }
        void RefreshItems() {
            if (loading) return; loading = true;
            var selectedId = Selected == null ? "" : Selected.Id;
            string group = groups.SelectedItem as string ?? "所有分组";
            groups.Items.Clear(); groups.Items.Add("所有分组"); foreach (var g in db.Items.Where(x => x.Snippet).Select(x => x.Group).Distinct().OrderBy(x => x)) groups.Items.Add(g);
            groups.SelectedItem = groups.Items.Contains(group) ? group : "所有分组"; groups.Enabled = view == "常用片段"; groups.Visible = false;
            SyncGroupStrip();
            var q = search.Text.Trim();
            var items = db.Items.Where(x => view == "常用片段" ? x.Snippet : view == "收藏" ? x.Pinned : !x.Snippet && (view == "全部历史" || x.Kind == view));
            if (view == "常用片段" && (string)groups.SelectedItem != "所有分组") items = items.Where(x => x.Group == (string)groups.SelectedItem);
            if (q.Length > 0) items = items.Where(x => (x.Preview + " " + x.Text + " " + x.Source + " " + x.Group).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            list.BeginUpdate(); list.Items.Clear(); foreach (var c in items.OrderByDescending(x => x.Pinned).ThenByDescending(x => x.Created)) list.Items.Add(c); list.EndUpdate();
            foreach (var b in nav) { b.BackColor = b.Text == view ? Accent : Color.FromArgb(247, 248, 250); b.ForeColor = b.Text == view ? Color.White : Ink; }
            loading = false;
            emptyState.Visible = list.Items.Count == 0; emptyState.Text = search.Text.Length > 0 ? "\u6ca1\u6709\u5339\u914d\u7684\u5185\u5bb9" : "\u590d\u5236\u6587\u5b57\u3001\u56fe\u7247\u6216\u6587\u4ef6\u5373\u53ef\u5f00\u59cb"; if (emptyState.Visible) emptyState.BringToFront(); int index = list.Items.Cast<Clip>().ToList().FindIndex(x => x.Id == selectedId); list.SelectedIndex = index >= 0 ? index : list.Items.Count > 0 ? 0 : -1; ShowDetail();
            SetStatus(String.Format("{0} · {1} 条    Ctrl+Alt+{2}    Enter 粘贴    Esc 收起", paused ? "记录已暂停" : "正在记录", list.Items.Count, db.Hotkey));
        }
        void ShowDetail() {
            if (loading) return; var c = Selected;
            if (picture.Image != null) { var old = picture.Image; picture.Image = null; old.Dispose(); }
            picture.Visible = c != null && c.Image != null; preview.Visible = !picture.Visible;
            detailTitle.Text = c == null ? "从一次复制开始" : c.Snippet ? c.Title : c.Kind + "预览";
            detailMeta.Text = c == null ? "复制文字、图片或文件\n它们会自动出现在这里" : c.Created.ToString("yyyy-MM-dd HH:mm:ss") + "\n" + (c.Snippet ? "分组：" + c.Group : c.Source);
            preview.Text = c == null ? "搜索历史、收藏常用内容，或创建你的第一个片段。\r\n\r\nCtrl + Alt + " + db.Hotkey + " 随时呼出。" : c.Text;
            if (picture.Visible) { try { using (var ms = new MemoryStream(c.Image)) using (var image = Image.FromStream(ms)) picture.Image = new Bitmap(image); } catch { picture.Visible = false; preview.Visible = true; preview.Text = "图片数据无法预览"; } }
        }
        void SetStatus(string message) { status.Text = message; activityLabel.Text = paused ? "记录已暂停 · 在更多菜单中继续" : "留住灵感，让复制更轻松"; }
        void Changed() { db.Prune(); RefreshItems(); save.Stop(); save.Start(); }
        void SaveNow() { if (storageBlocked) return; try { store.Save(db); } catch (Exception ex) { SetStatus("保存失败：" + ex.Message); } }
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e); if (!Native.AddClipboardFormatListener(Handle)) SetStatus("无法注册剪贴板监听");
            if (!RegisterShortcut(db.Hotkey)) BeginInvoke((Action)delegate { MessageBox.Show("Ctrl + Alt + " + db.Hotkey + " 已被其他应用占用，请在设置中更改。", "winCopy"); });
        }
        protected override void OnHandleDestroyed(EventArgs e) { Native.RemoveClipboardFormatListener(Handle); Native.UnregisterHotKey(Handle, 1); base.OnHandleDestroyed(e); }
        bool RegisterShortcut(string key) { return Native.RegisterHotKey(Handle, 1, 0x4003, (uint)key.ToUpperInvariant()[0]); }
        protected override void WndProc(ref Message m) {
            if (m.Msg == 0x0312) { OpenQuickMenu(); }
            if (m.Msg == 0x031D && !paused) { pendingSequence = Native.GetClipboardSequenceNumber(); if (pendingSequence != ownSequence) { attempts = 0; capture.Stop(); capture.Start(); } }
            base.WndProc(ref m);
        }
        void CaptureTick(object sender, EventArgs e) {
            capture.Stop(); if (paused || pendingSequence == ownSequence) return;
            try {
                string source = Native.ProcessName(Native.GetForegroundWindow());
                if (db.ExcludedApps.Split(',').Any(x => x.Trim().Length > 0 && String.Equals(Path.GetFileNameWithoutExtension(x.Trim()), source, StringComparison.OrdinalIgnoreCase))) return;
                var data = Clipboard.GetDataObject(); if (data == null) return;
                if (data.GetDataPresent("ExcludeClipboardContentFromMonitorProcessing") || data.GetDataPresent("Clipboard Viewer Ignore")) return;
                var clip = new Clip { Source = source };
                if (data.GetDataPresent(DataFormats.FileDrop)) { clip.Kind = "文件"; clip.Files = (string[])data.GetData(DataFormats.FileDrop); clip.Text = String.Join(Environment.NewLine, clip.Files); }
                else if (db.CaptureImages && data.GetDataPresent(DataFormats.Bitmap)) {
                    using (var image = Clipboard.GetImage()) { if (image == null || (long)image.Width * image.Height > 25000000) return; using (var ms = new MemoryStream()) { image.Save(ms, ImageFormat.Png); if (ms.Length > 8 * 1024 * 1024) { SetStatus("图片超过 8 MB，已跳过"); return; } clip.Image = ms.ToArray(); clip.Text = image.Width + " × " + image.Height; clip.Kind = "图片"; } }
                } else if (data.GetDataPresent(DataFormats.UnicodeText)) {
                    clip.Text = data.GetData(DataFormats.UnicodeText) as string ?? ""; if (String.IsNullOrEmpty(clip.Text) || clip.Text.Length > 1000000) return;
                    clip.Html = data.GetDataPresent(DataFormats.Html) ? data.GetData(DataFormats.Html) as string ?? "" : "";
                    clip.Rtf = data.GetDataPresent(DataFormats.Rtf) ? data.GetData(DataFormats.Rtf) as string ?? "" : "";
                    if (clip.Html.Length + clip.Rtf.Length > 2000000) { clip.Html = ""; clip.Rtf = ""; }
                } else return;
                if (Native.GetClipboardSequenceNumber() != pendingSequence) return;
                db.Add(clip); Changed();
            } catch (System.Runtime.InteropServices.ExternalException) { if (++attempts < 8) capture.Start(); else SetStatus("剪贴板忙，本次读取已跳过"); }
            catch (Exception ex) { SetStatus("无法读取该内容：" + ex.Message); }
        }
        void OpenPanel() { var h = Native.GetForegroundWindow(); if (Native.IsPasteTarget(h)) target = h; PositionPopup(); Show(); WindowState = FormWindowState.Normal; Native.SetForegroundWindow(Handle); Activate(); search.Focus(); search.SelectAll(); db.Prune(); RefreshItems(); }
        void UseSelected(bool plain, bool copyOnly) { UseClip(Selected, plain, copyOnly); }
        void UseClip(Clip c, bool plain, bool copyOnly) {
            if (c == null) return;
            try {
                var data = new DataObject();
                if (c.Image != null && !plain) { using (var ms = new MemoryStream(c.Image)) using (var image = Image.FromStream(ms)) using (var bitmap = new Bitmap(image)) { data.SetData(DataFormats.Bitmap, bitmap); Clipboard.SetDataObject(data, true, 5, 50); } }
                else {
                    if (c.Files != null && !plain) { if (c.Files.Any(x => !File.Exists(x) && !Directory.Exists(x))) { MessageBox.Show("部分文件已移动或删除，请检查文件路径。", "winCopy"); return; } var files = new StringCollection(); files.AddRange(c.Files); data.SetFileDropList(files); }
                    else { data.SetText(c.Text, TextDataFormat.UnicodeText); if (!plain) { if (c.Html.Length > 0) data.SetData(DataFormats.Html, c.Html); if (c.Rtf.Length > 0) data.SetData(DataFormats.Rtf, c.Rtf); } }
                    Clipboard.SetDataObject(data, true, 5, 50);
                }
                ownSequence = Native.GetClipboardSequenceNumber();
                if (copyOnly || !db.AutoPaste) { SetStatus("已复制到剪贴板"); return; }
                var destination = target;
                if (destination == IntPtr.Zero || !Native.IsWindow(destination)) { SetStatus("已复制，请切换到目标应用并按 Ctrl + V"); return; }
                Hide(); if (Native.IsIconic(destination)) Native.ShowWindow(destination, 9); Native.SetForegroundWindow(destination);
                var timer = new System.Windows.Forms.Timer { Interval = 60 }; int ticks = 0;
                timer.Tick += delegate {
                    if (++ticks < 25 && Native.ModifiersDown()) return;
                    timer.Stop(); timer.Dispose();
                    if (Native.ModifiersDown() || Native.GetForegroundWindow() != destination || !Native.Paste()) tray.ShowBalloonTip(2500, "winCopy", "内容已复制。请在目标应用中按 Ctrl + V。", ToolTipIcon.Info);
                }; timer.Start();
            } catch (Exception ex) { MessageBox.Show("复制失败，请稍后重试。\n" + ex.Message, "winCopy"); }
        }
        void HandleKeys(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Escape) { Hide(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.F) { search.Focus(); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Enter && !preview.Focused) { UseSelected(e.Shift, e.Control); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Down && search.Focused && list.Items.Count > 0) { list.Focus(); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Delete && list.Focused) { DeleteSelected(); e.SuppressKeyPress = true; }
        }
        void DeleteSelected() { var c = Selected; if (c == null) return; if ((c.Pinned || c.Snippet) && MessageBox.Show("删除此收藏或片段？", "winCopy", MessageBoxButtons.YesNo) != DialogResult.Yes) return; db.Items.Remove(c); Changed(); }
        void ClearHistory() {
            int count=db.Items.Count(x=>!x.Snippet&&!x.Pinned);
            if(count==0){SetStatus("没有可清空的普通历史");return;}
            if(MessageBox.Show(this,"清空全部 "+count+" 条普通历史？\n收藏、常用片段和系统剪贴板不受影响。此操作不可撤销。","清空历史",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
            var previous=db.Items.ToList();db.ClearHistory();
            try{if(storageBlocked)throw new IOException("当前数据存储不可用。");store.Save(db);save.Stop();RefreshItems();SetStatus("已清空 "+count+" 条普通历史，收藏和片段已保留");}
            catch(Exception ex){db.Items=previous;RefreshItems();MessageBox.Show(this,"清空未完成，历史已保留。\n"+ex.Message,"winCopy",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
        void EditSnippet(Clip original) {
            if (original != null && original.Kind != "文本") { SetStatus("只有文本可以保存为片段"); return; }
            using (var editor = new SnippetEditor(original, db.GetGroups())) if (editor.ShowDialog(this) == DialogResult.OK) {
                var c = original != null && original.Snippet ? original : new Clip(); c.Snippet = true; c.Title = editor.TitleValue; c.Group = editor.GroupValue; c.Text = editor.BodyValue; c.Html = ""; c.Rtf = ""; c.Created = DateTime.Now;
                if (!db.Items.Contains(c)) db.Items.Insert(0, c); view = "常用片段"; Changed(); groups.SelectedItem = c.Group; RefreshItems();
            }
        }
        void Settings() {
            using (var f = new SettingsDialog(db)) {
                f.StorageDirectory = store.DirectoryPath; f.UpdateAction = delegate { ShowUpdate(f); }; f.ExportAction = ExportSnippets; f.ImportAction = ImportSnippets;
                if (f.ShowDialog(this) != DialogResult.OK) return;
                if (f.Shortcut != db.Hotkey) { Native.UnregisterHotKey(Handle, 1); if (!RegisterShortcut(f.Shortcut)) { RegisterShortcut(db.Hotkey); MessageBox.Show("快捷键被占用，设置未保存。", "winCopy"); return; } }
                try {
                    if(!StorageLocation.Same(f.StorageDirectory,store.DirectoryPath)) {
                        if(storageBlocked)throw new IOException("当前数据读取失败，不能迁移。请先恢复原数据目录再重试。");
                        save.Stop(); db.Prune();
                        store=new StorageLocation(StorageLocation.DefaultDirectory).Migrate(store,f.StorageDirectory,db);
                    }
                } catch(Exception ex) {
                    if(f.Shortcut!=db.Hotkey){Native.UnregisterHotKey(Handle,1);RegisterShortcut(db.Hotkey);}
                    MessageBox.Show(this,"存储位置未更改，设置未保存。\n"+ex.Message,"winCopy",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;
                }
                db.Hotkey = f.Shortcut; db.Limit = f.HistoryLimit; db.RetentionDays = f.Days; db.AutoPaste = f.AutoPaste; db.CaptureImages = f.Images; db.RememberHistory = f.Remember; db.ExcludedApps = f.Excluded;
                try { using (var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true)) { if (f.Startup) key.SetValue("winCopy", "\"" + Application.ExecutablePath + "\" --background"); else key.DeleteValue("winCopy", false); } } catch (Exception ex) { MessageBox.Show("无法修改开机启动：" + ex.Message, "winCopy"); }
                tray.Text = "winCopy · Ctrl + Alt + " + db.Hotkey; Changed(); SaveNow();
            }
        }
        void ExportSnippets() {
            using (var dialog = new SaveFileDialog { Filter = "片段 XML|*.xml", FileName = "winCopy-snippets.xml" }) if (dialog.ShowDialog(this) == DialogResult.OK) {
                try { var root = new XElement("folders", db.Items.Where(x => x.Snippet).GroupBy(x => x.Group).Select(g => new XElement("folder", new XElement("title", g.Key), new XElement("snippets", g.Select(c => new XElement("snippet", new XElement("title", c.Title), new XElement("content", c.Text))))))); new XDocument(root).Save(dialog.FileName); MessageBox.Show("片段已导出为明文 XML，请妥善保管。", "winCopy"); } catch (Exception ex) { MessageBox.Show(ex.Message, "导出失败"); }
            }
        }
        void ImportSnippets() {
            using (var dialog = new OpenFileDialog { Filter = "Clipy / winCopy 片段 XML|*.xml" }) if (dialog.ShowDialog(this) == DialogResult.OK) {
                try {
                    var incoming = new List<Clip>(); using (var reader = XmlReader.Create(dialog.FileName, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 10000000 })) {
                        var document = XDocument.Load(reader);
                        foreach (var folder in document.Descendants("folder")) foreach (var s in folder.Elements("snippets").Elements("snippet")) {
                            var body = (string)s.Element("content") ?? ""; if (body.Length == 0) continue;
                            incoming.Add(new Clip { Snippet = true, Title = (string)s.Element("title") ?? "未命名", Group = (string)folder.Element("title") ?? "导入", Text = body });
                        }
                    }
                    int added = 0; foreach (var c in incoming) if (!db.Items.Any(x => x.Snippet && x.Title == c.Title && x.Group == c.Group && x.Text == c.Text)) { db.Items.Add(c); added++; }
                    Changed(); MessageBox.Show("已导入 " + added + " 个片段。", "winCopy");
                } catch (Exception ex) { MessageBox.Show("无法导入：" + ex.Message, "winCopy"); }
            }
        }
    }
}
