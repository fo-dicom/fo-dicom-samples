// Copyright (c) 2012-2022 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FellowOakDicom.Imaging.Codec;
using FellowOakDicom.Log;
using FellowOakDicom.Network;

namespace FellowOakDicom.Samples.CStoreSCP
{

    internal class Program
    {

        private const string _storagePath = @".\DICOM";

        private static void Main(string[] args)
        {
            // start DICOM server on port from command line argument or 11112
            var port = args != null && args.Length > 0 && int.TryParse(args[0], out int tmp) ? tmp : 11112;
            Console.WriteLine($"Starting C-Store SCP server on port {port}");

            using (var server = DicomServerFactory.Create<CStoreSCP>(port))
            {
                // end process
                Console.WriteLine("Press <return> to end...");
                Console.ReadLine();
            }
        }


        private class CStoreSCP : DicomService, IDicomServiceProvider, IDicomCStoreProvider, IDicomCEchoProvider
        {
            private static readonly DicomTransferSyntax[] _acceptedTransferSyntaxes = new DicomTransferSyntax[]
            {
               DicomTransferSyntax.ExplicitVRLittleEndian,
               DicomTransferSyntax.ExplicitVRBigEndian,
               DicomTransferSyntax.ImplicitVRLittleEndian
            };

            private static readonly DicomTransferSyntax[] _acceptedImageTransferSyntaxes = new DicomTransferSyntax[]
            {
               // Lossless
               DicomTransferSyntax.JPEGLSLossless,
               DicomTransferSyntax.JPEG2000Lossless,
               DicomTransferSyntax.JPEGProcess14SV1,
               DicomTransferSyntax.JPEGProcess14,
               DicomTransferSyntax.RLELossless,
               // Lossy
               DicomTransferSyntax.JPEGLSNearLossless,
               DicomTransferSyntax.JPEG2000Lossy,
               DicomTransferSyntax.JPEGProcess1,
               DicomTransferSyntax.JPEGProcess2_4,
               // Uncompressed
               DicomTransferSyntax.ExplicitVRLittleEndian,
               DicomTransferSyntax.ExplicitVRBigEndian,
               DicomTransferSyntax.ImplicitVRLittleEndian
            };


            public CStoreSCP(INetworkStream stream, Encoding fallbackEncoding, ILogger log, DicomServiceDependencies dependencies)
                : base(stream, fallbackEncoding, log, dependencies)
            {
            }


            public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
            {
                if (association.CalledAE != "STORESCP")
                {
                    return SendAssociationRejectAsync(
                        DicomRejectResult.Permanent,
                        DicomRejectSource.ServiceUser,
                        DicomRejectReason.CalledAENotRecognized);
                }

                foreach (var pc in association.PresentationContexts)
                {
                    if (pc.AbstractSyntax == DicomUID.Verification)
                    {
                        pc.AcceptTransferSyntaxes(_acceptedTransferSyntaxes);
                    }
                    else if (pc.AbstractSyntax.StorageCategory != DicomStorageCategory.None)
                    {
                        pc.AcceptTransferSyntaxes(_acceptedImageTransferSyntaxes);
                    }
                }

                return SendAssociationAcceptAsync(association);
            }


            public Task OnReceiveAssociationReleaseRequestAsync()
            {
                return SendAssociationReleaseResponseAsync();
            }


            public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
            {
                /* nothing to do here */
            }


            public void OnConnectionClosed(Exception exception)
            {
                /* nothing to do here */
            }


            public async Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request)
            {
                var studyUid = request.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID).Trim();
                var instUid = request.SOPInstanceUID.UID;

                var path = Path.GetFullPath(Program._storagePath);
                path = Path.Combine(path, studyUid);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                path = Path.Combine(path, instUid) + ".dcm";

                await request.File.SaveAsync(path);

                return new DicomCStoreResponse(request, DicomStatus.Success);
            }


            public Task OnCStoreRequestExceptionAsync(string tempFileName, Exception e)
            {
                // let library handle logging and error response
                return Task.CompletedTask;
            }


            public Task<DicomCEchoResponse> OnCEchoRequestAsync(DicomCEchoRequest request)
            {
                return Task.FromResult(new DicomCEchoResponse(request, DicomStatus.Success));
            }

        }
    }
}
