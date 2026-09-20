using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
namespace WinCopy {
    public partial class SettingsDialog {
        readonly Dictionary<string,Panel> pages=new Dictionary<string,Panel>();
        readonly Dictionary<string,Button> sections=new Dictionary<string,Button>();
        public void ShowSection(string name){if(!pages.ContainsKey(name))return;foreach(var pair in pages)pair.Value.Visible=pair.Key==name;foreach(var pair in sections){pair.Value.BackColor=pair.Key==name?Design.Accent:Color.White;pair.Value.ForeColor=pair.Key==name?Color.White:Design.Ink;}pages[name].BringToFront();}
        FlowLayoutPanel Page(Control host,string name){
            var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Visible=false};pages.Add(name,scroll);host.Controls.Add(scroll);
            var flow=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(4,10,4,12),Margin=Padding.Empty};scroll.Controls.Add(flow);
            bool resizing=false;Action resize=delegate{if(resizing)return;resizing=true;flow.SuspendLayout();try{int width=Math.Max(300,scroll.ClientSize.Width-SystemInformation.VerticalScrollBarWidth-12);flow.MaximumSize=new Size(width,0);
                foreach(Control c in flow.Controls){if(c is Panel){int h=Math.Max(44,Font.Height+22);foreach(Control child in c.Controls)h=Math.Max(h,child.Bottom+6);c.MinimumSize=new Size(0,h);c.MaximumSize=new Size(width-12,h);c.Size=new Size(width-12,h);foreach(Control child in c.Controls)if(!(child is Label)&&(string)c.Tag!="input")child.Left=Math.Max(185,c.Width-child.Width-4);}else c.MaximumSize=new Size(width-12,0);}
            }finally{flow.ResumeLayout(true);resizing=false;}};
            scroll.SizeChanged+=delegate{resize();};scroll.VisibleChanged+=delegate{resize();};Shown+=delegate{resize();};return flow;
        }
        void PageAction(Control flow,string text,Action action){var b=ActionButton(text);b.Click+=delegate{if(action!=null)action();};flow.Controls.Add(b);}
        void BuildSettings(Database db){
            Font=new Font("Microsoft YaHei UI",10);AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;Text="winCopy · 偏好设置";ClientSize=new Size(570,610);MinimumSize=new Size(480,420);MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(16),RowCount=3,ColumnCount=1};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(root);
            var nav=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,WrapContents=true,Margin=Padding.Empty};root.Controls.Add(nav);var host=new Panel{Dock=DockStyle.Fill};root.Controls.Add(host);
            foreach(var name in new[]{"常规","记录","存储","数据","关于"}){var captured=name;var b=new RoundedButton{Text=name,Size=new Size(78,36),Margin=new Padding(0,0,6,6)};b.Click+=delegate{ShowSection(captured);};sections.Add(name,b);nav.Controls.Add(b);}
            var general=Page(host,"常规");for(char c='A';c<='Z';c++)key.Items.Add(c.ToString());key.SelectedItem=db.Hotkey;if(key.SelectedIndex<0)key.SelectedItem="V";AddRow(general,"呼出快捷键：Ctrl + Alt +",key);
            foreach(var c in new[]{paste,startup}){c.AutoSize=true;c.Margin=new Padding(0,8,0,8);general.Controls.Add(c);}paste.Checked=db.AutoPaste;
            using(var run=Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run"))startup.Checked=run!=null&&run.GetValue("winCopy")!=null;
            general.Controls.Add(new Label{Text="Enter 粘贴 · Ctrl + Enter 仅复制 · Shift + Enter 纯文本\nEsc 收起 · 图片预览点击放大",AutoSize=true,Margin=new Padding(0,18,0,8)});
            var records=Page(host,"记录");AddRow(records,"历史上限（20–2000 条）",limit);limit.Value=Math.Max(20,Math.Min(2000,db.Limit));AddRow(records,"保留天数（1–365 天）",days);days.Value=Math.Max(1,Math.Min(365,db.RetentionDays));
            foreach(var c in new[]{images,remember}){c.AutoSize=true;c.Margin=new Padding(0,8,0,8);records.Controls.Add(c);}images.Checked=db.CaptureImages;remember.Checked=db.RememberHistory;
            records.Controls.Add(new Label{Text="不记录以下应用（进程名，用英文逗号分隔）",AutoSize=true,Margin=new Padding(0,14,0,6)});excluded.Text=db.ExcludedApps;var frame=Design.Input(excluded);frame.Tag="input";frame.Dock=DockStyle.None;records.Controls.Add(frame);
            records.Controls.Add(new Label{Text="应用排除按复制时前台窗口识别，不能识别所有密码内容。",AutoSize=true,Margin=new Padding(0,10,0,6)});
            PageAction(records,"按时间清理历史…",delegate{if(CleanupAction!=null)CleanupAction();});records.Controls.Add(new Label{Text="清理确认后立即生效，取消设置不会撤销清理。",AutoSize=true});
            var storage=Page(host,"存储");AddStorageSettings(storage,db);
            var data=Page(host,"数据");data.Controls.Add(new Label{Text="以下操作即时生效，不依赖底部的保存设置。",AutoSize=true,Margin=new Padding(0,6,0,14)});
            PageAction(data,"管理片段 / 分组…",delegate{if(ManageAction!=null)ManageAction();});PageAction(data,"创建加密备份…",delegate{if(BackupAction!=null)BackupAction();});PageAction(data,"从加密备份恢复…",delegate{if(RestoreAction!=null)RestoreAction();});
            data.Controls.Add(new Label{Text="加密备份可跨电脑恢复，包含历史、收藏和片段。\n恢复合并并去重，保留当前设置。\n文件仅备份路径，原文件需自行迁移。\n明文 XML 仅用于片段交换。",AutoSize=true,Margin=new Padding(0,10,0,10)});
            PageAction(data,"导入片段 XML",delegate{if(ImportAction!=null)ImportAction();});PageAction(data,"导出片段 XML",delegate{if(ExportAction!=null)ExportAction();});
            var about=Page(host,"关于");about.Controls.Add(new Label{Text="winCopy "+UpdateService.CurrentVersion.ToString(3),Font=new Font(Font.FontFamily,18,FontStyle.Bold),AutoSize=true});about.Controls.Add(new Label{Text="轻量本地剪贴板工具\n历史使用当前 Windows 账户加密。\n检查更新仅连接 GitHub，不上传剪贴板内容。",AutoSize=true,Margin=new Padding(0,16,0,16)});PageAction(about,"检查更新",delegate{if(UpdateAction!=null)UpdateAction();});
            var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,FlowDirection=FlowDirection.RightToLeft,WrapContents=false,Margin=Padding.Empty};var ok=ActionButton("保存设置");ok.DialogResult=DialogResult.OK;var cancel=ActionButton("取消");cancel.DialogResult=DialogResult.Cancel;actions.Controls.Add(ok);actions.Controls.Add(cancel);root.Controls.Add(actions);AcceptButton=ok;CancelButton=cancel;Design.Dialog(this);ShowSection("常规");
        }
        Button ActionButton(string text){return new RoundedButton{Text=text,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(108,36),Padding=new Padding(12,5,12,5),Margin=new Padding(4,4,4,6)};}
    }
}
