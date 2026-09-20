using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using WinCopy;
class ImageRegression {
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    static void Mouse(Control c,string method,MouseEventArgs args){c.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{args});}
    static System.Collections.Generic.IEnumerable<Control> All(Control c){foreach(Control child in c.Controls){yield return child;foreach(var other in All(child))yield return other;}}
    [STAThread]static int Main(){try{
        Application.EnableVisualStyles();
        using(var source=new Bitmap(1200,1800)){
            using(var g=Graphics.FromImage(source)){g.Clear(Color.White);g.FillRectangle(Brushes.MediumSlateBlue,100,100,1000,400);g.DrawString("winCopy image preview",SystemFonts.DefaultFont,Brushes.Black,100,550);}
            using(var viewer=new ImageViewer(source)){
                // The viewer owns an independent bitmap; history refresh can release its thumbnail.
                source.Dispose();viewer.Show();Application.DoEvents();var canvas=All(viewer).OfType<ImageCanvas>().Single();
                Check(canvas.Zoom>0&&canvas.Zoom<1,"Large image fits viewport");double fit=canvas.Zoom;
                All(viewer).OfType<Button>().First(x=>x.Text=="原始大小").PerformClick();Check(canvas.Zoom==1,"Original size");
                Mouse(canvas,"OnMouseWheel",new MouseEventArgs(MouseButtons.None,0,100,100,120));Check(canvas.Zoom>1,"Mouse wheel zoom");
                Mouse(canvas,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,100,100,0));Mouse(canvas,"OnMouseMove",new MouseEventArgs(MouseButtons.Left,0,140,170,0));Mouse(canvas,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,140,170,0));
                canvas.SetZoom(100);Check(canvas.Zoom==8,"Maximum zoom bounded");canvas.SetZoom(0);Check(canvas.Zoom>0,"Minimum zoom positive");canvas.Fit();Check(Math.Abs(canvas.Zoom-fit)<.001,"Fit restores scale");
                using(var bitmap=new Bitmap(viewer.Width,viewer.Height)){viewer.DrawToBitmap(bitmap,new Rectangle(Point.Empty,viewer.Size));bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"image-preview.png"));}
                typeof(Form).GetMethod("OnKeyDown",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(viewer,new object[]{new KeyEventArgs(Keys.Escape)});Check(!viewer.Visible,"Escape closes viewer");
            }
        }
        using(var small=new Bitmap(20,20))using(var canvas=new ImageCanvas(small)){canvas.Size=new Size(400,300);canvas.Fit();Check(canvas.Zoom==1,"Small image retains native size");}
        Console.WriteLine("PASS: independent image lifetime, fit, original size, wheel zoom, drag, zoom bounds, small image and Escape close.");return 0;
    }catch(Exception ex){Console.WriteLine(ex);return 1;}}
}
