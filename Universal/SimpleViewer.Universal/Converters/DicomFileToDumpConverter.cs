// Copyright (c) 2012-2018 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using Dicom;
using Dicom.Log;
using System;
using Windows.UI.Xaml.Data;

namespace SimpleViewer.Universal.Converters
{

   public class DicomFileToDumpConverter : IValueConverter
   {

      public object Convert(object value, Type targetType, object parameter, string language)
      {
         if (value == null)
         {
            return null;
         }

         if (!(value is DicomFile dicomFile))
         {
            throw new InvalidOperationException("Only DICOM files supported.");
         }

         var dump = dicomFile.WriteToString();
         return dump;
      }

      public object ConvertBack(object value, Type targetType, object parameter, string language)
      {
         throw new NotSupportedException();
      }

   }
}