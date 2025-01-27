﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.CodeCompletion;
using PRM.Model.Protocol.FileTransmit;
using PRM.Service;

namespace PRM.View.Settings.ProtocolConfig
{
    public partial class ExternalRunnerSettings : ExternalRunnerSettingsBase
    {
        public ExternalRunnerSettings()
        {
            InitializeComponent();
            TextEditor.TextArea.TextEntering += (sender, args) =>
            {
                if (args.Text.IndexOf("\n", StringComparison.Ordinal) >= 0
                   || args.Text.IndexOf("\r", StringComparison.Ordinal) >= 0)
                    args.Handled = true;
            };
            TextEditor.TextArea.TextEntered += TextAreaOnTextEntered;
        }
    }
}
