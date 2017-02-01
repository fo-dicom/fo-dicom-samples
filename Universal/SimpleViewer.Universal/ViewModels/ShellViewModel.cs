// Copyright (c) 2012-2016 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

namespace SimpleViewer.Universal.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Windows.UI.Xaml.Media;

    using Caliburn.Micro;

    using Dicom;
    using Dicom.Imaging;

    using SimpleViewer.Universal.Services;

    public class ShellViewModel : PropertyChangedBase
    {
        #region FIELDS

        private readonly IDicomFileReaderService readerService;

        private DicomFile file;

        private int currentImageIndex;

        private IList<ImageSource> images;

        private int numberOfImages;

        #endregion

        #region CONSTRUCTORS

        public ShellViewModel(IDicomFileReaderService readerService)
        {
            this.readerService = readerService;
        }

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

        public int NumberOfImages
        {
            get
            {
                return this.numberOfImages;
            }
            set
            {
                if (value == this.numberOfImages)
                {
                    return;
                }
                this.numberOfImages = value;
                this.NotifyOfPropertyChange(() => this.NumberOfImages);
                this.CurrentImageIndex = 0;
            }
        }

        public int CurrentImageIndex
        {
            get
            {
                return this.currentImageIndex;
            }
            set
            {
                this.currentImageIndex = value;
                this.NotifyOfPropertyChange(() => this.CurrentImageIndex);
                this.NotifyOfPropertyChange(() => this.CurrentImage);
            }
        }

        public ImageSource CurrentImage
            => this.NumberOfImages > 0 ? this.images[Math.Max(this.CurrentImageIndex - 1, 0)] : null;

        #endregion

        #region METHODS

        public async void OpenFiles()
        {
            var files = await this.readerService.GetFilesAsync();
            if (files == null || files.Count == 0)
            {
                return;
            }

            this.images = null;
            this.File = null;
            this.NumberOfImages = 0;

            if (files.Count == 1)
            {
                this.File = files.Single();
            }

            var imageFiles = files.Where(f => f.Dataset.Contains(DicomTag.PixelData)).ToList();
            if (imageFiles.Count > 0)
            {
                this.images = imageFiles.SelectMany(
                    imageFile =>
                        {
                            try
                            {
                                var dicomImage = new DicomImage(imageFile.Dataset);
                                var frames =
                                    Enumerable.Range(0, dicomImage.NumberOfFrames)
                                        .Select(frame => dicomImage.RenderImage(frame).As<ImageSource>());
                                return frames;
                            }
                            catch
                            {
                                return new ImageSource[0];
                            }
                        }).ToList();

                this.NumberOfImages = this.images.Count;
            }
        }

        #endregion
    }
}