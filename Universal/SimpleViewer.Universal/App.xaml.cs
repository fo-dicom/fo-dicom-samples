// Copyright (c) 2012-2016 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using Windows.UI.Xaml;

namespace SimpleViewer.Universal
{
    using System;
    using System.Collections.Generic;
    using Windows.ApplicationModel.Activation;

    using Caliburn.Micro;

    using SimpleViewer.Universal.Services;
    using SimpleViewer.Universal.ViewModels;

    public sealed partial class App
    {
        private WinRTContainer container;

        public App()
        {
            InitializeComponent();
        }

        protected override void Configure()
        {
            container = new WinRTContainer();
            container.RegisterWinRTServices();

            container.Singleton<IDicomFileReaderService, DicomFileReaderService>().PerRequest<ShellViewModel>();
        }

        protected override object GetInstance(Type service, string key)
        {
            return container.GetInstance(service, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            container.BuildUp(instance);
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (args.PreviousExecutionState == ApplicationExecutionState.Running)
                return;

            DisplayRootViewFor<ShellViewModel>();
        }
    }
}
