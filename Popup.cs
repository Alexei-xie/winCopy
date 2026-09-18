using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace WinCopy {
    public partial class MainWindow {
        bool popupMenuOpen;
        readonly Label emptyState = new Label();
        protected override CreateParams CreateParams {
            get { var p = base.CreateParams; p.ClassStyle |= 0x20000; return p; }
        }
        void SetupPopup() {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            MinimumSize = new Size(360, 420);
            ClientSize = new Size(440, 600);
            StartPosition = FormStartPosition.Manual;
            Padding = new Padding(1);
            BackColor = Color.FromArgb(247, 248, 250);
            DoubleBuffered = true;
            Resize += delegate { RoundPopup(); };
            Deactivate += delegate { ScheduleDismiss(); };
            RoundPopup(); PositionPopup();
        }
        void RoundPopup() {
            if (Width < 24 || Height < 24) return;
            using (var path = new GraphicsPath()) {
                int d = 22; path.AddArc(0, 0, d, d, 180, 90); path.AddArc(Width-d-1, 0, d, d, 270, 90);
                path.AddArc(Width-d-1, Height-d-1, d, d, 0, 90); path.AddArc(0, Height-d-1, d, d, 90, 90); path.CloseFigure();
                var previous = Region; Region = new Region(path); if (previous != null) previous.Dispose();
            }
        }
        void PositionPopup() {
            if (db.PopupPositionSet) { var saved = new Point(db.PopupX, db.PopupY); Location = ClampPosition(saved, Screen.FromPoint(saved).WorkingArea); return; }
            var point = Cursor.Position; var area = Screen.FromPoint(point).WorkingArea;
            Location = new Point(Math.Max(area.Left, Math.Min(point.X - 20, area.Right - Width)), Math.Max(area.Top, Math.Min(point.Y + 12, area.Bottom - Height)));
        }
        void ScheduleDismiss() {
            if (IsDisposed || !Visible) return;
            var timer = new System.Windows.Forms.Timer { Interval = 100 };
            timer.Tick += delegate {
                timer.Stop(); timer.Dispose(); if (IsDisposed || !Visible || !Enabled || popupMenuOpen || movingPopup) return;
                var h = Native.GetForegroundWindow();
                if (h != Handle && Native.ProcessName(h) != System.Diagnostics.Process.GetCurrentProcess().ProcessName) Hide();
            }; timer.Start();
        }
        void TrackMenu(ContextMenuStrip menu) {
            menu.Opened += delegate { popupMenuOpen = true; };
            menu.Closed += delegate { popupMenuOpen = false; ScheduleDismiss(); };
        }
        Button CompactButton(string text, Action action, int width) {
            var b = Button(text, action); b.AutoSize = false; b.MinimumSize = Size.Empty; b.Size = new Size(width, 29); b.Margin = new Padding(0, 0, 4, 0); b.Padding = Padding.Empty;
            b.Font = new Font(Font.FontFamily, 9); b.FlatAppearance.BorderSize = 0; b.BackColor = BackColor; return b;
        }
        void BuildUi() {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14, 10, 14, 9), ColumnCount = 1, RowCount = 8 };
            foreach (int h in new[] { 38, 40, 35, 31 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            foreach (int h in new[] { 86, 36, 22 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
            Controls.Add(layout);
            var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Margin = Padding.Empty };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36)); header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
            header.Controls.Add(new Label { Text = "winCopy", Font = new Font(Font.FontFamily, 14, FontStyle.Bold), ForeColor = Ink, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            var more = CompactButton("···", delegate {}, 32); more.AccessibleName = "更多操作";
            var menu = new ContextMenuStrip();
            menu.Items.Add("新建片段", null, delegate { EditSnippet(null); });
            menu.Items.Add("偏好设置", null, delegate { Settings(); });
            var pause = new ToolStripMenuItem("暂停记录"); pause.Click += delegate { paused = !paused; RefreshItems(); }; menu.Items.Add(pause);
            menu.Opening += delegate { pause.Checked = paused; };
            menu.Items.Add(new ToolStripSeparator()); menu.Items.Add("清空未收藏历史", null, delegate { ClearHistory(); });
            menu.Items.Add("退出 winCopy", null, delegate { quitting = true; Close(); }); TrackMenu(menu);
            more.Click += delegate { menu.Show(more, new Point(0, more.Height)); }; header.Controls.Add(more, 1, 0);
            var close = CompactButton("×", Hide, 28); close.AccessibleName = "收起面板"; header.Controls.Add(close, 2, 0); layout.Controls.Add(header, 0, 0); EnableWindowDrag(header); EnableWindowDrag(header.Controls[0]); EnableWindowDrag(layout); EnableWindowDrag(status);
            var searchBox = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10, 7, 10, 5), Margin = new Padding(0, 2, 0, 5) };
            search.BorderStyle = BorderStyle.None; search.Font = new Font(Font.FontFamily, 11); search.Dock = DockStyle.Fill; search.AccessibleName = "搜索剪贴板历史";
            search.HandleCreated += delegate { SendMessage(search.Handle, 0x1501, (IntPtr)1, "搜索剪贴板…"); }; search.TextChanged += delegate { RefreshItems(); }; searchBox.Controls.Add(search); layout.Controls.Add(searchBox, 0, 1);
            BuildGroupStrip(layout);
            var listHost = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Color.White };
            list.Dock = DockStyle.Fill; list.BorderStyle = BorderStyle.None; list.DrawMode = DrawMode.OwnerDrawFixed; list.ItemHeight = 56; list.IntegralHeight = false; list.BackColor = Color.White; list.DrawItem += DrawItem;
            list.SelectedIndexChanged += delegate { ShowDetail(); }; list.DoubleClick += delegate { UseSelected(false, false); }; list.AccessibleName = "历史记录"; listHost.Controls.Add(list);
            emptyState.Dock = DockStyle.Fill; emptyState.TextAlign = ContentAlignment.MiddleCenter; emptyState.ForeColor = Muted; emptyState.Text = "还没有复制记录\n\n复制文字、图片或文件即可开始"; listHost.Controls.Add(emptyState); layout.Controls.Add(listHost, 0, 4);
            var detail = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(8, 6, 8, 4), Margin = new Padding(0, 5, 0, 3), BackColor = Color.FromArgb(239, 241, 245) };
            detail.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); detail.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); detailTitle.Dock = DockStyle.Fill; detailTitle.ForeColor = Muted; detailTitle.AutoEllipsis = true; detailTitle.Font = new Font(Font.FontFamily, 9); detail.Controls.Add(detailTitle, 0, 0);
            var content = new Panel { Dock = DockStyle.Fill }; preview.Dock = DockStyle.Fill; preview.Multiline = true; preview.ReadOnly = true; preview.BorderStyle = BorderStyle.None; preview.BackColor = detail.BackColor; preview.ScrollBars = ScrollBars.Vertical; preview.Font = new Font(Font.FontFamily, 9); picture.Dock = DockStyle.Fill; picture.SizeMode = PictureBoxSizeMode.Zoom; content.Controls.Add(preview); content.Controls.Add(picture); detail.Controls.Add(content, 0, 1); layout.Controls.Add(detail, 0, 5);
            var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 3, 0, 0) };
            footer.Controls.Add(CompactButton("＋ 片段", delegate { EditSnippet(null); }, 72)); footer.Controls.Add(CompactButton("★ 收藏", TogglePin, 72)); footer.Controls.Add(CompactButton("复制", delegate { UseSelected(false, true); }, 60));
            var paste = CompactButton("粘贴  ↵", delegate { UseSelected(false, false); }, 88); paste.BackColor = Accent; paste.ForeColor = Color.White; footer.Controls.Add(paste); layout.Controls.Add(footer, 0, 6);
            status.Dock = DockStyle.Fill; status.AutoEllipsis = true; status.Font = new Font(Font.FontFamily, 8); status.ForeColor = Muted; status.TextAlign = ContentAlignment.MiddleLeft; layout.Controls.Add(status, 0, 7);
            var context = new ContextMenuStrip(); context.Items.Add("复制", null, delegate { UseSelected(false, true); }); context.Items.Add("以纯文本粘贴", null, delegate { UseSelected(true, false); }); context.Items.Add("收藏 / 取消收藏", null, delegate { TogglePin(); }); context.Items.Add("编辑 / 保存为片段", null, delegate { if (Selected != null) EditSnippet(Selected); }); context.Items.Add("删除", null, delegate { DeleteSelected(); }); TrackMenu(context); list.ContextMenuStrip = context;
            list.MouseDown += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Right) { int index = list.IndexFromPoint(e.Location); if (index >= 0) list.SelectedIndex = index; } };
        }
        void TogglePin() { var c = Selected; if (c != null) { c.Pinned = !c.Pinned; Changed(); } }
        void DrawItem(object sender, DrawItemEventArgs e) {
            if (e.Index < 0) return; var c = (Clip)list.Items[e.Index]; bool selected = (e.State & DrawItemState.Selected) != 0; var r = e.Bounds;
            using (var b = new SolidBrush(selected ? Color.FromArgb(232, 237, 255) : Color.White)) e.Graphics.FillRectangle(b, r);
            string icon = c.Pinned ? "★" : c.Kind == "图片" ? "▧" : c.Kind == "文件" ? "▤" : "≡";
            TextRenderer.DrawText(e.Graphics, icon, Font, new Rectangle(r.X + 9, r.Y + 12, 26, 28), Accent, TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(e.Graphics, c.Preview.Replace("\r", " ").Replace("\n", "  ").Replace("\t", " "), Font, new Rectangle(r.X + 39, r.Y + 7, r.Width - 51, 23), Ink, TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            using (var small = new Font(Font.FontFamily, 8)) TextRenderer.DrawText(e.Graphics, (c.Snippet ? c.Group : c.Kind) + "  ·  " + c.Created.ToString("HH:mm") + "  " + c.Source, small, new Rectangle(r.X + 39, r.Y + 31, r.Width - 51, 18), Muted, TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        }
    }
}
