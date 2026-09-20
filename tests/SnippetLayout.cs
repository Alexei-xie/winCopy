using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WinCopy;
class SnippetLayout {
    static System.Collections.Generic.IEnumerable<Control> All(Control c) {foreach(Control x in c.Controls){yield return x;foreach(var y in All(x))yield return y;}}
    static void Check(Form f) {
        Application.DoEvents();
        var buttons=All(f).OfType<Button>().Where(x=>x.Text=="保存"||x.Text=="取消").ToList();
        if(buttons.Count!=2)throw new Exception("Missing buttons");
        foreach(var b in buttons){if(b.Height<b.GetPreferredSize(Size.Empty).Height)throw new Exception("Button text clipped"); for(Control p=b.Parent;p!=null;p=p.Parent){if(!p.ClientRectangle.Contains(p.RectangleToClient(b.RectangleToScreen(b.ClientRectangle))))throw new Exception("Button clipped by "+p.GetType().Name);}}
        var body=All(f).OfType<TextBox>().First(x=>x.Multiline); if(body.Height<80)throw new Exception("Content field too small: body="+body.Size+" form="+f.Size+" min="+f.MinimumSize+" screen="+Screen.FromControl(f).WorkingArea);
    }
    [STAThread]static int Main(){Application.EnableVisualStyles();try{
        foreach(bool edit in new[]{false,true})foreach(float scale in new[]{1f,1.25f,1.5f,2f}){
            using(var f=new SnippetEditor(edit?new Clip{Snippet=true,Title="测试片段",Group="常用",Text="示例内容"}:null)){
                f.Show();Check(f);f.Size=f.MinimumSize;Check(f);f.Scale(new SizeF(scale,scale));Check(f);
                if(!edit&&scale==1f)using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,f.Size));b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"snippet-preview.png"));}
                if(edit){var save=All(f).OfType<Button>().First(x=>x.Text=="保存");save.PerformClick();if(f.DialogResult!=DialogResult.OK||f.TitleValue!="测试片段")throw new Exception("Save regression");}
                else {var cancel=All(f).OfType<Button>().First(x=>x.Text=="取消");cancel.PerformClick();if(f.DialogResult!=DialogResult.Cancel)throw new Exception("Cancel regression");}
            }
        }
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"snippet-layout-result.txt"),"PASS: new/edit dialogs, minimum window, 100/125/150/200% scaled layouts, button clipping, save and cancel actions.");return 0;
    }catch(Exception ex){Console.WriteLine(ex);File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"snippet-layout-result.txt"),"FAIL: "+ex);return 1;}}
}
