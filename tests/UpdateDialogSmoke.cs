using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using WinCopy;
class UpdateDialogSmoke {
    [STAThread]static int Main(){Application.EnableVisualStyles();int result=1;using(var f=new UpdateDialog())using(var timer=new Timer {Interval=100}){
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;int ticks=0;
        timer.Tick+=delegate{if((bool)typeof(UpdateDialog).GetField("checking",flags).GetValue(f)&&++ticks<250)return;timer.Stop();
            var heading=(Label)typeof(UpdateDialog).GetField("heading",flags).GetValue(f);
            var notes=(TextBox)typeof(UpdateDialog).GetField("notes",flags).GetValue(f);
            result=heading.Text.Contains("最新")||heading.Text.Contains("发现")?0:1;
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"update-ui-result.txt"),(result==0?"PASS: ":"FAIL: ")+heading.Text+"\n"+(result==0?"Native .NET Framework asynchronous update dialog completed.":notes.Text));
            using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,f.Size));b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"update-preview.png"));} f.Close();};
        f.Shown+=delegate{timer.Start();};Application.Run(f);
    }return result;}
}
