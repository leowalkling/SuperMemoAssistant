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
// Created On:   2018/06/21 12:26
// Modified On:  2018/08/31 14:04
// Modified By:  Alexis

#endregion




using System;
using SuperMemoAssistant.Interop.SuperMemo.Content.Controls;
using SuperMemoAssistant.Interop.SuperMemo.Content.Models;
using SuperMemoAssistant.Interop.SuperMemo.Registry.Members;

namespace SuperMemoAssistant.SuperMemo.SuperMemo17.Content.Controls
{
  public abstract class ControlTextBased : ComponentControlBase, IControlText
  {
    #region Constructors

    /// <inheritdoc />
    protected ControlTextBased(int           id,
                               ComponentType type,
                               ControlGroup  group)
      : base(id,
             type,
             group) { }

    #endregion




    #region Properties Impl - Public

    public virtual string Text
    {
      get => _group.GetText(this);
      set => _group.SetText(this,
                            value);
    }

    public virtual IText TextMember
    {
      get => SMA.SMA.Instance.Registry.Text[TextMemberId];
      set => TextMemberId = value?.Id ?? throw new ArgumentNullException();
    }

    public virtual int TextMemberId
    {
      get => _group.GetTextRegMember(this);
      set => _group.SetTextRegMember(this,
                                     value);
    }

    #endregion
  }
}
