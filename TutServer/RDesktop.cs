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
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
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
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
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
            System.Drawing.Rectangle scr = Screen.PrimaryScreen.WorkingArea;
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
            catch (Exception ex)
            {

            }
        }

        private void RDesktop_KeyDown(object sender, KeyEventArgs e)
        {
            if (Form1.rkeyboard == 1)
            {

                String keysToSend = "";
                if (e.Shift)
                    keysToSend += "+";
                if (e.Alt)
                    keysToSend += "%";
                if (e.Control)
                    keysToSend += "^";

                if (e.KeyValue >= 65 && e.KeyValue <= 90)
                    keysToSend += e.KeyCode.ToString().ToLower();
                else if (e.KeyCode.ToString().Equals("Back"))
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

                parent.loopSend("rtype-" + keysToSend);
            }
        }

        private void RDesktop_Shown(object sender, EventArgs e)
        {
            Timer t = new Timer();
            t.Interval = 100;
            t.Tick += new EventHandler(updateImage);
            t.Start();
        }

        private void updateImage(object sender, EventArgs e)
        {
            if (image != null)
            {
                pictureBox1.Image = image;
            }
        }
    }
}
