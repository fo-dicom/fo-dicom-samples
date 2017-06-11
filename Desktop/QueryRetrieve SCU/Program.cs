// Copyright (c) 2012-2017 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using Dicom;
using Dicom.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace QueryRetrieve_SCU
{
    class Program
    {

        private static string StoragePath = @".\DICOM";
        // values of the Query Retrieve Server to test with.
        private static string QRServerHost = "www.dicomserver.co.uk";
        private static int QRServerPort = 104;
        private static string QRServerAET = "STORESCP";
        private static string AET = "FODICOMSCU";



        static void Main(string[] args)
        {
            var client = new DicomClient();
            client.NegotiateAsyncOps();

            // Find a list of Studies

            var request = CreateStudyRequestByPatientName("Tester^P*");

            var studyUids = new List<string>();
            request.OnResponseReceived += (req, response) =>
            {
                DebugStudyResponse(response);
                studyUids.Add(response.Dataset?.Get<string>(DicomTag.StudyInstanceUID));
            };
            client.AddRequest(request);
            client.Send(QRServerHost, QRServerPort, false, AET, QRServerAET);

            // find all series from a study that previous was returned

            var studyUID = studyUids[3];
            request = CreateSeriesRequestByStudyUID(studyUID);
            var serieUids = new List<string>();
            request.OnResponseReceived += (req, response) =>
            {
                DebugSerieResponse(response);
                serieUids.Add(response.Dataset?.Get<string>(DicomTag.SeriesInstanceUID));
            };
            client.AddRequest(request);
            client.Send(QRServerHost, QRServerPort, false, AET, QRServerAET);

            // now get all the images of a serie with cGet in the same association

            var cGetRequest = CreateCGetBySeriesUID(studyUID, serieUids.First());
            client.OnCStoreRequest += (DicomCStoreRequest req) =>
            {
                SaveImage(req.Dataset);
                return new DicomCStoreResponse(req, DicomStatus.Success);
            };
            // the client has to accept storage of the images. We know that the requested images are of SOP class Secondary capture, 
            // so we add the Secondary capture to the additional presentation context
            // a more general approach would be to mace a cfind-request on image level and to read a list of distinct SOP classes of all
            // the images. these SOP classes shall be added here.
            client.AdditionalPresentationContexts.Add(DicomPresentationContext.GetScpRolePresentationContext(
                DicomUID.SecondaryCaptureImageStorage, DicomTransferSyntax.ImplicitVRBigEndian,
                DicomTransferSyntax.ExplicitVRBigEndian, DicomTransferSyntax.ExplicitVRLittleEndian));
            client.AddRequest(cGetRequest);
            client.Send(QRServerHost, QRServerPort, false, AET, QRServerAET);

            // if the images shall be sent to an existing storescp and this storescp is configured on the QR SCP then a CMove could be performed:

            // here we want to see how a error case looks like - because the test QR Server does not know the node FODICOMSCP
            var cMoveRequest = CreateCMoveBySeriesUID("FODICOMSCP", studyUID, serieUids.First());
            bool? moveSuccessfully = null;
            cMoveRequest.OnResponseReceived += (DicomCMoveRequest requ, DicomCMoveResponse response) =>
            {
                if (response.Status.State == DicomState.Pending)
                {
                    Console.WriteLine("Sending is in progress. please wait");
                }
                else if (response.Status.State == DicomState.Success)
                {
                    Console.WriteLine("Sending successfully finished");
                    moveSuccessfully = true;
                }
                else if (response.Status.State == DicomState.Failure)
                {
                    Console.WriteLine("Error sending datasets: " + response.Status.Description);
                    moveSuccessfully = false;
                }
                Console.WriteLine(response.Status);
            };
            client.AddRequest(cMoveRequest);
            client.Send(QRServerHost, QRServerPort, false, AET, QRServerAET);

            if (moveSuccessfully.GetValueOrDefault(false))
            {
                // images sent successfully from QR Server to the store scp
            }
            Console.ReadLine();
        }


        public static DicomCFindRequest CreateStudyRequestByPatientName(string patientName)
        {
            var request = new DicomCFindRequest(DicomQueryRetrieveLevel.Study);

            request.Dataset.AddOrUpdate(new DicomTag(0x8, 0x5), "ISO_IR 100");

            // add the dicom tags with empty values that should be included in the result
            request.Dataset.AddOrUpdate(DicomTag.StudyDate, "");
            request.Dataset.AddOrUpdate(DicomTag.StudyTime, "");
            request.Dataset.AddOrUpdate(DicomTag.AccessionNumber, "");
            request.Dataset.AddOrUpdate(DicomTag.ModalitiesInStudy, "");
            request.Dataset.AddOrUpdate(DicomTag.StudyDescription, "");
            request.Dataset.AddOrUpdate(DicomTag.InstitutionName, "");
            request.Dataset.AddOrUpdate(DicomTag.ReferringPhysicianName, "");
            request.Dataset.AddOrUpdate(DicomTag.ProtocolName, "");
            request.Dataset.AddOrUpdate(DicomTag.PatientName, "");
            request.Dataset.AddOrUpdate(DicomTag.PatientID, "");
            request.Dataset.AddOrUpdate(DicomTag.PatientBirthDate, "");
            request.Dataset.AddOrUpdate(DicomTag.PatientSex, "");
            request.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, "");
            request.Dataset.AddOrUpdate(DicomTag.StudyID, "");

            // add the dicom tags that contain the filter criterias
            request.Dataset.AddOrUpdate(DicomTag.PatientName, patientName);

            return request;
        }


        public static DicomCFindRequest CreateSeriesRequestByStudyUID(string studyInstanceUID)
        {
            var request = new DicomCFindRequest(DicomQueryRetrieveLevel.Series);

            request.Dataset.AddOrUpdate(new DicomTag(0x8, 0x5), "ISO_IR 100");

            // add the dicom tags with empty values that should be included in the result
            request.Dataset.AddOrUpdate(DicomTag.StudyDate, "");
            request.Dataset.AddOrUpdate(DicomTag.StudyTime, "");
            request.Dataset.AddOrUpdate(DicomTag.AccessionNumber, "");
            request.Dataset.AddOrUpdate(DicomTag.Modality, "");
            request.Dataset.AddOrUpdate(DicomTag.StudyDescription, "");
            request.Dataset.AddOrUpdate(DicomTag.InstitutionName, "");
            request.Dataset.AddOrUpdate(DicomTag.ReferringPhysicianName, "");
            request.Dataset.AddOrUpdate(DicomTag.ProtocolName, "");
            request.Dataset.AddOrUpdate(DicomTag.PatientName, "");
            request.Dataset.AddOrUpdate(DicomTag.PatientID, "");
            request.Dataset.AddOrUpdate(DicomTag.PatientBirthDate, "");
            request.Dataset.AddOrUpdate(DicomTag.PatientSex, "");
            request.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, "");
            request.Dataset.AddOrUpdate(DicomTag.StudyID, "");
            request.Dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, "");
            request.Dataset.AddOrUpdate(DicomTag.SeriesDescription, "");
            request.Dataset.AddOrUpdate(DicomTag.SeriesDate, "");
            request.Dataset.AddOrUpdate(DicomTag.SeriesTime, "");
            request.Dataset.AddOrUpdate(DicomTag.NumberOfSeriesRelatedInstances, "");
            request.Dataset.AddOrUpdate(DicomTag.SeriesNumber, "");

            // add the dicom tags that contain the filter criterias
            request.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, studyInstanceUID);

            return request;
        }


        public static DicomCGetRequest CreateCGetBySeriesUID(string studyUID, string seriesUID)
        {
            var request = new DicomCGetRequest(studyUID, seriesUID);
            // no more dicomtags have to be set
            return request;
        }


        public static DicomCMoveRequest CreateCMoveBySeriesUID(string destination, string studyUID, string seriesUID)
        {
            var request = new DicomCMoveRequest(destination, studyUID, seriesUID);
            // no more dicomtags have to be set
            return request;
        }


        public static void DebugStudyResponse(DicomCFindResponse response)
        {
            if (response.Status == DicomStatus.Pending)
            {
                // print the results
                Console.WriteLine($"Patient {response.Dataset.Get<String>(DicomTag.PatientName)}, {response.Dataset.Get<String>(DicomTag.ModalitiesInStudy, -1)}-Study from {response.Dataset.Get<DateTime>(DicomTag.StudyDate)} with UID {response.Dataset.Get<String>(DicomTag.StudyInstanceUID)} ");
            }
            if (response.Status == DicomStatus.Success)
            {
                Console.WriteLine(response.Status.ToString());
            }
        }


        public static void DebugSerieResponse(DicomCFindResponse response)
        {
            try
            {
                if (response.Status == DicomStatus.Pending)
                {
                    // print the results
                    Console.WriteLine($"Serie {response.Dataset.Get<String>(DicomTag.SeriesDescription)}, {response.Dataset.Get<String>(DicomTag.Modality)}, {response.Dataset.Get<int>(DicomTag.NumberOfSeriesRelatedInstances)} instances");
                }
                if (response.Status == DicomStatus.Success)
                {
                    Console.WriteLine(response.Status.ToString());
                }
            }
            catch (Exception ex)
            { }
        }


        public static void SaveImage(DicomDataset dataset)
        {
            var studyUid = dataset.Get<string>(DicomTag.StudyInstanceUID);
            var instUid = dataset.Get<string>(DicomTag.SOPInstanceUID);

            var path = Path.GetFullPath(Program.StoragePath);
            path = Path.Combine(path, studyUid);

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            path = Path.Combine(path, instUid) + ".dcm";

            new DicomFile(dataset).Save(path);
        }


    }
}
