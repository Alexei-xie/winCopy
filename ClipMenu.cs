using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace WinCopy {
    // Use the menu navigation engine for hover, keyboard, dismissal and screen-edge placement.
    public sealed class ClipMenu : ContextMenuStrip {
        readonly Font menuFont = new Font("Microsoft YaHei UI", 11F);
        public ClipMenu() {
            AutoSize = true; MinimumSize = new Size(282,0); MaximumSize = new Size(282,0); ShowImageMargin = false; ShowCheckMargin = false;
            Font = menuFont; BackColor = Color.FromArgb(244,244,246);
            Padding = new Padding(6); Renderer = new ClipMenuRenderer();
            ShowItemToolTips = false;
        }
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing); if(disposing)menuFont.Dispose();
        }
        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            if (Width < 2 || Height < 2) return;
            using(var path=Design.Round(new Rectangle(0,0,Width,Height),12)) {
                var old=Region; Region=new Region(path); if(old!=null)old.Dispose();
            }
        }
        public ToolStripMenuItem Row(string text, Action action) {
            var row=new ToolStripMenuItem(text) { AutoSize=false, Size=new Size(270,34) };
            if(action!=null)row.Click+=delegate{action();}; Items.Add(row); return row;
        }
        public void Heading(string text) { var row=Row(text,null); row.Enabled=false; row.Height=30; }
        public ClipMenu Folder(string text) {
            var row=Row(text,null); var child=new ClipMenu(); row.DropDown=child; return child;
        }
        public static ClipMenu Build(Database db, Action<Clip> choose) {
            var menu=new ClipMenu(); menu.Heading("历史");
            var history=db.Items.Where(x=>!x.Snippet).OrderByDescending(x=>x.Created).ToList();
            if(history.Count==0)menu.Heading("暂无历史");
            for(int start=0;start<history.Count;start+=10) {
                var page=menu.Folder((start+1)+" – "+Math.Min(start+10,history.Count));
                for(int i=start;i<Math.Min(start+10,history.Count);i++)AddClip(page,history[i],i+1,choose);
            }
            menu.Items.Add(new ToolStripSeparator()); menu.Heading("片段");
            var groups=db.GetGroups();
            foreach(var group in groups) {
                var folder=menu.Folder(Short(group)); int index=0;
                foreach(var clip in db.Items.Where(x=>x.Snippet&&x.Group==group).OrderByDescending(x=>x.Created))AddClip(folder,clip,++index,choose);
            }
            if(!db.Items.Any(x=>x.Snippet))menu.Heading("暂无片段");
            return menu;
        }
        static void AddClip(ClipMenu menu,Clip clip,int number,Action<Clip> choose) {
            var title=clip.Snippet&&!String.IsNullOrWhiteSpace(clip.Title)?clip.Title:clip.Preview;
            menu.MinimumSize=new Size(362,0); menu.MaximumSize=new Size(362,0);
            var row=menu.Row(number+". "+Short(title),delegate{choose(clip);}); row.Tag=clip; row.Width=350;
        }
        static string Short(string value) {
            value=(value??"").Replace("\r"," ").Replace("\n"," ").Replace("\t"," ").Replace("&","&&");
            return value.Length>32?value.Substring(0,32)+"…":value;
        }
    }
    sealed class ClipMenuRenderer : ToolStripProfessionalRenderer {
        public ClipMenuRenderer(){RoundedEdges=false;}
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) {
            e.Graphics.Clear(Color.FromArgb(244,244,246));
        }
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) {
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using(var p=new Pen(Color.FromArgb(205,205,210)))using(var path=Design.Round(new Rectangle(0,0,e.ToolStrip.Width-1,e.ToolStrip.Height-1),12))e.Graphics.DrawPath(p,path);
        }
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e) {
            if(e.Item.Selected&&e.Item.Enabled) {
                e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
                using(var brush=new SolidBrush(Color.FromArgb(15,105,215)))using(var path=Design.Round(new Rectangle(0,1,e.Item.Width,e.Item.Height-2),7))e.Graphics.FillPath(brush,path);
            }
        }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) {
            var item=e.Item as ToolStripMenuItem;
            bool folder=item!=null&&item.HasDropDownItems, file=e.Item.Tag is Clip;
            var color=!e.Item.Enabled?Color.FromArgb(145,145,150):e.Item.Selected?Color.White:Color.FromArgb(37,38,42);
            int left=folder||file?36:12;
            TextRenderer.DrawText(e.Graphics,e.Text,e.TextFont,new Rectangle(left,0,e.Item.Width-left-24,e.Item.Height),color,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using(var pen=new Pen(color,1.5F)) {
                int y=(e.Item.Height-17)/2;
                if(folder)e.Graphics.DrawLines(pen,new[]{new Point(12,y),new Point(19,y),new Point(22,y+3),new Point(30,y+3),new Point(30,y+17),new Point(12,y+17),new Point(12,y)});
                else if(file) {
                    e.Graphics.DrawLines(pen,new[]{new Point(15,y),new Point(24,y),new Point(29,y+5),new Point(29,y+18),new Point(15,y+18),new Point(15,y)});
                    e.Graphics.DrawLines(pen,new[]{new Point(24,y),new Point(24,y+5),new Point(29,y+5)});
                }
            }
        }
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e) {
            var color=e.Item.Selected?Color.White:Color.FromArgb(60,60,65); var r=new Rectangle(e.Item.Width-23, (e.Item.Height-16)/2, 12,16);
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using(var pen=new Pen(color,2))e.Graphics.DrawLines(pen,new[]{new Point(r.Left+3,r.Top+3),new Point(r.Left+8,r.Top+r.Height/2),new Point(r.Left+3,r.Bottom-3)});
        }
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e) {
            using(var p=new Pen(Color.FromArgb(215,215,221)))e.Graphics.DrawLine(p,12,e.Item.Height/2,e.Item.Width-12,e.Item.Height/2);
        }
    }
    public partial class MainWindow {
        ClipMenu quickMenu;
        void OpenQuickMenu() {
            if(quickMenu!=null&&quickMenu.Visible){quickMenu.Close();return;}
            var h=Native.GetForegroundWindow();
            if(Native.IsPasteTarget(h))target=h;
            Hide(); db.Prune();
            if(quickMenu!=null)quickMenu.Dispose();
            quickMenu=ClipMenu.Build(db,delegate(Clip clip){
                bool plain=(ModifierKeys&Keys.Shift)!=0, copy=(ModifierKeys&Keys.Control)!=0;
                RunMenuAction(delegate{UseClip(clip,plain,copy);});
            });
            quickMenu.Items.Add(new ToolStripSeparator());
            var clear=quickMenu.Row("清除历史…",delegate{RunMenuAction(ClearHistory);});clear.Enabled=db.Items.Any(x=>!x.Snippet&&!x.Pinned);
            quickMenu.Row("编辑片段…",delegate{RunMenuAction(delegate{view="常用片段";search.Clear();OpenPanel();});});
            quickMenu.Row("管理历史 / 搜索…",delegate{RunMenuAction(delegate{view="全部历史";OpenPanel();});});
            quickMenu.Row("偏好设置…",delegate{RunMenuAction(Settings);});
            quickMenu.Row(paused?"继续记录":"暂停记录",delegate{RunMenuAction(delegate{paused=!paused;SetStatus(paused?"已暂停记录":"正在记录剪贴板");});});
            quickMenu.Row("检查更新…",delegate{RunMenuAction(delegate{ShowUpdate();});});
            quickMenu.Items.Add(new ToolStripSeparator());
            quickMenu.Row("退出 winCopy",delegate{RunMenuAction(delegate{quitting=true;Close();});});
            quickMenu.Show(Cursor.Position);
            Native.SetForegroundWindow(quickMenu.Handle);
        }
        void RunMenuAction(Action action) {
            if(quickMenu!=null)quickMenu.Close();
            // Defer until ToolStrip has released its menu mode and foreground capture.
            BeginInvoke(action);
        }
    }
}
