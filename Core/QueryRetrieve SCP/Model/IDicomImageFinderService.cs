// Copyright (c) 2012-2025 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

namespace FellowOakDicom.Samples.QueryRetrieveSCP.Model
{
    public interface IDicomImageFinderService
    {

        /// <summary>
        /// Searches in a DICOM store for patient information. Returns a representative DICOM file per found patient
        /// </summary>
        List<string> FindPatientFiles(string PatientName, string PatientId);

        /// <summary>
        /// Searches in a DICOM store for study information. Returns a representative DICOM file per found study
        /// </summary>
        List<string> FindStudyFiles(string PatientName, string PatientId, string AccessionNbr, string StudyUID);

        /// <summary>
        /// Searches in a DICOM store for series information. Returns a representative DICOM file per found serie
        /// </summary>
        List<string> FindSeriesFiles(string PatientName, string PatientId, string AccessionNbr, string StudyUID, string SeriesUID, string Modality);

        /// <summary>
        /// Searches in a DICOM store for all files matching the given UIDs
        /// </summary>
        List<string> FindFilesByUID(string PatientId, string StudyUID, string SeriesUID);

    }
}
