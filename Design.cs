using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinCopy {
    internal static class Design {
        public static readonly Color Ink = Color.FromArgb(32,39,60), Muted = Color.FromArgb(116,125,147), Accent = Color.FromArgb(91,86,224), Canvas = Color.FromArgb(246,247,252), Border = Color.FromArgb(229,232,243);
        public static GraphicsPath Round(Rectangle r, int radius) {
            var p=new GraphicsPath(); int d=Math.Max(2,Math.Min(radius*2,Math.Min(r.Width,r.Height)));
            p.AddArc(r.Left,r.Top,d,d,180,90);p.AddArc(r.Right-d,r.Top,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.Left,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
        }
        public static void Surface(Graphics g,Rectangle r,Color fill,int radius,Color border) {
            if(r.Width<2||r.Height<2)return; g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var path=Round(r,radius)) {using(var b=new SolidBrush(fill))g.FillPath(b,path);if(border!=Color.Empty)using(var pen=new Pen(border))g.DrawPath(pen,path);}
        }
        public static Control Input(TextBox input) {
            var frame=new RoundedPanel {Dock=DockStyle.Fill,BackColor=Color.White,Padding=new Padding(12,8,12,8),Margin=new Padding(0,0,0,8),Radius=10};
            input.BorderStyle=BorderStyle.None;input.BackColor=Color.White;input.ForeColor=Ink;input.Dock=DockStyle.Fill;frame.Controls.Add(input);
            input.GotFocus+=delegate{frame.BorderColor=Accent;frame.Invalidate();};input.LostFocus+=delegate{frame.BorderColor=Border;frame.Invalidate();};return frame;
        }
        public static void Dialog(Form form) {
            form.BackColor=Canvas;form.ForeColor=Ink;
            Apply(form);
        }
        static void Apply(Control c) {
            foreach(Control child in c.Controls) {
                var b=child as RoundedButton;
                if(b!=null){bool primary=b.Text=="保存"||b.Text=="保存设置";b.BackColor=primary?Accent:Color.White;b.ForeColor=primary?Color.White:Ink;}
                if(child is Label)child.ForeColor=child.Font.Size>=16?Ink:Muted;
                if(child is CheckBox)child.ForeColor=Ink;
                Apply(child);
            }
        }
    }
    public class RoundedButton : Button {
        bool hover,pressed;
        public RoundedButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;UseVisualStyleBackColor=false;}
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){hover=false;pressed=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnMouseDown(MouseEventArgs e){if(e.Button==MouseButtons.Left)pressed=true;Invalidate();base.OnMouseDown(e);}
        protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
        protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);}
        protected override void OnLostFocus(EventArgs e){Invalidate();base.OnLostFocus(e);}
        protected override void OnPaint(PaintEventArgs e){
            e.Graphics.Clear(Parent==null?Design.Canvas:Parent.BackColor);
            bool primary=ForeColor.ToArgb()==Color.White.ToArgb();var fill=Enabled?BackColor:Color.FromArgb(239,241,247);
            if(Enabled&&(hover||pressed))fill=primary?(pressed?Color.FromArgb(64,58,181):Color.FromArgb(77,70,206)):(pressed?Color.FromArgb(224,227,243):Color.FromArgb(235,237,248));
            Design.Surface(e.Graphics,new Rectangle(1,1,Width-3,Height-3),fill,10,primary?Color.Empty:Design.Border);
            TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(Width<40?1:6,2,Width-(Width<40?2:12),Height-4),Enabled?ForeColor:Design.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding);
            if(Focused&&ShowFocusCues)using(var path=Design.Round(new Rectangle(4,4,Width-9,Height-9),7))using(var pen=new Pen(primary?Color.White:Design.Accent)){pen.DashStyle=DashStyle.Dot;e.Graphics.DrawPath(pen,path);}
        }
    }
    public class RoundedPanel : Panel {
        public int Radius=14;
        public Color BorderColor=Design.Border;
        public RoundedPanel(){DoubleBuffered=true;ResizeRedraw=true;}
        protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Parent==null?Design.Canvas:Parent.BackColor);Design.Surface(e.Graphics,new Rectangle(0,0,Width-1,Height-1),BackColor,Radius,BorderColor);}
    }
    public class RoundedTable : TableLayoutPanel {
        public RoundedTable(){DoubleBuffered=true;ResizeRedraw=true;}
        protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Parent==null?Design.Canvas:Parent.BackColor);Design.Surface(e.Graphics,new Rectangle(0,0,Width-1,Height-1),BackColor,14,Design.Border);}
    }
}
