using System;
using System.IO;
using System.Net;
using System.Threading;
using WinCopy;
class UpdateRegression {
    static void Check(bool ok,string description){if(!ok)throw new Exception(description);}
    static string Json(string version,string assets="[]",string extra=""){return "{\"tag_name\":\""+version+"\",\"body\":\"Release notes\",\"assets\":"+assets+extra+"}";}
    static void Reject(string json){bool rejected=false;try{UpdateService.Parse(json);}catch{rejected=true;}Check(rejected,"Invalid release was accepted");}
    static int Main(string[] args){try{
        var info=UpdateService.Parse(Json("v1.10.0"));Check(info.IsNewerThan(new Version(1,2,0,0)),"Numeric version ordering");
        Check(!UpdateService.Parse(Json("v1.2.0")).IsNewerThan(new Version(1,2,0,0)),"Equal version normalization");
        Check(!UpdateService.Parse(Json("v1.1.0")).IsNewerThan(new Version(1,2,0)),"Downgrade should not be offered");
        Reject("{}");Reject("not json");Reject(Json("v2.0.0-beta"));Reject(Json("v2.0.0","[]",",\"draft\":true"));Reject(Json("v2.0.0","[]",",\"prerelease\":true"));
        string asset="[{\"name\":\"winCopy-2.0.0-setup.exe\",\"browser_download_url\":\"https://github.com/Alexei-xie/winCopy/releases/download/v2.0.0/winCopy-2.0.0-setup.exe\"}]";
        Check(UpdateService.Parse(Json("v2.0.0",asset)).DownloadUrl!=null,"Official installer detection");
        Check(UpdateService.Parse(Json("v2.0.0",asset.Replace("github.com/","github.com.example/"))).DownloadUrl==null,"Untrusted installer host");
        Check(UpdateService.Parse(Json("v2.0.0")).DownloadUrl==null,"Missing asset handling");
        Check(UpdateService.ErrorMessage(new WebException("",WebExceptionStatus.Timeout)).Contains("超时"),"Timeout message");
        Check(UpdateService.ErrorMessage(new WebException("",WebExceptionStatus.NameResolutionFailure)).Contains("网络"),"Offline message");
        var cancel=new CancellationTokenSource();cancel.Cancel();bool cancelled=false;try{UpdateService.Fetch(cancel.Token);}catch(OperationCanceledException){cancelled=true;}Check(cancelled,"Cancellation before network request");
        Console.WriteLine("PASS: version ordering, normalization, downgrade prevention, release validation, trusted installer URL, missing assets, offline/timeout messages, cancellation.");
        if(Array.IndexOf(args,"--live")>=0){var latest=UpdateService.Fetch(CancellationToken.None);Console.WriteLine("PASS: live GitHub latest release = "+latest.Version);}
        return 0;
    }catch(Exception ex){Console.WriteLine("FAIL: "+ex);return 1;}}
}
