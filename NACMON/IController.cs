using Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NACMON
{
    interface IController
    {
        void Init(string AESConnectionString, string AESUser, string AESPassword, string PhantomExtension, string AlertDestination, int MaxTimeToAnswer, List<string> AgentList, List<string> HGExtensionListToMonitor);

        void Stop();

        void Close();

        void MonitorAgent(List<string> Agents);

        void ScanCNA(object sender, System.Timers.ElapsedEventArgs e);

        void ScanAgents(object sender, System.Timers.ElapsedEventArgs e);
    }
}