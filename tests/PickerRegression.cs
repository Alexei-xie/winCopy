using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using WinCopy;
class PickerRegression {
    static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Call(object obj,string method,object arg){obj.GetType().GetMethod(method,flags).Invoke(obj,new[]{arg});}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    [STAThread]static int Main(){Application.EnableVisualStyles();try{using(var form=new SnippetEditor(null,new[]{"邮箱","手机号","Steam","QQ","工作","项目","地址","常用回复"})){
        form.Show();Application.DoEvents();var picker=(GroupPicker)typeof(SnippetEditor).GetField("group",flags).GetValue(form);var input=picker.Controls.OfType<TextBox>().Single();
        Call(input,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,8,8,0));Application.DoEvents();Check(picker.IsOpen,"Input click must open popup");
        var popup=(Form)typeof(GroupPicker).GetField("popup",flags).GetValue(picker);Check(!popup.Controls.OfType<ComboBox>().Any(),"No native combo popup");
        Call(input,"OnKeyDown",new KeyEventArgs(Keys.Down));Call(input,"OnKeyDown",new KeyEventArgs(Keys.Down));Call(input,"OnKeyDown",new KeyEventArgs(Keys.Enter));Check(picker.Text=="手机号"&&!picker.IsOpen,"Keyboard navigation/selection");
        Call(picker,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,2,2,0));Check(picker.IsOpen,"Frame click opens");input.Text="邮";Check((string)popup.GetType().GetProperty("Selected").GetValue(popup,null)==null,"Input filters without silently choosing");
        Call(popup,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,40,45,0));Check(picker.Text=="邮箱","Mouse selection");
        picker.Open(false);input.Text="新分组";Call(input,"OnKeyDown",new KeyEventArgs(Keys.Escape));Check(picker.Text=="新分组"&&!picker.IsOpen,"Escape preserves custom name");
        picker.Open(false);Application.DoEvents();
        using(var b=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(b,new Rectangle(Point.Empty,form.Size));using(var pb=new Bitmap(popup.Width,popup.Height)){popup.DrawToBitmap(pb,new Rectangle(Point.Empty,popup.Size));using(var g=Graphics.FromImage(b)){var screen=form.PointToScreen(Point.Empty);g.DrawImageUnscaled(pb,popup.Left-form.Left,popup.Top-form.Top);}}b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"custom-picker-preview.png"));}
        picker.ClosePopup();Check(form.Icon!=null,"Dialog branding");using(var icon=Icon.ExtractAssociatedIcon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"winCopy.exe"))) {Check(icon!=null,"Executable icon");}
    }Console.WriteLine("PASS: input/frame click, keyboard navigation, mouse selection, filtering, custom names, Escape, dialog icon.");return 0;}catch(Exception ex){Console.WriteLine("FAIL: "+ex);return 1;}}
}
