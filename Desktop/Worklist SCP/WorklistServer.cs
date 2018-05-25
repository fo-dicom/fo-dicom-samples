// Copyright (c) 2012-2018 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using Dicom.Network;
using System;
using System.Collections.Generic;
using System.Threading;
using Worklist_SCP.Model;

namespace Worklist_SCP
{
    public class WorklistServer
    {

        private static IDicomServer _server;
        private static List<WorklistItem> _currentWorklistItems;
        private static Timer _itemsLoaderTimer;

        public static string AETitle { get; set; }


        public static IWorklistItemsSource CreateItemsSourceService => new WorklistItemsProvider();

        public static List<WorklistItem> CurrentWorklistItems => _currentWorklistItems;


        public static void Start(int port, string aet)
        {
            AETitle = aet;
            _server = DicomServer.Create<WorklistService>(port);
            // every 30 seconds the worklist source is queried and the current list of items is cached in _currentWorklistItems
            _itemsLoaderTimer = new Timer((state) =>
            {
                var newWorklistItems = CreateItemsSourceService.GetAllCurrentWorklistItems();
                _currentWorklistItems = newWorklistItems;
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }


        public static void Stop()
        {
            _server.Dispose();
        }


    }
}
