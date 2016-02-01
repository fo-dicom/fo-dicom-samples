// Copyright (c) 2012-2016 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

namespace SimpleViewer.Universal.ViewModels
{
    using System;
    using System.IO;

    using Windows.Storage.Pickers;
    using Windows.UI.Popups;
    using Windows.UI.Xaml.Media;

    using Caliburn.Micro;

    using Dicom;
    using Dicom.Imaging;

    public class ShellViewModel : PropertyChangedBase
    {
        #region FIELDS

        string name;

        private DicomFile file;

        private ImageSource image;

        #endregion

        #region PROPERTIES

        public DicomFile File
        {
            get
            {
                return this.file;
            }
            private set
            {
                if (Equals(value, this.file))
                {
                    return;
                }
                this.file = value;
                this.NotifyOfPropertyChange(() => this.File);
            }
        }

        public ImageSource Image
        {
            get
            {
                return this.image;
            }
            private set
            {
                if (Equals(value, this.image))
                {
                    return;
                }
                this.image = value;
                this.NotifyOfPropertyChange(() => this.Image);
            }
        }

        #endregion

        #region METHODS

        public async void OpenFile()
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".dcm");
            var selected = await picker.PickSingleFileAsync();
            if (selected == null) return;
            
            var stream = await selected.OpenStreamForReadAsync();

            this.File = await DicomFile.OpenAsync(stream);
            this.Image = null;

            if (this.File != null)
            {
                var dicomImage = new DicomImage(this.File.Dataset);
                if (dicomImage.Width > 0)
                {
                    this.Image = dicomImage.RenderImage().AsWriteableBitmap();
                }
            }
        }

        #endregion
    }
}