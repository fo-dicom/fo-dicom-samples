// Copyright (c) 2012-2022 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

namespace Wado.Models
{
    /// <summary>
    /// represents a service used to retrieve images by instance UID
    /// </summary>
    public interface IDicomImageFinderService
    {
        /// <summary>
        /// Returns the image path of the dicom file with instance UID = instanceUid
        /// </summary>
        /// <param name="instanceUid">instance uid of the image to find</param>
        /// <returns>the image path if found, else null</returns>
        string GetImageByInstanceUid(string instanceUid);
    }
}
