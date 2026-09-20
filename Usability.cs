using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WinCopy {
    public sealed class ThumbnailCache : IDisposable {
        readonly Dictionary<Clip,Bitmap> cache=new Dictionary<Clip,Bitmap>();
        readonly Queue<Clip> order=new Queue<Clip>();
        public int Count {get{return cache.Count;}}
        public Bitmap Get(Clip clip){
            Bitmap result;if(cache.TryGetValue(clip,out result))return result;
            if(cache.Count>=64){var oldest=order.Dequeue();var old=cache[oldest];if(old!=null)old.Dispose();cache.Remove(oldest);}
            result=null;
            try{if(clip.Image!=null)using(var stream=new MemoryStream(clip.Image))using(var source=Image.FromStream(stream)){
                if((long)source.Width*source.Height<=25000000){result=new Bitmap(64,64);using(var g=Graphics.FromImage(result)){g.Clear(Color.White);g.InterpolationMode=InterpolationMode.HighQualityBicubic;float scale=Math.Min(64f/source.Width,64f/source.Height);float w=source.Width*scale,h=source.Height*scale;g.DrawImage(source,(64-w)/2,(64-h)/2,w,h);}}
            }}catch(ArgumentException){if(result!=null)result.Dispose();result=null;}catch(OutOfMemoryException){if(result!=null)result.Dispose();result=null;}
            cache.Add(clip,result);order.Enqueue(clip);return result;
        }
        public void Retain(IEnumerable<Clip> items){var keep=new HashSet<Clip>(items);foreach(var key in cache.Keys.Where(x=>!keep.Contains(x)).ToArray()){if(cache[key]!=null)cache[key].Dispose();cache.Remove(key);}var valid=order.Where(cache.ContainsKey).ToArray();order.Clear();foreach(var key in valid)order.Enqueue(key);}
        public void Dispose(){foreach(var bitmap in cache.Values)if(bitmap!=null)bitmap.Dispose();cache.Clear();order.Clear();}
    }
    public sealed class DeleteUndo {
        Clip deleted;DateTime expires;
        public bool Available(DateTime now){if(deleted!=null&&now>=expires)Clear();return deleted!=null;}
        public void Remember(Clip clip,DateTime now){deleted=clip;expires=now.AddSeconds(10);}
        public void Clear(){deleted=null;}
        public Clip Restore(Database db,DateTime now){
            if(!Available(now))return null;var clip=deleted;Clear();
            var hash=clip.Fingerprint();var same=db.Items.FirstOrDefault(x=>!x.Snippet&&(x.Id==clip.Id||x.Fingerprint()==hash));if(same!=null)return same;
            db.Items.Insert(0,clip);db.Prune();return db.Items.Contains(clip)?clip:null;
        }
    }
    public static class SearchPresentation {
        public static string Excerpt(string value,string query){
            value=(value??"").Replace("\r"," ").Replace("\n"," ").Replace("\t"," ");
            int match=String.IsNullOrEmpty(query)?-1:value.IndexOf(query,StringComparison.OrdinalIgnoreCase);int start=match>28?match-16:0;
            return (start>0?"…":"")+value.Substring(start,Math.Min(240,value.Length-start));
        }
        public static void Draw(Graphics g,string text,string query,Font font,Rectangle bounds,Color color){
            var state=g.Save();g.SetClip(bounds);
            using(var format=(StringFormat)StringFormat.GenericTypographic.Clone())using(var ink=new SolidBrush(color))using(var mark=new SolidBrush(Color.FromArgb(255,225,126))){
                format.FormatFlags|=StringFormatFlags.NoWrap|StringFormatFlags.MeasureTrailingSpaces;
                Func<string,float> width=s=>s.Length==0?0:g.MeasureString(s,font,Int32.MaxValue,format).Width;
                if(width(text)>bounds.Width){int low=0,high=text.Length;while(low<high){int mid=(low+high+1)/2;if(width(text.Substring(0,mid)+"…")<=bounds.Width)low=mid;else high=mid-1;}if(low>0&&Char.IsHighSurrogate(text[low-1]))low--;text=text.Substring(0,low)+"…";}
                float x=bounds.Left,y=bounds.Top+(bounds.Height-font.GetHeight(g))/2;
                int at=0;
                while(at<text.Length){int match=String.IsNullOrEmpty(query)?-1:text.IndexOf(query,at,StringComparison.OrdinalIgnoreCase);
                    if(match<0){g.DrawString(text.Substring(at),font,ink,x,y,format);break;}
                    var before=text.Substring(at,match-at);g.DrawString(before,font,ink,x,y,format);x+=width(before);
                    var hit=text.Substring(match,query.Length);float w=width(hit);g.FillRectangle(mark,x,y,w,font.GetHeight(g));g.DrawString(hit,font,ink,x,y,format);x+=w;at=match+query.Length;
                }
            }g.Restore(state);
        }
    }
    public partial class MainWindow {
        readonly ThumbnailCache thumbnails=new ThumbnailCache();
        readonly DeleteUndo deletion=new DeleteUndo();
        readonly System.Windows.Forms.Timer undoTimer=new System.Windows.Forms.Timer(),feedbackTimer=new System.Windows.Forms.Timer();
        readonly LinkLabel undoLink=new LinkLabel {Text="撤销",AutoSize=false,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleRight,Visible=false,LinkColor=Design.Accent,AccessibleName="撤销删除，十秒内有效"};
        Button pinAction;
        void SetupFeedback(TableLayoutPanel layout){
            var row=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=2,Margin=Padding.Empty};row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,48));row.Controls.Add(status,0,0);row.Controls.Add(undoLink,1,0);layout.Controls.Add(row,0,7);
            tips.SetToolTip(undoLink,"撤销最近一次普通历史删除 · Ctrl + Z · 10 秒内有效");undoLink.LinkClicked+=delegate{UndoDeletion();};undoTimer.Interval=10000;undoTimer.Tick+=delegate{ClearUndo();};
            feedbackTimer.Interval=3500;feedbackTimer.Tick+=delegate{feedbackTimer.Stop();activityLabel.ForeColor=Muted;activityLabel.Text=paused?"记录已暂停 · 在更多菜单中继续":"留住灵感，让复制更轻松";};
            Disposed+=delegate{undoTimer.Dispose();feedbackTimer.Dispose();thumbnails.Dispose();deletion.Clear();if(picture.Image!=null){picture.Image.Dispose();picture.Image=null;}tips.Dispose();};
        }
        void NotifyAction(string message){SetStatus(message);activityLabel.Text=message;activityLabel.ForeColor=Accent;feedbackTimer.Stop();feedbackTimer.Start();}
        void ClearUndo(){deletion.Clear();undoTimer.Stop();undoLink.Visible=false;}
        void UndoDeletion(){var clip=deletion.Restore(db,DateTime.UtcNow);ClearUndo();if(clip==null){NotifyAction("撤销已过期或记录已被清理");return;}Changed();if(list.Items.Contains(clip))list.SelectedItem=clip;NotifyAction("已恢复记录");}
    }
    public static class StorageUsage {
        public static string FormatBytes(long bytes){return bytes>=1024*1024?(bytes/1048576.0).ToString("0.0")+" MB":bytes>=1024?(bytes/1024.0).ToString("0.0")+" KB":bytes+" B";}
        public static string Describe(Database db,string directory){
            string disk;try{var file=new FileInfo(Path.Combine(directory,"history.dat"));disk=file.Exists?FormatBytes(file.Length):"尚未保存";}catch(Exception ex){if(!(ex is IOException)&&!(ex is UnauthorizedAccessException)&&!(ex is ArgumentException)&&!(ex is System.Security.SecurityException))throw;disk="暂时无法读取";}
            long images=db.Items.Sum(x=>x.Image==null?0L:x.Image.LongLength);
            return "当前数据文件："+disk+"\n当前记录："+db.Items.Count+" 条，其中图片 "+db.Items.Count(x=>x.Image!=null)+" 张\n图片原始压缩数据："+FormatBytes(images)+"（非进程内存占用）\n文件大小为上次保存结果，不含临时文件和旧目录副本。";
        }
    }
}
