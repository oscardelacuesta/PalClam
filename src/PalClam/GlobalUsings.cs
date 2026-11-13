// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
// Namespaces de base compartidos por todas las clases.
// PalClam usa WPF para su interfaz y Windows Forms SOLO para NotifyIcon.
// Los alias resuelven los tipos con el mismo nombre en ambos frameworks;
// asi evitamos los CS0104 al activar <UseWindowsForms>true</UseWindowsForms>.
global using System.IO;
global using System.Net.Http;

global using Brush = System.Windows.Media.Brush;
global using Button = System.Windows.Controls.Button;
global using CheckBox = System.Windows.Controls.CheckBox;
global using ComboBox = System.Windows.Controls.ComboBox;
global using ListView = System.Windows.Controls.ListView;
global using ProgressBar = System.Windows.Controls.ProgressBar;
global using TextBox = System.Windows.Controls.TextBox;
global using DragEventArgs = System.Windows.DragEventArgs;
global using KeyEventArgs = System.Windows.Input.KeyEventArgs;


// Aliases WPF explícitos: al activar UseWindowsForms para NotifyIcon, el SDK
// incluye también nombres homónimos de Windows Forms y System.Drawing.
// La bandeja usa siempre "Forms." en MainWindow.Tray.cs.
global using Application = System.Windows.Application;
global using Binding = System.Windows.Data.Binding;
global using Brushes = System.Windows.Media.Brushes;
global using Clipboard = System.Windows.Clipboard;
global using Color = System.Windows.Media.Color;
global using Cursors = System.Windows.Input.Cursors;
global using DataFormats = System.Windows.DataFormats;
global using DragDropEffects = System.Windows.DragDropEffects;
global using FontFamily = System.Windows.Media.FontFamily;
global using MessageBox = PalClam.UI.LocalizedMessageBox;
global using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
global using Orientation = System.Windows.Controls.Orientation;
global using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
