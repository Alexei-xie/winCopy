using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using WinCopy;
class UsabilityRegression {
    static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool value,string why){if(!value)throw new Exception(why);}
    static object Field(object target,string name){return target.GetType().GetField(name,flags).GetValue(target);}
    static void Set(object target,string name,object value){target.GetType().GetField(name,flags).SetValue(target,value);}
    static void Call(object target,string name,params object[] args){target.GetType().GetMethod(name,flags).Invoke(target,args);}
    static System.Collections.Generic.IEnumerable<Control> All(Control c){foreach(Control child in c.Controls){yield return child;foreach(var other in All(child))yield return other;}}
    static void Shot(Form f,string file){using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,f.Size));b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,file));}}
    [STAThread]static int Main(){MainWindow main=null;try{
        Application.EnableVisualStyles();var now=DateTime.UtcNow;var clip=new Clip {Text="Undo this"};var db=new Database();var undo=new DeleteUndo();undo.Remember(clip,now);Check(undo.Restore(db,now.AddSeconds(9))==clip&&db.Items.Contains(clip),"Restore within timeout");
        db.Items.Clear();undo.Remember(clip,now);Check(undo.Restore(db,now.AddSeconds(11))==null&&db.Items.Count==0,"Expiry blocks restore");
        var duplicate=new Clip {Text=clip.Text};db.Items.Add(duplicate);undo.Remember(clip,now);Check(undo.Restore(db,now)==duplicate&&db.Items.Count==1,"Avoid duplicate on fresh clipboard capture");
        undo.Remember(clip,now);undo.Clear();Check(!undo.Available(now),"Clear invalidates undo");
        byte[] png;using(var b=new Bitmap(120,60)){using(var g=Graphics.FromImage(b)){g.Clear(Color.MediumSlateBlue);g.FillEllipse(Brushes.Gold,25,5,50,50);}using(var ms=new MemoryStream()){b.Save(ms,ImageFormat.Png);png=ms.ToArray();}}
        using(var cache=new ThumbnailCache()){
            var first=new Clip {Image=png};var image=cache.Get(first);Check(image.Width==64&&image.Height==64,"Bounded thumbnail resolution");Check(Object.ReferenceEquals(image,cache.Get(first)),"Cache reuse");
            for(int i=0;i<200;i++)cache.Get(new Clip {Image=png});Check(cache.Count==64,"Bounded thumbnail cache");cache.Retain(new Clip[0]);Check(cache.Count==0,"Deleted images release cache");Check(cache.Get(new Clip{Image=new byte[]{0,1,2}})==null,"Corrupt image fallback");
        }
        Check(SearchPresentation.Excerpt(new string('x',2000)+"TARGET tail","target").Contains("TARGET"),"Deep case insensitive match excerpt");
        main=new MainWindow(false);Set(main,"storageBlocked",true);var data=new Database {Hotkey="J",Limit=2000};
        var photo=new Clip {Kind="图片",Image=png,Text="120 × 60",Source="Demo"};data.Items.Add(photo);
        data.Items.Add(new Clip {Text="A demo sentence with demo repeated",Source="Demo"});data.Items.Add(new Clip {Text=new string('x',600)+"demo matching deep text",Source="Notes"});
        Set(main,"db",data);Call(main,"RefreshItems");main.Show();Application.DoEvents();
        var list=(ListBox)Field(main,"list");list.SelectedItem=photo;Call(main,"DeleteSelected");Check(!data.Items.Contains(photo)&&((LinkLabel)Field(main,"undoLink")).Visible,"UI delete offers undo");Call(main,"UndoDeletion");Check(data.Items.Contains(photo)&&!((LinkLabel)Field(main,"undoLink")).Visible,"UI undo restores image");
        list.SelectedItem=photo;Call(main,"TogglePin");Check(photo.Pinned&&((Button)Field(main,"pinAction")).Text=="已收藏","Favorite state feedback");
        ((TextBox)Field(main,"search")).Text="demo";Application.DoEvents();Check(list.Items.Count==3,"Source and deep text search");Shot(main,"usability-preview.png");
        using(var settings=new SettingsDialog(data)){
            settings.StorageDirectory=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"missing-usage-folder");settings.Show();Application.DoEvents();var label=All(settings).OfType<Label>().First(x=>x.Text.StartsWith("当前数据文件"));Check(label.Text.Contains("尚未保存")&&label.Text.Contains("图片 1 张"),"Storage stats scope");
            var scroll=All(settings).OfType<Panel>().First(x=>x.AutoScroll);scroll.ScrollControlIntoView(label);Application.DoEvents();Shot(settings,"usage-preview.png");settings.Close();
        }
        for(int i=0;i<1997;i++)data.Items.Add(new Clip{Text="Load test "+i});var watch=System.Diagnostics.Stopwatch.StartNew();((TextBox)Field(main,"search")).Text="1996";Application.DoEvents();watch.Stop();Check(list.Items.Count==1,"Search at 2000 records");Console.WriteLine("2000-entry search: "+watch.ElapsedMilliseconds+" ms");
        Console.WriteLine("PASS: timed undo, duplicate prevention, bounded thumbnail cache, invalid images, deep search excerpts, UI undo, favorite feedback, storage stats and 2000-entry search.");return 0;
    }catch(Exception ex){Console.WriteLine(ex);return 1;}finally{if(main!=null){Set(main,"quitting",true);main.Close();main.Dispose();}}}
}
