// Copyright (c) 2012-2018 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using Caliburn.Micro;
using Dicom;
using Dicom.Imaging;
using SimpleViewer.Universal.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Media;

namespace SimpleViewer.Universal.ViewModels
{

   public class ShellViewModel : PropertyChangedBase
   {

      #region FIELDS

      private readonly IDicomFileReaderService _readerService;

      private DicomFile _file;

      private int _currentImageIndex;

      private IList<ImageSource> _images;

      private int _numberOfImages;

      #endregion

      #region CONSTRUCTORS

      public ShellViewModel(IDicomFileReaderService readerService)
      {
         _readerService = readerService;
      }

      #endregion

      #region PROPERTIES

      public DicomFile File
      {
         get
         {
            return _file;
         }
         private set
         {
            if (Equals(value, _file))
            {
               return;
            }
            _file = value;
            NotifyOfPropertyChange(() => File);
         }
      }

      public int NumberOfImages
      {
         get
         {
            return _numberOfImages;
         }
         set
         {
            if (value == _numberOfImages)
            {
               return;
            }
            _numberOfImages = value;
            NotifyOfPropertyChange(() => NumberOfImages);
            CurrentImageIndex = 0;
         }
      }

      public int CurrentImageIndex
      {
         get
         {
            return _currentImageIndex;
         }
         set
         {
            _currentImageIndex = value;
            NotifyOfPropertyChange(() => CurrentImageIndex);
            NotifyOfPropertyChange(() => CurrentImage);
         }
      }

      public ImageSource CurrentImage
          => NumberOfImages > 0 ? _images[Math.Max(CurrentImageIndex - 1, 0)] : null;

      #endregion

      #region METHODS

      public async Task OpenFiles()
      {
         var files = await _readerService.GetFilesAsync();
         if (files == null || files.Count == 0)
         {
            return;
         }

         _images = null;
         File = null;
         NumberOfImages = 0;

         if (files.Count == 1)
         {
            File = files.Single();
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