using System;
using System.Drawing;
using System.Windows.Forms;

namespace TutServer
{
    public partial class RDesktop : Form
    {




        Form1 parent = new Form1();
        public Bitmap image;

        public RDesktop()
        {
            InitializeComponent();
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (Form1.rmouse == 1)
            {
                if (e.Button == MouseButtons.Left)
                {
                    parent.loopSend("rclick-left-down");
                }

                else
                {
                    parent.loopSend("rclick-right-down");
                }
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (Form1.rmouse == 1)
            {
                if (e.Button == MouseButtons.Left)
                {
                    parent.loopSend("rclick-left-up");
                }

                else
                {
                    parent.loopSend("rclick-right-up");
                }
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            Rectangle scr = Screen.PrimaryScreen.WorkingArea;
            if (!Form1.isrdFull)
            {
                scr = pictureBox1.DisplayRectangle;
            }
            //Console.Title = scr.Width + ";" + scr.Height;
            try
            {
                int mx = (e.X * Form1.resx) / scr.Width;
                int my = (e.Y * Form1.resy) / scr.Height;

                if (Form1.rmouse == 1)
                {
                    if (Form1.plx != e.X || Form1.ply != e.Y)
                    {
                        parent.loopSend("rmove-" + mx + ":" + my);
                        Form1.plx = e.X;
                        Form1.ply = e.Y;
                        // Program.xConsole(mx + ";" + my);
                    }
                }
            }
            catch (Exception )
            {
              //  MessageBox.Show("ERROR = " + ex.Message);
            }
        }
        // can now send lowercase and uppercase
        private void RDesktop_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                contextMenuStrip1.Show();
            }


            if (Form1.rkeyboard == 1)         
            {

                string keysToSend = "";

                if (e.Shift)
                    keysToSend += "+";
                if (e.Alt)
                    keysToSend += "%";
                if (e.Control)
                    keysToSend += "^";

                if (Console.CapsLock == false)//--added this to send uppercase needs more work it wont work properlly without this
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


                 if (e.KeyCode.ToString().Equals("Back"))
                    keysToSend += "{BS}";
                else if (e.KeyCode.ToString().Equals("Pause"))
                    keysToSend += "{BREAK}";
                else if (e.KeyCode.ToString().Equals("Capital"))
                    keysToSend += "{CAPSLOCK}";
                else if (e.KeyCode.ToString().Equals("Space"))
                    keysToSend += " ";
                else if (e.KeyCode.ToString().Equals("Home"))
                    keysToSend += "{HOME}";
                else if (e.KeyCode.ToString().Equals("Return"))
                    keysToSend += "{ENTER}";
                else if (e.KeyCode.ToString().Equals("End"))
                    keysToSend += "{END}";
                else if (e.KeyCode.ToString().Equals("Tab"))
                    keysToSend += "{TAB}";
                else if (e.KeyCode.ToString().Equals("Escape"))
                    keysToSend += "{ESC}";
                else if (e.KeyCode.ToString().Equals("Insert"))
                    keysToSend += "{INS}";
                else if (e.KeyCode.ToString().Equals("Up"))
                    keysToSend += "{UP}";
                else if (e.KeyCode.ToString().Equals("Down"))
                    keysToSend += "{DOWN}";
                else if (e.KeyCode.ToString().Equals("Left"))
                    keysToSend += "{LEFT}";
                else if (e.KeyCode.ToString().Equals("Right"))
                    keysToSend += "{RIGHT}";
                else if (e.KeyCode.ToString().Equals("PageUp"))
                    keysToSend += "{PGUP}";
                else if (e.KeyCode.ToString().Equals("Next"))
                    keysToSend += "{PGDN}";
                else if (e.KeyCode.ToString().Equals("Tab"))
                    keysToSend += "{TAB}";
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

               // Form1 f1 = new Form1();
                //f1.loopSend("rtype-" + keysToSend);
                parent.loopSend("rtype-" + keysToSend);
            }
        }

        private void RDesktop_Shown(object sender, EventArgs e)
        {
            Timer t = new Timer();
            t.Interval = FPS;
            t.Tick += new EventHandler(updateImage);
            t.Start();
        }

        private void updateImage(object sender, EventArgs e)
        {
                ScreenFPS(); //this to set the fps
                
                pictureBox1.Image = image;


            GC.Collect();  //-----added this to cleanup resources
            GC.WaitForPendingFinalizers();
            System.Threading.Thread.SpinWait(5000);


        }
        //----close this form to give focus back to the sizable window in tab control
        private void closeWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
                Form1 f1 = new Form1();
           
            if(f1.checkBoxrKeyboard.Checked)
            {
                f1.checkBoxrKeyboard.Checked = false;
            }
            if (f1.checkBoxrMouse.Checked)
            {
                f1.checkBoxrMouse.Checked = false;
            }

            Form1.isrdFull = false; //reset the picture back to form1 picturebox1
            

            
                Close(); 
           
        
        }

       
        //SCREEN FPS
        private int FPS = 80;
        public void ScreenFPS()
        {
            Form1 f1 = new Form1();
            int value = f1.trackBar1.Value;
            f1.lblQualityShow.Text = value.ToString();

            if (value < 25)  //frames per second the client also has to match this so a send cmd
                FPS = 100;  //low
            else if (value >= 85)
                FPS = 80; //best
            else if (value >= 75)
                FPS = 50; //high
            else if (value >= 25)
                FPS = 150; //mid
        }

     
    }
}
