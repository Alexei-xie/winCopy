using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using WinCopy;
class NumberRegression {
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    [STAThread]static int Main(){try{
        foreach(int max in new[]{2000,365})using(var number=new BoundedNumber {Minimum=max==2000?20:1,Maximum=max,Value=30}){
            var input=number.Controls.OfType<TextBox>().First();
            input.Text="999999999999999999999999999999999999999999999999999999999999999999999";
            Check(input.Text==max.ToString(),"Overflow clamps immediately");Check(number.Value==max,"Value respects upper bound");
            input.Text="123";Check(number.Value==123,"Normal input");input.Text="abc";Check(input.Text=="123","Invalid input rejected");
            input.Text="1";input.Text="100";Check(number.Value==100,"Typing below minimum is allowed as intermediate input");
            input.Text="0";Check(number.Value==number.Minimum,"Minimum enforced on commit");input.Text="";typeof(BoundedNumber).GetMethod("OnLeave",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(number,new object[]{EventArgs.Empty});Check(number.Value==number.Minimum,"Empty field on blur");
        }
        var db=new Database {Limit=int.MaxValue,RetentionDays=int.MaxValue};db.Prune();Check(db.Limit==2000&&db.RetentionDays==365,"Stored settings normalized");
        var brand=typeof(MainWindow).Assembly.GetType("WinCopy.Brand");using(var image=(Bitmap)brand.GetMethod("LoadLogo").Invoke(null,null)){GC.Collect();GC.WaitForPendingFinalizers();Check(image.Width==512&&image.GetPixel(128,60).B>150,"Logo decoded independently of source lifetime");}
        Console.WriteLine("PASS: immediate numeric limits, huge input/paste, invalid input, normal typing, minimum/empty values, stored limits and PNG logo lifetime.");return 0;
    }catch(Exception ex){Console.WriteLine("FAIL: "+ex);return 1;}}
}
