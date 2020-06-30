using System;
using System.IO;
using System.Windows.Forms;

namespace LB.PhotoResizer
{
    public partial class MainForm : Form
    {
        #region members
        private string[] _filePathList;
        private int _imagesToResize;
        #endregion

        public MainForm() 
        {
            InitializeComponent();
        }

        private void openImageToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open Image(s)";
            dlg.Filter = "Image Files (*.jpg)|*.jpg";
            dlg.Multiselect = true;

            if (dlg.ShowDialog() == DialogResult.OK)
                this.PopulateImageListFromFiles(dlg.FileNames);
        }
        
        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = "Select the folder containing the images you wish to resize.";
            dlg.ShowNewFolderButton = false;

            if (dlg.ShowDialog() == DialogResult.OK)
                this.PopulateImageListFromFolder(dlg.SelectedPath);
        }
        
        /// <summary>
        /// Gets the selected images into the image list.
        /// </summary>
        private void PopulateImageListFromFiles(string[] filenames) 
        {
            _filePathList = filenames;

            for (int i = 0; i < filenames.Length; i++)
                this.ImageList.Items.Add(new ListViewItem(Path.GetFileName(filenames[i])));

            this.StatusLabel.Text = filenames.Length + " image(s) selected";

            if (filenames.Length > 0)
            {
                this.ClearListBtn.Enabled = true;
                this.ResizeBtn.Enabled = true;
            }
        }

        /// <summary>
        /// Gets the images in a folder into the image list.
        /// </summary>
        private void PopulateImageListFromFolder(string folderPath) 
        {
            // build a collection of image filenames in the selected folder.
            this.PopulateImageListFromFiles(Directory.GetFiles(folderPath, "*.jpg"));
        }

        /// <summary>
        /// Removes all entries in the image list.
        /// </summary>
        private void ClearListBtn_Click(object sender, EventArgs e) 
        {
            this.ImageList.Items.Clear();
            this.StatusLabel.Text = String.Empty;
            this.ClearListBtn.Enabled = false;
            this.ResizeBtn.Enabled = false;
            this.ProgressBar.Value = 0;
        }

        /// <summary>
        /// Show the About dialog.
        /// </summary>
        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e) 
        {
            AboutBox dlg = new AboutBox();
            dlg.Show();
        }

        /// <summary>
        /// Close the program.
        /// </summary>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            Application.Exit();
        }

        /// <summary>
        /// Handles the execution of the resizing process.
        /// </summary>
        private void ResizeBtn_Click(object sender, EventArgs e) 
        {
            int width = 0;
            _imagesToResize = _filePathList.Length;

            if ((string)this.ResolutionList.SelectedItem == "Tiny (150x120)")
                width = 150;
            else if ((string)this.ResolutionList.SelectedItem == "Normal (800x600)")
                width = 800;
            else if ((string)this.ResolutionList.SelectedItem == "Large (1024x768)")
                width = 1024;
            else if ((string)this.ResolutionList.SelectedItem == "Supersize (1600x1200)")
                width = 1600;

            Resizer resizer = new Resizer();
            resizer.OnResizedEvent += new EventHandler(this.ImageResizedEventHandler);
            resizer.Filenames = _filePathList;
            resizer.ImageOutputQuality = 90L;
            resizer.ImageWidth = width;
            resizer.SaveToDesktop = true;

            this.ProgressBar.Maximum = _imagesToResize;

            // resize!
            resizer.Resize();
            MessageBox.Show("Resizing Complete!");
        }

        /// <summary>
        /// Handles the progression of the progress bar.
        /// </summary>
        private void ImageResizedEventHandler(object sender, EventArgs e) 
        {
            this.ProgressBar.Increment(1);
        }
    }
}