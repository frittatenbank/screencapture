﻿// Copyright (C) 2016  ScreenCapture
// 
// This file is part of ScreenCapture.
// 
// ScreenCapture is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ScreenCapture is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ScreenCapture.  If not, see <http://www.gnu.org/licenses/>.

#region

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using ScreenCapture.Properties;
using Timer = System.Threading.Timer;

#endregion

namespace ScreenCapture
{
    internal class ScreenCapture : Form
    {
        private readonly Icon _iconIdle = new Icon(Resources._1475625681_camera_photo, 32, 32);
        private readonly Icon _iconRecording = new Icon(Resources._1475625681_camera_photo_record, 32, 32);
        private readonly NotifyIcon _trayIcon = new NotifyIcon();
        private int _captureInterval = 5000; // in ms
        private string _outputDirectory;
        private Timer _timer;
        private readonly ContextMenu _trayMenu;
        private int _counter;

        public ScreenCapture()
        {
            // create menu
            _trayMenu = new ContextMenu();
            _trayMenu.MenuItems.Add(Resources.ScreenCapture_OnStartStop_Start, OnStartPauseResume);
            _trayMenu.MenuItems.Add("Stop", OnStop);
            _trayMenu.MenuItems.Add("Show", OnShow);
            _trayMenu.MenuItems.Add("-");
            _trayMenu.MenuItems.Add("Exit", OnExit);
            _trayMenu.MenuItems[0].DefaultItem = true;
            _trayMenu.MenuItems[1].Visible = false;

            // create systray icon
            _trayIcon.Text = Resources.ScreenCapture_ScreenCapture_Screen_Capture;
            _trayIcon.Icon = _iconIdle;
            _trayIcon.MouseClick += NotifyIcon_MouseClick;

            // add menu to systray icon
            _trayIcon.ContextMenu = _trayMenu;
            _trayIcon.Visible = true;
        }

        private void OnStop(object sender, EventArgs e)
        {
            var startStopMenuItem = (MenuItem)sender;
            _timer.Dispose();
            startStopMenuItem.DefaultItem = false;
            _trayMenu.MenuItems[0].Text = Resources.ScreenCapture_OnStartStop_Start;
            _trayIcon.Icon = _iconIdle;
            startStopMenuItem.Visible = false;
            _counter = 0;
        }

        private void OnShow(object sender, EventArgs e)
        {
            if (_outputDirectory == null)
            {
                MessageBox.Show(this, @"The output directory has not yet been specified.", @"Screen Capture");
            }
            else
            {
                var prc = new Process
                {
                    StartInfo = {FileName = _outputDirectory}
                };
                prc.Start();
            }
        }

        [STAThread]
        public static void Main()
        {
            Application.Run(new ScreenCapture());
        }

        private void OnStartPauseResume(object sender, EventArgs e)
        {
            var startStopMenuItem = (MenuItem) sender;
            if (startStopMenuItem.Text.Equals(Resources.ScreenCapture_OnStartStop_Start))
            {
                // prompt for output directory
                _outputDirectory = PromptForDirectory();
                if (_outputDirectory == null) return;

                // prompt for capture interval
                var interval =
                    Interaction.InputBox("Please specify the capture interval in milliseconds.",
                        "Screen Capture", _captureInterval.ToString());
                if (string.IsNullOrWhiteSpace(interval))
                    return;

                _captureInterval = Convert.ToInt32(interval);
                
                ResumeCapturing(startStopMenuItem);
            }
            else if (startStopMenuItem.Text.Equals(Resources.ScreenCapture_OnStartStop_Pause))
            {
                _timer.Dispose();
                startStopMenuItem.Text = Resources.ScreenCapture_OnStartStop_Resume;
                _trayIcon.Icon = _iconIdle;
            }
            else if (startStopMenuItem.Text.Equals(Resources.ScreenCapture_OnStartStop_Resume))
            {
                ResumeCapturing(startStopMenuItem);
            }
        }

        private void ResumeCapturing(MenuItem startStopMenuItem)
        {
            try
            {
                _timer = new Timer(CheckCondition, null, 0, _captureInterval);
                startStopMenuItem.Text = Resources.ScreenCapture_OnStartStop_Pause;
                startStopMenuItem.DefaultItem = true;
                _trayIcon.Icon = _iconRecording;
                _trayMenu.MenuItems[1].Visible = true;
            }
            catch (FormatException ex)
            {
                MessageBox.Show(this, ex.Message, @"Screen Capture");
            }
        }

        public string PromptForDirectory()
        {
            var fbd = new FolderBrowserDialog
            {
                Description = @"Please select the output directory for the screen captures."
            };
            fbd.ShowDialog();
            return !string.IsNullOrWhiteSpace(fbd.SelectedPath) ? fbd.SelectedPath : null;
        }

        public void CheckCondition(object state)
        {
            using (var bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
            {
                using (var gScreen = Graphics.FromImage(bmp))
                {
                    gScreen.CopyFromScreen(Screen.PrimaryScreen.Bounds.Location, Point.Empty,
                        Screen.PrimaryScreen.Bounds.Size);
                }
                Directory.CreateDirectory(_outputDirectory);
                bmp.Save(_outputDirectory + Path.DirectorySeparatorChar +
                    DateTime.Now.ToFileTime() + "_" + _counter + ".png",
                    ImageFormat.Png);
                bmp.Dispose();
                _counter++;
            }
        }

        private static void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // hide form window
            ShowInTaskbar = false; // remove from taskbar
            base.OnLoad(e);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _trayIcon.Dispose();
                _iconIdle.Dispose();
                _iconRecording.Dispose();
            }
            base.Dispose(isDisposing);
        }

        private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            var methodInfo = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                BindingFlags.Instance | BindingFlags.NonPublic);
            methodInfo.Invoke(_trayIcon, null);
        }
    }
}