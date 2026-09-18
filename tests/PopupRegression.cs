using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using WinCopy;

class PopupRegression {
    static BindingFlags F = BindingFlags.Instance|BindingFlags.NonPublic;
    static object Field(object o,string name) { return o.GetType().GetField(name,F).GetValue(o); }
    static void Set(object o,string name,object value) { o.GetType().GetField(name,F).SetValue(o,value); }
    static void Invoke(object o,string name,params object[] args) { o.GetType().GetMethod(name,F).Invoke(o,args); }
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    static void Shot(Form form,string name) { using(var b=new Bitmap(form.Width,form.Height)) {form.DrawToBitmap(b,new Rectangle(Point.Empty,form.Size)); b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,name));} }
    static System.Collections.Generic.IEnumerable<Control> Desc(Control root) { foreach(Control c in root.Controls) {yield return c; foreach(var child in Desc(c)) yield return child;} }
    static void CheckButtons(Form f) {
        foreach(var input in Desc(f).Where(x => x is NumericUpDown || x is ComboBox)) Check(input.Parent.Height >= input.Bottom, "Settings input clipped");
        var buttons=Desc(f).OfType<Button>().Where(x=>x.Text.Contains("XML")||x.Text=="取消"||x.Text=="保存设置").ToList(); Check(buttons.Count==4,"Missing footer actions");
        foreach(var b in buttons) {var r=f.RectangleToClient(b.RectangleToScreen(b.ClientRectangle)); Check(f.ClientRectangle.Contains(r),"Button outside settings: "+b.Text); Check(b.Height>=32,"Button height too small"); var p=b.Parent; while(p!=null&&p!=f) {Check(p.ClientRectangle.Contains(p.RectangleToClient(b.RectangleToScreen(b.ClientRectangle))),"Button clipped by parent: "+b.Text);p=p.Parent;} }
    }
    [STAThread] static int Main() {
        Application.EnableVisualStyles(); MainWindow main=null; SettingsDialog settings=null; Point cursor=Cursor.Position;
        var results=new System.Collections.Generic.List<string>();
        try {
            main=new MainWindow(false); Set(main,"storageBlocked",true);
            var db=new Database {Hotkey="J"}; foreach(var group in new[]{"邮箱","工作","常用回复","链接收藏","代码片段","其他"}) db.Items.Add(new Clip{Snippet=true,Title=group+"示例",Group=group,Text="这是用于界面验证的示例内容。"});
            db.GroupOrder.AddRange(new[]{"邮箱","工作","常用回复","链接收藏","代码片段","其他"}); Set(main,"db",db); Set(main,"view","常用片段"); Invoke(main,"RefreshItems"); main.Show(); Application.DoEvents();
            Check(!((ComboBox)Field(main,"groups")).Visible,"Legacy group dropdown still visible");
            var strip=(GroupStrip)Field(main,"groupStrip");
            Func<string,int> width=s=>(int)typeof(GroupStrip).GetMethod("ItemWidth",F).Invoke(strip,new object[]{s});
            int all=width("所有分组")+4, first=width("邮箱")+4;
            Invoke(strip,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,all+first+8,15,0));
            Invoke(strip,"OnMouseMove",new MouseEventArgs(MouseButtons.Left,0,all+2,15,0));
            Invoke(strip,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,all+2,15,0));
            Check(db.GroupOrder[0]=="工作","Group drag did not reorder"); results.Add("PASS: drag group left and persist order in model");
            Invoke(main,"RefreshItems");
            Invoke(strip,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,all+8,15,0)); Invoke(strip,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,all+8,15,0));
            Check(((ListBox)Field(main,"list")).Items.Count==1&&((Clip)((ListBox)Field(main,"list")).Items[0]).Group=="工作","Group filter failed"); results.Add("PASS: expanded group selection filters snippets");
            var label=Desc(main).OfType<Label>().First(x=>x.Text=="winCopy");
            Invoke(label,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,10,10,0)); Cursor.Position=new Point(cursor.X+25,cursor.Y+20); Invoke(label,"OnMouseMove",new MouseEventArgs(MouseButtons.Left,0,35,30,0)); Invoke(label,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,35,30,0));
            Check(db.PopupPositionSet,"Popup drag position not saved"); Point saved=main.Location; Invoke(main,"PositionPopup"); Check(main.Location==saved,"Popup position reset on reopen"); results.Add("PASS: title drag and retained popup position");
            string path=Path.Combine(Path.GetTempPath(),"winCopy-layout-"+Guid.NewGuid().ToString("N")); try {var store=new Store(path);store.Save(db);var restored=store.Load();Check(restored.GroupOrder.SequenceEqual(db.GroupOrder)&&restored.PopupPositionSet&&restored.PopupX==db.PopupX,"Layout persistence failed");} finally {if(Directory.Exists(path))Directory.Delete(path,true);} results.Add("PASS: encrypted roundtrip preserves group order and position");
            Shot(main,"groups-preview.png"); settings=new SettingsDialog(db); settings.Show(main);Application.DoEvents();CheckButtons(settings);Shot(settings,"settings-preview.png");
            settings.ClientSize=new Size(480,420);Application.DoEvents();CheckButtons(settings);Shot(settings,"settings-small-preview.png");
            settings.Scale(new SizeF(1.5f,1.5f));Application.DoEvents();CheckButtons(settings);results.Add("PASS: all four footer actions visible at normal, small and 150% scaled layouts");
            return 0;
        }catch(Exception ex){results.Add("FAIL: "+ex);return 1;}
        finally{Cursor.Position=cursor;if(settings!=null)settings.Dispose();if(main!=null){Set(main,"quitting",true);main.Close();main.Dispose();}File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"popup-regression.txt"),results);}
    }
}
