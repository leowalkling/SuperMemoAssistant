﻿#region License & Metadata

// The MIT License (MIT)
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// 
// 
// Created On:   2019/02/25 22:02
// Modified On:  2019/02/26 14:37
// Modified By:  Alexis

#endregion




using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Anotar.Serilog;
using CommandLine;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop;
using SuperMemoAssistant.PluginHost;

namespace SuperMemoAssistant.Plugins
{
  public partial class PluginManager
  {
    #region Constants & Statics

    private const int PluginStopTimeout = 3000;

#if DEBUG
    private const int PluginConnectTimeout = 30000;
#else
    private const int PluginConnectTimeout = 5000;
#endif

    #endregion




    #region Methods

    public async Task StartPlugin(PluginInstance pluginInstance)
    {
      var pluginPackage = pluginInstance.Package;
      var packageName   = pluginPackage.Id;

      try
      {
        using (await pluginInstance.Lock.LockAsync())
        {
          if (pluginInstance.Status != PluginStatus.Stopped)
            return;

          if (CanPluginStartOrPause(pluginInstance) == false)
            throw new InvalidOperationException("A plugin with the same Package name is already running");

          OnPluginStarting(pluginInstance);
        }

        var cmdLineParams = new PluginHostParameters
        {
          PackageName    = packageName,
          HomePath       = pluginPackage.GetHomeDir().FullPath,
          SessionString  = pluginInstance.Guid.ToString(),
          SMAChannelName = IpcServerChannelName,
          SMAProcessId   = System.Diagnostics.Process.GetCurrentProcess().Id,
          IsDeveloment   = pluginInstance.Metadata.IsDevelopment,
        };

        var pluginStartInfo = new ProcessStartInfo(
          SMAFileSystem.PluginHostExeFile.FullPath,
          Parser.Default.FormatCommandLine(cmdLineParams))
        {
          UseShellExecute = false,
        };

        pluginInstance.Process = System.Diagnostics.Process.Start(pluginStartInfo);

        if (pluginInstance.Process == null)
        {
          LogTo.Warning($"Failed to start process for {pluginInstance.Denomination} {packageName}");
          return;
        }

        pluginInstance.Process.EnableRaisingEvents = true;
        pluginInstance.Process.Exited += async (o, e) =>
        {
          using (await pluginInstance.Lock.LockAsync())
            OnPluginStopped(pluginInstance);
        };

        if (await pluginInstance.ConnectedEvent.WaitAsync(PluginConnectTimeout))
          return;

        if (pluginInstance.Status == PluginStatus.Stopped)
        {
          LogTo.Error($"{pluginInstance.Denomination.CapitalizeFirst()} {packageName} stopped unexpectedly.");
          pluginInstance.ConnectedEvent.Set();
          return;
        }
      }
      catch (Exception ex)
      {
        LogTo.Error(ex, $"An error occured while starting {pluginInstance.Denomination} {packageName}");
        return;
      }

      try
      {
        LogTo.Warning(
          $"{pluginInstance.Denomination.CapitalizeFirst()} {packageName} failed to connect in {PluginConnectTimeout}ms. Attempting to kill process");

        pluginInstance.Process.Refresh();

        if (pluginInstance.Process.HasExited)
        {
          LogTo.Warning($"{pluginInstance.Denomination.CapitalizeFirst()} {packageName} has already exited");
          return;
        }

        pluginInstance.Process.Kill();
      }
      catch (Exception ex)
      {
        LogTo.Error(ex, $"An error occured while starting {pluginInstance.Denomination} {packageName}");
      }
      finally
      {
        using (await pluginInstance.Lock.LockAsync())
          OnPluginStopped(pluginInstance);
      }
    }

    public async Task StopPlugin(PluginInstance pluginInstance)
    {
      try
      {
        using (await pluginInstance.Lock.LockAsync())
        {
          if (pluginInstance.Status == PluginStatus.Stopping)
            return;

          if (pluginInstance.Status == PluginStatus.Stopped)
            return;

          OnPluginStopping(pluginInstance);

          try
          {
            pluginInstance.Plugin.Dispose();
          }
          catch (Exception ex)
          {
            LogTo.Error(
              ex, $"An exception occured while gracefully stopping {pluginInstance.Denomination} {pluginInstance.Metadata.PackageName}.");
          }
        }

        try
        {
          if (await Task.Run(() => pluginInstance.Process.WaitForExit(PluginStopTimeout)))
            return;

          pluginInstance.Process.Refresh();

          if (pluginInstance.Process.HasExited)
            return;

          LogTo.Warning(
            $"{pluginInstance.Denomination.CapitalizeFirst()} {pluginInstance.Metadata.PackageName} didn't shut down gracefully after {PluginStopTimeout}ms. Attempting to kill process");

          pluginInstance.Process.Kill();
        }
        catch (Exception ex)
        {
          LogTo.Error(ex, $"An exception occured while killing {pluginInstance.Denomination} {pluginInstance.Metadata.PackageName}");
        }
      }
      finally
      {
        OnPluginStopped(pluginInstance);
      }
    }

    #endregion
  }
}
