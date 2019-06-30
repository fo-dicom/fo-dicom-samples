// Copyright (c) 2012-2019 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System.Collections.Generic;

namespace Worklist_SCP.Model
{
    public interface IMppsSource
    {

        /// <summary>
        /// the procedure with the given ProcedureStepId is set in progress. The firest parameter sopInstanceUID
        /// has to be stored in a database or similar, because the following messages like Discontinue or Completed
        /// do refer to this sopInstanceUID rather than to the procedureStepId
        /// </summary>
        bool SetInProgress(string sopInstanceUID, string procedureStepId);

        /// <summary>
        /// The procedure which was previous created with the sopInstanceUID is now discontinued
        /// </summary>
        bool SetDiscontinued(string sopInstanceUID, string reason);

        /// <summary>
        /// The procedure which was previous created with the sopInstanceUID is now completed and some
        /// additional information is provided
        /// </summary>
        bool SetCompleted(string sopInstanceUID, string doseDescription, List<string> affectedInstanceUIDs);

    }
}
