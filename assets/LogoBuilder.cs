using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
class LogoBuilder {
    static GraphicsPath Round(float x,float y,float w,float h,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(x,y,d,d,180,90);p.AddArc(x+w-d,y,d,d,270,90);p.AddArc(x+w-d,y+h-d,d,d,0,90);p.AddArc(x,y+h-d,d,d,90,90);p.CloseFigure();return p;}
    static int Main(string[] args){string dir=args[0];Directory.CreateDirectory(dir);using(var master=new Bitmap(512,512)){
        using(var g=Graphics.FromImage(master)){g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(2,2);
            using(var p=Round(8,8,240,240,58))using(var gradient=new LinearGradientBrush(new Rectangle(8,8,240,240),Color.FromArgb(137,119,255),Color.FromArgb(69,53,194),55))g.FillPath(gradient,p);
            using(var p=Round(49,43,127,150,22))using(var pen=new Pen(Color.FromArgb(150,255,255,255),9))g.DrawPath(pen,p);
            using(var p=Round(76,68,131,150,22))using(var b=new SolidBrush(Color.White))g.FillPath(b,p);
            using(var b=new SolidBrush(Color.FromArgb(87,215,194)))g.FillEllipse(b,171,49,39,39);
            using(var pen=new Pen(Color.FromArgb(98,78,220),12)){pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;pen.LineJoin=LineJoin.Round;g.DrawLines(pen,new[]{new Point(98,124),new Point(117,172),new Point(140,135),new Point(160,172),new Point(184,124)});}
        }
        master.Save(Path.Combine(dir,"logo.png"),ImageFormat.Png);
        int[] sizes={16,24,32,48,64,128,256};var images=new byte[sizes.Length][];
        for(int i=0;i<sizes.Length;i++)using(var b=new Bitmap(sizes[i],sizes[i])){using(var g=Graphics.FromImage(b)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(master,0,0,sizes[i],sizes[i]);}using(var ms=new MemoryStream()){b.Save(ms,ImageFormat.Png);images[i]=ms.ToArray();}}
        using(var writer=new BinaryWriter(File.Create(Path.Combine(dir,"winCopy.ico")))){writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;for(int i=0;i<sizes.Length;i++){writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(images[i].Length);writer.Write(offset);offset+=images[i].Length;}foreach(var data in images)writer.Write(data);}
    }return 0;}
}
