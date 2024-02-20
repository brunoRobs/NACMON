using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AvayaCTClass;

namespace Interface
{
    public delegate void EHOpenStreamConf();

    public delegate void EHMakeCallConf(int InvokeID, int CallID, string Device);

    public delegate void EHDelivered(int CallID, string Alerting, string Called, string Calling, int Cause, string ConnectionDevice, string LastRedirectionDevice, string UUI, string TrunkGroup, string TrunkMember, string UCID, string Queue);

    public delegate void EHEstablished(int CallID, string Answering, string Called, string Calling, int Cause, string ConnectionDevice, string LastRedirectionDevice, string UUI, int Reason, string UCID, string TrunkGroup, string TrunkMember);

    public delegate void EHMonitorConf(int InvokeID, int MonitorCrossRefID);

    public delegate void EHConnectionCleared(int CallID, string RealeasingDevice, string Device, int Cause);

    public delegate void EHQueryDeviceInfo(int InvokeID, string Login, string AssociatedDevice);

    public delegate void EHMonitorStopConf(int InvokeID);

    public delegate void EHOpenStreamFailure(int InvokeID, int ReturnCode);

    public delegate void EHUniversalFailure(int InvokeID, int ErrCode);

    public interface IAES
    {
        event EHOpenStreamConf OpenStreamConf;

        event EHMakeCallConf MakeCallConf;

        event EHDelivered Delivered;

        event EHEstablished Established;

        event EHMonitorConf MonitorConf;

        event EHConnectionCleared ConnectionCleared;

        event EHQueryDeviceInfo QueryDeviceInfoConf;

        event EHMonitorStopConf MonitorStopConf;

        event EHOpenStreamFailure OpenStreamFailure;

        event EHUniversalFailure UniversalFailure;

        void OpenStream(string ConnectionString, string User, string Password, string AppName);

        void Init(string ConnectionString, string User, string Password);

        void MonitorDevice(string Device);

        void MakeCall(string CallingDevice, string CalledDevice);

        void CloseStream();

        void QueryDeviceInfo(string Agent);

        void MonitorStop(int MonitorCrossRefID);
    }
}