// Copyright (c) 2012-2025 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using FellowOakDicom.Network.Client;
using FellowOakDicom.Network;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

const string _performedStationAETitle = "Modality";
const string _performedStationName = "Modality";


// Initialize log manager.
new DicomSetupBuilder()
    .RegisterServices(s => s.AddFellowOakDicom().AddLogging(config => config.AddConsole()))
    .Build();

// set the connection parameters
var serverIP = "localhost";
var serverPort = 8005;
var serverAET = "QRSCP";
var clientAET = "ModalityAET";

// query all worklist items from worklist server
var worklistItems = await GetAllItemsFromWorklistAsync(serverIP, serverPort, serverAET, clientAET);

Console.WriteLine($"received {worklistItems.Count} worklist items.");

// take the first result and set it to in-progress via mpps
var worklistItem = worklistItems.First();
var (affectedInstanceUid, responseStatus, responseMessage) = await SendMppsInProgressAsync(serverIP, serverPort, serverAET, clientAET, worklistItem);
Console.WriteLine($"in progress sent with response {responseStatus} ({responseMessage})");

// then send the compleded
var responseCompleted = await SendMppsCompletedAsync(serverIP, serverPort, serverAET, clientAET, affectedInstanceUid, worklistItem);
Console.WriteLine($"completed sent with response {responseCompleted.responseStatus} ({responseCompleted.responseMessage})");

Console.ReadLine();



