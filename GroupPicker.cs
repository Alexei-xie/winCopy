using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WinCopy {
    public class GroupPicker : RoundedPanel, IMessageFilter {
        readonly TextBox editor=new TextBox();
        readonly List<string> items=new List<string>();
        GroupPopup popup;
        Form owner;
        bool selecting;
        public List<string> Items {get{return items;}}
        public int MaxLength {get{return editor.MaxLength;}set{editor.MaxLength=value;}}
        public override string Text {get{return editor==null?base.Text:editor.Text;}set{if(editor!=null)editor.Text=value;else base.Text=value;}}
        public object SelectedItem {get{return items.FirstOrDefault(x=>x==Text);}set{Text=value as string??"";}}
        public bool IsOpen {get{return popup!=null&&popup.Visible;}}
        public GroupPicker(){BackColor=Color.White;Radius=10;Padding=new Padding(12,8,34,8);Margin=new Padding(0,0,0,8);TabStop=false;
            editor.BorderStyle=BorderStyle.None;editor.Dock=DockStyle.Fill;editor.BackColor=Color.White;editor.AccessibleName="分组名称，可选择或输入";Controls.Add(editor);
            editor.MouseDown+=delegate{Open(false);};MouseDown+=delegate(object s,MouseEventArgs e){if(e.Button==MouseButtons.Left){editor.Focus();Open(false);}};
            editor.TextChanged+=delegate{if(!selecting&&IsOpen)RefreshOptions(true);};
            editor.GotFocus+=delegate{BorderColor=Design.Accent;Invalidate();};editor.LostFocus+=delegate{BorderColor=Design.Border;Invalidate();if(IsDisposed||Disposing||!IsHandleCreated)return;BeginInvoke((Action)delegate{if(!IsDisposed&&!ContainsFocus)ClosePopup();});};
            editor.KeyDown+=delegate(object s,KeyEventArgs e){if(e.KeyCode==Keys.Down||e.KeyCode==Keys.Up){Open(false);popup.MoveSelection(e.KeyCode==Keys.Down?1:-1);e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Enter&&IsOpen){Choose(popup.Selected);e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Escape&&IsOpen){ClosePopup();e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Tab)ClosePopup();};
        }
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var p=new Pen(Design.Muted,1.6f)){int x=Width-20,y=Height/2;e.Graphics.DrawLines(p,new[]{new Point(x-4,y-2),new Point(x,y+2),new Point(x+4,y-2)});}}
        public void Open(bool filter){if(IsDisposed||!Visible||IsOpen)return;if(popup==null){popup=new GroupPopup(Choose);owner=FindForm();if(owner!=null){owner.Deactivate+=OwnerChanged;owner.LocationChanged+=OwnerChanged;owner.Resize+=OwnerChanged;}}
            RefreshOptions(filter);if(!IsOpen){Application.AddMessageFilter(this);popup.Show(owner);}Invalidate();}
        void RefreshOptions(bool filter){var values=items.Distinct().Where(x=>!filter||x.IndexOf(Text.Trim(),StringComparison.OrdinalIgnoreCase)>=0).ToList();popup.SetOptions(values,Text);var below=PointToScreen(new Point(0,Height+5));var area=Screen.FromControl(this).WorkingArea;int h=Math.Min(282,Math.Max(66,values.Count*40+38));popup.Size=new Size(Width,h);popup.Location=new Point(Math.Max(area.Left,Math.Min(below.X,area.Right-Width)),below.Y+h>area.Bottom?Math.Max(area.Top,PointToScreen(Point.Empty).Y-h-5):below.Y);}
        void Choose(string name){if(name!=null){selecting=true;Text=name;selecting=false;editor.SelectionStart=editor.TextLength;}ClosePopup();editor.Focus();}
        void OwnerChanged(object sender,EventArgs e){ClosePopup();}
        public void ClosePopup(){if(popup!=null)popup.Hide();Application.RemoveMessageFilter(this);Invalidate();}
        public bool PreFilterMessage(ref Message m){if(IsOpen && m.Msg==0x20A && popup.Bounds.Contains(Cursor.Position)){popup.ScrollRows(-(short)((m.WParam.ToInt64() >> 16) & 0xffff));return true;}if(IsOpen&&(m.Msg==0x201||m.Msg==0x204||m.Msg==0x207||m.Msg==0xA1)){var p=Cursor.Position;if(!RectangleToScreen(ClientRectangle).Contains(p)&&!popup.Bounds.Contains(p))ClosePopup();}return false;}
        protected override void Dispose(bool disposing){if(disposing){ClosePopup();if(owner!=null){owner.Deactivate-=OwnerChanged;owner.LocationChanged-=OwnerChanged;owner.Resize-=OwnerChanged;}if(popup!=null)popup.Dispose();}base.Dispose(disposing);}
    }
    internal class GroupPopup : Form {
        readonly Action<string> choose;
        List<string> values=new List<string>();int selected=-1,offset;
        public string Selected {get{return selected>=0&&selected<values.Count?values[selected]:null;}}
        public GroupPopup(Action<string> onChoose){choose=onChoose;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;BackColor=Color.White;Font=new Font("Microsoft YaHei UI",10);DoubleBuffered=true;Cursor=Cursors.Hand;}
        protected override bool ShowWithoutActivation {get{return true;}}
        protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x08000080;p.ClassStyle|=0x20000;return p;}}
        protected override void WndProc(ref Message m){if(m.Msg==0x21){m.Result=(IntPtr)3;return;}base.WndProc(ref m);}
        public void SetOptions(List<string> names,string current){values=names;selected=values.IndexOf(current);offset=0;Invalidate();}
        public void MoveSelection(int direction){if(values.Count==0)return;selected=Math.Max(0,Math.Min(values.Count-1,selected+direction));int shown=Math.Max(1,(Height-34)/40);if(selected<offset)offset=selected;if(selected>=offset+shown)offset=selected-shown+1;Invalidate();}
        protected override void OnResize(EventArgs e){base.OnResize(e);if(Width>4&&Height>4)using(var path=Design.Round(new Rectangle(0,0,Width,Height),12)){var old=Region;Region=new Region(path);if(old!=null)old.Dispose();}}
        protected override void OnPaint(PaintEventArgs e){Design.Surface(e.Graphics,new Rectangle(0,0,Width-1,Height-1),Color.White,12,Design.Border);
            using(var small=new Font(Font.FontFamily,8.5f))TextRenderer.DrawText(e.Graphics,values.Count==0?"无匹配分组，输入名称后保存即可创建":"选择已有分组 · 可输入新名称，滚轮查看更多",small,new Rectangle(14,8,Width-28,22),Design.Muted,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
            for(int i=offset;i<values.Count;i++){int y=32+(i-offset)*40;if(y+38>Height)break;bool active=i==selected;if(active)Design.Surface(e.Graphics,new Rectangle(7,y,Width-14,36),Color.FromArgb(239,238,255),8,Color.Empty);TextRenderer.DrawText(e.Graphics,values[i],Font,new Rectangle(18,y+6,Width-54,26),active?Design.Accent:Design.Ink,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);if(active)TextRenderer.DrawText(e.Graphics,"✓",Font,new Rectangle(Width-34,y+6,24,26),Design.Accent);}}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);int index=offset+(e.Y-32)/40;if(e.Y>=32&&index<values.Count){selected=index;Invalidate();}}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);int i=offset+(e.Y-32)/40;if(e.Button==MouseButtons.Left&&e.Y>=32&&i<values.Count)choose(values[i]);}
        public void ScrollRows(int direction){offset=Math.Max(0,Math.Min(Math.Max(0,values.Count-Math.Max(1,(Height-34)/40)),offset+Math.Sign(direction)));Invalidate();}
        protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);ScrollRows(-e.Delta);}
    }
}
