// Copyright (c) 2012-2023 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using FellowOakDicom;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Worklist_SCP.Model
{

    public static class WorklistHandler
    {

        public static IEnumerable<DicomDataset> FilterWorklistItems(DicomDataset request, List<WorklistItem> allWorklistItems)
        {
            var exams = allWorklistItems.AsQueryable();

            if (request.TryGetSingleValue(DicomTag.PatientID, out string patientId))
            {
                exams = exams.Where(x => x.PatientID.Equals(patientId));
            }

            var patientName = request.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
            if (!string.IsNullOrEmpty(patientName))
            {
                exams = AddNameCondition(exams, patientName);
            }

            DicomDataset procedureStep = null;
            if (request.Contains(DicomTag.ScheduledProcedureStepSequence))
            {
                procedureStep = request.GetSequence(DicomTag.ScheduledProcedureStepSequence).First();

                // Required Matching keys
                var scheduledStationAET = procedureStep.GetSingleValueOrDefault(DicomTag.ScheduledStationAETitle, string.Empty);
                if (!string.IsNullOrEmpty(scheduledStationAET))
                {
                    exams = exams.Where(x => x.ScheduledAET == scheduledStationAET);
                }

                var performingPhysician = procedureStep.GetSingleValueOrDefault(DicomTag.PerformingPhysicianName, string.Empty);
                if (!string.IsNullOrEmpty(performingPhysician))
                {
                    exams = exams.Where(x => x.PerformingPhysician == performingPhysician);
                }

                var modality = procedureStep.GetSingleValueOrDefault(DicomTag.Modality, string.Empty);
                if (!string.IsNullOrEmpty(modality))
                {
                    exams = exams.Where(x => x.Modality == modality);
                }

                // if only date is specified, then using standard matching
                // but if both are specified, then MWL defines a combined match
                var scheduledProcedureStepStartDateTime = procedureStep.GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepStartDateTime, string.Empty);
                if (!string.IsNullOrEmpty(scheduledProcedureStepStartDateTime))
                {
                    exams = AddDateCondition(exams, scheduledProcedureStepStartDateTime);
                }

                // Optional (but commonly used) matching keys.
                var procedureStepLocation = procedureStep.GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepLocation, string.Empty);
                if (!string.IsNullOrEmpty(procedureStepLocation))
                {
                    exams = exams.Where(x => x.ExamRoom.Equals(procedureStepLocation));
                }

                var procedureDescription = procedureStep.GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepDescription, string.Empty);
                if (!string.IsNullOrEmpty(procedureDescription))
                {
                    exams = exams.Where(x => x.ExamDescription.Equals(procedureDescription));
                }
            }

            var results = exams.ToList();

            //  Parsing result 
            foreach (var result in results)
            {
                var resultingSPS = new DicomDataset();
                var resultDataset = new DicomDataset();
                var resultingSPSSequence = new DicomSequence(DicomTag.ScheduledProcedureStepSequence, resultingSPS);

                if (procedureStep != null)
                {
                    resultDataset.Add(resultingSPSSequence);
                }

                // add results to "main" dataset
                AddIfExistsInRequest(resultDataset, request, DicomTag.AccessionNumber, result.AccessionNumber);    // T2
                AddIfExistsInRequest(resultDataset, request, DicomTag.InstitutionName, result.HospitalName);
                AddIfExistsInRequest(resultDataset, request, DicomTag.ReferringPhysicianName, result.ReferringPhysician); // T2

                AddIfExistsInRequest(resultDataset, request, DicomTag.PatientName, result.Surname + "^" + result.Forename + "^^" + result.Title); //T1
                AddIfExistsInRequest(resultDataset, request, DicomTag.PatientID, result.PatientID); // T1
                AddIfExistsInRequest(resultDataset, request, DicomTag.PatientBirthDate, result.DateOfBirth); // T2
                AddIfExistsInRequest(resultDataset, request, DicomTag.PatientSex, result.Sex); //T2

                AddIfExistsInRequest(resultDataset, request, DicomTag.StudyInstanceUID, result.StudyUID); // T1

                AddIfExistsInRequest(resultDataset, request, DicomTag.RequestingPhysician, result.ReferringPhysician); //T2
                AddIfExistsInRequest(resultDataset, request, DicomTag.RequestedProcedureDescription, result.ExamDescription); //T1C

                AddIfExistsInRequest(resultDataset, request, DicomTag.RequestedProcedureID, result.ProcedureID); // T1

                // Scheduled Procedure Step sequence T1
                // add results to procedure step dataset
                // Return if requested
                if (procedureStep != null)
                {
                    AddIfExistsInRequest(resultingSPS, procedureStep, DicomTag.ScheduledStationAETitle, result.ScheduledAET); // T1
                    AddIfExistsInRequest(resultingSPS, procedureStep, DicomTag.ScheduledProcedureStepStartDate, result.ExamDateAndTime); //T1
                    AddIfExistsInRequest(resultingSPS, procedureStep, DicomTag.ScheduledProcedureStepStartTime, result.ExamDateAndTime); //T1
                    AddIfExistsInRequest(resultingSPS, procedureStep, DicomTag.Modality, result.Modality); // T1

                    AddIfExistsInRequest(resultingSPS, procedureStep, DicomTag.ScheduledPerformingPhysicianName, result.PerformingPhysician); //T2
                    AddIfExistsInRequest(resultingSPS, procedureStep, DicomTag.ScheduledProcedureStepDescription, result.ExamDescription); // T1C
                    AddIfExistsInRequest(resultingSPS, procedureStep, DicomTag.ScheduledProcedureStepID, result.ProcedureStepID); // T1
                    AddIfExistsInRequest(resultingSPS, procedureStep, DicomTag.ScheduledStationName, result.ExamRoom); //T2
                    AddIfExistsInRequest(resultingSPS, procedureStep, DicomTag.ScheduledProcedureStepLocation, result.ExamRoom); //T2
                }

                // Put blanks in for unsupported fields which are type 2 (i.e. must have a value even if NULL)
                // In a real server, you may wish to support some or all of these, but they are not commonly supported
                AddIfExistsInRequest(resultDataset, request, DicomTag.ReferencedStudySequence, new DicomDataset());         // Ref//d Study Sequence
                AddIfExistsInRequest(resultDataset, request, DicomTag.Priority, "");                                  // Priority
                AddIfExistsInRequest(resultDataset, request, DicomTag.PatientTransportArrangements, "");              // Transport Arrangements
                AddIfExistsInRequest(resultDataset, request, DicomTag.AdmissionID, "");                               // Admission ID
                AddIfExistsInRequest(resultDataset, request, DicomTag.CurrentPatientLocation, "");                    // Patient Location
                AddIfExistsInRequest(resultDataset, request, DicomTag.ReferencedPatientSequence, new DicomDataset());       // Ref//d Patient Sequence
                AddIfExistsInRequest(resultDataset, request, DicomTag.PatientWeight, "");                             // Weight
                AddIfExistsInRequest(resultDataset, request, DicomTag.ConfidentialityConstraintOnPatientDataDescription, ""); // Confidentiality Constraint

                // Send Reponse Back
                yield return resultDataset;
            }
        }


        //Splits patient name into 2 separte strings surname and forename and send then to the addstringcondition subroutine.
        internal static IQueryable<WorklistItem> AddNameCondition(IQueryable<WorklistItem> exams, string dicomName)
        {
            if (string.IsNullOrEmpty(dicomName) || dicomName == "*")
            {
                return exams;
            }

            var personName = new DicomPersonName(DicomTag.PatientName, dicomName);
            if (dicomName.Contains("*"))
            {
                var firstNameRegex = new Regex("^" + Regex.Escape(personName.First).Replace("\\*", ".*") + "$");
                var lastNameRegex = new Regex("^" + Regex.Escape(personName.Last).Replace("\\*", ".*") + "$");
                exams = exams.Where(x => firstNameRegex.IsMatch(x.Forename) || lastNameRegex.IsMatch(x.Surname));
            }
            else
            {
                exams = exams.Where(x => (x.Forename.Equals(personName.First) && x.Surname.Equals(personName.Last)));
            }

            return exams;
        }


        internal static IQueryable<WorklistItem> AddDateCondition(IQueryable<WorklistItem> exams, string dateCondition)
        {
            if (!string.IsNullOrEmpty(dateCondition) && dateCondition != "*")
            {
                var range = new DicomDateTime(DicomTag.ScheduledProcedureStepStartDate, dateCondition).Get<DicomDateRange>();
                exams = exams.Where(x => range.Contains(x.ExamDateAndTime));
            }
            return exams;
        }


        internal static void AddIfExistsInRequest<T>(DicomDataset result, DicomDataset request, DicomTag tag, T value)
        {
            // Only send items which have been requested
            if (request.Contains(tag))
            {
                if (value == null)
                {
                    value = default;
                }

                result.AddOrUpdate(tag, value);
            }
        }


    }
}
