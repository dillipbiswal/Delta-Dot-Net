using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceProcess;
using System.Configuration;
using System.IO;
using System.Threading;

namespace UpdateAgent
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Agent started Updating...");

            if (CreateBackupFolder())
            {               
                AgentLog("Backup Folder sucessfully created");
                string stragentservice = ConfigurationManager.AppSettings["AgentServiceName"];
                string stragentUpdaterservice = ConfigurationManager.AppSettings["AgentUpdaterServiceName"];

                StopService(stragentservice);
               StopService(stragentUpdaterservice);
                UpdatedAgentFiles();
                StartService(stragentservice);
                StartService(stragentUpdaterservice);  
                Console.WriteLine ("Agent Updated Sucessfully");

            }
            else
            {
                AgentLog("Agent is not installed");
                System.Console.WriteLine("Agent Updation Failed...");

            }

            System.Console.WriteLine("Press any key to exit...");
            System.Console.ReadKey();
        }       


        #region Creating New BackUp folder and files

        /// <summary>
        /// Creating new backup folder and copying all Agent files to backup folder
        /// </summary>
        /// <returns></returns>

        static bool CreateBackupFolder()
        {

            string GetfileName;
            string Backupfile="";
            DateTime dt = DateTime.Now;
            string SourcefolderName = ConfigurationManager.AppSettings["SourcePath"];
            string strBackupFolder = ConfigurationManager.AppSettings["BackupFolder"];
            string BackupPath = System.IO.Path.Combine(SourcefolderName, strBackupFolder);
            string UpadatedAgentPatch="";
            string targetPath="";

            try
            {
                // Determine whether the directory exists. 
                if (Directory.Exists(SourcefolderName))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(BackupPath);
                        AgentLog("Created backup folder " + strBackupFolder + " in " + BackupPath + "...");
                        UpadatedAgentPatch =  ConfigurationManager.AppSettings["UpadatedAgentpatch"] ;
                        targetPath = SourcefolderName;
                        AgentLog("Started copying to Backup Files ...");
                        foreach (var filesinUpadatedAgentpatch in Directory.GetFiles(UpadatedAgentPatch))
                        {                            
                            GetfileName = Path.GetFileName(filesinUpadatedAgentpatch);
                            Backupfile = Path.Combine(targetPath, GetfileName) ;                            
                            System.IO.File.Copy(Backupfile, BackupPath + @"\" + GetfileName, true);
                            AgentLog("\tBack up file " + GetfileName + " copyed to " + BackupPath);
                        }

                    }
                    catch(Exception ex)
                    {
                        AgentLog(Backupfile + ":" + BackupPath);
                        AgentLog("Unhandled Exception: " + ex.ToString());                        
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                AgentLog("Bach up folder/file error:  "+ ex.InnerException);
            }
            return false;
            
        }
        #endregion Creating New BackUp folder and files        

        #region Update Agent Files

        static void UpdatedAgentFiles()
        {
            string GetfileName;
            string Newfiles = "";
            string SourcefolderName = ConfigurationManager.AppSettings["SourcePath"];  
            string UpadatedAgentPatch = "";
            string targetPath = "";
            try
            {
                UpadatedAgentPatch = ConfigurationManager.AppSettings["UpadatedAgentpatch"];
                targetPath = SourcefolderName;
                AgentLog("Started copying to Updated Agent Files ...");
                foreach (var filesinUpadatedAgentpatch in Directory.GetFiles(UpadatedAgentPatch))
                {

                    GetfileName = Path.GetFileName(filesinUpadatedAgentpatch);
                    Newfiles = Path.Combine(targetPath, GetfileName);
                    //System.IO.File.Copy(@"C:\Test\Datavail\a.txt", @"C:\Test\Datavail\BackUp_28-5-2015\" + GetfileName, true);
                    System.IO.File.Copy(UpadatedAgentPatch + @"\" + GetfileName, Newfiles, true);
                    AgentLog("Agent file " + GetfileName + " copyed to " + SourcefolderName);

                }
            }
            catch (Exception ex)
            {
                AgentLog("Error" + ex.InnerException);
            }            
        }

        #endregion Update Agent Files

        #region Start Service

        /// <summary>
        /// Start Delta Agent Service
        /// </summary>

        public static void StartService(string strservicename)
        {
            ServiceController service = new ServiceController(strservicename);
            try
            {
                AgentLog(strservicename + " Services Started ...");

                if (service.Status.Equals(ServiceControllerStatus.Stopped) || service.Status.Equals(ServiceControllerStatus.StopPending))
                {
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(60));
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }                
            }
            catch (Exception ex)
            {
                AgentLog("Error:  " + ex.InnerException);
            }
            
        }

        #endregion Start  Service

        #region Stop  Service
        /// <summary>
        ///  Stop Delta Agent Services
        /// </summary>
        public static void StopService(string strservicename)
        {
            ServiceController service = new ServiceController(strservicename);            
            try
            {
                if (service.Status.Equals(ServiceControllerStatus.Running) || service.Status.Equals(ServiceControllerStatus.StartPending))
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(60));
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }

                AgentLog(strservicename+" Services Stoped ...");
            }
            catch (Exception ex)
            {
                AgentLog("Error:  " + ex.InnerException);
            }
            
        }

        #endregion Stop  Service

        #region Log file
        /// <summary>
        /// Agent Log File Created/updated
        /// </summary>
        /// <param name="strLogText"></param>

        static void AgentLog(string strLogText)
        {
            //string strLogText = "Some details you want to log.";

            // Create a writer and open the file:
            StreamWriter log;
           
            string strlogfile = "logfile.txt";

            if (!File.Exists(strlogfile))
            {
                log = new StreamWriter(strlogfile);
            }
            else
            {
                log = File.AppendText(strlogfile);
            }

            // Write to the file:
            log.WriteLine(DateTime.Now + ": " + strLogText);
            
            // Close the stream:
            log.Close();

        }

        #endregion Log file

    }
}
