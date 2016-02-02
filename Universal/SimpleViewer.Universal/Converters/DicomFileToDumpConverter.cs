// Copyright (c) 2012-2016 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

namespace SimpleViewer.Universal.Converters
{
    using System;

    using Windows.UI.Xaml.Data;

    using Dicom;
    using Dicom.Log;

    public class DicomFileToDumpConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return null;
            }

            var dicomFile = value as DicomFile;
            if (dicomFile == null)
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