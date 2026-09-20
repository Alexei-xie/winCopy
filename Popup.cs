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
            MinimumSize = new Size(440, 560);
            ClientSize = new Size(480, 680);
            StartPosition = FormStartPosition.Manual;
            Padding = new Padding(1);
            BackColor = Design.Canvas;
            DoubleBuffered = true;
            Resize += delegate { RoundPopup(); };
            Deactivate += delegate { ScheduleDismiss(); };
            RoundPopup(); PositionPopup();
        }
        void RoundPopup() {
            if (Width < 24 || Height < 24) return;
            using (var path = new GraphicsPath()) {
                int d = 36; path.AddArc(0, 0, d, d, 180, 90); path.AddArc(Width-d-1, 0, d, d, 270, 90);
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
            var b = Button(text, action); b.AutoSize = false; b.MinimumSize = Size.Empty; b.Size = new Size(width, 36); b.Margin = new Padding(0, 0, 4, 0); b.Padding = Padding.Empty;
            b.Font = new Font(Font.FontFamily, 9); b.FlatAppearance.BorderSize = 0; b.BackColor = Color.White; return b;
        }
        void BuildUi() {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 20, 12), ColumnCount = 1, RowCount = 8 };
            foreach (int h in new[] { 68, 56, 40, 32 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            foreach (int h in new[] { 108, 50, 24 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
            Controls.Add(layout);
            var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Margin = Padding.Empty };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36)); header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
            BuildBrand(header);
            var more = CompactButton("···", delegate {}, 32); more.AccessibleName = "更多操作";
            var menu = new ContextMenuStrip();
            menu.Items.Add("新建片段", null, delegate { EditSnippet(null); });
            menu.Items.Add("偏好设置", null, delegate { Settings(); }); menu.Items.Add("检查更新", null, delegate { ShowUpdate(); });
            var pause = new ToolStripMenuItem("暂停记录"); pause.Click += delegate { paused = !paused; RefreshItems(); }; menu.Items.Add(pause);
            menu.Opening += delegate { pause.Checked = paused; };
            menu.Items.Add(new ToolStripSeparator()); menu.Items.Add("清空未收藏历史", null, delegate { ClearHistory(); });
            menu.Items.Add("退出 winCopy", null, delegate { quitting = true; Close(); }); TrackMenu(menu);
            more.Click += delegate { menu.Show(more, new Point(0, more.Height)); }; header.Controls.Add(more, 1, 0);
            var close = CompactButton("×", Hide, 28); close.AccessibleName = "收起面板"; header.Controls.Add(close, 2, 0); layout.Controls.Add(header, 0, 0); EnableWindowDrag(header); EnableWindowDrag(header.Controls[0]); EnableWindowDrag(layout); EnableWindowDrag(status);
            var searchBox = new RoundedPanel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(14, 12, 14, 10), Margin = new Padding(0, 2, 0, 5) };
            search.BorderStyle = BorderStyle.None; search.Font = new Font(Font.FontFamily, 11); search.Dock = DockStyle.Fill; search.AccessibleName = "搜索剪贴板历史";
            search.HandleCreated += delegate { SendMessage(search.Handle, 0x1501, (IntPtr)1, "搜索内容、来源或片段…"); }; search.TextChanged += delegate { RefreshItems(); }; searchBox.Controls.Add(search); layout.Controls.Add(searchBox, 0, 1);
            BuildGroupStrip(layout);
            var listHost = new RoundedPanel { Padding = new Padding(6), Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Color.White };
            list.Dock = DockStyle.Fill; list.BorderStyle = BorderStyle.None; list.DrawMode = DrawMode.OwnerDrawFixed; list.ItemHeight = 68; list.IntegralHeight = false; list.BackColor = Color.White; list.DrawItem += DrawModernItem;
            list.SelectedIndexChanged += delegate { ShowDetail(); }; list.DoubleClick += delegate { UseSelected(false, false); }; list.AccessibleName = "历史记录"; listHost.Controls.Add(list);
            emptyState.Dock = DockStyle.Fill; emptyState.TextAlign = ContentAlignment.MiddleCenter; emptyState.ForeColor = Muted; emptyState.Text = "还没有复制记录\n\n复制文字、图片或文件即可开始"; listHost.Controls.Add(emptyState); layout.Controls.Add(listHost, 0, 4);
            var detail = new RoundedTable { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(14, 10, 14, 10), Margin = new Padding(0, 10, 0, 2), BackColor = Color.White };
            detail.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); detail.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); detailTitle.Dock = DockStyle.Fill; detailTitle.ForeColor = Muted; detailTitle.AutoEllipsis = true; detailTitle.Font = new Font(Font.FontFamily, 9); detail.Controls.Add(detailTitle, 0, 0);
            var content = new Panel { Dock = DockStyle.Fill }; preview.Dock = DockStyle.Fill; preview.Multiline = true; preview.ReadOnly = true; preview.BorderStyle = BorderStyle.None; preview.BackColor = detail.BackColor; preview.ScrollBars = ScrollBars.Vertical; preview.Font = new Font(Font.FontFamily, 9); picture.Dock = DockStyle.Fill; picture.SizeMode = PictureBoxSizeMode.Zoom; picture.Cursor=Cursors.Hand; picture.AccessibleName="图片预览，点击放大"; tips.SetToolTip(picture,"点击放大查看图片"); picture.Click+=delegate{OpenImagePreview();}; content.Controls.Add(preview); content.Controls.Add(picture); detail.Controls.Add(content, 0, 1); layout.Controls.Add(detail, 0, 5);
            BuildActions(layout);
            status.Dock = DockStyle.Fill; status.AutoEllipsis = true; status.Font = new Font(Font.FontFamily, 8); status.ForeColor = Muted; status.TextAlign = ContentAlignment.MiddleLeft; layout.Controls.Add(status, 0, 7);
            var context = new ContextMenuStrip(); context.Items.Add("复制", null, delegate { UseSelected(false, true); }); context.Items.Add("以纯文本粘贴", null, delegate { UseSelected(true, false); }); context.Items.Add("收藏 / 取消收藏", null, delegate { TogglePin(); }); context.Items.Add("编辑 / 保存为片段", null, delegate { if (Selected != null) EditSnippet(Selected); }); context.Items.Add("删除", null, delegate { DeleteSelected(); }); TrackMenu(context); list.ContextMenuStrip = context;
            list.MouseDown += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Right) { int index = list.IndexFromPoint(e.Location); if (index >= 0) list.SelectedIndex = index; } };
        }
        void TogglePin() { var c = Selected; if (c != null) { c.Pinned = !c.Pinned; Changed(); } }
    }
}
