using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.CompilerServices;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            string dir = "";
            bool debug = true;

            if (debug)
            {
                dir = @"C:\aa\200-CSharp\020-WPF\WPF_Kazuki\WPF4.5\ReactiveProperty";
            }
            else
            {
                FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
                if (DialogResult.OK != folderBrowserDialog.ShowDialog())
                {
                    return;
                }
                dir = folderBrowserDialog.SelectedPath;
            }

            string[] files = Directory.GetFiles(dir,//ReactiveProperty
                "*.csproj", SearchOption.AllDirectories);

            bb parseCSProject = new bb();

            foreach (string file in files)
            {
                try
                {
                    parseCSProject.start2(file);
                }
                catch
                {

                }
            }

            this.Shown += (e1, e2) => this.Close();
        }        
    }
}
