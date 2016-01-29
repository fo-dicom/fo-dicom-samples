// Copyright (c) 2012-2016 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

namespace SimpleViewer.Universal.ViewModels
{
    using System;

    using Windows.UI.Popups;

    using Caliburn.Micro;

    public class ShellViewModel : PropertyChangedBase
    {
        string name;

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                NotifyOfPropertyChange(() => Name);
                NotifyOfPropertyChange(() => CanSayHello);
            }
        }

        public bool CanSayHello => !string.IsNullOrWhiteSpace(this.Name);

        public async void SayHello()
        {
            await new MessageDialog(string.Format("Hello {0}!", Name)).ShowAsync();
        }
    }
}