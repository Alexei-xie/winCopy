using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
namespace WinCopy {
    public static class SnippetTemplates {
        static readonly Regex tokens=new Regex(@"\{\{([\p{L}\p{N}_ -]{1,40})\}\}",RegexOptions.CultureInvariant);
        static bool Builtin(string name){return new[]{"date","time","datetime"}.Contains(name.ToLowerInvariant());}
        public static string[] Variables(string text){var names=tokens.Matches(text??"").Cast<Match>().Select(m=>m.Groups[1].Value.Trim()).Where(x=>!Builtin(x)).Distinct().ToArray();if(names.Any(x=>x.Length==0)||names.Length>20)throw new ArgumentException("模板最多支持 20 个变量，变量名称不能为空。");return names;}
        public static string Render(string text,IDictionary<string,string> values,DateTime now){
            Variables(text);int length=0;
            var result=tokens.Replace(text??"",m=>{string name=m.Groups[1].Value.Trim(),value;
                switch(name.ToLowerInvariant()){case "date":value=now.ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);break;case "time":value=now.ToString("HH:mm:ss",System.Globalization.CultureInfo.InvariantCulture);break;case "datetime":value=now.ToString("yyyy-MM-dd HH:mm:ss",System.Globalization.CultureInfo.InvariantCulture);break;default:if(!values.TryGetValue(name,out value))throw new ArgumentException("未填写变量："+name);break;}
                value=value??"";length+=value.Length;if(length>1000000)throw new ArgumentException("展开后的模板超过长度限制。");return value;
            });if(result.Length>1000000)throw new ArgumentException("展开后的模板超过长度限制。");return result;
        }
        public static string NormalizeKey(string key){key=(key??"").Trim().ToUpperInvariant();if(key.Length!=0&&(key.Length!=1||!(key[0]>='A'&&key[0]<='Z'||key[0]>='0'&&key[0]<='9')))throw new ArgumentException("片段快捷键只支持 A–Z 和 0–9。");return key;}
        public static void ValidateKeys(Database db){var used=new HashSet<string>();foreach(var clip in db.Items.Where(x=>x.Snippet)){var key=NormalizeKey(clip.ShortcutKey);if(key.Length>0&&!used.Add(key))throw new ArgumentException("片段快捷键重复：Ctrl + Alt + Shift + "+key);}}
    }
    public class SnippetOptionsDialog : Form {
        readonly CheckBox template=new ToggleSwitch{Text="将内容作为模板展开"};readonly ComboBox key=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};
        public bool IsTemplate {get{return template.Checked;}}public string ShortcutKey {get{return key.SelectedIndex<=0?"":(string)key.SelectedItem;}}
        public SnippetOptionsDialog(Clip clip){Text="模板与独立快捷键";Font=new Font("Microsoft YaHei UI",10);ClientSize=new Size(470,270);StartPosition=FormStartPosition.CenterParent;MinimizeBox=false;MaximizeBox=false;
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=5};Controls.Add(root);root.RowStyles.Add(new RowStyle(SizeType.Absolute,40));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,28));root.RowStyles.Add(new RowStyle(SizeType.Absolute,40));root.RowStyles.Add(new RowStyle(SizeType.Absolute,42));template.Checked=clip.IsTemplate;template.AutoSize=true;root.Controls.Add(template);
            root.Controls.Add(new Label{Text="在片段内容中使用 {{date}}、{{time}}、{{datetime}}。\n使用 {{姓名}} 等自定义变量，粘贴前会要求填写。\n取消填写不会修改剪贴板，变量值不会保存。",Dock=DockStyle.Fill});root.Controls.Add(new Label{Text="独立快捷键：Ctrl + Alt + Shift +",AutoSize=true});key.Items.Add("无");for(char c='A';c<='Z';c++)key.Items.Add(c.ToString());for(char c='0';c<='9';c++)key.Items.Add(c.ToString());key.SelectedItem=String.IsNullOrEmpty(clip.ShortcutKey)?"无":clip.ShortcutKey;if(key.SelectedIndex<0)key.SelectedIndex=0;key.Width=130;root.Controls.Add(key);
            var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var save=new RoundedButton{Text="保存",DialogResult=DialogResult.OK,Size=new Size(94,36)};var cancel=new RoundedButton{Text="取消",DialogResult=DialogResult.Cancel,Size=new Size(94,36)};actions.Controls.Add(save);actions.Controls.Add(cancel);root.Controls.Add(actions);AcceptButton=save;CancelButton=cancel;Design.Dialog(this);
        }
    }
    public class TemplateValuesDialog : Form {
        readonly Dictionary<string,TextBox> inputs=new Dictionary<string,TextBox>();
        public Dictionary<string,string> Values {get{return inputs.ToDictionary(x=>x.Key,x=>x.Value.Text);}}
        public TemplateValuesDialog(string title,string[] names){Text="填写模板 · "+title;Font=new Font("Microsoft YaHei UI",10);ClientSize=new Size(470,Math.Min(560,130+names.Length*70));MinimumSize=new Size(400,240);StartPosition=FormStartPosition.CenterParent;MinimizeBox=false;MaximizeBox=false;
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(16),ColumnCount=1,RowCount=2};root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,46));Controls.Add(root);var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true};root.Controls.Add(scroll);var flow=new TableLayoutPanel{AutoSize=true,ColumnCount=1,Dock=DockStyle.Top,Padding=new Padding(0,0,18,0)};scroll.Controls.Add(flow);
            foreach(var name in names){flow.RowStyles.Add(new RowStyle(SizeType.AutoSize));flow.Controls.Add(new Label{Text=name,AutoSize=true});var input=new TextBox{MaxLength=10000};inputs.Add(name,input);flow.RowStyles.Add(new RowStyle(SizeType.Absolute,44));flow.Controls.Add(Design.Input(input));}
            var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var ok=new RoundedButton{Text="继续",DialogResult=DialogResult.OK,Size=new Size(94,36)};var cancel=new RoundedButton{Text="取消",DialogResult=DialogResult.Cancel,Size=new Size(94,36)};footer.Controls.Add(ok);footer.Controls.Add(cancel);root.Controls.Add(footer);AcceptButton=ok;CancelButton=cancel;Design.Dialog(this);
        }
    }
    public partial class MainWindow {
        readonly Dictionary<int,string> snippetHotkeys=new Dictionary<int,string>();string shortcutSignature;bool templatePromptOpen;
        bool CheckSnippetShortcut(string key){key=SnippetTemplates.NormalizeKey(key);if(key.Length==0)return true;int id=100+key[0];if(snippetHotkeys.ContainsKey(id))return true;if(!Native.RegisterHotKey(Handle,900,0x4007,key[0]))return false;Native.UnregisterHotKey(Handle,900);return true;}
        void SyncSnippetHotkeys(){if(!IsHandleCreated)return;var clips=db.Items.Where(x=>x.Snippet&&!String.IsNullOrEmpty(x.ShortcutKey)).ToArray();string signature=String.Join("|",clips.OrderBy(x=>x.Id).Select(x=>x.Id+":"+x.ShortcutKey));if(signature==shortcutSignature)return;
            foreach(var id in snippetHotkeys.Keys)Native.UnregisterHotKey(Handle,id);snippetHotkeys.Clear();shortcutSignature=signature;var failed=new List<string>();
            foreach(var clip in clips){string key;try{key=SnippetTemplates.NormalizeKey(clip.ShortcutKey);}catch{failed.Add(clip.Title);continue;}if(key.Length==0)continue;int id=100+key[0];if(snippetHotkeys.ContainsKey(id)||!Native.RegisterHotKey(Handle,id,0x4007,key[0]))failed.Add(clip.Title);else snippetHotkeys[id]=clip.Id;}
            if(failed.Count>0){SetStatus("片段快捷键冲突："+String.Join("、",failed));tray.ShowBalloonTip(3000,"winCopy","部分片段快捷键被占用，请在片段管理中重新设置。",ToolTipIcon.Warning);}
        }
        void UseSnippetShortcut(int id){string clipId;if(!Enabled||templatePromptOpen||!snippetHotkeys.TryGetValue(id,out clipId)||Application.OpenForms.Cast<Form>().Any(f=>f!=this&&f.Visible))return;var clip=db.Items.FirstOrDefault(x=>x.Id==clipId);if(clip==null)return;var h=Native.GetForegroundWindow();if(h!=IntPtr.Zero&&Native.ProcessName(h)!=System.Diagnostics.Process.GetCurrentProcess().ProcessName)target=h;UseClip(clip,false,false);}
        void ConfigureSelectedSnippet(){var clip=Selected;if(clip==null||!clip.Snippet){NotifyAction("请先选择一个常用片段");return;}using(var dialog=new SnippetOptionsDialog(clip))if(dialog.ShowDialog(this)==DialogResult.OK)try{var next=DataManagement.Clone(db);var updated=next.Items.First(x=>x.Id==clip.Id);updated.IsTemplate=dialog.IsTemplate;updated.ShortcutKey=dialog.ShortcutKey;if(updated.IsTemplate)SnippetTemplates.Variables(updated.Text);SnippetTemplates.ValidateKeys(next);if(!CheckSnippetShortcut(updated.ShortcutKey))throw new InvalidOperationException("快捷键已被其他应用占用。");CommitManagedData(next);NotifyAction("模板与快捷键已保存");}catch(Exception ex){MessageBox.Show(this,ex.Message,"无法保存");}}
        Clip ExpandSnippet(Clip clip){if(!clip.Snippet||!clip.IsTemplate)return clip;var names=SnippetTemplates.Variables(clip.Text);var values=new Dictionary<string,string>();if(names.Length>0){templatePromptOpen=true;try{using(var dialog=new TemplateValuesDialog(clip.Title,names)){if(dialog.ShowDialog(this)!=DialogResult.OK)return null;values=dialog.Values;}}finally{templatePromptOpen=false;}}
            return new Clip{Text=SnippetTemplates.Render(clip.Text,values,DateTime.Now),Kind="文本"};
        }
    }
}
