using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using OpenPop.Pop3;
using System.Diagnostics;

namespace MailCheck
{
    public partial class Form1 : Form
    {
        //path kjer shranjujemo id-je
        public static string path;// = @"data.txt";

        //id-ji iz .txt
        public static List<string> listStaro = new List<string>();

        public static List<string> listNovo = new List<string>();

        public Form1()
        {
            InitializeComponent();
        }

        //on load
        private void Form1_Load(object sender, EventArgs e)
        {
            path = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName;
            if ( Environment.OSVersion.Version.Major >= 6 ) 
            {
                path = Directory.GetParent(path).ToString();
            }
            path += @"\data.txt";
            
            if (Properties.Settings.Default.provider != "") //nismo 1. tukaj
            {
                this.Opacity = 0; //skrijemo formo

                try //probamo uvozit že prebrane
                {
                    var logFile = File.ReadAllLines(path); //preberemo ze prebrane maile
                    listStaro = new List<string>(logFile);
                }
                catch (Exception ex)
                {
                    //prebrani ne obstajajo
                    Debug.Print("Problemi pri readu iz fajla!");
                }

                Debug.Print("start");
                GetNew(); //osvežimo
                Debug.Print("over");

                //preštejemo nove maile in jih izpišemo
                notifyIcon1.BalloonTipText = "You have " + listNovo.Count + " new mails!";
                notifyIcon1.ShowBalloonTip(50);

                //zaženemo timer
                timer1.Start();
            }
        }

        //zagon button
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                //shranimo v settings
                Properties.Settings.Default.provider = tProvider.Text;
                Properties.Settings.Default.port = Int32.Parse(tPort.Text);
                Properties.Settings.Default.user = tUser.Text;
                Properties.Settings.Default.pass = tPass.Text;
                Properties.Settings.Default.encrypt = cEnc.Checked;
                Properties.Settings.Default.Save();

                //pisem v registre za startup
                if (checkBox1.Checked)
                {
                    RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    rk.SetValue("MailCheck", Application.ExecutablePath.ToString());
                }

                //lunch app
                this.Opacity = 0;
                GetNew(); //posodobimo za maile
                notifyIcon1.BalloonTipText = "You have " + listNovo.Count + " new mails!";
                notifyIcon1.ShowBalloonTip(50);

                //start timer1
                timer1.Start();

            }
            catch (Exception ex)
            {
                Properties.Settings.Default.Reset();
                RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                rk.DeleteValue("MailCheck", false);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                MessageBox.Show("Something goes wrong!");
            }
        }

        //posodobimo nove maile
        public static void GetNew()
        {
            try
            {
                string hostname = Properties.Settings.Default.provider;
                int port = Properties.Settings.Default.port;
                bool useSsl = Properties.Settings.Default.encrypt;
                string username = Properties.Settings.Default.user;
                string password = Properties.Settings.Default.pass;

                using (Pop3Client client = new Pop3Client())
                {
                    client.Connect(hostname, port, useSsl);
                    client.Authenticate(username, password);
                    List<string> uids = client.GetMessageUids();

                    for (int i = 0; i < uids.Count; i++)
                    {
                        string currentUidOnServer = uids[i];

                        if (!listStaro.Contains(currentUidOnServer) && !listNovo.Contains(currentUidOnServer))
                        {
                            listNovo.Add(currentUidOnServer);
                        }
                    }

                }
            }
            catch { }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            GetNew();
            if (listNovo.Count != 0)
            {
                //nova sporočila
                notifyIcon1.BalloonTipText = "You have " + listNovo.Count + " new mails!";
                notifyIcon1.ShowBalloonTip(50);
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void FlashSaved_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Reset();
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            rk.DeleteValue("MailCheck", false);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            Application.Exit();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                timer1.Stop();
                GetNew();
                notifyIcon1.BalloonTipText = "You have " + listNovo.Count + " new mails!";
                notifyIcon1.ShowBalloonTip(50);
                timer1.Start();
            }
        }

        private void makeAsReadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listStaro.AddRange(listNovo);
            listNovo.Clear();
            File.WriteAllLines(path, listStaro);
        }
    }
}
