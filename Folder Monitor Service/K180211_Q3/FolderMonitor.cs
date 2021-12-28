using System;
using System.Data;
using System.Linq;
using System.IO;
using System.Timers;
using System.ServiceProcess;
using System.Configuration;


namespace K180211_Q3
{
    public partial class FolderMonitor : ServiceBase
    {
        Timer timeDelay;
     
        static string monitoringdirectorypath = ConfigurationManager.AppSettings["directorytomonitor"];
        static string updatedfilespath = ConfigurationManager.AppSettings["updatednewfiles"];
        static string filestatus = ConfigurationManager.AppSettings["filestatus"];
        static string LogFilesPath = ConfigurationManager.AppSettings["LogsFilePath"];
        public FolderMonitor()
        {
            InitializeComponent();
            timeDelay = new Timer();
            timeDelay.Elapsed += new ElapsedEventHandler(Watcher);
            timeDelay.Interval = 60000; //1 minute= 60000 milliseconds
           
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

        private static void LogService(string content)
        {
            FileStream fs = new FileStream(LogFilesPath, FileMode.OpenOrCreate, FileAccess.Write);

            StreamWriter sw = new StreamWriter(fs);
            sw.BaseStream.Seek(0, SeekOrigin.End);
            sw.WriteLine(content);
            sw.Flush();
            sw.Close();
        }
        private void Watcher(object sender, ElapsedEventArgs e)
        {
            bool status;

            status = IsFolderUpdated();

            if (status)
            {
                //reset time to 1 minute
                timeDelay.Interval = 60000;
            }
            else //and time not 1 hour  //no changes
            {
                if(timeDelay.Interval < 3600000)
                {
                    timeDelay.Interval += 120000; //time = time + 2min
                }
               
                
            }
        }

        static bool IsFolderUpdated() //check if files in the folder is modified or there is addition of file in a folder
        {
            int current_count = Directory.GetFiles(monitoringdirectorypath, "*", SearchOption.TopDirectoryOnly).Length; //count of changes that occur in monitorfiles folder
            int old_count = GetOldCounts(); //count of files already present
            DirectoryInfo info = new DirectoryInfo(monitoringdirectorypath);
            FileInfo[] files = info.GetFiles().OrderBy(p => p.CreationTime).ToArray();

            bool status = false;
            if (old_count != current_count)
            {
                CopyUpdatedFiles();
                status = true;
            }

            //Checks for modification in file by comparing creation vs last write time
            foreach (FileInfo file in files)
            {
                DateTime creationTime = file.CreationTime;
                DateTime lastWriteTime = file.LastWriteTime;
                
                if (creationTime != lastWriteTime)
                {
                    String source = monitoringdirectorypath + "//" + file.Name;
                    String destination = updatedfilespath + "//" + file.Name;
                    File.Delete(destination);
                    File.Copy(source, destination, true);
                    status = true;

                }
            }

            return status;                
        }
        static int GetOldCounts() //get old counts of files that has been copied to updatedfile folder
        {
            int count = 0;
            using (StreamReader reader = new StreamReader(filestatus))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    count += 1;
                }
            }
            return count;
        }


        static void CopyUpdatedFiles() //copies the file from monitoringfiles folder to updated file
        {
            DirectoryInfo monitored_directory = new DirectoryInfo(monitoringdirectorypath);

            try
            {
                foreach (FileInfo finfo in monitored_directory.GetFiles())
                {
                    if (!File.Exists(updatedfilespath + finfo.Name) && FilesNotInAlreadyWatched(finfo.Name))
                    {
                        File.Copy(finfo.FullName, updatedfilespath + "\\" + finfo.Name);
                        AddToWatchedFiles(finfo.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                LogService(ex.InnerException.Message);
                throw;
            }

        }

        static bool FilesNotInAlreadyWatched(string filename) //returns false if filename is present in filestatus file
        {
            bool status = true;
            foreach (string line in File.ReadLines(filestatus))
            {
                if (line.Contains(filename))
                {
                    status = false;
                }
            }
            return status;
        }

        static void AddToWatchedFiles(string filename) //add the filename to filestatus.txt file
        {
            using (StreamWriter sw = File.AppendText(filestatus))
            {
                sw.WriteLine(filename);
            }
        }


    }
}
