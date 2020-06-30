using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace LB.PhotoResizer
{
    /// <summary>
    /// Is responsible for handling all processing actions for resizing images.
    /// </summary>
    class Resizer
    {
        #region members
        private bool _resizeOriginals;
        private bool _saveToDesktop;
        private string _savePath;
        private long _quality;
        private int _width;
        private string[] _filenames;
        #endregion

        #region accessors
        /// <summary>
        /// Determines if the resized images should be placed in a new folder on the desktop. This option
        /// contradicts resizing the originals option.
        /// </summary>
        public bool SaveToDesktop { get { return _saveToDesktop; } set { _saveToDesktop = value; } }
        /// <summary>
        /// Determines if the original images should be resized themselves. This option contradicts the
        /// option to save to another path.
        /// </summary>
        public bool ResizeOriginals { get { return _resizeOriginals; } set { _resizeOriginals = value; } }
        /// <summary>
        /// Specifies a particular path to output the resized images to. This option contradicts the ability
        /// to resize the originals.
        /// </summary>
        public string SavePath { get { return _savePath; } set { _savePath = value; } }
        /// <summary>
        /// The final output image quality. 90 by default.
        /// </summary>
        public long ImageOutputQuality 
        { 
            get { return _quality; } 
            set 
            { 
                _quality = value;
                if (_quality > 100)
                    _quality = 100;

                if (_quality < 1)
                    _quality = 1;
            } 
        }
        /// <summary>
        /// Determines the output image width in pixels, or the height if it's a portrait image.
        /// Images will not be enlarged, so if an image is smaller than the ImageWidth then it will
        /// be left as is.
        /// </summary>
        public int ImageWidth { get { return _width; } set { _width = value; } }
        /// <summary>
        /// The list of images and their paths to be resized. Critical to the process.
        /// </summary>
        public string[] Filenames { get { return _filenames; } set { _filenames = value; } }
        #endregion

        #region constructors
        /// <summary>
        /// Creates a new Resizer object.
        /// </summary>
        public Resizer() 
        {
            this.SetDefaultOptions();
        }
        #endregion

        #region public methods
        /// <summary>
        /// Performs the image resizings by taking into consideration the options (properties) and a collection
        /// of image paths by parameter.
        /// </summary>
        /// <param name="imagePaths">The list of image paths to resize.</param>
        public void Resize() 
        {
            if (this.Filenames == null || this.Filenames.Length < 1)
                return;

            // set the output path, create any folders necessary.
            string outputPath = this.DetermineOutputPath();

            foreach (string path in this.Filenames)
            {
                this.ResizeImage(path, outputPath);

                // fire off an OnImageResized event.
                if (OnResizedEvent != null)
                    OnResizedEvent(this, EventArgs.Empty);
            }

            // all done, clean-up.
            if (this.ResizeOriginals)
                this.ReplaceOriginals(outputPath);
        }
        #endregion

        #region private methods
        /// <summary>
        /// Sets the class options to their default values.
        /// </summary>
        private void SetDefaultOptions() 
        {
            _saveToDesktop = true;
            _savePath = String.Empty;
            _quality = 90;
            _width = 800;
        }

        /// <summary>
        /// Determines where to save the files to. Creates the path if it doesn't exist.
        /// </summary>
        private string DetermineOutputPath() 
        {
            if (this.SaveToDesktop)
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

                // require a unique path.
                bool isUnique = false;
                string folderName = "PhotoResizer Images";
                string path = String.Empty;

                while (!isUnique)
                {
                    if (Directory.Exists(desktop + "\\" + folderName))
                    {
                        if (folderName == "PhotoResizer Images")
                            folderName += " -";

                        folderName += DateTime.Now.Second;
                    }
                    else
                    {
                        isUnique = true;
                    }
                }

                path = desktop + "\\" + folderName;
                Directory.CreateDirectory(path);
                return path;
            }
            else if (this.ResizeOriginals)
            {
                // save to a temp sub-folder and move back when complete.
                string path = Path.GetFullPath(this.Filenames[0]) + "\\tmp";
                Directory.CreateDirectory(path);
                return path;
            }
            else
            {
                // save to a specific path.
                if (!Directory.Exists(this.SavePath))
                    Directory.CreateDirectory(this.SavePath);

                return this.SavePath;
            }
        }

        /// <summary>
        /// To resize the originals, new files are created in a temporary folder and then the originals
        /// deleted and the new ones moved back in their place. This can't be done at resize-time due to
        /// file locks in place.
        /// </summary>
        private void ReplaceOriginals(string outputPath) 
        {
            string originalPath = Path.GetFullPath(this.Filenames[0]);

            // remove all the original files.
            for (int i = 0; i < this.Filenames.Length; i++)
                File.Delete(this.Filenames[i]);

            // move over the resized images.
            string[] newImages = Directory.GetFiles(outputPath);
            for (int i = 0; i < newImages.Length; i++)
                File.Move(newImages[i], originalPath + "\\" + Path.GetFileName(newImages[i]));

            // remove the temporary folder.
            Directory.Delete(outputPath);
        }

        /// <summary>
        /// Performs the resizing of an image and saves it to a specific location.
        /// </summary>
        /// <param name="sourcePath">The full path of the image to resize.</param>
        /// <param name="outputPath">The path of where to save the resized image.</param>
        private void ResizeImage(string sourcePath, string outputPath) 
        {
            int scaledWidth = 0;
            int scaledHeight = 0;
            int primaryDimension = 0;
            Image imageToResize = Image.FromFile(sourcePath);
            Enums.ImageOrientation orientation = Enums.ImageOrientation.Landscape;
            
            if (imageToResize.Width > imageToResize.Height)
                orientation = Enums.ImageOrientation.Landscape;
            else if (imageToResize.Width == imageToResize.Height)
                orientation = Enums.ImageOrientation.Square;
            else
                orientation = Enums.ImageOrientation.Portrait;

            // check the image is larger than the desired size.
            if (orientation == Enums.ImageOrientation.Landscape || orientation == Enums.ImageOrientation.Square)
                primaryDimension = imageToResize.Width;
            else
                primaryDimension = imageToResize.Height;

            if (primaryDimension <= this.ImageWidth)
            {
                // do not continue, just copy the image over as it's too small to be resized.
                imageToResize.Dispose();
                File.Copy(sourcePath, outputPath + "\\" + Path.GetFileName(sourcePath));
                return;
            }
            
            // get scaled dimensions.
            if (orientation == Enums.ImageOrientation.Landscape || orientation == Enums.ImageOrientation.Square)
            {
                scaledWidth = this.ImageWidth;
                scaledHeight = Convert.ToInt32(scaledWidth * imageToResize.Height / imageToResize.Width);
            }
            else
            {
                scaledWidth = Convert.ToInt32(this.ImageWidth * imageToResize.Width / imageToResize.Height);
                scaledHeight = this.ImageWidth;
            }

            //-----------------------------------------------------------------------------------------------------//

            System.Drawing.Image newImage = new Bitmap(scaledWidth, scaledHeight);
            System.Drawing.Graphics graphic = System.Drawing.Graphics.FromImage(newImage);

            graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphic.SmoothingMode = SmoothingMode.HighQuality;
            graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphic.CompositingQuality = CompositingQuality.HighQuality;

            graphic.DrawImage(imageToResize, 0, 0, scaledWidth, scaledHeight);

            System.Drawing.Imaging.ImageCodecInfo[] Info = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
            System.Drawing.Imaging.EncoderParameters Params = new System.Drawing.Imaging.EncoderParameters(1);
            Params.Param[0] = new EncoderParameter(Encoder.Quality, this.ImageOutputQuality);

            newImage.Save(outputPath + "\\" + Path.GetFileName(sourcePath), Info[1], Params);

            imageToResize.Dispose();
            graphic.Dispose();
            newImage.Dispose();

            //-----------------------------------------------------------------------------------------------------//

            /*
            Bitmap newBitmap = new Bitmap(scaledWidth, scaledHeight);
            Graphics tempGraphic = Graphics.FromImage(newBitmap);

            tempGraphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
            tempGraphic.DrawImage(imageToResize, new Rectangle(0, 0, scaledWidth, scaledHeight));
            tempGraphic.Dispose();

            // jpeg compression settings.
            EncoderParameters encParams = new EncoderParameters(1);
            encParams.Param[0] = new EncoderParameter(Encoder.Quality, this.ImageOutputQuality);
            ImageCodecInfo codec = this.GetCodecInfo("image/jpeg");

            // save the resized image to the file-system.
            newBitmap.Save(outputPath + "\\" + Path.GetFileName(sourcePath), codec, encParams);
            newBitmap.Dispose();
            tempGraphic.Dispose();
            imageToResize.Dispose();
            */

        }

        /// <summary>
        /// Returns the correct ImageCodecInfo object for a given format signiture, i.e. "image/jpeg".
        /// </summary>
        private ImageCodecInfo GetCodecInfo(string mimeType) 
        {
            ImageCodecInfo[] ici = ImageCodecInfo.GetImageEncoders();
            int idx = 0;

            for (int ii = 0; ii < ici.Length; ii++)
            {
                if (ici[ii].MimeType == mimeType)
                {
                    idx = ii;
                    break;
                }
            }

            return ici[idx];
        }
        #endregion

        #region events
        public EventHandler OnResizedEvent;
        #endregion
    }
}