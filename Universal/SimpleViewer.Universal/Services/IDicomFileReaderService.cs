// Copyright (c) 2012-2016 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

namespace SimpleViewer.Universal.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Dicom;

    public interface IDicomFileReaderService
    {
        Task<IList<DicomFile>> GetFilesAsync();
    }
}