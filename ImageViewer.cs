using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinCopy {
    public sealed class ImageViewer : Form {
        readonly ImageCanvas canvas;
        readonly Label info=new Label();
        public ImageViewer(Image image) {
            Text="winCopy · 图片预览";Font=new Font("Microsoft YaHei UI",10);StartPosition=FormStartPosition.CenterParent;
            var area=Screen.FromPoint(Cursor.Position).WorkingArea;
            ClientSize=new Size(Math.Min(900,area.Width-60),Math.Min(700,area.Height-80));MinimumSize=new Size(420,320);KeyPreview=true;ShowInTaskbar=false;MinimizeBox=false;
            int pixelWidth=image.Width,pixelHeight=image.Height;
            canvas=new ImageCanvas(image) {Dock=DockStyle.Fill};
            var root=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(12)};
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,30));Controls.Add(root);
            var toolbar=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoSize=true,WrapContents=true,Margin=new Padding(0,0,0,8)};
            AddButton(toolbar,"−",()=>canvas.ZoomBy(1/1.25));AddButton(toolbar,"＋",()=>canvas.ZoomBy(1.25));AddButton(toolbar,"适应窗口",canvas.Fit);AddButton(toolbar,"原始大小",()=>canvas.SetZoom(1));
            root.Controls.Add(toolbar,0,0);root.Controls.Add(canvas,0,1);info.Dock=DockStyle.Fill;info.TextAlign=ContentAlignment.MiddleLeft;info.AutoEllipsis=true;root.Controls.Add(info,0,2);
            canvas.ViewChanged+=delegate{info.Text=pixelWidth+" × "+pixelHeight+"  ·  "+(canvas.Zoom*100).ToString("0.#")+"%  ·  滚轮缩放，拖动查看，Esc 关闭";};
            Shown+=delegate{canvas.Fit();canvas.Focus();};KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Escape){Close();e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Add||e.KeyCode==Keys.Oemplus){canvas.ZoomBy(1.25);e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Subtract||e.KeyCode==Keys.OemMinus){canvas.ZoomBy(1/1.25);e.SuppressKeyPress=true;}};
            Design.Dialog(this);
        }
        void AddButton(Control toolbar,string text,Action action){var b=new RoundedButton {Text=text,AutoSize=true,MinimumSize=new Size(text.Length>1?108:42,36),Margin=new Padding(0,0,8,0)};b.Click+=delegate{action();canvas.Focus();};toolbar.Controls.Add(b);}
    }
    public sealed class ImageCanvas : Control {
        readonly Bitmap image;
        double zoom=1;
        bool fitting=true,dragging;
        Point start;PointF offset,origin;
        public double Zoom {get{return zoom;}}
        public event Action ViewChanged;
        public ImageCanvas(Image source){
            if(source==null)throw new ArgumentNullException("source");image=new Bitmap(source);BackColor=Color.FromArgb(236,238,244);TabStop=true;Cursor=Cursors.Hand;AccessibleName="放大图片，滚轮缩放，拖动查看";
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
        }
        double FitZoom(){return Math.Min(1,Math.Max(0.000001,Math.Min((ClientSize.Width-24.0)/image.Width,(ClientSize.Height-24.0)/image.Height)));}
        public void Fit(){fitting=true;zoom=FitZoom();offset=PointF.Empty;Changed();}
        public void ZoomBy(double factor){SetZoom(zoom*factor);}
        public void SetZoom(double value){ZoomAt(value,new Point(Width/2,Height/2));}
        void ZoomAt(double value,Point anchor){
            if(Double.IsNaN(value)||Double.IsInfinity(value))return;
            double next=Math.Max(Math.Min(.01,FitZoom()),Math.Min(8,value));double ratio=next/zoom;
            offset=new PointF((float)(anchor.X-Width/2.0-(anchor.X-Width/2.0-offset.X)*ratio),(float)(anchor.Y-Height/2.0-(anchor.Y-Height/2.0-offset.Y)*ratio));
            zoom=next;fitting=false;Changed();
        }
        void Changed(){Clamp();Invalidate();if(ViewChanged!=null)ViewChanged();}
        void Clamp(){float x=(float)Math.Max(0,(image.Width*zoom-Width)/2),y=(float)Math.Max(0,(image.Height*zoom-Height)/2);offset=new PointF(Math.Max(-x,Math.Min(x,offset.X)),Math.Max(-y,Math.Min(y,offset.Y)));}
        protected override void OnResize(EventArgs e){base.OnResize(e);if(image==null)return;if(fitting)Fit();else Changed();}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.InterpolationMode=zoom>=1?InterpolationMode.NearestNeighbor:InterpolationMode.HighQualityBicubic;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;
            float w=(float)(image.Width*zoom),h=(float)(image.Height*zoom);var rect=new RectangleF((Width-w)/2+offset.X,(Height-h)/2+offset.Y,w,h);
            using(var brush=new SolidBrush(Color.White))e.Graphics.FillRectangle(brush,rect);e.Graphics.DrawImage(image,rect);
        }
        protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);ZoomAt(zoom*Math.Pow(1.25,e.Delta/120.0),e.Location);}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();if(e.Button==MouseButtons.Left){dragging=true;start=e.Location;origin=offset;Capture=true;Cursor=Cursors.SizeAll;}}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging){offset=new PointF(origin.X+e.X-start.X,origin.Y+e.Y-start.Y);Changed();}}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Left){dragging=false;Capture=false;Cursor=Cursors.Hand;}}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture){dragging=false;Cursor=Cursors.Hand;}}
        protected override void OnMouseDoubleClick(MouseEventArgs e){base.OnMouseDoubleClick(e);if(fitting)SetZoom(1);else Fit();}
        protected override void Dispose(bool disposing){if(disposing)image.Dispose();base.Dispose(disposing);}
    }
    public partial class MainWindow {
        void OpenImagePreview(){if(picture.Image==null)return;using(var viewer=new ImageViewer(picture.Image))viewer.ShowDialog(this);}
    }
}
