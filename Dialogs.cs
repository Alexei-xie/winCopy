using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WinCopy {
    public class SnippetEditor : Form {
        TextBox title = new TextBox(), body = new TextBox();
        GroupPicker group = new GroupPicker();
        public string TitleValue { get { return title.Text.Trim(); } }
        public string GroupValue { get { return group.Text.Trim(); } }
        public string BodyValue { get { return body.Text; } }
        public SnippetEditor(Clip clip) : this(clip, new string[0]) { }
        public SnippetEditor(Clip clip, IEnumerable<string> existingGroups) {
            Text = clip != null && clip.Snippet ? "编辑片段" : "新建片段"; ClientSize = new Size(570, 520); MinimumSize = new Size(480, 440); StartPosition = FormStartPosition.CenterParent; Font = new Font("Microsoft YaHei UI", 10); MinimizeBox = false; MaximizeBox = false;
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 7 };
            foreach (int h in new[] { 28, 52, 28, 52, 28 }) grid.RowStyles.Add(new RowStyle(SizeType.Absolute, h)); grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); grid.RowStyles.Add(new RowStyle(SizeType.AutoSize)); Controls.Add(grid);
            grid.Controls.Add(new Label { Text = "片段名称", AutoSize = true }); title.Dock = DockStyle.Fill; title.MaxLength = 100; title.Text = clip == null ? "" : clip.Snippet ? clip.Title : clip.Text.Substring(0, Math.Min(24, clip.Text.Length)).Replace("\r", " ").Replace("\n", " "); grid.Controls.Add(Design.Input(title));
            grid.Controls.Add(new Label { Text = "分组", AutoSize = true }); group.Dock = DockStyle.Fill; group.MaxLength = 80;  group.AccessibleName = "分组，可选择已有分组或输入新名称"; foreach(var name in existingGroups.Where(x => !String.IsNullOrWhiteSpace(x)).Distinct()) group.Items.Add(name); group.Text = clip == null ? "常用" : clip.Group; grid.Controls.Add(group);
            grid.Controls.Add(new Label { Text = "内容", AutoSize = true }); body.Dock = DockStyle.Fill; body.Multiline = true; body.AcceptsReturn = true; body.ScrollBars = ScrollBars.Vertical; body.MaxLength = 1000000; body.Text = clip == null ? "" : clip.Text; grid.Controls.Add(Design.Input(body));
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, FlowDirection = FlowDirection.RightToLeft, Margin = Padding.Empty, Padding = new Padding(0, 12, 0, 4) }; var ok = new RoundedButton { Text = "保存", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(96, 36), Padding = new Padding(12, 5, 12, 5), Margin = new Padding(6, 0, 0, 0) }; ok.Click += delegate { if (TitleValue.Length == 0 || GroupValue.Length == 0 || BodyValue.Trim().Length == 0) { MessageBox.Show("请填写名称、分组和内容。"); return; } DialogResult = DialogResult.OK; }; buttons.Controls.Add(ok); var cancel = new RoundedButton { Text = "取消", DialogResult = DialogResult.Cancel, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(96, 36), Padding = new Padding(12, 5, 12, 5), Margin = new Padding(6, 0, 0, 0) }; buttons.Controls.Add(cancel); CancelButton = cancel; grid.Controls.Add(buttons); Design.Dialog(this);
        }
    }
    public partial class SettingsDialog : Form {
        NumericUpDown limit = new NumericUpDown { Minimum = 20, Maximum = 2000 }, days = new NumericUpDown { Minimum = 1, Maximum = 365 };
        ComboBox key = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        CheckBox paste = new ToggleSwitch { Text = "选择内容后自动粘贴到之前的应用" }, images = new ToggleSwitch { Text = "记录图片（单张最多 8 MB）" }, remember = new ToggleSwitch { Text = "退出后保留历史（收藏和片段始终保留）" }, startup = new ToggleSwitch { Text = "登录 Windows 时在托盘启动" };
        TextBox excluded = new TextBox();
        public Action ExportAction, ImportAction, UpdateAction;
        public int HistoryLimit { get { return (int)limit.Value; } }
        public int Days { get { return (int)days.Value; } }
        public string Shortcut { get { return (string)key.SelectedItem; } }
        public bool AutoPaste { get { return paste.Checked; } }
        public bool Images { get { return images.Checked; } }
        public bool Remember { get { return remember.Checked; } }
        public bool Startup { get { return startup.Checked; } }
        public string Excluded { get { return excluded.Text; } }
        public SettingsDialog(Database db) { BuildSettings(db); }
        void AddRow(FlowLayoutPanel flow, string caption, Control input) {
            var row = new Panel {Width=478,Height=44}; row.Controls.Add(new Label {Text=caption,AutoSize=true,Location=new Point(0,10)});
            var frame=new RoundedPanel {Location=new Point(328,2),Size=new Size(144,36),BackColor=Color.White,Radius=9};
            input.Location=new Point(10,6);input.Width=124;input.BackColor=Color.White;input.ForeColor=Design.Ink;
            var number=input as NumericUpDown;if(number!=null)number.BorderStyle=BorderStyle.None;
            var combo=input as ComboBox;if(combo!=null)combo.FlatStyle=FlatStyle.Flat;
            frame.Controls.Add(input);row.Controls.Add(frame);flow.Controls.Add(row);
        }
    }
}