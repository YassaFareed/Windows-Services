using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.IO;
using System.Timers;
using System.Configuration;
using Newtonsoft.Json;
using System.Net.Mail;
using System.Net;
using System.Net.Configuration;

namespace K180211_Q1
{
    public partial class SMTPService : ServiceBase
    {
        Timer timeDelay;
        static string JsonFilesPath = ConfigurationManager.AppSettings["JSONfiles"];
        static string EmailCompletedFiles = ConfigurationManager.AppSettings["EmailCompletedFiles"];
        static string LogFilesPath = ConfigurationManager.AppSettings["LogsFilePath"];
        public SMTPService()
        {
            InitializeComponent();
            timeDelay = new Timer();
            timeDelay.Interval = 900000; //15 minutes = 900000 milliseconds
            timeDelay.Elapsed += new ElapsedEventHandler(StartProcess);

        }
        protected override void OnStart(string[] args)
        {
            LogService("Service Starting");
            timeDelay.Enabled = true;
        }

        protected override void OnStop()
        {
            LogService("Service Stoping");
            timeDelay.Enabled = false;
        }

        static void LoadJsonAndSendEmail(string jsonfile)// load json into messageinfo object and send email
        {
            
            using (StreamReader r = new StreamReader(jsonfile))
            {
                string json = r.ReadToEnd();
                Message messageinfo = JsonConvert.DeserializeObject<Message>(json);
                SendEmail(messageinfo); 
            }

        }


        static void SendEmail(Message messageinfo) //send email given credentials, the subject,body and to attribute of messageinfo is used
        {
            SmtpSection smtpSection = (SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp");
            using (MailMessage mm = new MailMessage(smtpSection.From, messageinfo.to))
            {
                mm.Subject = messageinfo.subject;
                mm.Body = messageinfo.messagebody;
                mm.IsBodyHtml = false;

                SmtpClient smtp = new SmtpClient();
                smtp.EnableSsl = smtpSection.Network.EnableSsl;
                NetworkCredential networkCred = new NetworkCredential(smtpSection.Network.UserName, smtpSection.Network.Password);

                smtp.UseDefaultCredentials = smtpSection.Network.DefaultCredentials;
                smtp.Credentials = networkCred;
                smtp.Send(mm);
            }

        }
        static string ExtractFileName(string file) //extract filename given path to the file
        {
            string[] words = file.Split('\\');

            string filename = words[words.Length - 1];

            return filename;
        }

        static void WriteInFile(string filename) // write files that has been used for emails in a file
        {

            using (StreamWriter sw = File.AppendText(EmailCompletedFiles))
            {
                sw.WriteLine(filename);
            }

        }

        static bool CheckEmailCompletedStatus(string filename) //checks if file already used for email or not
        {

            bool status = true;

            if (File.Exists(EmailCompletedFiles)) //If file doesn't exist then don't need to check anything b/c it would be created in next step
            {
                List<string> previous_completed_files = File.ReadAllLines(EmailCompletedFiles).ToList();


                foreach (var name in previous_completed_files)
                {
                    if (name == filename)
                    {
                        status = false; //if exist then status = false
                    }

                }
            }
            else
            {
                File.WriteAllText(EmailCompletedFiles, String.Empty);
            }

            return status;

        }
        private void StartProcess(object sender, ElapsedEventArgs e)
        {
            try
            {
                string[] files = Directory.GetFiles(JsonFilesPath, "*.json", SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    string filename = ExtractFileName(file);
                    if (CheckEmailCompletedStatus(filename))
                    {
                        WriteInFile(filename);
                        LoadJsonAndSendEmail(file);
                    }
                    else
                    {
                        Console.WriteLine("{0} was already used to send email", filename);
                    }
                }

                Console.ReadKey();

            }
            catch (Exception ex)
            {
                LogService(ex.InnerException.Message);
                throw;
            }
        
    }


 
        private static void LogService(string content)
        {
           FileStream fs = new FileStream(LogFilesPath, FileMode.OpenOrCreate, FileAccess.Write);

            StreamWriter sw = new StreamWriter(fs);
            sw.BaseStream.Seek(0, SeekOrigin.End);
            sw.WriteLine(content);
            sw.Flush();
            sw.Close();
        }
    }
}
