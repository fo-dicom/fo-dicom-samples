// Copyright (c) 2012-2025 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System.Text.RegularExpressions;

namespace FellowOakDicom.Samples.QueryRetrieveSCP.Model
{
    public class StupidSlowFinderService : IDicomImageFinderService
    {
        private const string _storagePath = @".\DICOM";


        public List<string> FindPatientFiles(string PatientName, string PatientId) =>
            // usually here a SQL statement is built to query a Patient-table
            SearchInFilesystem(
                dcmFile => dcmFile.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty),
                dcmFile =>
                {
                    bool matches = true;
                    matches &= MatchFilter(PatientName, dcmFile.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty));
                    matches &= MatchFilter(PatientId, dcmFile.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty));
                    return matches;
                });


        public List<string> FindStudyFiles(string PatientName, string PatientId, string AccessionNbr, string StudyUID) =>
            // usually here a SQL statement is built to query a Study-table
            SearchInFilesystem(
                dcmFile => dcmFile.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                dcmFile =>
                {
                    bool matches = true;
                    matches &= MatchFilter(PatientName, dcmFile.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty));
                    matches &= MatchFilter(PatientId, dcmFile.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty));
                    matches &= MatchFilter(AccessionNbr, dcmFile.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty));
                    matches &= MatchFilter(StudyUID, dcmFile.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty));
                    return matches;
                });


        public List<string> FindSeriesFiles(string PatientName, string PatientId, string AccessionNbr, string StudyUID, string SeriesUID, string Modality) =>
            // usually here a SQL statement is built to query a Series-table
            SearchInFilesystem(
                dcmFile => dcmFile.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
                dcmFile =>
                {
                    bool matches = true;
                    matches &= MatchFilter(PatientName, dcmFile.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty));
                    matches &= MatchFilter(PatientId, dcmFile.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty));
                    matches &= MatchFilter(AccessionNbr, dcmFile.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty));
                    matches &= MatchFilter(StudyUID, dcmFile.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty));
                    matches &= MatchFilter(SeriesUID, dcmFile.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty));
                    matches &= MatchFilter(Modality, dcmFile.GetSingleValueOrDefault(DicomTag.Modality, string.Empty));
                    return matches;
                });


        private List<string> SearchInFilesystem(Func<DicomDataset, string> level, Func<DicomDataset, bool> matches)
        {
            string dicomRootDirectory = _storagePath;
            var allFilesOnHarddisk = Directory.GetFiles(dicomRootDirectory, "*.dcm", SearchOption.AllDirectories);
            var matchingFiles = new List<string>(); // holds the file matching the criteria. one representative file per key
            var foundKeys = new List<string>(); // holds the list of keys that have already been found so that only one file per key is returned

            foreach (string fileNameToTest in allFilesOnHarddisk)
            {
                try
                {
                    var dcmFile = DicomFile.Open(fileNameToTest);

                    var key = level(dcmFile.Dataset);
                    if (!string.IsNullOrEmpty(key)
                        && !foundKeys.Contains(key)
                        && matches(dcmFile.Dataset))
                    {
                        matchingFiles.Add(fileNameToTest);
                        foundKeys.Add(key);
                    }
                }
                catch (Exception)
                {
                    // invalid file, ignore here
                }
            }
            return matchingFiles;
        }


        public List<string> FindFilesByUID(string PatientId, string StudyUID, string SeriesUID)
        {
            // normally here a SQL query is constructed. But this implementation searches in file system
            string dicomRootDirectory = _storagePath;
            var allFilesOnHarddisk = Directory.GetFiles(dicomRootDirectory, "*.dcm", SearchOption.AllDirectories);
            var matchingFiles = new List<string>();

            foreach (string fileNameToTest in allFilesOnHarddisk)
            {
                try
                {
                    var dcmFile = DicomFile.Open(fileNameToTest);

                    bool matches = true;
                    matches &= MatchFilter(PatientId, dcmFile.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty));
                    matches &= MatchFilter(StudyUID, dcmFile.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty));
                    matches &= MatchFilter(SeriesUID, dcmFile.Dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty));

                    if (matches)
                    {
                        matchingFiles.Add(fileNameToTest);
                    }
                }
                catch (Exception)
                {
                    // invalid file, ignore here
                }
            }
            return matchingFiles;
        }


        private bool MatchFilter(string filterValue, string valueToTest)
        {
            if (string.IsNullOrEmpty(filterValue))
            {
                // if the QR SCU sends an empty tag, then no filtering should happen
                return true;
            }
            // take into account, that strings may contain a *-wildcard
            var filterRegex = "^" + Regex.Escape(filterValue).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(valueToTest, filterRegex, RegexOptions.IgnoreCase);
        }


    }
}
