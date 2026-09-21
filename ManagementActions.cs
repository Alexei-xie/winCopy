using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WinCopy {
    public class BackupPasswordDialog : Form {
        readonly TextBox password=new TextBox {UseSystemPasswordChar=true,MaxLength=256},confirm=new TextBox {UseSystemPasswordChar=true,MaxLength=256};
        public string Password {get{return password.Text;}}
        public BackupPasswordDialog(bool creating){
            Text=creating?"设置备份密码":"输入备份密码";Font=new Font("Microsoft YaHei UI",10);ClientSize=new Size(480,creating?300:240);StartPosition=FormStartPosition.CenterParent;MinimizeBox=false;MaximizeBox=false;
            var root=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=creating?6:4};Controls.Add(root);
            var note=new Label {Text=creating?"备份当前历史、收藏、片段及分组排序（含本次会话历史）。\n密码至少 8 位，丢失后无法恢复。文件记录仅备份路径。":"密码用于解密备份。恢复会合并记录，保留当前设置。",AutoSize=true,MaximumSize=new Size(430,0)};root.Controls.Add(note);root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(new Label {Text="密码",AutoSize=true});root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.Controls.Add(Design.Input(password));root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
            if(creating){root.Controls.Add(new Label{Text="再次输入密码",AutoSize=true});root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.Controls.Add(Design.Input(confirm));root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));}
            var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,FlowDirection=FlowDirection.RightToLeft};var ok=new RoundedButton {Text="继续",Size=new Size(92,36)};ok.Click+=delegate{if(Password.Length<8||creating&&Password!=confirm.Text){MessageBox.Show(this,"密码至少 8 位，两次输入需一致。","备份密码");return;}DialogResult=DialogResult.OK;};footer.Controls.Add(ok);var cancel=new RoundedButton{Text="取消",DialogResult=DialogResult.Cancel,Size=new Size(92,36)};footer.Controls.Add(cancel);root.Controls.Add(footer);root.RowStyles.Add(new RowStyle(SizeType.AutoSize));AcceptButton=ok;CancelButton=cancel;Design.Dialog(this);
        }
    }
    public class CleanupDialog : Form {
        readonly DateTimePicker cutoff=new DateTimePicker {Format=DateTimePickerFormat.Short,Value=DateTime.Today.AddDays(-7)};
        public DateTime Before {get{return cutoff.Value.Date;}}
        public CleanupDialog(Database db){
            Text="按时间清理历史";Font=new Font("Microsoft YaHei UI",10);ClientSize=new Size(450,220);StartPosition=FormStartPosition.CenterParent;MinimizeBox=false;MaximizeBox=false;
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=4};Controls.Add(root);root.RowStyles.Add(new RowStyle(SizeType.Absolute,36));root.RowStyles.Add(new RowStyle(SizeType.Absolute,40));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
            root.Controls.Add(new Label{Text="删除早于以下日期 00:00 的普通历史：",AutoSize=true});root.Controls.Add(cutoff);var count=new Label{Dock=DockStyle.Fill};root.Controls.Add(count);Action update=delegate{count.Text="将清理 "+db.Items.Count(x=>!x.Snippet&&!x.Pinned&&x.Created<Before)+" 条记录。\n收藏、片段和系统剪贴板保留。此操作不可撤销。";};cutoff.ValueChanged+=delegate{update();};update();
            var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var ok=new RoundedButton{Text="确认清理",DialogResult=DialogResult.OK,Size=new Size(110,36)};var cancel=new RoundedButton{Text="取消",DialogResult=DialogResult.Cancel,Size=new Size(90,36)};footer.Controls.Add(ok);footer.Controls.Add(cancel);root.Controls.Add(footer);CancelButton=cancel;Design.Dialog(this);
        }
    }
    public partial class MainWindow {
        void CommitManagedData(Database next){
            if(storageBlocked)throw new IOException("当前数据存储不可用，未保存更改。");SnippetTemplates.ValidateKeys(next);store.Save(next);
            save.Stop();ClearUndo();db.Items=next.Items;db.GroupOrder=next.GroupOrder;db.SnippetOrder=next.SnippetOrder;shortcutSignature=null;SyncSnippetHotkeys();RefreshItems();
        }
        DialogResult ShowSnippetManager(SnippetManager manager,Form owner){manager.ShortcutAvailable=CheckSnippetShortcut;return manager.ShowDialog(owner);}
        void ManageSnippets(Form owner=null){try{using(var manager=new SnippetManager(db))if(ShowSnippetManager(manager,owner??this)==DialogResult.OK){var next=DataManagement.Clone(db);next.Items.RemoveAll(x=>x.Snippet);next.Items.AddRange(manager.Working.Items.Where(x=>x.Snippet));next.GroupOrder=manager.Working.GroupOrder;next.SnippetOrder=manager.Working.SnippetOrder;next.Prune();CommitManagedData(next);NotifyAction("片段管理更改已保存");}}catch(Exception ex){MessageBox.Show(this,"无法保存片段管理更改：\n"+ex.Message,"winCopy");}}
        void BackupData(Form owner){
            using(var file=new SaveFileDialog{Filter="winCopy 加密备份|*.wcbak",DefaultExt="wcbak",FileName="winCopy-"+DateTime.Now.ToString("yyyyMMdd-HHmm")+".wcbak"})if(file.ShowDialog(owner)==DialogResult.OK)using(var password=new BackupPasswordDialog(true))if(password.ShowDialog(owner)==DialogResult.OK){
                try{if(!String.Equals(Path.GetExtension(file.FileName),".wcbak",StringComparison.OrdinalIgnoreCase))throw new IOException("请使用 .wcbak 备份扩展名。");owner.UseWaitCursor=true;BackupArchive.Save(file.FileName,db,password.Password);MessageBox.Show(owner,"加密备份已保存。可在另一台电脑中使用该密码恢复。","winCopy");}catch(Exception ex){MessageBox.Show(owner,"备份失败：\n"+ex.Message,"winCopy");}finally{owner.UseWaitCursor=false;}
            }
        }
        void RestoreData(Form owner){using(var file=new OpenFileDialog{Filter="winCopy 加密备份|*.wcbak"})if(file.ShowDialog(owner)==DialogResult.OK)using(var password=new BackupPasswordDialog(false))if(password.ShowDialog(owner)==DialogResult.OK){
            try{owner.UseWaitCursor=true;var incoming=BackupArchive.Load(file.FileName,password.Password);owner.UseWaitCursor=false;
                if(MessageBox.Show(owner,"备份包含 "+incoming.Items.Count+" 条记录。\n将合并并去重，保留当前设置和存储目录；普通历史遵守当前保留天数及数量限制。\n新导入片段的独立快捷键需在本机重新配置。\n继续恢复？","恢复备份",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
                var next=DataManagement.Clone(db);DataManagement.Merge(next,incoming);CommitManagedData(next);MessageBox.Show(owner,"已合并备份，当前共 "+db.Items.Count+" 条记录。","winCopy");
            }catch(Exception ex){MessageBox.Show(owner,"恢复未完成，当前记录未被替换：\n"+ex.Message,"winCopy");}finally{owner.UseWaitCursor=false;}
        }}
        void CleanupData(Form owner){using(var dialog=new CleanupDialog(db))if(dialog.ShowDialog(owner)==DialogResult.OK)try{var next=DataManagement.Clone(db);int count=DataManagement.CleanBefore(next,dialog.Before);CommitManagedData(next);MessageBox.Show(owner,"已清理 "+count+" 条普通历史，收藏和片段已保留。","winCopy");}catch(Exception ex){MessageBox.Show(owner,"清理未完成：\n"+ex.Message,"winCopy");}}
    }
}
