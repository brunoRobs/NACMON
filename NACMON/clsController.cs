using Desafio_BSA;
using Log;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
//using System.Windows.Forms;
using ScanTimer = System.Timers.Timer;

namespace NACMON
{
    public class clsController : IController
    {
        #region NACMONComponents

        string AESConnectionString, AESUser, AESPassword, PhantomExtension, AlertDestination;

        int MaxTimeToAnswer;

        Dictionary<string, KeyValuePair<string, int>> AgentsList = new Dictionary<string, KeyValuePair<string, int>>();

        List<string> HGExtensionsListToMonitor;

        Dictionary<string, JsonObject> Nacmon = new Dictionary<string, JsonObject>();

        #endregion

        clsAES mAES = new clsAES();

        clsLog Log = new clsLog();

        public string Path;

        public int Size;

        Thread ScannerCNA, ScannerAgents;

        ScanTimer TimerCNA, TimerAgents;

        bool Locked, Sleeping;

        string LogMonitorAgent;

        string LogAgentStop, LogDeviceUpdate, LogAgentDelete;

        enum MonitorStopCase
        {
            Delete = -1,
            Update = 0,
            Stop = 1
        }

        MonitorStopCase Operation;

        //#region FormComponents

        //private Form mParentForm;

        //public delegate void FormListBox(string Message);

        //public event FormListBox ListBox;

        //public Form ParentForm
        //{
        //    get { return mParentForm; }
        //    set
        //    {
        //        mParentForm = value;
        //    }

        //}

        //#endregion

        #region Events

        private void MAES_OpenStreamConf()
        {
            Log.Log($"{DateTime.Now} - Conexão AES efetuada\n\n");
        }

        private void MAES_Delivered(int CallID, string Alerting, string Called, string Calling, int Cause, string ConnectionDevice, string LastRedirectionDevice, string UUI, string TrunkGroup, string TrunkMember, string UCID, string Queue)
        {
            Locked = true;

            if (HGExtensionsListToMonitor.Contains(Queue))
            {
                if (!Nacmon.ContainsKey($"{CallID}!{ConnectionDevice}"))
                {
                    lock (Nacmon)
                    {
                        JsonObject properties = new JsonObject();

                        properties.Add("CallID", CallID);

                        properties.Add("Alerting", Alerting);

                        properties.Add("Called", Called);

                        properties.Add("Calling", Calling);

                        properties.Add("Cause", Cause);

                        properties.Add("ConnectionDevice", ConnectionDevice);

                        properties.Add("LastRedirectionDevice", LastRedirectionDevice);

                        properties.Add("UUI", UUI);

                        properties.Add("TrunkGroup", TrunkGroup);

                        properties.Add("TrunkMember", TrunkMember);

                        properties.Add("UCID", UCID);

                        properties.Add("Queue", Queue);

                        string Hour = $"{DateTime.Now.ToShortTimeString()}:{DateTime.Now.Second.ToString("D2")}";

                        properties.Add("DELIVEREDTS", Hour);

                        properties.Add("LASTALERTCALL", "");

                        Nacmon[$"{CallID}!{ConnectionDevice}"] = properties;

                        Log.Log($"{DateTime.Now} - NACMON - {ConnectionDevice} adicionado \n\n");
                    }
                }
            }

            Locked = false;
        }

        private void MAES_Established(int CallID, string Answering, string Called, string Calling, int Cause, string ConnectionDevice, string LastRedirectionDevice, string UUI, int Reason, string UCID, string TrunkGroup, string TrunkMember)
        {
            Locked = true;

            if (Nacmon.ContainsKey($"{CallID}!{Answering}"))
            {
                lock (Nacmon)
                {
                    Nacmon.Remove($"{CallID}!{Answering}");

                    Log.Log($"{DateTime.Now} - NACMON - {Answering} removido - ({Cause} - Em chamada)\n\n");
                }
            }

            Locked = false;
        }

        private void MAES_ConnectionCleared(int CallID, string RealeasingDevice, string Device, int Cause)
        {
            Locked = true;

            if (Nacmon.ContainsKey($"{CallID}!{Device}"))
            {
                lock (Nacmon)
                {
                    Nacmon.Remove($"{CallID}!{Device}");

                    Log.Log($"{DateTime.Now} - NACMON - {Device} removido - ({Cause} - Em desconexão)\n\n");
                }
            }

            Locked = false;
        }

