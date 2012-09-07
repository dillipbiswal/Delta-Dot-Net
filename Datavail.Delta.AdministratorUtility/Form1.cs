using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Datavail.Delta.Infrastructure.Azure;
using Datavail.Delta.Infrastructure.Azure.QueueMessage;
using Datavail.Framework.Azure.Configuration;
using Datavail.Framework.Azure.Queue;
using Microsoft.WindowsAzure;

namespace Datavail.Delta.AdministratorUtility
{
    public partial class Form1 : Form
    {
        public Form1() { InitializeComponent(); }

        private void button1_Click(object sender, EventArgs e)
        {
            dataGridView1.AutoGenerateColumns = false;
            GetMessages();
        }

        private void GetMessages()
        {
            var accountConnectionString = ConfigurationManager.AppSettings["DeltaStorageConnectionString"];
            var account = AzureConfiguration.GetStorageAccount(accountConnectionString);

            var queue = new PartitionedAzureQueue<DataCollectionMessageWithError>(account, AzureConstants.Queues.CollectionErrors);
            var messages = new List<DataCollectionMessageWithError>();

            var count = 0;
            var message = queue.GetMessage();
            while (message != null && count < 25)
            {
                count++;
                messages.Add(message);
                message = queue.GetMessage();
            }

            dataGridView1.DataSource = messages;
            dataGridView1.Refresh();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            dataGridView1.Width = ActiveForm.Width - 50;
            textBox1.Width = ActiveForm.Width - 50;
        }

        private void dataGridView1_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            var item = dataGridView1.Rows[e.RowIndex].DataBoundItem as DataCollectionMessageWithError;
            textBox1.Clear();
            textBox1.Text = item.ExceptionMessage;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox2.Visible = true;

            backgroundWorker1.RunWorkerAsync();
            button2.Enabled = false;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Change the value of the ProgressBar to the BackgroundWorker progress.
            textBox2.Text = e.ProgressPercentage + " messages processed";
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var accountConnectionString = ConfigurationManager.AppSettings["DeltaStorageConnectionString"];
            var account = CloudStorageAccount.Parse(accountConnectionString);
            var errorQueue = new PartitionedAzureQueue<DataCollectionMessageWithError>(account, AzureConstants.Queues.CollectionErrors);
            var queue = new PartitionedAzureQueue<DataCollectionMessage>(account, AzureConstants.Queues.Collections);
            AutoMapper.Mapper.CreateMap<DataCollectionMessageWithError, DataCollectionMessage>();
            
            var counter = 0;
            var messages = errorQueue.GetMessages(32).ToArray();
            while (messages.Any())
            {
                foreach (var dataCollectionMessageWithError in messages)
                {
                    var message = AutoMapper.Mapper.Map<DataCollectionMessage>(dataCollectionMessageWithError);
                    queue.AddMessage(message);
                    errorQueue.DeleteMessage(dataCollectionMessageWithError);
                    counter++;
                }

                backgroundWorker1.ReportProgress(counter);
                messages = errorQueue.GetMessages(32).ToArray();
            }
        }
    }
}
