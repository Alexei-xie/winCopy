using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinCopy;
class TemplateRegression {
    [DllImport("user32.dll")]static extern bool RegisterHotKey(IntPtr h,int id,uint mods,uint key);
    [DllImport("user32.dll")]static extern bool UnregisterHotKey(IntPtr h,int id);
    static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    static void Reject(Action action,string why){try{action();}catch{return;}throw new Exception(why);}
    static object Call(object o,string method,params object[] args){return o.GetType().GetMethod(method,flags).Invoke(o,args);}
    static void Set(object o,string field,object value){o.GetType().GetField(field,flags).SetValue(o,value);}
    static void Shot(Form f,string name){using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,f.Size));b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,name));}}
    [STAThread]static int Main(){MainWindow main=null;try{
        Application.EnableVisualStyles();var now=new DateTime(2026,9,21,14,5,6);var values=new Dictionary<string,string>{{"姓名","小王"},{"备注","{{date}}"}};
        Check(SnippetTemplates.Render("{{date}} {{time}} {{姓名}} / {{姓名}}",values,now)=="2026-09-21 14:05:06 小王 / 小王","Date/time and repeated variables");
        Check(SnippetTemplates.Render("{{备注}}",values,now)=="{{date}}","Inserted values are literal, not recursively evaluated");
        Check(SnippetTemplates.Variables("{{date}} {{datetime}} {{姓名}} {{姓名}}").SequenceEqual(new[]{"姓名"}),"Builtins and duplicate variables");
        Reject(()=>SnippetTemplates.Render("{{缺少}}",values,now),"Missing variable rejected");Reject(()=>SnippetTemplates.Variables(String.Join(" ",Enumerable.Range(0,21).Select(i=>"{{v"+i+"}}"))),"Variable count bounded");
        Reject(()=>SnippetTemplates.NormalizeKey("F12"),"Invalid key rejected");Check(SnippetTemplates.NormalizeKey("q")=="Q","Key normalization");
        var clip=new Clip{Snippet=true,Title="问候模板",Text="{{date}} 你好，{{姓名}}！",IsTemplate=true,ShortcutKey="Q"};var db=new Database{Hotkey="J"};db.Items.Add(clip);
        var copy=DataManagement.Clone(db);Check(copy.Items[0].IsTemplate&&copy.Items[0].ShortcutKey=="Q","Serialized automation metadata");copy.Items.Add(new Clip{Snippet=true,ShortcutKey="Q"});Reject(()=>SnippetTemplates.ValidateKeys(copy),"Duplicate key rejected");
        Reject(()=>SnippetTemplates.Render(String.Concat(Enumerable.Repeat("{{姓名}}",101)),new Dictionary<string,string>{{"姓名",new string('x',10000)}},now),"Expanded content size bounded");
        var target=new Database();DataManagement.Merge(target,DataManagement.Clone(db));Check(target.Items[0].IsTemplate&&target.Items[0].ShortcutKey=="","Backup merge retains templates and clears imported bindings");
        using(var options=new SnippetOptionsDialog(clip)){options.Show();Application.DoEvents();Check(options.IsTemplate&&options.ShortcutKey=="Q","Options retain state");Shot(options,"template-options.png");options.Close();}
        using(var input=new TemplateValuesDialog("测试",new[]{"姓名","备注"})){input.Show();Application.DoEvents();Shot(input,"template-values.png");input.Close();}
        main=new MainWindow(false);Set(main,"storageBlocked",true);var empty=new Database{Hotkey="J"};Set(main,"db",empty);main.Show();Application.DoEvents();
        using(var reserved=new Form()){
            char chosen='\0';for(char key='A';key<='Z';key++)if(RegisterHotKey(reserved.Handle,1000,0x4007,key)){chosen=key;break;}
            Check(chosen!='\0',"Test shortcut available");try{Check(!(bool)Call(main,"CheckSnippetShortcut",chosen.ToString()),"External shortcut conflict detected");}finally{UnregisterHotKey(reserved.Handle,1000);}
            Check((bool)Call(main,"CheckSnippetShortcut",chosen.ToString()),"Released shortcut becomes available");
            empty.Items.Add(new Clip{Snippet=true,Title="Binding test",Text="test",ShortcutKey=chosen.ToString()});Call(main,"SyncSnippetHotkeys");
            Check(!RegisterHotKey(reserved.Handle,1001,0x4007,chosen),"Snippet shortcut registered with OS");empty.Items.Clear();Call(main,"SyncSnippetHotkeys");Check(RegisterHotKey(reserved.Handle,1001,0x4007,chosen),"Deleted snippet releases shortcut");UnregisterHotKey(reserved.Handle,1001);
        }
        using(var timer=new Timer{Interval=100}){timer.Tick+=delegate{var prompt=Application.OpenForms.OfType<TemplateValuesDialog>().FirstOrDefault();if(prompt!=null){timer.Stop();prompt.Close();}};timer.Start();Check(Call(main,"ExpandSnippet",clip)==null,"Cancel stops expansion");}
        var literal=new Clip{Snippet=true,Text="{{date}}"};Check(Object.ReferenceEquals(Call(main,"ExpandSnippet",literal),literal),"Legacy snippets remain literal");
        Console.WriteLine("PASS: date/time expansion, literal input, repeated variables, size/count limits, metadata roundtrip, duplicate and OS shortcut conflict checks, restore binding reset and cancellation.");return 0;
    }catch(Exception ex){Console.WriteLine(ex);return 1;}finally{if(main!=null){Set(main,"quitting",true);main.Close();main.Dispose();}}}
}
