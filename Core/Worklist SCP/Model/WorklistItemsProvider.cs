// Copyright (c) 2012-2025 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

namespace FellowOakDicom.Samples.WorklistSCP.Model
{
    public class WorklistItemsProvider : IWorklistItemsSource
    {

        /// <summary>
        /// This method returns some hard coded worklist items - of course they should be loaded from database or some other service
        /// </summary>
        public List<WorklistItem> GetAllCurrentWorklistItems()
        {
            var item1 = new WorklistItem
            {
                AccessionNumber = "AB123",
                DateOfBirth = new DateTime(1975, 2, 14),
                PatientID = "100015",
                Surname = "Test",
                Forename = "Hilbert",
                Sex = "M",
                Title = null,

                Modality = "MR",
                ExamDescription = "mr knee left",
                ExamRoom = "MR1",
                HospitalName = null,
                PerformingPhysician = null,
                ProcedureID = "200001",
                ProcedureStepID = "200002",
                StudyUID = "1.2.34.567890.1234567890.1",
                ScheduledAET = "MRMODALITY",
                ReferringPhysician = "Smith^John^Md",
                ExamDateAndTime = DateTime.Now
            };

            var item2 = new WorklistItem
            {
                AccessionNumber = "AB123",
                DateOfBirth = new DateTime(1975, 2, 14),
                PatientID = "100015",
                Surname = "Test",
                Forename = "Hilbert",
                Sex = "M",
                Title = null,

                Modality = "MR",
                ExamDescription = "mr knee right",
                ExamRoom = "MR1",
                HospitalName = null,
                PerformingPhysician = null,
                ProcedureID = "200003",
                ProcedureStepID = "200004",
                StudyUID = "1.2.34.567890.1234567890.2",
                ScheduledAET = "MRMODALITY",
                ReferringPhysician = "Smith^John^Md",
                ExamDateAndTime = DateTime.Now
            };

            var item3 = new WorklistItem
            {
                AccessionNumber = "AB125",
                DateOfBirth = new DateTime(1984, 10, 2),
                PatientID = "100019",
                Surname = "Miller",
                Forename = "Albert",
                Sex = "M",
                Title = null,

                Modality = "CR",
                ExamDescription = "cp",
                ExamRoom = "CR2",
                HospitalName = null,
                PerformingPhysician = null,
                ProcedureID = "200005",
                ProcedureStepID = "200006",
                StudyUID = "1.2.34.567890.1234567890.3",
                ScheduledAET = "CRMODALITY",
                ReferringPhysician = "Daniels^Jack^Md",
                ExamDateAndTime = DateTime.Now
            };

            return new List<WorklistItem> { item1, item2, item3 };
        }

    }
}
