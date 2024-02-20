using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;

namespace NACMON
{
    public partial class Nacmon : ServiceBase
    {
        clsController Controller = new clsController();

        string AESConnectionString, AESUser, AESPassword, PhantomExtension, AlertDestination;

        int MaxTimeToAnswer;

        List<string> AgentsList, HGExtensionsListToMonitor;

        public Nacmon()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            AESConnectionString = ConfigurationManager.AppSettings["AESConnectionString"];

            AESUser = ConfigurationManager.AppSettings["AESUser"];

            AESPassword = ConfigurationManager.AppSettings["AESPassword"];

            PhantomExtension = ConfigurationManager.AppSettings["PhantomExtension"];

            AlertDestination = ConfigurationManager.AppSettings["AlertDestination"];

            MaxTimeToAnswer = Int32.Parse(ConfigurationManager.AppSettings["MaxTimeToAnswer"]);

            AgentsList = ConfigurationManager.AppSettings["AgentsList"].Split(',').ToList<string>();

            HGExtensionsListToMonitor = ConfigurationManager.AppSettings["HGExtensionsListToMonitor"].Split(',').ToList<string>();

            Controller.Path = $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory)}{ConfigurationManager.AppSettings["Log"]}";

            Controller.Size = Int32.Parse(ConfigurationManager.AppSettings["LogSize"]);

            Controller.Init(AESConnectionString, AESUser, AESPassword, PhantomExtension, AlertDestination, MaxTimeToAnswer, AgentsList, HGExtensionsListToMonitor);
        }

        protected override void OnStop()
        {
            Controller.Stop();

            Controller.Close();
        }
    }
}
