// Copyright (c) 2012-2017 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using Dicom.Log;
using System.Collections.Generic;
using System.Linq;

namespace Worklist_SCP.Model
{

    /// <summary>
    /// An implementation of IMppsSource, that does only logging but does not store the MPPS messages
    /// </summary>
    class MppsHandler : IMppsSource
    {

        // this variable stores the list of pending operations, that have been set in progress but not completed
        private readonly static Dictionary<string, WorklistItem> _pendingProcedures = new Dictionary<string, WorklistItem>();
        public static Dictionary<string, WorklistItem> PendingProcedures => _pendingProcedures;

        private Logger _logger;


        public MppsHandler(Logger logger)
        {
            _logger = logger;
        }


        public bool SetInProgress(string sopInstanceUID, string procedureStepId)
        {
            var workItem = WorklistServer.CurrentWorklistItems
                .Where(w => w.ProcedureStepID == procedureStepId)
                .FirstOrDefault();
            if (workItem == null)
            {
                // the procedureStepId provided cannot be found any more, so the data is invalid or the 
                // modality tries to start a procedure that has been deleted/changed on the ris side...
                return false;
            }

            // now here change the sate of the procedure in the database or do similar stuff...
            _logger.Info($"Procedure with id {workItem.ProcedureStepID} of Patient {workItem.Surname} {workItem.Forename} is started");

            // remember the sopInstanceUID and store the worklistitem to which the sopInstanceUID belongs. 
            // You should do this more permanent like in database or in file
            PendingProcedures.Add(sopInstanceUID, workItem);
            return true;
        }


        public bool SetDiscontinued(string sopInstanceUID, string reason)
        {
            if (!PendingProcedures.ContainsKey(sopInstanceUID))
            {
                // there is no pending procedure with this sopInstanceUID!
                return false;
            }
            var workItem = PendingProcedures[sopInstanceUID];

            // now here change the sate of the procedure in the database or do similar stuff...
            _logger.Info($"Procedure with id {workItem.ProcedureStepID} of Patient {workItem.Surname} {workItem.Forename} is discontinued for reason {reason}");

            // since the procedure was stopped, we remove it from the list of pending procedures
            PendingProcedures.Remove(sopInstanceUID);
            return true;
        }


        public bool SetCompleted(string sopInstanceUID, string doseDescription, List<string> affectedInstanceUIDs)
        {
            if (!PendingProcedures.ContainsKey(sopInstanceUID))
            {
                // there is no pending procedure with this sopInstanceUID!
                return false;
            }
            var workItem = PendingProcedures[sopInstanceUID];

            // now here change the sate of the procedure in the database or do similar stuff...
            _logger.Info($"Procedure with id {workItem.ProcedureStepID} of Patient {workItem.Surname} {workItem.Forename} is completed");

            // the MPPS completed message contains some additional informations about the performed procedure.
            // this informations are very vendor depending, so read the DICOM Conformance Statement or read
            // the DICOM logfiles to see which informations the vendor sends

            // since the procedure was completed, we remove it from the list of pending procedures
            PendingProcedures.Remove(sopInstanceUID);
            return true;
        }


    }
}
