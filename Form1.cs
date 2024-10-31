using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using DarkModeForms;

namespace MPConfigselector
{
    public partial class MPStarterForm : DarkModeBaseForm
    {

        //First is the filename which will be an unique key, second is the value with the description
        public Dictionary<string,string> configfiles;


        public MPStarterForm()
        {
            InitializeComponent();
            base.dm = new DarkModeCS(this);
            getConfigFilesList();
            //set status bar text to the path of the Mission Planner executable
            toolStripStatusLabel1.Text = getMPPath();

        }

        string getMPPath()
        {
            string mplocSetting = Properties.Settings.Default.MPLocation;
            var regex = new Regex("([%][^%]+[%])");
            string mploc = regex.Replace(mplocSetting, (match) => {
                // get rid of %%
                string value = match.Value.Substring(1, match.Value.Length - 2);
                var specialFolder = (Environment.SpecialFolder)Enum.Parse(typeof(Environment.SpecialFolder), value, true);
                return Environment.GetFolderPath(specialFolder);
            });
            return mploc;

        }


        private void getConfigFilesList()
        {
            string configdir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            configfiles = new Dictionary<string, string>();
            string[] f = Directory.GetFiles(configdir + @"\Mission Planner\", "config*.xml");

            foreach (var name in f)
            {
                configfiles.Add(Path.GetFileName(name), getConfigName(name));
            }

            bindingSource1.DataSource = configfiles;
            listBox1.DisplayMember = "Value";
            listBox1.ValueMember = "Key";
            listBox1.DataSource = bindingSource1;
            listBox1.SelectedIndex = 0;

        }

        private string getConfigName(string filepath)
        {
            string retval = Path.GetFileName(filepath);
            try
            {
                using (XmlTextReader xmlreader = new XmlTextReader(filepath))
                {
                    while (xmlreader.Read())
                    {
                        if (xmlreader.NodeType == XmlNodeType.Element)
                        {
                            try
                            {
                                switch (xmlreader.Name)
                                {
                                    case "Config":
                                        break;
                                    case "xml":
                                        break;
                                    default:
                                        var key = xmlreader.Name;
                                        var value = xmlreader.ReadString();
                                        if (key == "MPConfigDesc")
                                        {
                                            retval = value;
                                        }
                                        break;
                                }
                            }
                            // silent fail on bad entry
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }

            if (retval == "config.xml") retval = "Default Config";
            return retval;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Console.WriteLine(listBox1.SelectedValue);
            //get the full filename from the selected item datasource
            string filename = listBox1.SelectedValue.ToString();
            string app = getMPPath();
            try
            {
                Process.Start(app, "-config " + filename);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting Mission Planner! " + ex.Message + "\r\n" + app, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Console.WriteLine(listBox1.SelectedValue);
        }

        private void bNew_Click(object sender, EventArgs e)
        {
            string fileName = "";
            if (InputBox.Show("New config file","Enter the name of the new file", ref fileName) == DialogResult.OK)
            {

                var validFileName = Path.GetInvalidFileNameChars().Aggregate(fileName, (f, c) => f.Replace(c, '_'));
                Console.WriteLine(validFileName);

                configfiles.Add("config_" + validFileName + ".xml", fileName);


                bindingSource1.DataSource = null;
                bindingSource1.DataSource = configfiles;

                Console.WriteLine();
            }

        }

        private void bDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure? The config file will be deleted!", "You're about to delete a config file",
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
            {
                string filename = listBox1.SelectedValue.ToString();
                string configdir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                File.Delete(configdir + @"\Mission Planner\" + filename);
                configfiles.Remove(filename);
                bindingSource1.DataSource = null;
                bindingSource1.DataSource = configfiles;
            }

        }

        private void bSetMpLoc_Click(object sender, EventArgs e)
        {
            //Open file select dialog and get the filename and path for MissionPlanner.exe
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Executable Files|*.exe";
            openFileDialog1.Title = "Select Mission Planner executable";
            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.MPLocation = openFileDialog1.FileName;
                Properties.Settings.Default.Save();
                toolStripStatusLabel1.Text = getMPPath();

            }
        }
    }
}
