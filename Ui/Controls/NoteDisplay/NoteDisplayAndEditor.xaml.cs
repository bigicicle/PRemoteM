﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using PRM.Model;
using PRM.Model.Protocol.Base;
using Shawn.Utils;
using Shawn.Utils.Wpf;
using Shawn.Utils.Wpf.Controls;

namespace PRM.Controls.NoteDisplay
{
    public partial class NoteDisplayAndEditor : UserControl
    {
        public static readonly DependencyProperty ServerProperty =
            DependencyProperty.Register("Server", typeof(ProtocolBase), typeof(NoteDisplayAndEditor),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnServerChanged));
        private static void OnServerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var server1 = e.NewValue as ProtocolBase;
            var server0 = e.OldValue as ProtocolBase;
            if (d is NoteDisplayAndEditor control)
            {
                control.EndEdit();
                if (server0 != null)
                    server0.PropertyChanged -= control.ServerOnPropertyChanged;
                if (server1 != null)
                    server1.PropertyChanged += control.ServerOnPropertyChanged;
            }
        }

        private void ServerOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProtocolBase.Note))
            {
                MarkdownViewer.Markdown = Server?.Note ?? "";
            }
        }

        public ProtocolBase? Server
        {
            get => (ProtocolBase)GetValue(ServerProperty);
            set => SetValue(ServerProperty, value);
        }


        public static readonly DependencyProperty CommandOnCloseRequestProperty = DependencyProperty.Register(
            "CommandOnCloseRequest", typeof(RelayCommand), typeof(NoteDisplayAndEditor), new FrameworkPropertyMetadata(default(RelayCommand), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public RelayCommand CommandOnCloseRequest
        {
            get => (RelayCommand)GetValue(CommandOnCloseRequestProperty);
            set => SetValue(CommandOnCloseRequestProperty, value);
        }
        
        public bool CloseEnable { get; set; }
        public bool EditEnable { get; set; }

        public NoteDisplayAndEditor()
        {
            InitializeComponent();
            Loaded += NoteDisplayAndEditor_Loaded;
        }

        private void NoteDisplayAndEditor_Loaded(object sender, RoutedEventArgs e)
        {
            ButtonEdit.IsEnabled = EditEnable;
            ButtonEdit.Visibility = EditEnable ? Visibility.Visible : Visibility.Collapsed;
            ButtonClose.IsEnabled = CloseEnable;
            ButtonClose.Visibility = CloseEnable ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OpenHyperlink(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            try
            {
                var url = e?.Parameter?.ToString();
                if (url != null)
                {
                    HyperlinkHelper.OpenUriBySystem(url);
                }
            }
            catch (Exception ex)
            {
                SimpleLogHelper.Error(ex);
            }
        }

        private void ClickOnImage(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            //MessageBox.Show($"URL: {e.Parameter}");
            try
            {
                var url = e?.Parameter?.ToString();
                if (url != null)
                {
                    HyperlinkHelper.OpenUriBySystem(url);
                }
            }
            catch (Exception ex)
            {
                SimpleLogHelper.Error(ex);
            }
        }

        private void EndEdit()
        {
            MarkdownViewer.Markdown = Server?.Note ?? "";
            MarkdownViewer.Visibility = Visibility.Visible;
            GridEditor.Visibility = Visibility.Collapsed;
        }
        private void StartEdit()
        {
            MarkdownViewer.Visibility = Visibility.Collapsed;
            GridEditor.Visibility = Visibility.Visible;
        }

        private void ButtonSave_OnClick(object sender, RoutedEventArgs e)
        {
            if (Server != null && Server.Note.Trim() != TbMarkdown.Text.Trim())
            {
                Server.Note = TbMarkdown.Text.Trim();
                IoC.Get<GlobalData>().UpdateServer(Server, doInvoke: false);
                EndEdit();
            }
        }

        private void ButtonCancelEdit_OnClick(object sender, RoutedEventArgs e)
        {
            EndEdit();
        }

        private void ButtonNoteStartEdit_OnClick(object sender, RoutedEventArgs e)
        {
            TbMarkdown.Text = Server?.Note ?? "";
            StartEdit();
        }

        private void ButtonClose_OnClick(object sender, RoutedEventArgs e)
        {
            CommandOnCloseRequest?.Execute();
        }
    }
}
