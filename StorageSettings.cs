using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinCopy {
    public partial class SettingsDialog {
        readonly TextBox storagePath=new TextBox();
        public string StorageDirectory {get{return storagePath.Text; } set{storagePath.Text=value;}}
        void AddStorageSettings(FlowLayoutPanel flow,Database db) {
            flow.Controls.Add(new Label {Text="存储位置",AutoSize=true,Font=new Font(Font,FontStyle.Bold),Margin=new Padding(0,18,0,8)});
            var usage=new Label {AutoSize=true,Margin=new Padding(0,0,0,10)};
            var currentDirectory=storagePath.Text;
            Shown+=delegate{currentDirectory=storagePath.Text;usage.Text=StorageUsage.Describe(db,currentDirectory);};
            var refresh=ActionButton("刷新占用统计");refresh.Click+=delegate{usage.Text=StorageUsage.Describe(db,currentDirectory);};
            flow.Controls.Add(usage);flow.Controls.Add(refresh);
            storagePath.ReadOnly=true;storagePath.Text=StorageLocation.DefaultDirectory;
            var frame=Design.Input(storagePath);frame.Tag="input";frame.Dock=DockStyle.None;flow.Controls.Add(frame);
            var choose=ActionButton("更改存储目录…");
            choose.Click+=delegate{using(var picker=new FolderBrowserDialog {Description="选择 winCopy 数据存储目录。保存设置后迁移；目标目录不能已有 history.dat。",SelectedPath=storagePath.Text,ShowNewFolderButton=true}){if(picker.ShowDialog(this)==DialogResult.OK)storagePath.Text=picker.SelectedPath;}};
            flow.Controls.Add(choose);
            flow.Controls.Add(new Label {Text="保存设置后迁移历史、收藏、片段及设置。\n原目录保留恢复副本，不会被删除或继续更新。\n路径配置仍保存在默认目录，数据仍由当前账户加密。",AutoSize=true,ForeColor=Design.Muted,Margin=new Padding(0,6,0,12)});
        }
    }
}
