using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WinCopy {
    public class GroupNameDialog : Form {
        readonly GroupPicker input=new GroupPicker();
        public string Value {get{return input.Text.Trim();}}
        public GroupNameDialog(string title,string current,string[] groups){
            Text=title;Font=new Font("Microsoft YaHei UI",10);ClientSize=new Size(440,160);StartPosition=FormStartPosition.CenterParent;MinimizeBox=false;MaximizeBox=false;
            var root=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(20),RowCount=3,ColumnCount=1};root.RowStyles.Add(new RowStyle(SizeType.Absolute,28));root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));Controls.Add(root);
            root.Controls.Add(new Label {Text="选择已有分组或输入新名称",AutoSize=true});input.MaxLength=80;input.Items.AddRange(groups);input.Text=current;input.Dock=DockStyle.Fill;root.Controls.Add(input);
            var actions=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var ok=new RoundedButton {Text="保存",Size=new Size(90,34)};ok.Click+=delegate{if(Value.Length==0){input.Focus();return;}DialogResult=DialogResult.OK;};actions.Controls.Add(ok);var cancel=new RoundedButton{Text="取消",DialogResult=DialogResult.Cancel,Size=new Size(90,34)};actions.Controls.Add(cancel);root.Controls.Add(actions);AcceptButton=ok;CancelButton=cancel;Design.Dialog(this);
        }
    }
    public class SnippetManager : Form {
        public readonly Database Working;
        readonly ListBox groups=new ListBox();readonly ListView rows=new ListView();
        string CurrentGroup {get{return groups.SelectedItem as string;}}
        Clip[] Selected {get{return rows.SelectedItems.Cast<ListViewItem>().Select(x=>(Clip)x.Tag).ToArray();}}
        public SnippetManager(Database source){
            Working=DataManagement.Clone(source);Text="winCopy · 片段管理";Font=new Font("Microsoft YaHei UI",10);ClientSize=new Size(800,540);MinimumSize=new Size(620,440);StartPosition=FormStartPosition.CenterParent;MinimizeBox=false;
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(16)};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,32));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(root);
            var tools=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,WrapContents=true};root.Controls.Add(tools);
            Add(tools,"新建",()=>Edit(null));Add(tools,"编辑",()=>{if(Selected.Length==1)Edit(Selected[0]);});Add(tools,"复制片段",Duplicate);Add(tools,"重命名分组",Rename);Add(tools,"移动选中",MoveSelected);Add(tools,"删除选中",Delete);Add(tools,"上移",()=>Shift(-1));Add(tools,"下移",()=>Shift(1));
            var split=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,154));split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.Controls.Add(split);
            groups.Dock=DockStyle.Fill;groups.BorderStyle=BorderStyle.None;groups.IntegralHeight=false;groups.SelectedIndexChanged+=delegate{ReloadRows();};split.Controls.Add(groups);
            rows.Dock=DockStyle.Fill;rows.View=View.Details;rows.FullRowSelect=true;rows.MultiSelect=true;rows.HideSelection=false;rows.AllowDrop=true;rows.Columns.Add("片段名称",210);rows.Columns.Add("内容预览",330);split.Controls.Add(rows);
            rows.DoubleClick+=delegate{if(Selected.Length==1)Edit(Selected[0]);};rows.ItemDrag+=delegate{if(Selected.Length>0)rows.DoDragDrop(Selected.Select(x=>x.Id).ToArray(),DragDropEffects.Move);};
            rows.DragOver+=delegate(object sender,DragEventArgs e){e.Effect=e.Data.GetDataPresent(typeof(string[]))?DragDropEffects.Move:DragDropEffects.None;};
            rows.DragDrop+=delegate(object sender,DragEventArgs e){var ids=e.Data.GetData(typeof(string[])) as string[];if(ids==null||CurrentGroup==null)return;var point=rows.PointToClient(new Point(e.X,e.Y));var hit=rows.GetItemAt(point.X,point.Y);var current=Working.OrderedSnippets().Where(x=>x.Group==CurrentGroup).ToList();if(ids.Any(id=>!current.Any(x=>x.Id==id)))return;var moving=current.Where(x=>ids.Contains(x.Id)).ToList();var remaining=current.Except(moving).ToList();int index=hit==null?remaining.Count:remaining.FindIndex(x=>x==(Clip)hit.Tag);if(index<0)return;if(hit!=null&&point.Y>hit.Bounds.Top+hit.Bounds.Height/2)index++;remaining.InsertRange(index,moving);DataManagement.OrderGroup(Working,CurrentGroup,remaining.Select(x=>x.Id));ReloadRows(ids);};
            root.Controls.Add(new Label{Text="Ctrl / Shift 多选；拖动可调整组内顺序。点击保存更改后生效。",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft});
            var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,FlowDirection=FlowDirection.RightToLeft};var save=Add(footer,"保存更改",()=>{DialogResult=DialogResult.OK;});save.BackColor=Design.Accent;save.ForeColor=Color.White;var cancel=Add(footer,"取消",()=>{DialogResult=DialogResult.Cancel;});CancelButton=cancel;root.Controls.Add(footer);Design.Dialog(this);ReloadGroups(null);
        }
        Button Add(Control parent,string text,Action action){var b=new RoundedButton{Text=text,AutoSize=true,MinimumSize=new Size(76,36),Margin=new Padding(3)};b.Click+=delegate{action();};parent.Controls.Add(b);return b;}
        void ReloadGroups(string preferred){Working.RemoveEmptyGroups();groups.Items.Clear();groups.Items.AddRange(Working.GetGroups());if(preferred!=null&&groups.Items.Contains(preferred))groups.SelectedItem=preferred;else if(groups.Items.Count>0)groups.SelectedIndex=0;else ReloadRows();}
        void ReloadRows(){ReloadRows(new string[0]);}
        void ReloadRows(string[] selected){rows.BeginUpdate();rows.Items.Clear();foreach(var c in Working.OrderedSnippets().Where(x=>x.Group==CurrentGroup)){var item=new ListViewItem(c.Title){Tag=c};item.SubItems.Add(SearchPresentation.Excerpt(c.Text,""));rows.Items.Add(item);item.Selected=selected.Contains(c.Id);}rows.EndUpdate();}
        void Edit(Clip clip){using(var editor=new SnippetEditor(clip??new Clip{Group=CurrentGroup??"常用"},Working.GetGroups()))if(editor.ShowDialog(this)==DialogResult.OK){var c=clip??new Clip{Snippet=true};c.Title=editor.TitleValue;c.Text=editor.BodyValue;c.Group=editor.GroupValue;c.Html="";c.Rtf="";if(clip==null)Working.Items.Add(c);ReloadGroups(c.Group);ReloadRows(new[]{c.Id});}}
        void Rename(){if(CurrentGroup==null)return;string old=CurrentGroup;using(var prompt=new GroupNameDialog("重命名分组",old,Working.GetGroups()))if(prompt.ShowDialog(this)==DialogResult.OK){if(prompt.Value!=old&&Working.GetGroups().Contains(prompt.Value)&&MessageBox.Show(this,"目标分组已存在，将合并两个分组。继续？","合并分组",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;DataManagement.RenameGroup(Working,old,prompt.Value);ReloadGroups(prompt.Value);}}
        void MoveSelected(){var selected=Selected;if(selected.Length==0)return;using(var prompt=new GroupNameDialog("移动 "+selected.Length+" 个片段",CurrentGroup,Working.GetGroups()))if(prompt.ShowDialog(this)==DialogResult.OK){foreach(var c in selected)c.Group=prompt.Value;ReloadGroups(prompt.Value);ReloadRows(selected.Select(x=>x.Id).ToArray());}}
        void Delete(){var selected=Selected;if(selected.Length==0)return;if(MessageBox.Show(this,"删除选中的 "+selected.Length+" 个片段？保存更改后生效。","删除片段",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;string group=CurrentGroup;foreach(var c in selected)Working.Items.Remove(c);ReloadGroups(group);}
        void Duplicate(){foreach(var c in Selected)Working.Items.Add(new Clip{Snippet=true,Group=c.Group,Title=c.Title.Substring(0,Math.Min(95,c.Title.Length))+" 副本",Text=c.Text});ReloadRows();}
        void Shift(int direction){if(CurrentGroup==null)return;var selected=Selected;var order=Working.OrderedSnippets().Where(x=>x.Group==CurrentGroup).ToList();if(direction<0){for(int i=1;i<order.Count;i++)if(selected.Contains(order[i])&&!selected.Contains(order[i-1])){var c=order[i];order[i]=order[i-1];order[i-1]=c;}}else{for(int i=order.Count-2;i>=0;i--)if(selected.Contains(order[i])&&!selected.Contains(order[i+1])){var c=order[i];order[i]=order[i+1];order[i+1]=c;}}DataManagement.OrderGroup(Working,CurrentGroup,order.Select(x=>x.Id));ReloadRows(selected.Select(x=>x.Id).ToArray());}
    }
}
