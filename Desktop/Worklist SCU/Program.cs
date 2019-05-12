// Copyright (c) 2012-2018 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using Dicom;
using Dicom.Log;
using Dicom.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Worklist_SCU
{
   public class Program
   {

      private const string PerformedStationAETitle = "Modality";
      private const string PerformedStationName = "Modality";

      protected Program()
      {
      }

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
         var (affectedInstanceUid, responseStatus, responseMessage) = SendMppsInProgress(serverIP, serverPort, serverAET, clientAET, worklistItem);
         Console.WriteLine($"in progress sent with response {responseStatus} ({responseMessage})");

         // then send the compleded
         var responseCompleted = SendMppsCompleted(serverIP, serverPort, serverAET, clientAET, affectedInstanceUid, worklistItem);
         Console.WriteLine($"completed sent with response {responseCompleted.responseStatus} ({responseCompleted.responseMessage})");

         Console.ReadLine();
      }


      private static (string responseStatus, string responseMessage) SendMppsCompleted(string serverIP, int serverPort, string serverAET, string clientAET, DicomUID affectedInstanceUid, DicomDataset worklistItem)
      {
         var client = new DicomClient();
         var dataset = new DicomDataset();

         DicomSequence procedureStepSq = worklistItem.GetSequence(DicomTag.ScheduledProcedureStepSequence);
         // A worklistitem may have a list of scheduledprocedureSteps.
         // For each of them you have to send separate MPPS InProgress- and Completed-messages.
         // there in this example we will only send for the first procedure step
         var procedureStep = procedureStepSq.First();

         // data
         dataset.Add(DicomTag.PerformedProcedureStepEndDate, DateTime.Now);
         dataset.Add(DicomTag.PerformedProcedureStepEndTime, DateTime.Now);
         dataset.Add(DicomTag.PerformedProcedureStepStatus, "COMPLETED");
         dataset.Add(DicomTag.PerformedProcedureStepDescription, procedureStep.GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepID, string.Empty));
         dataset.Add(DicomTag.PerformedProcedureTypeDescription, string.Empty);

         dataset.Add(DicomTag.PerformedProtocolCodeSequence, new DicomDataset());

         // dose and reports
         dataset.Add(DicomTag.ImageAndFluoroscopyAreaDoseProduct, 0.0m); // if there has bee sone dose while examination
         dataset.Add(DicomTag.CommentsOnRadiationDose, string.Empty); // a free text that contains all dose parameters

         // images created
         var performedSeriesSq = new DicomSequence(DicomTag.PerformedSeriesSequence);
         // iterate all Series that have been created while examination
         var serie = new DicomDataset
         {
            { DicomTag.RetrieveAETitle, string.Empty }, // the aetitle of the archive where the images have been sent to
            { DicomTag.SeriesDescription, "serie 1" },
            { DicomTag.PerformingPhysicianName, string.Empty },
            { DicomTag.OperatorsName, string.Empty },
            { DicomTag.ProtocolName, string.Empty },
            { DicomTag.SeriesInstanceUID, DicomUID.Generate() }
         };
         var refImagesInSerie = new DicomSequence(DicomTag.ReferencedImageSequence);
         // iterate all images in the serie
         var image = new DicomDataset
         {
            { DicomTag.ReferencedSOPClassUID, DicomUID.SecondaryCaptureImageStorage },
            { DicomTag.ReferencedSOPInstanceUID, DicomUID.Generate() }
         };
         refImagesInSerie.Items.Add(image);
         serie.Add(refImagesInSerie);
         performedSeriesSq.Items.Add(serie);
         dataset.Add(performedSeriesSq);

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
         client.SendAsync(serverIP, serverPort, false, clientAET, serverAET).Wait();

         return (responseStatus, responseMessage);
      }


      private static (DicomUID affectedInstanceUid, string responseStatus, string responseMessage) SendMppsInProgress(string serverIP, int serverPort, string serverAET, string clientAET, DicomDataset worklistItem)
      {
         var client = new DicomClient();
         var dataset = new DicomDataset();

         DicomSequence procedureStepSq = worklistItem.GetSequence(DicomTag.ScheduledProcedureStepSequence);
         // A worklistitem may have a list of scheduledprocedureSteps.
         // For each of them you have to send separate MPPS InProgress- and Completed-messages.
         // there in this example we will only send for the first procedure step
         var procedureStep = procedureStepSq.First();

         DicomDataset content = new DicomDataset();
         // get study instance UID from MWL query resault
         string studyInstanceUID = worklistItem.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, DicomUID.Generate().ToString());

         // set Attribute Sequence data 
         content.Add(DicomTag.StudyInstanceUID, studyInstanceUID);
         content.Add(DicomTag.ReferencedStudySequence, new DicomDataset());
         content.Add(DicomTag.AccessionNumber, worklistItem.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty));
         content.Add(DicomTag.RequestedProcedureID, worklistItem.GetSingleValueOrDefault(DicomTag.RequestedProcedureID, string.Empty));
         content.Add(DicomTag.RequestedProcedureDescription, worklistItem.GetSingleValueOrDefault(DicomTag.RequestedProcedureDescription, string.Empty));
         content.Add(DicomTag.ScheduledProcedureStepID, procedureStep.GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepID, string.Empty));
         content.Add(DicomTag.ScheduledProcedureStepDescription, procedureStep.GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepDescription, string.Empty));
         content.Add(DicomTag.ScheduledProtocolCodeSequence, new DicomDataset());

         DicomSequence attr_Sequence = new DicomSequence(DicomTag.ScheduledStepAttributesSequence, content);//"Scheduled Step Attribute Sequence"
         dataset.Add(attr_Sequence);

         dataset.Add(DicomTag.PatientName, worklistItem.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty));
         dataset.Add(DicomTag.PatientID, worklistItem.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty));
         dataset.Add(DicomTag.PatientBirthDate, worklistItem.GetSingleValueOrDefault(DicomTag.PatientBirthDate, string.Empty));
         dataset.Add(DicomTag.PatientSex, worklistItem.GetSingleValueOrDefault(DicomTag.PatientSex, string.Empty));

         dataset.Add(DicomTag.ReferencedPatientSequence, new DicomDataset());
         dataset.Add(DicomTag.PerformedProcedureStepID, procedureStep.GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepID, string.Empty));
         dataset.Add(DicomTag.PerformedStationAETitle, PerformedStationAETitle);
         dataset.Add(DicomTag.PerformedStationName, PerformedStationName);
         dataset.Add(DicomTag.PerformedLocation, string.Empty);
         dataset.Add(DicomTag.PerformedProcedureStepStartDate, DateTime.Now);
         dataset.Add(DicomTag.PerformedProcedureStepStartTime, DateTime.Now);
         // set status
         dataset.Add(DicomTag.PerformedProcedureStepStatus, "IN PROGRESS");
         dataset.Add(DicomTag.PerformedProcedureStepDescription, procedureStep.GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepID, string.Empty));
         dataset.Add(DicomTag.PerformedProcedureTypeDescription, string.Empty);

         dataset.Add(DicomTag.PerformedProcedureStepEndDate, string.Empty);
         dataset.Add(DicomTag.PerformedProcedureStepEndTime, string.Empty);
         // get modality from MWL query resault
         dataset.Add(DicomTag.Modality, procedureStep.GetSingleValueOrDefault(DicomTag.Modality, string.Empty));
         dataset.Add(DicomTag.StudyID, worklistItem.GetSingleValueOrDefault(DicomTag.StudyID, string.Empty));
         dataset.Add(DicomTag.PerformedProtocolCodeSequence, new DicomDataset());

         // create an unique UID as the effectedinstamceUid, this id will be needed for the N-SET also
         DicomUID effectedinstamceUid = DicomUID.Generate("effectedinstamceUid");
         var dicomStartRequest = new DicomNCreateRequest(DicomUID.ModalityPerformedProcedureStepSOPClass, effectedinstamceUid)
         {
            Dataset = dataset
         };

         string responseStatus = string.Empty;
         string responseMessage = string.Empty;

         dicomStartRequest.OnResponseReceived += (req, response) =>
         {
            if (response != null)
            {
               Console.WriteLine(response);
               responseStatus = response.Status.ToString();
               responseMessage = response.ToString();
            }
         };

         client.AddRequest(dicomStartRequest);
         client.SendAsync(serverIP, serverPort, false, clientAET, serverAET).Wait();

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
