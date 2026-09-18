using System;
using System.IO;
using System.Linq;
using System.Text;

namespace WinCopy {
    public class StorageLocation {
        public static string DefaultDirectory { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"winCopy"); } }
        readonly string bootstrap;
        public StorageLocation(string bootstrapDirectory) { bootstrap=Path.GetFullPath(bootstrapDirectory); }
        public string ConfigPath { get { return Path.Combine(bootstrap,"storage-location.txt"); } }
        public Store Resolve() {
            if(!File.Exists(ConfigPath))return new Store(bootstrap);
            string path=Normalize(File.ReadAllText(ConfigPath,Encoding.UTF8).Trim());
            if(!Directory.Exists(path)||!File.Exists(Path.Combine(path,"history.dat")))throw new IOException("自定义存储目录或 history.dat 不可用："+path+"。请连接对应磁盘后重启，程序不会覆盖原数据。");
            return new Store(path);
        }
        public static string Normalize(string path) {
            if(String.IsNullOrWhiteSpace(path)||!Path.IsPathRooted(path))throw new ArgumentException("请选择绝对目录路径。");
            string full=Path.GetFullPath(path);string root=Path.GetPathRoot(full);
            return full.Length>root.Length?full.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar):full;
        }
        public static bool Same(string a,string b) {return String.Equals(Normalize(a),Normalize(b),StringComparison.OrdinalIgnoreCase);}
        public Store Migrate(Store source,string destination,Database db) {
            string path=Normalize(destination);if(Same(path,source.DirectoryPath))return source;
            var target=new Store(path);
            if(File.Exists(target.FilePath))throw new IOException("所选目录已有 history.dat。为避免覆盖，请选择一个没有 winCopy 数据的目录。");
            Directory.CreateDirectory(path);
            // Verify encrypted data before changing the bootstrap pointer. Keep the source as a recovery copy.
            target.Save(db);var loaded=target.Load();
            var expected=db.Items.Where(x=>db.RememberHistory||x.Pinned||x.Snippet).Select(x=>x.Id).OrderBy(x=>x).ToArray();
            if(!expected.SequenceEqual(loaded.Items.Select(x=>x.Id).OrderBy(x=>x)))throw new IOException("新目录的数据校验失败，仍使用原存储目录。");
            Directory.CreateDirectory(bootstrap);string temp=ConfigPath+".tmp";
            File.WriteAllText(temp,path,Encoding.UTF8);
            if(File.Exists(ConfigPath))File.Replace(temp,ConfigPath,null);else File.Move(temp,ConfigPath);
            return target;
        }
    }
}
