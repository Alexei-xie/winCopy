using System;
using System.IO;
using System.Linq;
using WinCopy;
class StorageRegression {
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Reject(Action action,string message){bool rejected=false;try{action();}catch{rejected=true;}Check(rejected,message);}
    static int Main(){string root=Path.Combine(Path.GetTempPath(),"winCopy-storage-"+Guid.NewGuid().ToString("N"));try{
        string original=Path.Combine(root,"original"),destination=Path.Combine(root,"custom");
        var locator=new StorageLocation(original);var source=locator.Resolve();Check(source.DirectoryPath==original,"Default path");
        var db=new Database();db.Items.Add(new Clip{Text="ordinary"});db.Items.Add(new Clip{Text="favorite",Pinned=true});db.Items.Add(new Clip{Text="snippet",Snippet=true});source.Save(db);
        var migrated=locator.Migrate(source,destination,db);Check(File.Exists(source.FilePath),"Original recovery copy retained");Check(migrated.Load().Items.Count==3,"Migration preserves data");
        Check(new StorageLocation(original).Resolve().DirectoryPath==destination,"Restart loads custom directory");
        Check(locator.Migrate(migrated,destination+Path.DirectorySeparatorChar,db)==migrated,"Same path is a no-op");
        string existing=Path.Combine(root,"existing");Directory.CreateDirectory(existing);File.WriteAllText(Path.Combine(existing,"history.dat"),"do not overwrite");
        Reject(()=>locator.Migrate(migrated,existing,db),"Reject existing destination");Check(File.ReadAllText(Path.Combine(existing,"history.dat"))=="do not overwrite","Existing data unchanged");Check(locator.Resolve().DirectoryPath==destination,"Pointer unchanged on failure");
        string blocked=Path.Combine(root,"not-a-directory");File.WriteAllText(blocked,"blocked");Reject(()=>locator.Migrate(migrated,blocked,db),"Invalid target rejected");Check(locator.Resolve().DirectoryPath==destination,"Failed target does not switch store");
        var blockedLocator=new StorageLocation(blocked); Reject(()=>blockedLocator.Migrate(migrated,Path.Combine(root,"pointer-failure"),db),"Bootstrap write failure"); Check(locator.Resolve().DirectoryPath==destination,"Original pointer intact after config write failure");
        Check(db.ClearHistory()==1,"Only ordinary history cleared");migrated.Save(db);var reloaded=locator.Resolve().Load();Check(reloaded.Items.Count==2&&reloaded.Items.All(x=>x.Pinned||x.Snippet),"Clear persisted and favorites/snippets retained");
        File.Move(migrated.FilePath,migrated.FilePath+".offline");Reject(()=>locator.Resolve(),"Missing custom data cannot silently reset");
        Reject(()=>StorageLocation.Normalize("relative"),"Relative path rejected");
        Console.WriteLine("PASS: default path, verified migration, source recovery copy, restart resolution, same path, occupied/invalid target, failure rollback, persisted clear with favorites/snippets, missing custom storage.");return 0;
    }catch(Exception ex){Console.WriteLine("FAIL: "+ex);return 1;}finally{if(Directory.Exists(root))Directory.Delete(root,true);}}
}
