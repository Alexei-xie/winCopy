using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using WinCopy;
class GroupRegression {
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    [STAThread]static int Main(string[] args){string temp=Path.Combine(Path.GetTempPath(),"winCopy-groups-"+Guid.NewGuid().ToString("N"));try{
        var db=new Database();var first=new Clip {Snippet=true,Title="工作邮箱",Group="邮箱",Text="example@example.com"};var second=new Clip {Snippet=true,Title="签名",Group="工作",Text="谢谢"};db.Items.Add(first);db.Items.Add(second);db.Items.Add(new Clip{Group="普通历史",Text="history"});db.GroupOrder.AddRange(new[]{"工作","空分组","邮箱","工作"});db.Prune();
        Check(db.GetGroups().SequenceEqual(new[]{"工作","邮箱"}),"Only populated snippet groups, preserving order");
        using(var editor=new SnippetEditor(null,db.GetGroups())){
            var group=(ComboBox)typeof(SnippetEditor).GetField("group",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(editor);
            Check(group.DropDownStyle==ComboBoxStyle.DropDown,"Editable dropdown");Check(group.Items.Count==2,"Existing groups populated");group.SelectedItem="邮箱";Check(editor.GroupValue=="邮箱","Select existing group");group.Text="  新分组  ";Check(editor.GroupValue=="新分组","Type and trim a new group");Check(db.GetGroups().Length==2,"Unsaved names do not create groups");
            if(Array.IndexOf(args,"--ui")>=0){Application.EnableVisualStyles();editor.Show();Application.DoEvents();using(var b=new Bitmap(editor.Width,editor.Height)){editor.DrawToBitmap(b,new Rectangle(Point.Empty,editor.Size));b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"group-editor-preview.png"));}}
        }
        using(var editor=new SnippetEditor(first,db.GetGroups()))Check(editor.GroupValue=="邮箱","Edit retains original group");
        first.Group="新分组";db.Prune();Check(!db.GetGroups().Contains("邮箱")&&!db.GroupOrder.Contains("邮箱"),"Moving last snippet removes old group and order");
        db.Items.Remove(second);db.Prune();Check(db.GetGroups().SequenceEqual(new[]{"新分组"})&&!db.GroupOrder.Contains("工作"),"Deleting last snippet removes empty group");
        var store=new Store(temp);store.Save(db);Check(store.Load().GetGroups().SequenceEqual(new[]{"新分组"}),"Persisted empty-group cleanup");
        Console.WriteLine("PASS: editable dropdown, existing group order, new name input, edit preservation, no unsaved groups, remove/move last snippet, persisted cleanup.");return 0;
    }catch(Exception ex){Console.WriteLine("FAIL: "+ex);return 1;}finally{if(Directory.Exists(temp))Directory.Delete(temp,true);}}
}
