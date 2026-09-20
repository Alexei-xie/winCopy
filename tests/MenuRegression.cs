using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WinCopy;
class MenuRegression {
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    [STAThread]static int Main(){try{
        Application.EnableVisualStyles();
        var db=new Database();
        for(int i=0;i<23;i++)db.Items.Add(new Clip {Text="History "+i,Created=DateTime.Now.AddMinutes(-i)});
        db.Items.Add(new Clip {Snippet=true,Title="Email address",Text="demo@example.com",Group="Personal"});
        db.Items.Add(new Clip {Snippet=true,Title="Meeting notes",Text="Agenda",Group="Work"});
        db.Items.Add(new Clip {Snippet=true,Title=new string('W',100),Text="Long title",Group="Personal"});
        db.GroupOrder.Add("Personal");db.GroupOrder.Add("Work");db.GroupOrder.Add("Empty");
        Clip chosen=null;
        using(var menu=ClipMenu.Build(db,c=>chosen=c)) {
            var folders=menu.Items.OfType<ToolStripMenuItem>().Where(x=>x.HasDropDownItems).ToArray();
            Check(folders.Length==5,"History pages and nonempty groups");
            Check(folders[0].Text=="1 – 10"&&folders[2].Text=="21 – 23","Page boundaries");
            Check(folders[0].DropDownItems.Count==10&&folders[2].DropDownItems.Count==3,"Page sizes");
            Check(folders[3].Text=="Personal"&&folders[4].Text=="Work","Group order");
            folders[0].DropDownItems[0].PerformClick();Check(chosen==db.Items[0],"History newest first and exact selection");
            var leaf=folders[3].DropDownItems.Cast<ToolStripItem>().First(x=>((Clip)x.Tag).Title=="Email address");leaf.PerformClick();Check(chosen.Text=="demo@example.com","Snippet resolves to content, not title");
            Check(folders[3].DropDownItems.Cast<ToolStripItem>().Any(x=>x.Text.EndsWith("…")),"Long title abbreviated");
            if(Environment.GetCommandLineArgs().Contains("--ui")) {
                menu.Row("清除历史…",()=>{});menu.Row("编辑片段…",()=>{});menu.Row("偏好设置…",()=>{});menu.Items.Add(new ToolStripSeparator());menu.Row("退出 winCopy",()=>{});
                menu.Show(new Point(80,80));Application.DoEvents();folders[3].Select();folders[3].ShowDropDown();Application.DoEvents();
                var child=folders[3].DropDown;child.Items[0].Select();
                using(var root=new Bitmap(menu.Width,menu.Height))using(var sub=new Bitmap(child.Width,child.Height))using(var output=new Bitmap(menu.Width+child.Width+12,Math.Max(menu.Height,child.Height+180))) {
                    menu.DrawToBitmap(root,new Rectangle(Point.Empty,root.Size));child.DrawToBitmap(sub,new Rectangle(Point.Empty,sub.Size));
                    using(var g=Graphics.FromImage(output)){g.Clear(Color.FromArgb(45,45,48));g.DrawImage(root,0,0);g.DrawImage(sub,menu.Width+6,180);}output.Save("menu-preview.png");
                }
                menu.Close();Check(!child.Visible,"Root dismissal closes children");
            }
        }
        using(var empty=ClipMenu.Build(new Database(),c=>{}))Check(!empty.Items.OfType<ToolStripMenuItem>().Any(x=>x.HasDropDownItems),"Empty state has no empty folders");
        Console.WriteLine("PASS: history paging, order, group order, exact leaf actions, long labels, empty state and menu disposal.");return 0;
    }catch(Exception ex){Console.WriteLine(ex);return 1;}}
}
