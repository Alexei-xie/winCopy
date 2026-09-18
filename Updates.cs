using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace WinCopy {
    public class ReleaseAsset { public string name; public string browser_download_url; }
    public class ReleaseData { public string tag_name; public string body; public bool draft; public bool prerelease; public ReleaseAsset[] assets; }
    public class UpdateInfo {
        public Version Version;
        public string Notes, DownloadUrl, PageUrl;
        public bool IsNewerThan(Version current) { return Version.CompareTo(UpdateService.Normalize(current)) > 0; }
    }
    public static class UpdateService {
        public const string ReleasesUrl = "https://github.com/Alexei-xie/winCopy/releases";
        public static Version CurrentVersion { get { return Normalize(typeof(UpdateService).Assembly.GetName().Version); } }
        public static Version Normalize(Version v) { return new Version(v.Major,v.Minor,Math.Max(0,v.Build),Math.Max(0,v.Revision)); }
        public static UpdateInfo Parse(string json) {
            var r=new JavaScriptSerializer {MaxJsonLength=1048576}.Deserialize<ReleaseData>(json);
            if(r==null||r.draft||r.prerelease||r.tag_name==null||!Regex.IsMatch(r.tag_name,@"^v?\d+\.\d+\.\d+(\.\d+)?$")) throw new InvalidDataException("发布信息不完整或版本号不受支持。");
            Version version;if(!Version.TryParse(r.tag_name.TrimStart('v'),out version))throw new InvalidDataException("无效的版本号。");
            var info=new UpdateInfo {Version=Normalize(version),Notes=r.body??"此版本没有提供更新说明。",PageUrl=ReleasesUrl+"/tag/"+Uri.EscapeDataString(r.tag_name)};
            string expected="winCopy-"+version.ToString(3)+"-setup.exe";
            foreach(var asset in r.assets??new ReleaseAsset[0]) {
                if(asset==null||asset.name!=expected)continue;
                string canonical=ReleasesUrl+"/download/"+Uri.EscapeDataString(r.tag_name)+"/"+expected;
                if(String.Equals(asset.browser_download_url,canonical,StringComparison.Ordinal))info.DownloadUrl=canonical;
            }
            return info;
        }
        public static UpdateInfo Fetch(CancellationToken cancellation) {
            cancellation.ThrowIfCancellationRequested();
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var request=(HttpWebRequest)WebRequest.Create("https://api.github.com/repos/Alexei-xie/winCopy/releases/latest");
            request.UserAgent="winCopy/"+CurrentVersion.ToString(3); request.Accept="application/vnd.github+json";
            request.Headers["X-GitHub-Api-Version"]="2022-11-28";
            request.Timeout=15000;request.ReadWriteTimeout=15000;request.AllowAutoRedirect=false;
            using(cancellation.Register(request.Abort))
            using(var response=(HttpWebResponse)request.GetResponse()) {
                if(response.StatusCode!=HttpStatusCode.OK)throw new WebException("GitHub 返回了非预期响应。");
                using(var stream=response.GetResponseStream())using(var data=new MemoryStream()) {
                    byte[] buffer=new byte[8192];int count;
                    while((count=stream.Read(buffer,0,buffer.Length))>0){cancellation.ThrowIfCancellationRequested();if(data.Length+count>1048576)throw new InvalidDataException("发布信息过大。");data.Write(buffer,0,count);}
                    return Parse(Encoding.UTF8.GetString(data.ToArray()));
                }
            }
        }
        public static string ErrorMessage(Exception ex) {
            var web=ex as WebException;
            if(web!=null){var response=web.Response as HttpWebResponse; if(response!=null){int code=(int)response.StatusCode;response.Dispose();if(code==404)return "暂时没有可用的正式版本。";if(code==403||code==429)return "GitHub 暂时限制了请求，请稍后再试。";}if(web.Status==WebExceptionStatus.Timeout)return "检查超时，请检查网络后重试。";return "无法连接 GitHub，请检查网络或稍后重试。";}
            return "无法读取更新信息，请稍后重试或打开发布页查看。";
        }
    }
    public class UpdateDialog : Form {
        readonly Label heading=new Label(),summary=new Label();
        readonly TextBox notes=new TextBox();
        readonly RoundedButton retry=new RoundedButton(),download=new RoundedButton();
        readonly CancellationTokenSource cancellation=new CancellationTokenSource();
        bool checking;
        string downloadUrl;
        public UpdateDialog() {
            Text="winCopy · 检查更新";Font=new Font("Microsoft YaHei UI",10);ClientSize=new Size(540,420);MinimumSize=new Size(480,370);StartPosition=FormStartPosition.CenterParent;MinimizeBox=false;MaximizeBox=false;BackColor=Design.Canvas;
            var grid=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(24),ColumnCount=1,RowCount=5};
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute,44));grid.RowStyles.Add(new RowStyle(SizeType.Absolute,58));grid.RowStyles.Add(new RowStyle(SizeType.Percent,100));grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(grid);
            heading.Text="检查新版本";heading.Font=new Font(Font.FontFamily,18,FontStyle.Bold);heading.Dock=DockStyle.Fill;grid.Controls.Add(heading,0,0);
            summary.Text="当前版本 "+UpdateService.CurrentVersion.ToString(3);summary.Dock=DockStyle.Fill;summary.ForeColor=Design.Muted;grid.Controls.Add(summary,0,1);
            notes.Multiline=true;notes.ReadOnly=true;notes.ScrollBars=ScrollBars.Vertical;grid.Controls.Add(Design.Input(notes),0,2);
            var actions=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoSize=true,FlowDirection=FlowDirection.RightToLeft};
            download.Text="下载安装包";download.AutoSize=true;download.MinimumSize=new Size(130,38);download.BackColor=Design.Accent;download.ForeColor=Color.White;download.Enabled=false;download.Click+=delegate{OpenUrl(downloadUrl);};actions.Controls.Add(download);
            retry.Text="重新检查";retry.AutoSize=true;retry.MinimumSize=new Size(105,38);retry.BackColor=Color.White;retry.Click+=async delegate{await CheckAsync();};actions.Controls.Add(retry);
            var page=new RoundedButton {Text="发布页",AutoSize=true,MinimumSize=new Size(90,38),BackColor=Color.White};page.Click+=delegate{OpenUrl(UpdateService.ReleasesUrl);};actions.Controls.Add(page);grid.Controls.Add(actions,0,3);
            grid.Controls.Add(new Label {Text="仅查询公开版本信息，不会上传剪贴板内容。",ForeColor=Design.Muted,AutoSize=true,Margin=new Padding(0,12,0,0)},0,4);
            Shown+=async delegate{await CheckAsync();};FormClosing+=delegate{cancellation.Cancel();};
        }
        async Task CheckAsync() {
            if(checking||IsDisposed)return;checking=true;retry.Enabled=false;download.Enabled=false;heading.Text="正在检查…";notes.Text="正在连接 GitHub Releases，请稍候。";
            try {
                var result=await Task.Factory.StartNew(()=>UpdateService.Fetch(cancellation.Token),cancellation.Token);
                if(IsDisposed||cancellation.IsCancellationRequested)return;
                bool newer=result.IsNewerThan(UpdateService.CurrentVersion);
                heading.Text=newer?"发现新版本 "+result.Version.ToString(3):"当前已是最新版本";
                summary.Text="当前版本 "+UpdateService.CurrentVersion.ToString(3)+"  ·  最新正式版 "+result.Version.ToString(3);
                notes.Text=(newer?"下载完成后，请先从托盘退出旧版再安装。\r\n\r\n":"")+result.Notes;
                downloadUrl=result.DownloadUrl;download.Enabled=newer&&downloadUrl!=null;
                if(newer&&downloadUrl==null)notes.Text="安装包尚未就绪，可打开发布页查看。\r\n\r\n"+notes.Text;
            }catch(Exception ex){if(!IsDisposed&&!cancellation.IsCancellationRequested){heading.Text="暂时无法完成检查";notes.Text=UpdateService.ErrorMessage(ex);}}
            finally{checking=false;if(!IsDisposed)retry.Enabled=true;}
        }
        void OpenUrl(string url) {if(String.IsNullOrEmpty(url))return;try{Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}catch{MessageBox.Show(this,"无法打开浏览器，请访问 github.com/Alexei-xie/winCopy/releases。","winCopy");}}
    }
    public partial class MainWindow {
        UpdateDialog updateDialog;
        void ShowUpdate(Form owner=null) {
            if(updateDialog!=null&&!updateDialog.IsDisposed){updateDialog.Activate();return;}
            using(var dialog=new UpdateDialog()){updateDialog=dialog;try{dialog.ShowDialog(owner??this);}finally{updateDialog=null;}}
        }
    }
}
