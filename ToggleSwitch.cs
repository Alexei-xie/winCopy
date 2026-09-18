using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinCopy {
    public class ToggleSwitch : CheckBox {
        public ToggleSwitch(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);Cursor=Cursors.Hand;}
        public override Size GetPreferredSize(Size proposedSize){var text=TextRenderer.MeasureText(Text,Font);return new Size(text.Width+54,Math.Max(32,text.Height+10));}
        protected override void OnCheckedChanged(EventArgs e){base.OnCheckedChanged(e);Invalidate();}
        protected override void OnPaint(PaintEventArgs e){
            e.Graphics.Clear(Parent==null?Design.Canvas:Parent.BackColor);int y=(Height-22)/2;
            Design.Surface(e.Graphics,new Rectangle(1,y,36,22),Checked?Design.Accent:Color.FromArgb(211,217,231),11,Color.Empty);
            using(var b=new SolidBrush(Color.White))e.Graphics.FillEllipse(b,Checked?18:4,y+3,16,16);
            TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(48,0,Width-50,Height),Enabled?Design.Ink:Design.Muted,TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);
            if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(46,2,Width-48,Height-4));
        }
    }
}
