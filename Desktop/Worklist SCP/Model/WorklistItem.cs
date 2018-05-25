// Copyright (c) 2012-2018 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System;

namespace Worklist_SCP.Model
{

    /// <summary>
    /// This class contains the most important values that are transmitted per worklist
    /// </summary>
    [Serializable]
    public class WorklistItem
    {

        public string AccessionNumber { get; set; }

        public string PatientID { get; set; }

        public string Surname { get; set; }

        public string Forename { get; set; }

        public string Title { get; set; }

        public string Sex { get; set; }

        public DateTime DateOfBirth { get; set; }

        public string ReferringPhysician { get; set; }

        public string PerformingPhysician { get; set; }

        public string Modality { get; set; }

        public DateTime ExamDateAndTime { get; set; }

        public string ExamRoom { get; set; }

        public string ExamDescription { get; set; }

        public string StudyUID { get; set; }

        public string ProcedureID { get; set; }

        public string ProcedureStepID { get; set; }

        public string HospitalName { get; set; }

        public string ScheduledAET { get; set; }

    }
}
