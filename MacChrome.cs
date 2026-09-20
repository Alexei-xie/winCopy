using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WinCopy {
    public sealed class MacTitleBar : Control {
        readonly Image logo=Brand.LoadLogo();
        readonly Button close=new CloseDot();
        Point pointer, origin;
        bool dragging;
        public IButtonControl CloseButton {get{return close;}}
        public Func<Point> ReadPosition;
        public Action<Point> MoveWindow;
        public Action DragStarted, DragEnded;
        public MacTitleBar(string title, Action dismiss) {
            Text=title; Height=48; BackColor=Design.Canvas; ForeColor=Design.Ink;
            SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.ResizeRedraw,true);
            close.AccessibleName="关闭窗口"; close.Click+=delegate{dismiss();}; Controls.Add(close);
            Cursor=Cursors.SizeAll; AccessibleName=title+"，拖动以移动窗口";
        }
        protected override void OnResize(EventArgs e){base.OnResize(e);close.SetBounds(12,Math.Max(0,(Height-24)/2),24,24);}
        protected override void OnPaint(PaintEventArgs e){
            base.OnPaint(e); e.Graphics.Clear(BackColor);
            float scale=Height/48f; int size=(int)(26*scale), inset=(int)(14*scale);
            e.Graphics.DrawImage(logo,new Rectangle(Width-inset-size,(Height-size)/2,size,size));
            TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(65,0,Math.Max(1,Width-120),Height),ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
            using(var p=new Pen(Design.Border))e.Graphics.DrawLine(p,0,Height-1,Width,Height-1);
        }
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||ReadPosition==null)return;pointer=Cursor.Position;origin=ReadPosition();dragging=true;if(DragStarted!=null)DragStarted();Capture=true;}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging&&Capture&&MoveWindow!=null){var p=Cursor.Position;MoveWindow(new Point(origin.X+p.X-pointer.X,origin.Y+p.Y-pointer.Y));}}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Left)EndDrag();}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)EndDrag();}
        void EndDrag(){if(!dragging)return;dragging=false;Capture=false;if(DragEnded!=null)DragEnded();}
        protected override void Dispose(bool disposing){if(disposing)logo.Dispose();base.Dispose(disposing);}
        sealed class CloseDot : Button {
            public CloseDot(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Text="";Cursor=Cursors.Hand;SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);}
            protected override void OnPaint(PaintEventArgs e){
                e.Graphics.Clear(Parent.BackColor);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var r=new Rectangle(5,5,Width-10,Height-10);using(var b=new SolidBrush(Color.FromArgb(255,95,87)))e.Graphics.FillEllipse(b,r);
                if(ClientRectangle.Contains(PointToClient(Cursor.Position))||Focused)using(var p=new Pen(Color.FromArgb(120,38,33),1.2f)){e.Graphics.DrawLine(p,9,9,Width-9,Height-9);e.Graphics.DrawLine(p,Width-9,9,9,Height-9);}
            }
            protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);Invalidate();} protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);Invalidate();}
        }
    }
    public static class MacChrome {
        public static Point Clamp(Point point, Size size) {
            var area=Screen.FromPoint(point).WorkingArea;
            return new Point(Math.Max(area.Left,Math.Min(point.X,area.Right-size.Width)),Math.Max(area.Top,Math.Min(point.Y,area.Bottom-size.Height)));
        }
        public static void Attach(Form form) {
            if(form.Controls.OfType<MacTitleBar>().Any())return;
            var content=form.ClientSize; var minimum=form.MinimumSize;
            form.FormBorderStyle=FormBorderStyle.None;form.BackColor=Design.Canvas;
            form.Padding=new Padding(1);form.ClientSize=new Size(content.Width+2,content.Height+50);
            form.MinimumSize=minimum;
            var title=new MacTitleBar(form.Text.Replace("winCopy · ",""),delegate{form.Close();}) {Font=form.Font,Dock=DockStyle.Top,Height=48};
            title.ReadPosition=()=>form.Location;title.MoveWindow=p=>form.Location=p;title.DragEnded=delegate{form.Location=Clamp(form.Location,form.Size);};
            form.Controls.Add(title);
            Action layout=delegate{

                if(form.Width<2||form.Height<2)return;
                using(var path=Design.Round(new Rectangle(0,0,form.Width,form.Height),12)){var old=form.Region;form.Region=new Region(path);if(old!=null)old.Dispose();}form.Invalidate();
            };
            form.SizeChanged+=delegate{layout();};form.PaddingChanged+=delegate{layout();};form.TextChanged+=delegate{title.Text=form.Text.Replace("winCopy · ","");title.Invalidate();};
            form.Paint+=delegate(object sender,PaintEventArgs e){using(var p=new Pen(Design.Border))using(var path=Design.Round(new Rectangle(0,0,form.Width-1,form.Height-1),12))e.Graphics.DrawPath(p,path);};
            if(form.CancelButton==null)form.CancelButton=title.CloseButton;
            layout();
        }
    }
    public static class MacMessage {
        public static DialogResult Show(string text,string caption="winCopy",MessageBoxButtons buttons=MessageBoxButtons.OK,MessageBoxIcon icon=MessageBoxIcon.None,MessageBoxDefaultButton selected=MessageBoxDefaultButton.Button1) {
            return Show(Form.ActiveForm,text,caption,buttons,icon,selected);
        }
        public static DialogResult Show(IWin32Window owner,string text,string caption="winCopy",MessageBoxButtons buttons=MessageBoxButtons.OK,MessageBoxIcon icon=MessageBoxIcon.None,MessageBoxDefaultButton selected=MessageBoxDefaultButton.Button1) {
            using(var form=new Form {Text=caption,Font=new Font("Microsoft YaHei UI",10),ClientSize=new Size(460,220),MinimumSize=new Size(400,220),StartPosition=FormStartPosition.CenterParent,ShowInTaskbar=false}) {
                var grid=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(24),ColumnCount=1,RowCount=2};grid.RowStyles.Add(new RowStyle(SizeType.Percent,100));grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));form.Controls.Add(grid);
                var body=new TextBox {Text=text,Multiline=true,ReadOnly=true,BorderStyle=BorderStyle.None,BackColor=Design.Canvas,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical};grid.Controls.Add(body);
                var actions=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoSize=true,FlowDirection=FlowDirection.RightToLeft};grid.Controls.Add(actions);
                var ok=new RoundedButton {Text=buttons==MessageBoxButtons.YesNo?"确认":"好",DialogResult=buttons==MessageBoxButtons.YesNo?DialogResult.Yes:DialogResult.OK,Size=new Size(92,36),BackColor=Design.Accent,ForeColor=Color.White};actions.Controls.Add(ok);form.AcceptButton=ok;
                if(buttons==MessageBoxButtons.YesNo){var cancel=new RoundedButton {Text="取消",DialogResult=DialogResult.No,Size=new Size(92,36),BackColor=Color.White};actions.Controls.Add(cancel);form.CancelButton=cancel;if(selected==MessageBoxDefaultButton.Button2)form.AcceptButton=cancel;}
                else form.CancelButton=ok;
                form.Icon=Brand.LoadIcon();MacChrome.Attach(form);form.Shown+=delegate{((Control)form.AcceptButton).Focus();};return owner==null?form.ShowDialog():form.ShowDialog(owner);
            }
        }
    }
}
