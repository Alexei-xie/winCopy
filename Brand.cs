using System.Drawing;
namespace WinCopy {
    internal static class Brand {
        public static Icon LoadIcon(){using(var stream=typeof(Brand).Assembly.GetManifestResourceStream("WinCopy.AppIcon"))using(var icon=new Icon(stream,32,32))return (Icon)icon.Clone();}
    }
}
