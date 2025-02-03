// Copyright (c) 2012-2025 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using FellowOakDicom.Printing;

namespace FellowOakDicom.Samples.PrintSCU
{
    internal class PrintJob
    {
        public string CallingAE { get; set; }
        public string CalledAE { get; set; }
        public string RemoteAddress { get; set; }
        public int RemotePort { get; set; }

        public FilmSession FilmSession { get; private set; }

        private FilmBox _currentFilmBox;

        public PrintJob(string jobLabel)
        {
            FilmSession = new FilmSession(DicomUID.BasicFilmSession)
            {
                FilmSessionLabel = jobLabel,
                MediumType = "PAPER",
                NumberOfCopies = 1
            };
        }

        public FilmBox StartFilmBox(string format, string orientation, string filmSize)
        {
            var filmBox = new FilmBox(FilmSession, null, DicomTransferSyntax.ExplicitVRLittleEndian)
            {
                ImageDisplayFormat = format,
                FilmOrientation = orientation,
                FilmSizeID = filmSize,
                MagnificationType = "NONE",
                BorderDensity = "BLACK",
                EmptyImageDensity = "BLACK"
            };

            filmBox.Initialize();
            FilmSession.BasicFilmBoxes.Add(filmBox);

            _currentFilmBox = filmBox;
            return filmBox;
        }

        public void AddImage(ImageSharpImage bitmap, int index)
        {
            if (FilmSession.IsColor)
            {
                AddColorImage(bitmap, index);
            }
            else
            {
                AddGreyscaleImage(bitmap, index);
            }
        }

        private void AddGreyscaleImage(ImageSharpImage bitmap, int index)
        {
            if (_currentFilmBox == null)
            {
                throw new InvalidOperationException("Start film box first!");
            }
            if (index < 0 || index > _currentFilmBox.BasicImageBoxes.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Image box index out of range");
            }

            var dataset = new DicomDataset();
            dataset.Add<ushort>(DicomTag.Columns, (ushort)bitmap.Width)
                .Add<ushort>(DicomTag.Rows, (ushort)bitmap.Height)
                .Add<ushort>(DicomTag.BitsAllocated, 8)
                .Add<ushort>(DicomTag.BitsStored, 8)
                .Add<ushort>(DicomTag.HighBit, 7)
                .Add(DicomTag.PixelRepresentation, (ushort)PixelRepresentation.Unsigned)
                .Add(DicomTag.PlanarConfiguration, (ushort)PlanarConfiguration.Interleaved)
                .Add<ushort>(DicomTag.SamplesPerPixel, 1)
                .Add(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value);

            var pixelData = DicomPixelData.Create(dataset, true);

            var pixels = GetGreyBytes(bitmap);
            var buffer = new MemoryByteBuffer(pixels);

            pixelData.AddFrame(buffer);

            var imageBox = _currentFilmBox.BasicImageBoxes[index];
            imageBox.ImageSequence = dataset;
        }

        private void AddColorImage(ImageSharpImage bitmap, int index)
        {
            if (_currentFilmBox == null)
            {
                throw new InvalidOperationException("Start film box first!");
            }
            if (index < 0 || index > _currentFilmBox.BasicImageBoxes.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Image box index out of range");
            }

            var dataset = new DicomDataset();
            dataset.Add<ushort>(DicomTag.Columns, (ushort)bitmap.Width)
                .Add<ushort>(DicomTag.Rows, (ushort)bitmap.Height)
                .Add<ushort>(DicomTag.BitsAllocated, 8)
                .Add<ushort>(DicomTag.BitsStored, 8)
                .Add<ushort>(DicomTag.HighBit, 7)
                .Add(DicomTag.PixelRepresentation, (ushort)PixelRepresentation.Unsigned)
                .Add(DicomTag.PlanarConfiguration, (ushort)PlanarConfiguration.Interleaved)
                .Add<ushort>(DicomTag.SamplesPerPixel, 3)
                .Add(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Rgb.Value);

            var pixelData = DicomPixelData.Create(dataset, true);

            var pixels = GetColorbytes(bitmap);
            var buffer = new MemoryByteBuffer(pixels);

            pixelData.AddFrame(buffer);

            var imageBox = _currentFilmBox.BasicImageBoxes[index];
            imageBox.ImageSequence = dataset;
        }

        public void EndFilmBox()
        {
            _currentFilmBox = null;
        }

        public async Task Print()
        {
            var dicomClient = DicomClientFactory.Create(RemoteAddress, RemotePort, false, CallingAE, CalledAE);

            await dicomClient.AddRequestAsync(
                new DicomNCreateRequest(FilmSession.SOPClassUID, FilmSession.SOPInstanceUID)
                {
                    Dataset = FilmSession
                });


            foreach (var filmbox in FilmSession.BasicFilmBoxes)
            {

                var imageBoxRequests = new List<DicomNSetRequest>();

                var filmBoxRequest = new DicomNCreateRequest(FilmBox.SOPClassUID, filmbox.SOPInstanceUID)
                {
                    Dataset = filmbox
                };
                filmBoxRequest.OnResponseReceived = (request, response) =>
                {
                    if (response.HasDataset)
                    {
                        var seq = response.Dataset.GetSequence(DicomTag.ReferencedImageBoxSequence);
                        for (int i = 0; i < seq.Items.Count; i++)
                        {
                            var req = imageBoxRequests[i];
                            var imageBox = req.Dataset;
                            var sopInstanceUid = seq.Items[i].GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID);
                            imageBox.AddOrUpdate(DicomTag.SOPInstanceUID, sopInstanceUid);
                            req.Command.AddOrUpdate(DicomTag.RequestedSOPInstanceUID, sopInstanceUid);
                        }
                    }
                };
                await dicomClient.AddRequestAsync(filmBoxRequest);

                foreach (var image in filmbox.BasicImageBoxes)
                {
                    var req = new DicomNSetRequest(image.SOPClassUID, image.SOPInstanceUID) { Dataset = image };

                    imageBoxRequests.Add(req);
                    await dicomClient.AddRequestAsync(req);
                }
            }

            await dicomClient.AddRequestAsync(new DicomNActionRequest(FilmSession.SOPClassUID, FilmSession.SOPInstanceUID, 0x0001));

            await dicomClient.SendAsync();
        }


        private static byte[] GetGreyBytes(ImageSharpImage bitmap)
        {
            var pixels = new byte[bitmap.Width * bitmap.Height];

            for (int i = 0; i < bitmap.Height; i++)
            {
                var lineStart = i * bitmap.Width;
                for (int j = 0; j < bitmap.Width; j++)
                {
                    var pixel = bitmap.GetPixel(j, i);
                    var gray = (byte)(pixel.B * 0.3 + pixel.G * 0.59 + pixel.R * 0.11);

                    // convert to RGB
                    pixels[lineStart + j] = gray;
                }
            }

            return pixels;
        }


        private static byte[] GetColorbytes(ImageSharpImage bitmap)
        {
            var pixels = new byte[bitmap.Width * bitmap.Height * 3];

            for (int i = 0; i < bitmap.Height; i++)
            {
                var lineStart = i * bitmap.Width * 3;
                for (int j = 0; j < bitmap.Width; j++)
                {
                    var pixelStart = lineStart + j * 3;
                    var pixel = bitmap.GetPixel(j, i);

                    // convert to RGB
                    pixels[pixelStart] = pixel.R;
                    pixels[pixelStart + 1] = pixel.G;
                    pixels[pixelStart + 2] = pixel.B;
                }
            }

            return pixels;
        }

    }
}