        private void MAES_QueryDeviceInfoConf(int InvokeID, string Login, string AssociatedDevice)
        {
            Locked = true;

            if (!AssociatedDevice.Equals(""))
            {
                if (AgentsList.ContainsKey(Login))
                {
                    if (!AgentsList[Login].Key.Equals(AssociatedDevice))
                    {
                        if (AgentsList[Login].Key.Equals(""))
                        {
                            LogMonitorAgent = Login;

                            Log.Log($"{DateTime.Now} - Nova tentativa de monitoramento: {Login}\n\n");

                            AgentsList[Login] = new KeyValuePair<string, int>(AssociatedDevice, 0);

                            mAES.MonitorDevice(AssociatedDevice);
                        }
                        else
                        {
                            Operation = MonitorStopCase.Update;

                            LogDeviceUpdate = AgentsList[Login].Key;

                            LogMonitorAgent = Login;

                            AgentsList[Login] = new KeyValuePair<string, int>(AssociatedDevice, AgentsList[Login].Value);

                            mAES.MonitorStop(AgentsList[Login].Value);
                        }
                    }
                    else if (AgentsList[Login].Value != 0)
                    {
                        Log.Log($"{DateTime.Now} - {Login} já está em monitoramento\n\n");
                    }
                    else
                    {
                        Log.Log($"{DateTime.Now} - Nova tentativa de monitoramento: {Login}\n\n");

                        LogMonitorAgent = Login;

                        mAES.MonitorDevice(AssociatedDevice);
                    }
                }
                else
                {
                    AgentsList[Login] = new KeyValuePair<string, int>(AssociatedDevice, 0);

                    LogMonitorAgent = Login;

                    mAES.MonitorDevice(AssociatedDevice);
                }
            }
            else
            {
                if (AgentsList.ContainsKey(Login) && !AgentsList[Login].Key.Equals(AssociatedDevice))
                {
                    Operation = MonitorStopCase.Delete;

                    mAES.MonitorStop(Int32.Parse(AgentsList[Login].Value.ToString()));

                    AgentsList[Login] = new KeyValuePair<string, int>("", -1);

                    LogAgentDelete = Login;
                }
                else
                {
                    AgentsList[Login] = new KeyValuePair<string, int>("", -1);

                    Log.Log($"{DateTime.Now} - Não foi possível concluir o monitoramento: {Login} indisponível\n\n");
                }
            }

            Locked = false;
        }

        private void MAES_MonitorConf(int InvokeID, int MonitorCrossRefID)
        {
            Locked = true;

            if (AgentsList[LogMonitorAgent].Value == 0)
            {
                AgentsList[LogMonitorAgent] = new KeyValuePair<string, int>(AgentsList[LogMonitorAgent].Key, MonitorCrossRefID);

                Log.Log($"{DateTime.Now} - Monitoramento concluído: {LogMonitorAgent} - {AgentsList[LogMonitorAgent].Key} - {MonitorCrossRefID}\n\n");
            }
            else
            {
                AgentsList[LogMonitorAgent] = new KeyValuePair<string, int>(AgentsList[LogMonitorAgent].Key, MonitorCrossRefID);

                Log.Log($"{DateTime.Now} - Monitoramento atualizado: {LogMonitorAgent} - {AgentsList[LogMonitorAgent].Key} - {MonitorCrossRefID}\n\n");
            }

            LogMonitorAgent = "";

            Locked = false;
        }

        private void MAES_MonitorStopConf(int InvokeID)
        {
            if (Operation == MonitorStopCase.Stop)
            {
                Log.Log($"{DateTime.Now} - Monitoramento geral encerrado: {LogAgentStop.ToString()}");

                LogAgentStop = "";
            }
            else if (Operation == MonitorStopCase.Update)
            {
                mAES.MonitorDevice(LogDeviceUpdate.ToString());

                LogDeviceUpdate = "";
            }
            else
            {
                Log.Log($"{DateTime.Now} - Monitoramento encerrado: {LogAgentDelete.ToString()} indisponível\n\n");

                LogAgentDelete = "";
            }
        }

        private void MAES_MakeCallConf(int InvokeID, int CallID, string Device)
        {
            Log.Log($"{DateTime.Now} - Ligação de alerta: {Device} -> {AlertDestination}\n\n");
        }

        private void MAES_OpenStreamFailure(int InvokeID, int ReturnCode)
        {
            Log.Log($"{DateTime.Now} - Falha de inicialização: {ReturnCode}\n\n");
        }

        private void MAES_UniversalFailure(int InvokeID, int ErrCode)
        {
            Log.Log($"{DateTime.Now} - Falha universal detectada: {ErrCode}\n\n");
        }

        #endregion

        #region Methods

