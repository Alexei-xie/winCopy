using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinCopy {
    public partial class MainWindow {
        readonly Label activityLabel=new Label();
        readonly ToolTip tips=new ToolTip();
        void BuildBrand(TableLayoutPanel header) {
            var brand=new Panel {Dock=DockStyle.Fill,Margin=Padding.Empty};
            var title=new Label {Text="winCopy",Font=new Font(Font.FontFamily,17,FontStyle.Bold),ForeColor=Ink,AutoSize=true,Location=new Point(48,0)};
            activityLabel.Text="留住灵感，让复制更轻松";activityLabel.ForeColor=Muted;activityLabel.Font=new Font(Font.FontFamily,9);activityLabel.AutoSize=true;activityLabel.Location=new Point(49,33);
            using(var icon=Brand.LoadIcon()){var mark=new PictureBox {Image=icon.ToBitmap(),SizeMode=PictureBoxSizeMode.Zoom,Size=new Size(38,38),Location=new Point(0,5)}; brand.Controls.Add(mark); EnableWindowDrag(mark);}brand.Controls.Add(title);brand.Controls.Add(activityLabel);header.Controls.Add(brand,0,0);EnableWindowDrag(brand);EnableWindowDrag(title);EnableWindowDrag(activityLabel);
        }
        void DrawModernItem(object sender,DrawItemEventArgs e) {
            if(e.Index<0)return;var c=(Clip)list.Items[e.Index];bool selected=(e.State&DrawItemState.Selected)!=0;var r=e.Bounds;
            using(var background=new SolidBrush(Color.White))e.Graphics.FillRectangle(background,r);
            var card=new Rectangle(r.X+2,r.Y+3,r.Width-5,r.Height-6);
            Design.Surface(e.Graphics,card,selected?Color.FromArgb(239,238,255):Color.White,11,selected?Color.FromArgb(215,212,248):Color.Empty);
            Design.Surface(e.Graphics,new Rectangle(r.X+12,r.Y+15,34,34),selected?Color.FromArgb(225,222,252):Design.Canvas,9,Color.Empty);
            string icon=c.Pinned?"★":c.Kind=="图片"?"▧":c.Kind=="文件"?"▤":"T";
            TextRenderer.DrawText(e.Graphics,icon,Font,new Rectangle(r.X+12,r.Y+15,34,34),Accent,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(e.Graphics,c.Preview.Replace("\r"," ").Replace("\n","  ").Replace("\t"," "),Font,new Rectangle(r.X+58,r.Y+10,r.Width-88,24),Ink,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);
            using(var small=new Font(Font.FontFamily,8.5f))TextRenderer.DrawText(e.Graphics,(c.Snippet?c.Group:c.Kind)+"  ·  "+c.Created.ToString("HH:mm")+(String.IsNullOrEmpty(c.Source)?"":"  ·  "+c.Source),small,new Rectangle(r.X+58,r.Y+37,r.Width-85,20),Muted,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);
            if(selected)TextRenderer.DrawText(e.Graphics,"↵",Font,new Rectangle(r.Right-28,r.Y+18,20,25),Accent,TextFormatFlags.VerticalCenter);
        }
        void BuildActions(TableLayoutPanel layout) {
            var footer=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=5,Margin=new Padding(0,6,0,0)};
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,86));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,68));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,76));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,112));
            var add=CompactButton("＋ 片段",delegate{EditSnippet(null);},80);var pin=CompactButton("收藏",TogglePin,62);var copy=CompactButton("复制",delegate{UseSelected(false,true);},70);var paste=CompactButton("粘贴  ↵",delegate{UseSelected(false,false);},112);
            paste.BackColor=Accent;paste.ForeColor=Color.White;
            footer.Controls.Add(add,0,0);footer.Controls.Add(pin,1,0);footer.Controls.Add(copy,3,0);footer.Controls.Add(paste,4,0);
            tips.SetToolTip(add,"把常用文字保存为片段");tips.SetToolTip(pin,"收藏或取消收藏当前记录");tips.SetToolTip(copy,"只复制，不切换窗口 · Ctrl + Enter");tips.SetToolTip(paste,"粘贴到之前的应用 · Enter");layout.Controls.Add(footer,0,6);
        }
    }
}
