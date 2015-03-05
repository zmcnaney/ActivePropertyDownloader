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

namespace ActivePropertyDownloader
{
    public partial class Form1 : Form
    {
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
                bw.RunWorkerAsync();
            }
            else
            {
                if (bw.WorkerSupportsCancellation == true)
                {
                    bw.CancelAsync();
                }
            }


        }

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

     



        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

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


        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            countryProgress.Value = e.ProgressPercentage;
        }

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

    }

}
