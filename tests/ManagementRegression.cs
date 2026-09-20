using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using WinCopy;
class ManagementRegression {
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    static void Reject(Action action,string why){try{action();}catch{ return;}throw new Exception(why);}
    static System.Collections.Generic.IEnumerable<Control> All(Control c){foreach(Control x in c.Controls){yield return x;foreach(var child in All(x))yield return child;}}
    static void Shot(Form f,string name){using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,f.Size));b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,name));}}
    [STAThread]static int Main(){string path=Path.Combine(Path.GetTempPath(),"winCopy-management-"+Guid.NewGuid().ToString("N"));try{
        Application.EnableVisualStyles();var db=new Database {RetentionDays=365};var a=new Clip{Snippet=true,Title="工作签名",Group="工作",Text="示例签名"};var b=new Clip{Snippet=true,Title="常用地址",Group="工作",Text="示例地址"};var c=new Clip{Snippet=true,Title="问候",Group="个人",Text="你好"};db.Items.AddRange(new[]{a,b,c});db.GroupOrder.AddRange(new[]{"工作","个人"});
        DataManagement.OrderGroup(db,"工作",new[]{b.Id,a.Id});Check(db.OrderedSnippets()[0]==b,"Group order");Reject(()=>DataManagement.OrderGroup(db,"工作",new[]{a.Id}),"Reject incomplete order");
        var clone=DataManagement.Clone(db);clone.Items[0].Text="Changed";Check(a.Text!="Changed","Manager snapshot independent");
        DataManagement.RenameGroup(clone,"工作","个人");Check(clone.GetGroups().Length==1&&clone.Items.All(x=>x.Group=="个人"),"Rename merges and removes empty group");
        var encrypted=BackupArchive.Encrypt(db,"test-password-123");var decrypted=BackupArchive.Decrypt(encrypted,"test-password-123");Check(decrypted.OrderedSnippets()[0].Id==b.Id,"Encrypted backup preserves order");
        Reject(()=>BackupArchive.Decrypt(encrypted,"wrong-password"),"Wrong password rejected");var bad=(byte[])encrypted.Clone();bad[45]^=1;Reject(()=>BackupArchive.Decrypt(bad,"test-password-123"),"Tampering rejected");Reject(()=>BackupArchive.Decrypt(new byte[12],"test-password-123"),"Truncated backup rejected");
        Directory.CreateDirectory(path);string archive=Path.Combine(path,"sample.wcbak");BackupArchive.Save(archive,db,"test-password-123");BackupArchive.Save(archive,db,"test-password-123");Check(BackupArchive.Load(archive,"test-password-123").Items.Count==3,"Atomic backup overwrite");
        var target=new Database {Hotkey="K",RetentionDays=365};DataManagement.Merge(target,decrypted);DataManagement.Merge(target,BackupArchive.Decrypt(encrypted,"test-password-123"));Check(target.Items.Count==3&&target.Hotkey=="K","Restore dedupe and settings preserved");
        target.Items.Add(new Clip {Created=DateTime.Today.AddDays(-10),Text="old"});target.Items.Add(new Clip {Created=DateTime.Today.AddDays(-10),Pinned=true,Text="favorite"});target.Items.Add(new Clip {Created=DateTime.Today,Text="recent"});Check(DataManagement.CleanBefore(target,DateTime.Today.AddDays(-7))==1&&target.Items.Any(x=>x.Pinned)&&target.Items.Count(x=>x.Snippet)==3,"Cleanup preserves favorites, snippets and new history");
        var store=new Store(path);store.Save(db);Check(store.Load().OrderedSnippets()[0].Id==b.Id,"Persist custom snippet order");
        using(var manager=new SnippetManager(db)){manager.Show();Application.DoEvents();Shot(manager,"manager-preview.png");var rows=All(manager).OfType<ListView>().Single();rows.Items[0].Selected=true;All(manager).OfType<Button>().First(x=>x.Text=="下移").PerformClick();Check(manager.Working.OrderedSnippets()[1].Id==b.Id,"UI selected reorder");manager.Close();Check(db.OrderedSnippets()[0].Id==b.Id,"Cancel leaves source untouched");}
        using(var settings=new SettingsDialog(db)){settings.Show();foreach(var section in new[]{"常规","记录","存储","数据","关于"}){settings.ShowSection(section);Application.DoEvents();foreach(var button in All(settings).OfType<Button>().Where(x=>x.Text=="保存设置"||x.Text=="取消"))Check(settings.ClientRectangle.Contains(settings.RectangleToClient(button.RectangleToScreen(button.ClientRectangle))),"Fixed settings actions stay inside window");if(section=="数据")Shot(settings,"management-settings.png");}settings.Size=settings.MinimumSize;settings.ShowSection("记录");Application.DoEvents();Shot(settings,"settings-minimum.png");settings.Close();}
        using(var prompt=new BackupPasswordDialog(true)){prompt.Show();Application.DoEvents();Shot(prompt,"backup-password.png");prompt.Close();}
        using(var main=new MainWindow(false)){
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;typeof(MainWindow).GetField("storageBlocked",flags).SetValue(main,false);typeof(MainWindow).GetField("db",flags).SetValue(main,db);
            string blocked=Path.Combine(path,"blocked");File.WriteAllText(blocked,"not a directory");typeof(MainWindow).GetField("store",flags).SetValue(main,new Store(blocked));
            var next=DataManagement.Clone(db);next.Items.Clear();Reject(()=>typeof(MainWindow).GetMethod("CommitManagedData",flags).Invoke(main,new object[]{next}),"Failed storage commit must fail");Check(db.Items.Count==3,"Failed commit preserves live records");typeof(MainWindow).GetField("storageBlocked",flags).SetValue(main,true);
        }
        Console.WriteLine("PASS: group order and merge, independent edits, authenticated backup, wrong password/tamper rejection, atomic backup, restore dedupe, cleanup scope, saved ordering and settings categories.");return 0;
    }catch(Exception ex){Console.WriteLine(ex);return 1;}finally{if(Directory.Exists(path)){foreach(var file in Directory.GetFiles(path))File.Delete(file);Directory.Delete(path);}}}
}
