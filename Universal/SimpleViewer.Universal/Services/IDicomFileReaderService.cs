// Copyright (c) 2012-2018 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using Dicom;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleViewer.Universal.Services
{

   public interface IDicomFileReaderService
    {
        Task<IList<DicomFile>> GetFilesAsync();
    }
}