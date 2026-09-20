using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WinCopy {
    public partial class SettingsDialog {
        void BuildSettings(Database db) {
            Font = new Font("Microsoft YaHei UI",10); AutoScaleDimensions = new SizeF(96,96); AutoScaleMode = AutoScaleMode.Dpi;
            Text = "winCopy · 偏好设置"; ClientSize = new Size(560,660); MinimumSize = new Size(480,420); MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;
            var root = new TableLayoutPanel { Dock=DockStyle.Fill, RowCount=2, ColumnCount=1, Padding=new Padding(16) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); root.RowStyles.Add(new RowStyle(SizeType.Percent,100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute,116)); Controls.Add(root);
            var scroll = new Panel { Dock=DockStyle.Fill, AutoScroll=true, Margin=Padding.Empty }; root.Controls.Add(scroll,0,0);
            var flow = new FlowLayoutPanel { AutoSize=true, AutoSizeMode=AutoSizeMode.GrowAndShrink, FlowDirection=FlowDirection.TopDown, WrapContents=false, Padding=new Padding(4,4,4,12), Margin=Padding.Empty }; scroll.Controls.Add(flow);
            flow.Controls.Add(new Label { Text="让复制更顺手", Font=new Font(Font.FontFamily,18,FontStyle.Bold), AutoSize=true, Margin=new Padding(0,0,0,18) });
            AddRow(flow,"历史上限（20–2000 条）",limit); limit.Value=Math.Max(20,Math.Min(2000,db.Limit)); AddRow(flow,"保留天数（1–365 天）",days); days.Value=Math.Max(1,Math.Min(365,db.RetentionDays));
            for(char c='A';c<='Z';c++) key.Items.Add(c.ToString()); key.SelectedItem=db.Hotkey; if(key.SelectedItem==null) key.SelectedItem="V"; AddRow(flow,"呼出快捷键：Ctrl + Alt +",key);
            foreach(var c in new[] {paste,images,remember,startup}) { c.AutoSize=true; c.Margin=new Padding(0,7,0,7); flow.Controls.Add(c); }
            paste.Checked=db.AutoPaste; images.Checked=db.CaptureImages; remember.Checked=db.RememberHistory;
            using(var run=Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run")) startup.Checked=run!=null&&run.GetValue("winCopy")!=null;
            flow.Controls.Add(new Label {Text="不记录以下应用（进程名，用英文逗号分隔）",AutoSize=true,Margin=new Padding(0,14,0,6)}); excluded.Text=db.ExcludedApps; var exclusionFrame=Design.Input(excluded); exclusionFrame.Tag="input"; exclusionFrame.Dock=DockStyle.None; flow.Controls.Add(exclusionFrame);
            flow.Controls.Add(new Label {Text="历史使用当前 Windows 账户加密。检查更新仅连接 GitHub。\n应用排除按复制时前台窗口识别；无法识别所有密码内容。",AutoSize=true,ForeColor=Color.DimGray,Margin=new Padding(0,12,0,14)});
            AddStorageSettings(flow);
            // Actions are outside the scrollable content and use preferred heights, including DPI scaling.
            var footer=new TableLayoutPanel {Dock=DockStyle.Fill,AutoSize=false,ColumnCount=1,RowCount=2,Margin=Padding.Empty,Padding=new Padding(0,12,0,0)};
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); footer.RowStyles.Add(new RowStyle(SizeType.Percent,50)); footer.RowStyles.Add(new RowStyle(SizeType.Percent,50)); root.Controls.Add(footer,0,1);
            var transfer=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoSize=false,WrapContents=false,Margin=Padding.Empty};
            var import=ActionButton("导入片段 XML"); import.Click+=delegate{if(ImportAction!=null) ImportAction();}; transfer.Controls.Add(import);
            var export=ActionButton("导出片段 XML"); export.Click+=delegate{if(ExportAction!=null) ExportAction();}; transfer.Controls.Add(export); var update=ActionButton("检查更新"); update.Click+=delegate{if(UpdateAction!=null)UpdateAction();}; transfer.Controls.Add(update); footer.Controls.Add(transfer,0,0);
            var actions=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoSize=false,FlowDirection=FlowDirection.RightToLeft,WrapContents=false,Margin=Padding.Empty};
            var ok=ActionButton("保存设置"); ok.DialogResult=DialogResult.OK; var cancel=ActionButton("取消"); cancel.DialogResult=DialogResult.Cancel; actions.Controls.Add(ok);actions.Controls.Add(cancel); footer.Controls.Add(actions,0,1); AcceptButton=ok;CancelButton=cancel;
            bool resizing=false;
            Action resize=delegate {
                if(resizing)return;resizing=true;flow.SuspendLayout();
                try {
                    int width=Math.Max(250,scroll.ClientSize.Width-SystemInformation.VerticalScrollBarWidth-16);
                    flow.MaximumSize=new Size(width,0);
                    foreach(Control c in flow.Controls) {
                        if(c is Panel) {
                            int height=Math.Max(44,Font.Height+22); foreach(Control child in c.Controls) height=Math.Max(height,child.Bottom+6);
                            c.MinimumSize=new Size(0,height);c.MaximumSize=new Size(width-16,height);c.Size=new Size(width-16,height);
                            foreach(Control child in c.Controls)if(!(child is Label)&&(string)c.Tag!="input")child.Left=Math.Max(200,c.Width-child.Width-4);
                        } else c.MaximumSize=new Size(Math.Max(1,width-16),0);
                    }
                } finally {flow.ResumeLayout(true);resizing=false;}            }; scroll.SizeChanged+=delegate{resize();}; Shown+=delegate{resize();}; Design.Dialog(this);
        }
        Button ActionButton(string text) {return new RoundedButton {Text=text,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(108,36),Padding=new Padding(12,5,12,5),Margin=new Padding(4,4,4,6)};}
    }
}
