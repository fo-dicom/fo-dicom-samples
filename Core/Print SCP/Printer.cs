﻿// Copyright (c) 2012-2025 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

namespace FellowOakDicom.Samples.PrintSCP
{
    public class Printer : DicomDataset
    {
        #region Properties and Attributes

        public string PrinterAet { get; private set; }

        /// <summary>
        /// Printer device status
        /// </summary>
        /// <remarks>
        /// Enumerated values:
        /// <list type="bullet">
        /// <item><description>NORMAL</description></item>
        /// <item><description>WARNING</description></item>
        /// <item><description>FAILURE</description></item>
        /// </list>
        /// </remarks>
        public string PrinterStatus
        {
            get => GetSingleValueOrDefault(DicomTag.PrinterStatus, "NORMAL");
            private set => Add(DicomTag.PrinterStatus, value);
        }

        /// <summary>
        /// Additional information about printer status (2110,0020)
        /// </summary>
        /// <remarks>
        /// Defined terms when the printer status is equal to NORMAL: NORMAL
        /// See section C.13.9.1 for defined terms when the printer status is equal to WARNING or FAILURE
        /// </remarks>
        public string PrinterStatusInfo
        {
            get => GetSingleValueOrDefault(DicomTag.PrinterStatusInfo, "NORMAL");
            private set => Add(DicomTag.PrinterStatusInfo, value);
        }

        /// <summary>
        /// User defined name identifying the printer
        /// </summary>
        public string PrinterName
        {
            get => GetSingleValueOrDefault(DicomTag.PrinterName, string.Empty);
            private set => Add(DicomTag.PrinterName, value);
        }

        /// <summary>
        /// Manufacturer of the printer
        /// </summary>
        public string Manufacturer
        {
            get => GetSingleValueOrDefault(DicomTag.Manufacturer, "Nebras Technology");
            private set => Add(DicomTag.Manufacturer, value);
        }

        /// <summary>
        /// Manufacturer's model number of the printer
        /// </summary>
        public string ManufacturerModelName
        {
            get => GetSingleValueOrDefault(DicomTag.ManufacturerModelName, "PaXtreme Printer");
            private set => Add(DicomTag.ManufacturerModelName, value);
        }

        /// <summary>
        /// Manufacturer's serial number of the printer
        /// </summary>
        public string DeviceSerialNumber
        {
            get => GetSingleValueOrDefault(DicomTag.DeviceSerialNumber, string.Empty);
            private set => Add(DicomTag.DeviceSerialNumber, value);
        }

        /// <summary>
        /// Manufacturer's designation of software version of the printer
        /// </summary>
        public string SoftwareVersions
        {
            get => GetSingleValueOrDefault(DicomTag.SoftwareVersions, string.Empty);
            private set => Add(DicomTag.SoftwareVersions, value);
        }

        /// <summary>
        /// Date and Time when the printer was last calibrated
        /// </summary>
        public DateTime DateTimeOfLastCalibration
        {
            get => this.GetDateTime(DicomTag.DateOfLastCalibration, DicomTag.TimeOfLastCalibration);
            private set
            {
                Add(DicomTag.DateOfLastCalibration, value);
                Add(DicomTag.TimeOfLastCalibration, value);

            }
        }

        #endregion

        #region Constructors and Initialization

        /// <summary>
        /// 
        /// </summary>
        /// <param name="printerEntity"></param>
        public Printer(string aet)
        {
            PrinterAet = aet;
            DateTimeOfLastCalibration = DateTime.Now;

            PrinterStatus = "NORMAL";
            PrinterStatusInfo = "NORMAL";

        }

        #endregion
    }
}
