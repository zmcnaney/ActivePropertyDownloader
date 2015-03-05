using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;

namespace ActivePropertyDownloader
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new Form1());






        }
    }


    public class HotelItem
    {
        public string HotelCode { get; set; }
        public string CityCode { get; set; }
        public string ItemCode { get; set; }






    }




    public class ComboboxItem
    {


        public string Text { get; set; }
        public object Value { get; set; }

        public ComboboxItem(string txt, string val)
        {
            this.Text = txt;
            this.Value = val;

        }


        public override string ToString()
        {
            return Text;
        }
    }

}
