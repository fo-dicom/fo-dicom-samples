// Copyright (c) 2012-2018 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using Dicom;
using Dicom.Log;
using Dicom.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Worklist_SCU
{
   class Program
   {

      private const string PerformedStationAETitle = "Modality";
      private const string PerformedStationName = "Modality";

      public static void Main(string[] args)
      {
         // Initialize log manager.
         LogManager.SetImplementation(ConsoleLogManager.Instance);

         // set the connection parameters
         var serverIP = "localhost";
         var serverPort = 8005;
         var serverAET = "QRSCP";
         var clientAET = "ModalityAET";

         // query all worklist items from worklist server
         var worklistItems = GetAllItemsFromWorklist(serverIP, serverPort, serverAET, clientAET);

         Console.WriteLine($"received {worklistItems.Count} worklist items.");

         // take the first result and set it to in-progress via mpps
         var worklistItem = worklistItems.First();
         var responseProgress = SendMppsInProgress(serverIP, serverPort, serverAET, clientAET, worklistItem);
         Console.WriteLine($"in progress sent with response {responseProgress.responseStatus} ({responseProgress.responseMessage})");

         // then send the compleded
         var responseCompleted = SendMppsCompleted(serverIP, serverPort, serverAET, clientAET, responseProgress.affectedInstanceUid, worklistItem);
         Console.WriteLine($"completed sent with response {responseCompleted.responseStatus} ({responseCompleted.responseMessage})");

         Console.ReadLine();
      }

      private static (string responseStatus, string responseMessage) SendMppsCompleted(string serverIP, int serverPort, string serverAET, string clientAET, DicomUID affectedInstanceUid, DicomDataset worklistItem)
      {
         var client = new DicomClient();
         var dataset = new DicomDataset();

         dataset.Add(DicomTag.PerformedProcedureStepStatus, "COMPLETED");

         var dicomFinished = new DicomNSetRequest(DicomUID.ModalityPerformedProcedureStepSOPClass, affectedInstanceUid)
         {
            Dataset = dataset
         };

         string responseStatus = string.Empty;
         string responseMessage = string.Empty;

         dicomFinished.OnResponseReceived += (req, response) =>
         {
            if (response != null)
            {
               Console.WriteLine(response);
               responseStatus = response.Status.ToString();
               responseMessage = response.ToString();
            }
         };

         client.AddRequest(dicomFinished);
         client.Send(serverIP, serverPort, false, clientAET, serverAET);

         return (responseStatus, responseMessage);
      }

      private static (DicomUID affectedInstanceUid, string responseStatus, string responseMessage) SendMppsInProgress(string serverIP, int serverPort, string serverAET, string clientAET, DicomDataset worklistItem)
      {
         var client = new DicomClient();
         var dataset = new DicomDataset();

         DicomSequence sq = worklistItem.GetSequence(DicomTag.ScheduledProcedureStepSequence);

         DicomDataset content = new DicomDataset();
         // get study instance UID from MWL query resault
         string studyInstanceUID = worklistItem.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, DicomUID.Generate().ToString());
         DicomUID instanceDicomUid = DicomUID.Generate();
         DicomUID sopDicomUid = DicomUID.Generate();

         // set Attribute Sequence data 
         content.Add(DicomTag.StudyInstanceUID, studyInstanceUID);
         content.Add(DicomTag.ReferencedStudySequence, new DicomDataset());
         content.Add(DicomTag.AccessionNumber, worklistItem.GetSingleValueOrDefault(DicomTag.AccessionNumber, String.Empty));
         content.Add(DicomTag.RequestedProcedureID, worklistItem.GetSingleValueOrDefault(DicomTag.RequestedProcedureID, String.Empty));
         content.Add(DicomTag.RequestedProcedureDescription, worklistItem.GetSingleValueOrDefault(DicomTag.RequestedProcedureDescription, String.Empty));
         content.Add(DicomTag.ScheduledProcedureStepID, sq.First().GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepID, String.Empty));
         content.Add(DicomTag.ScheduledProcedureStepDescription, sq.First().GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepID, String.Empty));
         content.Add(DicomTag.ScheduledProtocolCodeSequence, new DicomDataset());

         DicomSequence attr_Sequence = new DicomSequence(DicomTag.ScheduledStepAttributesSequence, content);//"Scheduled Step Attribute Sequence"
         dataset.Add(attr_Sequence);

         dataset.Add(DicomTag.PatientName, worklistItem.GetSingleValueOrDefault(DicomTag.PatientName, String.Empty));
         dataset.Add(DicomTag.PatientID, worklistItem.GetSingleValueOrDefault(DicomTag.PatientID, String.Empty));
         dataset.Add(DicomTag.PatientBirthDate, worklistItem.GetSingleValueOrDefault(DicomTag.PatientBirthDate, String.Empty));
         dataset.Add(DicomTag.PatientSex, worklistItem.GetSingleValueOrDefault(DicomTag.PatientSex, String.Empty));

         dataset.Add(DicomTag.ReferencedPatientSequence, new DicomDataset());
         dataset.Add(DicomTag.PerformedProcedureStepID, sq.First().GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepID, String.Empty));
         dataset.Add(DicomTag.PerformedStationAETitle, PerformedStationAETitle);
         dataset.Add(DicomTag.PerformedStationName, PerformedStationName);
         dataset.Add(DicomTag.PerformedLocation, string.Empty);
         dataset.Add(DicomTag.PerformedProcedureStepStartDate, DateTime.Now);
         dataset.Add(DicomTag.PerformedProcedureStepStartTime, DateTime.Now);
         // set status 
         dataset.Add(DicomTag.PerformedProcedureStepStatus, "IN PROGRESS");
         dataset.Add(DicomTag.PerformedProcedureStepDescription, sq.First().GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepID, String.Empty));
         dataset.Add(DicomTag.PerformedProcedureTypeDescription, string.Empty);

         dataset.Add(DicomTag.PerformedProcedureStepEndDate, string.Empty);
         dataset.Add(DicomTag.PerformedProcedureStepEndTime, string.Empty);
         // get modality from MWL query resault 
         dataset.Add(DicomTag.Modality, sq.First().GetSingleValueOrDefault(DicomTag.Modality, String.Empty));
         dataset.Add(DicomTag.StudyID, worklistItem.GetSingleValueOrDefault(DicomTag.StudyID, string.Empty));
         dataset.Add(DicomTag.PerformedProtocolCodeSequence, new DicomDataset());
         dataset.Add(DicomTag.PerformedSeriesSequence, new DicomDataset());
         // I used the studyInstanceUID as the effectedinstamceUid, this id will be needed for the N-SET also
         DicomUID effectedinstamceUid = new DicomUID(studyInstanceUID, "effectedinstamceUid", DicomUidType.SOPInstance);// = new DicomDataset();

         var dicomStart = new DicomNCreateRequest(DicomUID.ModalityPerformedProcedureStepSOPClass, effectedinstamceUid)
         {
            Dataset = dataset
         };

         string responseStatus = string.Empty;
         string responseMessage = string.Empty;

         dicomStart.OnResponseReceived += (req, response) =>
         {
            if (response != null)
            {
               Console.WriteLine(response);
               responseStatus = response.Status.ToString();
               responseMessage = response.ToString();
            }
         };

         client.AddRequest(dicomStart);
         client.Send(serverIP, serverPort, false, clientAET, serverAET);

         return (effectedinstamceUid, responseStatus, responseMessage);
      }


      private static List<DicomDataset> GetAllItemsFromWorklist(string serverIP, int serverPort, string serverAET, string clientAET)
      {
         var worklistItems = new List<DicomDataset>();
         var cfind = DicomCFindRequest.CreateWorklistQuery(); // no filter, so query all awailable entries
         cfind.OnResponseReceived = (DicomCFindRequest rq, DicomCFindResponse rp) =>
         {
            Console.WriteLine("Study UID: {0}", rp.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID));
            worklistItems.Add(rp.Dataset);
         };

         var client = new DicomClient();
         client.AddRequest(cfind);
         client.SendAsync(serverIP, serverPort, false, clientAET, serverAET).GetAwaiter().GetResult();

         return worklistItems;
      }


   }
}
