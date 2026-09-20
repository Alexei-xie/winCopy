using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace WinCopy {
    public static class DataManagement {
        static readonly XmlSerializer serializer=new XmlSerializer(typeof(Database));
        public static byte[] Serialize(Database db){using(var stream=new MemoryStream()){serializer.Serialize(stream,db);return stream.ToArray();}}
        public static Database Read(byte[] bytes){
            using(var stream=new MemoryStream(bytes))using(var reader=XmlReader.Create(stream,new XmlReaderSettings {DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=150000000})){
                var db=(Database)serializer.Deserialize(reader);
                Validate(db);
                db.GroupOrder=db.GroupOrder??new List<string>();db.SnippetOrder=db.SnippetOrder??new List<string>();return db;
            }
        }
        public static void Validate(Database db){
                if(db==null||db.Items==null||db.Items.Count>10000)throw new InvalidDataException("备份记录数量不受支持。");
                var ids=new HashSet<string>();foreach(var c in db.Items){
                    if(c==null||String.IsNullOrEmpty(c.Id)||!ids.Add(c.Id)||c.Text==null||c.Title==null||c.Group==null||c.Kind==null||c.Html==null||c.Rtf==null||c.Source==null||c.Text.Length>1000000||c.Html.Length+c.Rtf.Length>2000000||c.Title.Length>100||c.Group.Length>80||c.Image!=null&&c.Image.Length>8*1024*1024||c.Files!=null&&c.Files.Length>10000)throw new InvalidDataException("备份包含无效记录。");
                }
        }
        public static Database Clone(Database db){return Read(Serialize(db));}
        public static int Merge(Database target,Database incoming){
            var restoredOrder=incoming.OrderedSnippets();int added=0;var ids=new HashSet<string>(target.Items.Select(x=>x.Id));
            var history=target.Items.Where(x=>!x.Snippet).GroupBy(x=>x.Fingerprint()).ToDictionary(g=>g.Key,g=>g.First());
            foreach(var c in incoming.Items){
                if(c.Snippet){if(target.Items.Any(x=>x.Snippet&&x.Title==c.Title&&x.Group==c.Group&&x.Text==c.Text))continue;}
                else {Clip existing;if(history.TryGetValue(c.Fingerprint(),out existing)){existing.Pinned|=c.Pinned;continue;}}
                if(ids.Contains(c.Id))c.Id=Guid.NewGuid().ToString("N");ids.Add(c.Id);target.Items.Add(c);if(!c.Snippet)history[c.Fingerprint()]=c;added++;
            }
            target.GroupOrder=target.GroupOrder.Concat(incoming.GetGroups()).Distinct().ToList();
            target.SnippetOrder=target.SnippetOrder.Concat(restoredOrder.Select(x=>x.Id)).Distinct().ToList();
            target.Items=target.Items.OrderByDescending(x=>x.Created).ToList();target.Prune();return added;
        }
        public static int CleanBefore(Database db,DateTime before){return db.Items.RemoveAll(x=>!x.Snippet&&!x.Pinned&&x.Created<before);}
        public static void RenameGroup(Database db,string oldName,string newName){
            newName=(newName??"").Trim();if(newName.Length==0||newName.Length>80)throw new ArgumentException("分组名称须为 1–80 个字符。");
            foreach(var clip in db.Items.Where(x=>x.Snippet&&x.Group==oldName))clip.Group=newName;
            db.GroupOrder=db.GroupOrder.Select(x=>x==oldName?newName:x).Distinct().ToList();db.RemoveEmptyGroups();
        }
        public static void OrderGroup(Database db,string group,IEnumerable<string> ids){
            var expected=new HashSet<string>(db.Items.Where(x=>x.Snippet&&x.Group==group).Select(x=>x.Id));var order=ids.ToList();
            if(order.Count!=expected.Count||order.Distinct().Count()!=order.Count||!expected.SetEquals(order))throw new ArgumentException("片段排序不完整。");
            db.SnippetOrder=db.SnippetOrder.Where(x=>!expected.Contains(x)).Concat(order).ToList();
        }
    }
    public static class BackupArchive {
        static readonly byte[] magic=Encoding.ASCII.GetBytes("WINCOPY2");
        const int MaxBytes=128*1024*1024;
        static byte[] Random(int length){var data=new byte[length];using(var rng=RandomNumberGenerator.Create())rng.GetBytes(data);return data;}
        public static byte[] Encrypt(Database db,string password){
            if(password==null||password.Length<8)throw new ArgumentException("备份密码至少 8 个字符。");
            DataManagement.Validate(db);byte[] plain=DataManagement.Serialize(db);if(plain.Length>MaxBytes-1024)throw new InvalidDataException("备份超过 128 MB 限制。");
            byte[] salt=Random(16),iv=Random(16),key;
            using(var derive=new Rfc2898DeriveBytes(password,salt,150000,HashAlgorithmName.SHA256))key=derive.GetBytes(64);
            try{byte[] cipher;using(var aes=Aes.Create()){aes.Key=key.Take(32).ToArray();aes.IV=iv;using(var encrypt=aes.CreateEncryptor())cipher=encrypt.TransformFinalBlock(plain,0,plain.Length);}
                byte[] body=magic.Concat(salt).Concat(iv).Concat(cipher).ToArray();using(var mac=new HMACSHA256(key.Skip(32).ToArray()))return body.Concat(mac.ComputeHash(body)).ToArray();
            }finally{Array.Clear(plain,0,plain.Length);Array.Clear(key,0,key.Length);}
        }
        public static Database Decrypt(byte[] archive,string password){
            if(archive==null||archive.Length<88||archive.Length>MaxBytes||!archive.Take(8).SequenceEqual(magic))throw new InvalidDataException("不是受支持的 winCopy 备份。");
            byte[] key;using(var derive=new Rfc2898DeriveBytes(password??"",archive.Skip(8).Take(16).ToArray(),150000,HashAlgorithmName.SHA256))key=derive.GetBytes(64);
            try{int count=archive.Length-32;byte[] hash;using(var mac=new HMACSHA256(key.Skip(32).ToArray()))hash=mac.ComputeHash(archive,0,count);int diff=0;for(int i=0;i<32;i++)diff|=hash[i]^archive[count+i];if(diff!=0)throw new InvalidDataException("密码不正确或备份文件已损坏。");
                byte[] plain;using(var aes=Aes.Create()){aes.Key=key.Take(32).ToArray();aes.IV=archive.Skip(24).Take(16).ToArray();using(var decrypt=aes.CreateDecryptor())plain=decrypt.TransformFinalBlock(archive,40,count-40);}
                try{return DataManagement.Read(plain);}finally{Array.Clear(plain,0,plain.Length);}
            }finally{Array.Clear(key,0,key.Length);}
        }
        public static Database Load(string path,string password){var info=new FileInfo(path);if(info.Length>MaxBytes)throw new InvalidDataException("备份超过 128 MB 限制。");return Decrypt(File.ReadAllBytes(path),password);}
        public static void Save(string path,Database db,string password){
            byte[] archive=Encrypt(db,password);string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{File.WriteAllBytes(temp,archive);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}
        }
    }
}
