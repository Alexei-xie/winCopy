using System;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace WinCopy {
    public class BoundedNumber : NumericUpDown {
        readonly TextBox editor;
        bool normalizing;
        string previous="0";
        public BoundedNumber(){editor=Controls.OfType<TextBox>().First();editor.TextChanged+=NormalizeInput;}
        void NormalizeInput(object sender,EventArgs e){
            if(normalizing)return;string text=editor.Text;if(text.Length==0)return;
            string replacement=text;
            if(text.Any(c=>c<'0'||c>'9'))replacement=previous;
            else {decimal value;if(!decimal.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture,out value)||value>Maximum)replacement=Maximum.ToString(CultureInfo.InvariantCulture);}
            normalizing=true;
            try{if(replacement!=text){editor.Text=replacement;editor.SelectionStart=editor.TextLength;}previous=replacement;}finally{normalizing=false;}
        }
        protected override void OnLeave(EventArgs e){if(String.IsNullOrEmpty(editor.Text))Text=Minimum.ToString(CultureInfo.InvariantCulture);base.OnLeave(e);}
    }
}
