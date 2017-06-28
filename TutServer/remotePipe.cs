using System.Windows.Forms;

namespace TutServer
{
    public partial class remotePipe : Form
    {
        private Form1 ctx;
        public string pname;
        public bool RemoteRemove = true;
        public RichTextBox outputBox;

        public remotePipe(string pipeName, Form1 context)
        {
            InitializeComponent();
            Text = "Remote Pipe Connection (" + pipeName + ")";
            pname = pipeName;
            ctx = context;
            outputBox = richTextBox1;
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string data = textBox1.Text;
                string cmd = "writeipc§" + pname + "§" + data;
                ctx.loopSend(cmd);
                textBox1.Text = "";
            }
        }

        private delegate void SetTextCallback(string text);

        private void SetText(string text)
        {
            if (InvokeRequired)
            {
                SetTextCallback stc = new SetTextCallback(SetText);
                Invoke(stc, new object[] { text });
                return;
            }

            richTextBox1.Text = text;
        }

        public void WriteOutput(string output)
        {
            SetText(output);
        }

        private void remotePipe_FormClosing(object sender, FormClosingEventArgs e)
        {
            ctx.RemovePipe(this, RemoteRemove);
        }
    }
}
