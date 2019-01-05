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
// Created On:   2018/05/19 20:53
// Modified On:  2018/05/30 23:26
// Modified By:  Alexis

#endregion




using System;
using System.Threading.Tasks;
using SuperMemoAssistant.Interop;
using SuperMemoAssistant.Interop.SuperMemo.Registry.Members;
using SuperMemoAssistant.Interop.SuperMemo.Registry.Types;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.SuperMemo.SuperMemo17.Files;
using SuperMemoAssistant.SuperMemo.SuperMemo17.Registry.Members;
using SuperMemoAssistant.Sys;

namespace SuperMemoAssistant.SuperMemo.SuperMemo17.Registry.Types
{
  [InitOnLoad]
  public class VideoRegistry : RegistryBase<Video, IVideo>, IVideoRegistry
  {
    #region Constants & Statics

    public static VideoRegistry Instance { get; } = new VideoRegistry();

    #endregion




    #region Properties & Fields - Non-Public

    protected override string MemFileName => SMConst.Files.VideoMemFileName;
    protected override string RtxFileName => SMConst.Files.VideoRtxFileName;
    protected override string RtfFileName => null;
    protected override IntPtr RegistryPtr => new IntPtr(SMNatives.TRegistry.VideoRegistryInstance.Read<int>(Svc.SM.Memory));
    protected override bool   IsOptional  => true;

    #endregion




    #region Constructors

    protected VideoRegistry() { }

    #endregion




    #region Methods Impl

    protected override Video Create(int id, RegMemElem mem, RegRtElem rtxOrRtf)
    {
      return new Video(id, mem, rtxOrRtf);
    }

    public Task<IVideo> AddAsync(string videoName, string videoPath)
    {
      throw new NotImplementedException();
    }

    #endregion
  }
}
