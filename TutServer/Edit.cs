using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace TutServer
{
    public partial class Edit : Form
    {
        private String content;
        private Form1 prt;

        public Edit(String textFile, Form1 parent)
        {
            content = textFile;
            prt = parent;
            InitializeComponent();
        }

        private void Edit_Shown(object sender, EventArgs e)
        {
            richTextBox1.ReadOnly = true;
            richTextBox1.BackColor = SystemColors.Window;
            richTextBox1.Text = content;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            bool isReadOnly = !checkBox1.Checked;
            richTextBox1.ReadOnly = isReadOnly;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                String[] lines = richTextBox1.Lines;
                List<String> decrypted = new List<String>();
                foreach (String line in lines)
                {
                    decrypted.Add(prt.Decrypt(line));
                }
                richTextBox1.Lines = decrypted.ToArray();
                richTextBox1.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error", "Error trying to decrypt content!\n" + ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                String[] lines = richTextBox1.Lines;
                List<String> encrypted = new List<String>();
                foreach (String line in lines)
                {
                    encrypted.Add(prt.Encrypt(line));
                }
                richTextBox1.Lines = encrypted.ToArray();
                richTextBox1.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error", "Error trying to encrypt content!\n" + ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            richTextBox1.Redo();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            richTextBox1.Undo();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            prt.saveFile(richTextBox1.Text);
        }
    }
}
