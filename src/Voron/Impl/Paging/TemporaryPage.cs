﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Sparrow;
using Voron.Global;
using Voron.Data.BTrees;

namespace Voron.Impl.Paging
{
    public unsafe class TemporaryPage : IDisposable
    {
        private readonly byte[] _tempPageBuffer;
        private GCHandle _tempPageHandle;
        private IntPtr _tempPage;
        internal readonly int PageSize;

        public TemporaryPage(StorageEnvironmentOptions options, int? pageSize = null)
        {
            PageSize = pageSize ?? Constants.Storage.PageSize;
            _tempPageBuffer = new byte[PageSize];
            _tempPageHandle = GCHandle.Alloc(_tempPageBuffer, GCHandleType.Pinned);
            Memory.RegisterVerification(_tempPageHandle.AddrOfPinnedObject(), new UIntPtr((uint)_tempPageBuffer.Length), "GcHandleAlloc");
            _tempPage = _tempPageHandle.AddrOfPinnedObject();
        }

        public TemporaryPage(byte* ptr, int pageSize)
        {
            PageSize = pageSize;
            SetPointer(ptr);
        }

        public void Dispose()
        {
            if (_tempPageHandle.IsAllocated)
            {
                Memory.UnregisterVerification(_tempPageHandle.AddrOfPinnedObject(), new UIntPtr((uint)_tempPageBuffer.Length), "GcHandleFree");
                _tempPageHandle.Free();
            }
        }

        public void SetPointer(byte* ptr)
        {
            _tempPage = new IntPtr(ptr);
        }

        public byte[] TempPageBuffer => _tempPageBuffer;

        public byte* TempPagePointer => (byte*)_tempPage.ToPointer();

        public TreePage GetTempPage()
        {
            var upper = (ushort)PageSize;

            if (upper < PageSize)
            {
                // we have overflown Upper which is ushort 
                // it means the page size is 64KB
                // we have special handling for this in AllocateNewNode

                Debug.Assert(PageSize == Constants.Compression.MaxPageSize);

                upper = ushort.MaxValue;
            }

            return new TreePage((byte*)_tempPage.ToPointer(), PageSize)
            {
                Upper = upper,
                Lower = (ushort) Constants.Tree.PageHeaderSize,
                TreeFlags = TreePageFlags.None,
            };
        }
        public IDisposable ReturnTemporaryPageToPool { get; set; }
    }
}
