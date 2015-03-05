using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Xml;
using System.Net;
using System.Threading;

namespace ActivePropertyDownloader
{
    public partial class Form1 : Form
    {

        //This backgroundworker thing is  to load the properties in the background, fucking threads
        BackgroundWorker bw = new BackgroundWorker();
        
        public Form1()
        {
            InitializeComponent();

            var appSettings = ConfigurationManager.AppSettings;

            if (appSettings.Count == 0)
            {
                MessageBox.Show("Unable to Load site list");
            }
            else
            {
                foreach (var key in appSettings.AllKeys)
                {
                    SiteList.Items.Add(new ComboboxItem(key, appSettings[key]));
                }
            }

            
            bw.WorkerSupportsCancellation = true;
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.ProgressChanged +=  new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.RunWorkerCompleted +=  new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);

        }

        private void button1_Click(object sender, EventArgs e)
        {

            if (bw.IsBusy != true)
            {
                downloadbutton.Text = "Cancel Load";
                //Lets send the currently values of our setup to the new thread so it doesn't bitch about how I shouldn't cross my streams....
                object[] parameters = new string[] { (SiteList.SelectedItem as ComboboxItem).Value, ClientID.Value.ToString(), emailBox.Text, passwordBox.Text };
                bw.RunWorkerAsync(parameters);
            }
            else
            {
                if (bw.WorkerSupportsCancellation == true)
                {
                    downloadbutton.Text = "Attempting to Cancel";
                    bw.CancelAsync();
                }
            }


        }


        //This is the selector thing to say where to save the file.  I mean, it seems to work, so screw it.
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            SaveFileDialog d = new SaveFileDialog();
            d.InitialDirectory = "\\My documents";
            d.Filter = "CSV|*.csv";

            if(DialogResult.OK == d.ShowDialog()) {
            
             linkLabel1.Text = d.FileName;
                } else {
                    MessageBox.Show("Please choose a valid location");
                    linkLabel1.Text = "Safe File Location";
                }
            
            
            


        }

     


        // This is the magical thing that makes work happen.  This is where I actually do the work so as not to freeze the whole bloody thing.  I don't know, I just do work
        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            //Thread things I don't fully understand, I just do things
            BackgroundWorker worker = sender as BackgroundWorker;

            //Take in the state of the application so it stops thinking the world ends when I cross the threads
            var auths = e.Argument as string[];

            var PostingURL = auths[0];
            var ClientID = auths[1];
            var Email = auths[2];
            var Password = auths[3];
            
            var requestCountries = new t_Request();
            requestCountries.Source = NewHeader(ClientID, Email, Password);


            requestCountries.RequestDetails = new t_RequestDetails();

            var SearchCountryRequest = new t_SearchCountryRequest();
            SearchCountryRequest.ISO = true;
            SearchCountryRequest.CountryName = "";
            SearchCountryRequest.CountryCode = "";
            SearchCountryRequest.ISOSpecified = true;


            requestCountries.RequestDetails.AddItem(ItemsChoiceType.SearchCountryRequest ,SearchCountryRequest);





            
            var XMLresult = Retry.Do(() => SendRequest(requestCountries, PostingURL), TimeSpan.FromSeconds(1));
            XmlNodeList nodes = XMLresult.DocumentElement.SelectNodes("//Country");
            var i2 = 0;
            foreach (XmlNode n in nodes)
            {
                i2++;
                if ((worker.CancellationPending == true))
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    // Perform a time consuming operation and report progress.

                    //MessageBox.Show("BOOM: " + n.Attributes["Code"].Value + nodes.Count.ToString() +  "% Complete | " + ((int)((float)i2 / (float)nodes.Count) * 100).ToString());
                    System.Threading.Thread.Sleep(500);
                    worker.ReportProgress(  (int)((float)i2 / (float)nodes.Count *100) , (i2.ToString() + " / " + nodes.Count.ToString() + "   " + n.Attributes["Code"].Value ) );
                }

            }


            




            for (int i = 1; (i <= 100); i++)
            {
                if ((worker.CancellationPending == true))
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    // Perform a time consuming operation and report progress.
                    System.Threading.Thread.Sleep(500);
                    worker.ReportProgress((i * 1));
                }
            }
        }


        //Main progress indicator, I think I'll need more of these
        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            countryProgress.Value = e.ProgressPercentage;
            countryProgressText.Text = (string)e.UserState ;
        }


        //Main BGWorker completed.  Holy shit, we've done all the work, or we've exploded.  I mean, both are possible.  I hate my life.
        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((e.Cancelled == true))
            {
                downloadbutton.Text = "Download Properties";
                MessageBox.Show("Canceled!");
            }

            else if (!(e.Error == null))
            {
                downloadbutton.Text = "Download Properties";
                MessageBox.Show("Error: " + e.Error.Message);
            }

            else
            {
                downloadbutton.Text = "Download Properties";
                MessageBox.Show("Done!");
            }
        }




        //Lets make a sexy sexy header.  YAY.
        public t_Source NewHeader(string ClientID, string emailBox, string passwordBox)
        {
            var s = new t_Source();

            s.RequestorID = new t_RequestorID();
            s.RequestorID.Client = ClientID;
            s.RequestorID.EMailAddress = emailBox;
            s.RequestorID.Password = passwordBox;

            s.RequestorPreferences = new t_RequestorPreferences();
            s.RequestorPreferences.Country = "US";
            s.RequestorPreferences.Currency = "USD";
            s.RequestorPreferences.Language = "en";
            s.RequestorPreferences.RequestMode = t_RequestMode.SYNCHRONOUS;


            return s;

        } 


        //Abstracted out this sendRequest thing, so this way I can just get a useful XML object.   I wanted to desearlize it back into a t_Response object but I'm apparently a moron and I don't know how.  XML Parsing FTW.
        //No error catching here btw, I'll be doing my error catching in a retry clause and if the same request fails 3 times, well then, buggeritall we'll just crash the application
        public XmlDocument SendRequest(t_Request RequestToSend, string PostingURL)
        {
            var xml = new XmlDocument();
            var request = WebRequest.Create(PostingURL);

            request.Method = "POST";

            request.ContentLength = RequestToSend.Serialize().Length;
            request.ContentType = "text/xml";


            
            var dataStream = request.GetRequestStream();
            dataStream.Write(System.Text.Encoding.UTF8.GetBytes(RequestToSend.Serialize()), 0, RequestToSend.Serialize().Length);
            dataStream.Close();

            var response = request.GetResponse();

            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), UTF8Encoding.UTF8))
            {
                string responseText = reader.ReadToEnd();

                xml.LoadXml(responseText);
            }


            return xml;
        }

    }


    public static class Retry
    {
        public static void Do(
            Action action,
            TimeSpan retryInterval,
            int retryCount = 3)
        {
            Do<object>(() =>
            {
                action();
                return null;
            }, retryInterval, retryCount);
        }

        public static T Do<T>(
            Func<T> action,
            TimeSpan retryInterval,
            int retryCount = 3)
        {
            var exceptions = new List<Exception>();

            for (int retry = 0; retry < retryCount; retry++)
            {
                try
                {
                    return action();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    Thread.Sleep(retryInterval);
                }
            }

            throw new AggregateException(exceptions);
        }
    }

}