static async Task<(string responseStatus, string responseMessage)> SendMppsCompletedAsync(string serverIP, int serverPort, string serverAET, string clientAET, DicomUID affectedInstanceUid, DicomDataset worklistItem)
{
    var client = DicomClientFactory.Create(serverIP, serverPort, false, clientAET, serverAET);
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

    var performedProtocolCodeSequence = new DicomSequence(DicomTag.PerformedProtocolCodeSequence);
    dataset.Add(performedProtocolCodeSequence);

    // images created
    var performedSeriesSq = new DicomSequence(DicomTag.PerformedSeriesSequence);
    // iterate all Series that have been created while examination
    var serie = new DicomDataset
         {
            { DicomTag.RetrieveAETitle, string.Empty }, // the aetitle of the archive where the images have been sent to
            { DicomTag.SeriesDescription, "serie 1" },
            { DicomTag.PerformingPhysicianName, string.Empty },
            { DicomTag.OperatorsName, string.Empty },
            { DicomTag.ProtocolName, "ProtocolName" },
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

    var referencedNonImageCompositeSOPInstanceSequence = new DicomSequence(DicomTag.ReferencedNonImageCompositeSOPInstanceSequence);
    serie.Add(referencedNonImageCompositeSOPInstanceSequence);

    performedSeriesSq.Items.Add(serie);
    dataset.Add(performedSeriesSq);

    var dicomFinished = new DicomNSetRequest(DicomUID.ModalityPerformedProcedureStep, affectedInstanceUid)
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

    await client.AddRequestAsync(dicomFinished);
    await client.SendAsync();

    return (responseStatus, responseMessage);
}


static async Task<(DicomUID affectedInstanceUid, string responseStatus, string responseMessage)> SendMppsInProgressAsync(string serverIP, int serverPort, string serverAET, string clientAET, DicomDataset worklistItem)
{
    var client = DicomClientFactory.Create(serverIP, serverPort, false, clientAET, serverAET);
    var dataset = new DicomDataset();

    DicomSequence procedureStepSq = worklistItem.GetSequence(DicomTag.ScheduledProcedureStepSequence);
    // A worklistitem may have a list of scheduledprocedureSteps.
    // For each of them you have to send separate MPPS InProgress- and Completed-messages.
    // there in this example we will only send for the first procedure step
    var procedureStep = procedureStepSq.First();

    var content = new DicomDataset();
    // get study instance UID from MWL query resault
    string studyInstanceUID = worklistItem.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, DicomUID.Generate().ToString());

    // set Attribute Sequence data 
    content.Add(DicomTag.StudyInstanceUID, studyInstanceUID);

    var referencedStudySequence = new DicomSequence(DicomTag.ReferencedStudySequence);
    content.Add(referencedStudySequence);

    content.Add(DicomTag.AccessionNumber, worklistItem.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty));
    content.Add(DicomTag.RequestedProcedureID, worklistItem.GetSingleValueOrDefault(DicomTag.RequestedProcedureID, string.Empty));
    content.Add(DicomTag.RequestedProcedureDescription, worklistItem.GetSingleValueOrDefault(DicomTag.RequestedProcedureDescription, string.Empty));
    content.Add(DicomTag.ScheduledProcedureStepID, procedureStep.GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepID, string.Empty));
    content.Add(DicomTag.ScheduledProcedureStepDescription, procedureStep.GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepDescription, string.Empty));

    var scheduledProtocolCodeSequence = new DicomSequence(DicomTag.ScheduledProtocolCodeSequence);
    content.Add(scheduledProtocolCodeSequence);

    var attr_Sequence = new DicomSequence(DicomTag.ScheduledStepAttributesSequence, content);//"Scheduled Step Attribute Sequence"
    dataset.Add(attr_Sequence);

    var procedureCodeSequence = new DicomSequence(DicomTag.ProcedureCodeSequence);
    dataset.Add(procedureCodeSequence);

    var performedSeriesSq = new DicomSequence(DicomTag.PerformedSeriesSequence);
    dataset.Add(performedSeriesSq);

    dataset.Add(DicomTag.PatientName, worklistItem.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty));
    dataset.Add(DicomTag.PatientID, worklistItem.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty));
    dataset.Add(DicomTag.PatientBirthDate, worklistItem.GetSingleValueOrDefault(DicomTag.PatientBirthDate, string.Empty));
    dataset.Add(DicomTag.PatientSex, worklistItem.GetSingleValueOrDefault(DicomTag.PatientSex, string.Empty));

    var referencedPatientSequence = new DicomSequence(DicomTag.ReferencedPatientSequence);
    dataset.Add(referencedPatientSequence);

    dataset.Add(DicomTag.PerformedProcedureStepID, procedureStep.GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepID, string.Empty));
    dataset.Add(DicomTag.PerformedStationAETitle, _performedStationAETitle);
    dataset.Add(DicomTag.PerformedStationName, _performedStationName);
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

    var performedProtocolCodeSequence = new DicomSequence(DicomTag.PerformedProtocolCodeSequence);
    dataset.Add(performedProtocolCodeSequence);

    // create an unique UID as the effectedinstamceUid, this id will be needed for the N-SET also
    var effectedinstamceUid = DicomUID.Generate();
    var dicomStartRequest = new DicomNCreateRequest(DicomUID.ModalityPerformedProcedureStep, effectedinstamceUid)
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

    await client.AddRequestAsync(dicomStartRequest);
    await client.SendAsync();

    return (effectedinstamceUid, responseStatus, responseMessage);
}


static async Task<List<DicomDataset>> GetAllItemsFromWorklistAsync(string serverIP, int serverPort, string serverAET, string clientAET)
{
    var worklistItems = new List<DicomDataset>();
    var cfind = DicomCFindRequest.CreateWorklistQuery(); // no filter, so query all awailable entries
    cfind.OnResponseReceived = (DicomCFindRequest rq, DicomCFindResponse rp) =>
    {
        if (rp.HasDataset)
        {
            Console.WriteLine("Study UID: {0}", rp.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID));
            worklistItems.Add(rp.Dataset);
        }
        else
        {
            Console.WriteLine(rp.Status.ToString());
        }
    };

    var client = DicomClientFactory.Create(serverIP, serverPort, false, clientAET, serverAET);
    await client.AddRequestAsync(cfind);
    await client.SendAsync();

    return worklistItems;
}

