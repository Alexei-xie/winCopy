using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using WinCopy;
class MacRegression {
    static void Check(bool yes,string message){if(!yes)throw new Exception(message);}
    static void Mouse(Control c,string method,int x,int y){c.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(c,new object[]{new MouseEventArgs(MouseButtons.Left,1,x,y,0)});}
    static void Shot(Control c,string name){using(var b=new Bitmap(c.Width,c.Height)){c.DrawToBitmap(b,new Rectangle(Point.Empty,c.Size));b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,name));}}
    [STAThread]static int Main(){Point cursor=Cursor.Position;try{
        Application.EnableVisualStyles();
        var db=new Database();db.Items.Add(new Clip {Snippet=true,Title="邮件签名",Group="工作",Text="示例签名"});db.Items.Add(new Clip {Text="一条示例历史"});
        using(var menu=ClipMenu.Build(db,c=>{})){
            Point saved=Point.Empty;menu.PositionMoved+=p=>saved=p;
            menu.Row("编辑片段…",()=>{});menu.Row("偏好设置…",()=>{});
            menu.Show(new Point(100,100));Application.DoEvents();
            var host=menu.Items.OfType<ToolStripControlHost>().First();var bar=(MacTitleBar)host.Control;
            var before=menu.Location;Mouse(bar,"OnMouseDown",90,20);Check(!menu.AutoClose,"Menu stays open while dragging");Cursor.Position=new Point(cursor.X+35,cursor.Y+25);Mouse(bar,"OnMouseMove",125,45);Mouse(bar,"OnMouseUp",125,45);
            Check(menu.Visible&&menu.AutoClose,"Drag ends without dismissing menu");Check(menu.Location!=before&&saved==menu.Location,"Position callback follows drag");
            var folder=menu.Items.OfType<ToolStripMenuItem>().First(x=>x.HasDropDownItems);folder.ShowDropDown();Application.DoEvents();Check(folder.DropDown.Visible,"Submenu opens after move");folder.HideDropDown();Shot(menu,"mac-menu.png");
            bar.Controls.OfType<Button>().First().PerformClick();Check(!menu.Visible,"Red button closes menu");
        }
        using(var editor=new SnippetEditor(null)){
            editor.Show();Application.DoEvents();Check(editor.FormBorderStyle==FormBorderStyle.None,"Editor has custom chrome");var bar=editor.Controls.OfType<MacTitleBar>().Single();Check(editor.Controls.OfType<TableLayoutPanel>().Single().Top>=bar.Bottom,"Title reserves content space");Shot(editor,"mac-editor.png");bar.Controls.OfType<Button>().First().PerformClick();Check(!editor.Visible,"Editor close");
        }
        using(var settings=new SettingsDialog(new Database())){settings.Show();Application.DoEvents();Check(settings.Controls.OfType<MacTitleBar>().Any(),"Settings chrome");Shot(settings,"mac-settings.png");settings.Close();}
        using(var update=new UpdateDialog()){Check(update.FormBorderStyle==FormBorderStyle.None&&update.Controls.OfType<MacTitleBar>().Any(),"Update chrome without fetching network");}
        using(var timer=new Timer {Interval=100}) {
            timer.Tick+=delegate{var prompt=Application.OpenForms.Cast<Form>().FirstOrDefault(f=>f.Text=="测试确认");if(prompt==null||prompt.Text!="测试确认")return;timer.Stop();Shot(prompt,"mac-confirm.png");((Button)prompt.CancelButton).PerformClick();};timer.Start();
            Check(MacMessage.Show("这是用于检查布局的示例提示。\n取消后不执行操作。","测试确认",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)==DialogResult.No,"Confirmation cancel is non-destructive");
        }
        db.MenuPositionSet=true;db.MenuX=123;db.MenuY=234;
        string path=Path.Combine(Path.GetTempPath(),"winCopy-mac-"+Guid.NewGuid().ToString("N"));try{var store=new Store(path);store.Save(db);var loaded=store.Load();Check(loaded.MenuPositionSet&&loaded.MenuX==123&&loaded.MenuY==234,"Position persists");}finally{Directory.Delete(path,true);}
        Console.WriteLine("PASS: menu dragging, capture, position callback, reopening children, logo titlebars, closing, dialog chrome and position persistence.");return 0;
    }catch(Exception ex){Console.WriteLine(ex);return 1;}finally{Cursor.Position=cursor;}}
}