        public void Init(string AESConnectionString, string AESUser, string AESPassword, string PhantomExtension, string AlertDestination, int MaxTimeToAnswer, List<string> AgentsList, List<string> HGExtensionsListToMonitor)
        {
            if (Sleeping)
            {
                Sleeping = false;

                MonitorAgent(AgentsList);
            }
            else
            {
                this.AESConnectionString = AESConnectionString;

                this.AESUser = AESUser;

                this.AESPassword = AESPassword;

                this.PhantomExtension = PhantomExtension;

                this.AlertDestination = AlertDestination;

                this.MaxTimeToAnswer = MaxTimeToAnswer;

                this.HGExtensionsListToMonitor = HGExtensionsListToMonitor;

                Log.Init(Path, Size);

                mAES.Init(this.AESConnectionString, this.AESUser, this.AESPassword);

                Log.Log($"{DateTime.Now} - NACMON iniciado\n\n");

                ScannerCNA = new Thread(StartScannerCNA)
                {
                    Name = "ScannerCNA",
                    IsBackground = true
                };

                ScannerAgents = new Thread(StartScannerAgents)
                {
                    Name = "ScannerAgents",
                    IsBackground = true
                };

                mAES.OpenStreamConf += MAES_OpenStreamConf;

                mAES.Delivered += MAES_Delivered;

                mAES.Established += MAES_Established;

                mAES.QueryDeviceInfoConf += MAES_QueryDeviceInfoConf;

                mAES.ConnectionCleared += MAES_ConnectionCleared;

                mAES.MonitorConf += MAES_MonitorConf;

                mAES.MakeCallConf += MAES_MakeCallConf;

                mAES.OpenStreamFailure += MAES_OpenStreamFailure;

                mAES.UniversalFailure += MAES_UniversalFailure;

                mAES.MonitorStopConf += MAES_MonitorStopConf;

                ScannerCNA.Start();

                ScannerAgents.Start();

                MonitorAgent(AgentsList);
            }
        }

        public void Stop()
        {
            Operation = MonitorStopCase.Stop;

            Locked = true;

            for (int i = 0; i < AgentsList.Count; i++)
            {
                var Agent = AgentsList.ElementAt(i);

                LogAgentStop = Agent.Key;

                mAES.MonitorStop(Agent.Value.Value);

                AgentsList[Agent.Key] = new KeyValuePair<string, int>("", -1);
            }

            Nacmon.Clear();

            Sleeping = true;
        }

        public void Close()
        {
            mAES.CloseStream();

            Log.Log($"{DateTime.Now} - NACMON encerrado\n\n");
        }

        public void MonitorAgent(List<string> Agents)
        {
            foreach (var Agent in Agents) mAES.QueryDeviceInfo(Agent);
        }

        #endregion

        #region Scanners

        private void StartScannerCNA()
        {
            TimerCNA = new ScanTimer(1000);

            TimerCNA.Elapsed += ScanCNA;

            TimerCNA.Start();
        }

        public void ScanCNA(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!Sleeping)
            {
                if (Nacmon.Count > 0 && !Locked)
                {
                    lock (Nacmon)
                    {
                        foreach (var Agent in Nacmon)
                        {
                            int DeliveredSeconds = Int32.Parse(Agent.Value["DELIVEREDTS"].GetValue<string>().Substring(6));

                            if ((DeliveredSeconds + MaxTimeToAnswer) % 60 == DateTime.Now.Second)
                            {
                                if (Agent.Value["LASTALERTCALL"].GetValue<string>().Equals(""))
                                {
                                    Log.Log($"{DateTime.Now} - CNA Detectada: {Agent.Value["ConnectionDevice"].GetValue<string>()}\n\n");

                                    mAES.MakeCall(PhantomExtension, AlertDestination);

                                    Agent.Value["LASTALERTCALL"] = $"{DateTime.Now.ToShortTimeString()}:{DateTime.Now.Second.ToString("D2")}";

                                    Thread.Sleep(5000);
                                }
                                else
                                {
                                    int LACMinutes = Int32.Parse(Agent.Value["LASTALERTCALL"].GetValue<string>().Substring(3, 2));

                                    int LACSeconds = Int32.Parse(Agent.Value["LASTALERTCALL"].GetValue<string>().Substring(6, 2));

                                    if ((DateTime.Now.Minute == LACMinutes + 2 && DateTime.Now.Second == LACSeconds) && (DeliveredSeconds + MaxTimeToAnswer) % 60 == DateTime.Now.Second)
                                    {
                                        Log.Log($"{DateTime.Now} - CNA Retomada: {Agent.Value["ConnectionDevice"].GetValue<string>()}\n\n");

                                        mAES.MakeCall(PhantomExtension, AlertDestination);

                                        Agent.Value["LASTALERTCALL"] = $"{DateTime.Now.ToShortTimeString()}:{DateTime.Now.Second.ToString("D2")}";

                                        Thread.Sleep(5000);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Thread.Sleep(1000);
            }
        }

        private void StartScannerAgents()
        {
            TimerAgents = new ScanTimer(60000);

            TimerAgents.Elapsed += ScanAgents;

            TimerAgents.Start();
        }

        public void ScanAgents(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!Sleeping)
            {
                if (AgentsList.Count > 0 && !Locked)
                {
                    foreach (var Agent in AgentsList) mAES.QueryDeviceInfo(Agent.Key);
                }
            }
            else
            {
                Thread.Sleep(60000);
            }
        }

        #endregion
    }
}