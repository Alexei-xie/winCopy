using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WinCopy {
    public partial class MainWindow {
        readonly GroupStrip groupStrip = new GroupStrip();
        Button sectionSwitch;
        readonly Label sectionLabel = new Label();
        bool movingPopup;
        Point moveStart, windowStart;
        void BuildGroupStrip(TableLayoutPanel layout) {
            var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
            groupStrip.Dock = DockStyle.Fill; groupStrip.Margin = new Padding(0,0,6,0); groupStrip.Font = new Font(Font.FontFamily, 9); groupStrip.BackColor = BackColor; groupStrip.ForeColor = Ink;
            groupStrip.Selected += delegate(string name) { if (view == "常用片段") { loading = true; groups.SelectedItem = name; loading = false; } else view = name; RefreshItems(); };
            groupStrip.Reordered += delegate(string[] names) { db.GroupOrder = names.Where(x => x != "所有分组").ToList(); save.Stop(); save.Start(); };
            sectionSwitch = CompactButton("常用片段", delegate { view = view == "常用片段" ? "全部历史" : "常用片段"; RefreshItems(); }, 82);
            row.Controls.Add(groupStrip,0,0); row.Controls.Add(sectionSwitch,1,0); layout.Controls.Add(row,0,2);
            sectionLabel.Dock = DockStyle.Fill; sectionLabel.ForeColor = Muted; sectionLabel.TextAlign = ContentAlignment.MiddleLeft; layout.Controls.Add(sectionLabel,0,3);
            groups.DropDownStyle = ComboBoxStyle.DropDownList; // Retains filtering state, never displayed.
        }
        void SyncGroupStrip() {
            bool snippets = view == "常用片段"; sectionSwitch.Text = snippets ? "返回历史" : "常用片段"; groupStrip.ReorderEnabled = snippets;
            sectionLabel.Text = snippets ? "片段分组 · 拖拽排序，滚轮左右浏览" : "最近复制";
            if (snippets) {
                var available = db.Items.Where(x => x.Snippet).Select(x => x.Group).Distinct().ToList();
                var ordered = db.GroupOrder.Where(available.Contains).Concat(available.Where(x => !db.GroupOrder.Contains(x)).OrderBy(x => x)).Distinct();
                groupStrip.SetItems(new[] { "所有分组" }.Concat(ordered), groups.SelectedItem as string ?? "所有分组");
            } else groupStrip.SetItems(new[] { "全部历史", "收藏", "文本", "图片", "文件" }, view);
        }
        void EnableWindowDrag(Control control) {
            control.MouseDown += delegate(object s, MouseEventArgs e) { if (e.Button != MouseButtons.Left) return; movingPopup = true; moveStart = Cursor.Position; windowStart = Location; control.Capture = true; };
            control.MouseMove += delegate { if (movingPopup && control.Capture) { var p = Cursor.Position; Location = new Point(windowStart.X + p.X-moveStart.X, windowStart.Y + p.Y-moveStart.Y); } };
            control.MouseUp += delegate(object s, MouseEventArgs e) { if (!movingPopup || e.Button != MouseButtons.Left) return; movingPopup = false; control.Capture = false; var area = Screen.FromRectangle(Bounds).WorkingArea; Location = ClampPosition(Location,area); db.PopupPositionSet = true; db.PopupX = Left; db.PopupY = Top; save.Stop(); save.Start(); };
            control.MouseCaptureChanged += delegate { if (!control.Capture) movingPopup = false; };
        }
        Point ClampPosition(Point point, Rectangle area) { return new Point(Math.Max(area.Left, Math.Min(point.X, area.Right-Width)), Math.Max(area.Top, Math.Min(point.Y,area.Bottom-Height))); }
    }
}
