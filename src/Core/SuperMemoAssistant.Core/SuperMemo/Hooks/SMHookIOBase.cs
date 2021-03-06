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
// Created On:   2019/02/13 13:55
// Modified On:  2019/02/24 22:45
// Modified By:  Alexis

#endregion




using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Anotar.Serilog;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop.SuperMemo.Core;
using SuperMemoAssistant.SMA.Hooks;
using SuperMemoAssistant.Sys;
using SuperMemoAssistant.Sys.SparseClusteredArray;

namespace SuperMemoAssistant.SuperMemo.Hooks
{
  public abstract class SMHookIOBase : PerpetualMarshalByRefObject, ISMAHookIO, IDisposable
  {
    #region Properties & Fields - Non-Public

    protected SMCollection Collection => SMA.SMA.Instance.Collection;

    protected ConcurrentDictionary<IntPtr, (UInt32 position, SparseClusteredArray<byte> sca)> FileHandles { get; } =
      new ConcurrentDictionary<IntPtr, (UInt32 position, SparseClusteredArray<byte> sca)>();

    #endregion




    #region Constructors

    protected SMHookIOBase()
    {
      SMA.SMA.Instance.OnSMStartingEvent += OnSMStarting;
      SMA.SMA.Instance.OnSMStoppedEvent  += OnSMStopped;
    }

    public virtual void Dispose()
    {
      OnSMStopped(null, null).Wait();
    }

    #endregion




    #region Methods Impl

    //
    // Hooks-related

    public void OnFileCreate(string filePath,
                             IntPtr fileHandle)
    {
      string fileName = Path.GetFileName(filePath)?.ToLowerInvariant();
      var    sca      = GetSCAForFileName(fileName);

      if (sca != null)
        FileHandles[fileHandle] = (0, sca);
    }

    public void OnFileSeek(IntPtr fileHandle,
                           uint   position)
    {
      var hdlData = FileHandles.SafeGet(fileHandle);

      if (hdlData.sca != null)
        FileHandles[fileHandle] = (position, hdlData.sca);
    }

    public void OnFileWrite(IntPtr fileHandle,
                            byte[] buffer,
                            uint   count)
    {
      var (position, sca) = FileHandles.SafeGet(fileHandle);

      if (sca != null)
      {
        sca.Write(buffer, (int)position, new SparseClusteredArray<byte>.Bounds(0, (int)count - 1));

        FileHandles[fileHandle] = (position + count, sca);
      }
    }

    public void OnFileClose(IntPtr fileHandle)
    {
      FileHandles.TryRemove(fileHandle, out _);

      if (FileHandles.Count == 0)
        CommitFromMemory();
    }

    #endregion




    #region Methods

    private async Task OnSMStarting(object      sender,
                              SMEventArgs e)
    {
      LogTo.Debug($"Initializing {GetType().Name}");

      await Task.Run((Action)Initialize);
    }

    private async Task OnSMStopped(object        sender,
                             SMProcessArgs e)
    {
      LogTo.Debug($"Cleaning up {GetType().Name}");

      FileHandles.Clear();
      
      await Task.Run((Action)Cleanup);
    }

    protected static Dictionary<int, TContainer> StreamToStruct<TContainer, TStruct>(
      Stream                      structStream,
      int                         sizeOfStruct,
      Func<TStruct, TContainer>   wrapperFunc,
      Dictionary<int, TContainer> dict = null)
    {
      var ret = dict ?? new Dictionary<int, TContainer>();

      if (structStream.Position % sizeOfStruct != 0 || structStream.Length % sizeOfStruct != 0)
        throw new InvalidDataException("Invalid Position or Length for struct Stream");

      int elemId    = (int)(structStream.Position / sizeOfStruct) + 1;
      int elemCount = (int)(structStream.Length / sizeOfStruct);

      using (BinaryReader binStream = new BinaryReader(structStream, Encoding.Default, true))
        for (int i = 0; i < elemCount; i++)
        {
          var elem = binStream.ReadStruct<TStruct>();

          ret[elemId++] = wrapperFunc(elem);
        }

      return ret;
    }

    #endregion




    #region Methods Abs

    //
    // TBD

    public abstract    IEnumerable<string>        GetTargetFilePaths();
    protected abstract SparseClusteredArray<byte> GetSCAForFileName(string fileName);

    protected abstract void Initialize();
    protected abstract void Cleanup();
    protected abstract void CommitFromMemory();

    #endregion
  }
}
