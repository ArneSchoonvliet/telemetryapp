using rF2SMMonitor.rFactor2Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace TelemetryApp
{
    public class MappedDoubleBuffer<MappedBufferT> : IMappedDoubleBuffer<MappedBufferT>
    {
        readonly int RF2_BUFFER_HEADER_SIZE_BYTES = Marshal.SizeOf(typeof(rF2BufferHeader));
        readonly int RF2_BUFFER_HEADER_WITH_SIZE_SIZE_BYTES = Marshal.SizeOf(typeof(rF2MappedBufferHeaderWithSize));

        readonly int BUFFER_SIZE_BYTES;
        readonly string BUFFER1_NAME;
        readonly string BUFFER2_NAME;
        readonly string MUTEX_NAME;

        // Holds the entire byte array that can be marshalled to a MappedBufferT.  Partial updates
        // only read changed part of buffer, ignoring trailing uninteresting bytes.  However,
        // to marshal we still need to supply entire structure size.  So, on update new bytes are copied
        // (outside of the mutex).
        byte[] fullSizeBuffer = null;

        Mutex mutex = null;
        MemoryMappedFile memoryMappedFile1 = null;
        MemoryMappedFile memoryMappedFile2 = null;

        public MappedDoubleBuffer(string buff1Name, string buff2Name, string mutexName)
        {
            this.BUFFER_SIZE_BYTES = Marshal.SizeOf(typeof(MappedBufferT));
            this.BUFFER1_NAME = buff1Name;
            this.BUFFER2_NAME = buff2Name;
            this.MUTEX_NAME = mutexName;
        }

        public void Connect()
        {
            this.mutex = Mutex.OpenExisting(this.MUTEX_NAME);
            this.memoryMappedFile1 = MemoryMappedFile.OpenExisting(this.BUFFER1_NAME);
            this.memoryMappedFile2 = MemoryMappedFile.OpenExisting(this.BUFFER2_NAME);

            // NOTE: Make sure that BUFFER_SIZE matches the structure size in the plugin (debug mode prints that).
            this.fullSizeBuffer = new byte[this.BUFFER_SIZE_BYTES];
        }

        public void Disconnect()
        {
            if (this.memoryMappedFile1 != null)
                this.memoryMappedFile1.Dispose();

            if (this.memoryMappedFile2 != null)
                this.memoryMappedFile2.Dispose();

            if (this.mutex != null)
                this.mutex.Dispose();

            this.memoryMappedFile1 = null;
            this.memoryMappedFile2 = null;
            this.fullSizeBuffer = null;
            this.mutex = null;
        }

        public void GetMappedData(ref MappedBufferT mappedData)
        {
            //
            // IMPORTANT:  Clients that do not need consistency accross the whole buffer, like dashboards that visualize data, _do not_ need to use mutexes.
            //

            // Note: if it is critical for client minimize wait time, same strategy as plugin uses can be employed.
            // Pass 0 timeout and skip update if someone holds the lock.
            if (this.mutex.WaitOne(5000))
            {
                byte[] sharedMemoryReadBuffer = null;
                try
                {
                    bool buf1Current = false;
                    // Try buffer 1:
                    using (var sharedMemoryStreamView = this.memoryMappedFile1.CreateViewStream())
                    {
                        var sharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                        sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(this.RF2_BUFFER_HEADER_SIZE_BYTES);

                        // Marhsal header.
                        var headerHandle = GCHandle.Alloc(sharedMemoryReadBuffer, GCHandleType.Pinned);
                        var header = (rF2MappedBufferHeader)Marshal.PtrToStructure(headerHandle.AddrOfPinnedObject(), typeof(rF2MappedBufferHeader));
                        headerHandle.Free();

                        if (header.mCurrentRead == 1)
                        {
                            sharedMemoryStream.BaseStream.Position = 0;
                            sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(this.BUFFER_SIZE_BYTES);
                            buf1Current = true;
                        }
                    }

                    // Read buffer 2
                    if (!buf1Current)
                    {
                        using (var sharedMemoryStreamView = this.memoryMappedFile2.CreateViewStream())
                        {
                            var sharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                            sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(this.BUFFER_SIZE_BYTES);
                        }
                    }
                }
                finally
                {
                    this.mutex.ReleaseMutex();
                }

                // Marshal rF2 State buffer
                var handle = GCHandle.Alloc(sharedMemoryReadBuffer, GCHandleType.Pinned);

                mappedData = (MappedBufferT)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(MappedBufferT));

                handle.Free();
            }
        }

        public void GetMappedDataPartial(ref MappedBufferT mappedData)
        {
            //
            // IMPORTANT:  Clients that do not need consistency accross the whole buffer, like dashboards that visualize data, _do not_ need to use mutexes.
            //

            // Note: if it is critical for client minimize wait time, same strategy as plugin uses can be employed.
            // Pass 0 timeout and skip update if someone holds the lock.

            // Using partial buffer copying reduces time under lock.  Scoring by 30%, telemetry by 70%.
            if (this.mutex.WaitOne(5000))
            {
                byte[] sharedMemoryReadBuffer = null;
                try
                {
                    bool buf1Current = false;
                    // Try buffer 1:
                    using (var sharedMemoryStreamView = this.memoryMappedFile1.CreateViewStream())
                    {
                        var sharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                        sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(this.RF2_BUFFER_HEADER_WITH_SIZE_SIZE_BYTES);

                        // Marhsal header.
                        var headerHandle = GCHandle.Alloc(sharedMemoryReadBuffer, GCHandleType.Pinned);
                        var header = (rF2MappedBufferHeaderWithSize)Marshal.PtrToStructure(headerHandle.AddrOfPinnedObject(), typeof(rF2MappedBufferHeaderWithSize));
                        headerHandle.Free();

                        if (header.mCurrentRead == 1)
                        {
                            sharedMemoryStream.BaseStream.Position = 0;
                            sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(header.mBytesUpdatedHint != 0 ? header.mBytesUpdatedHint : this.BUFFER_SIZE_BYTES);
                            buf1Current = true;
                        }
                    }

                    // Read buffer 2
                    if (!buf1Current)
                    {
                        using (var sharedMemoryStreamView = this.memoryMappedFile2.CreateViewStream())
                        {
                            var sharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                            sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(this.RF2_BUFFER_HEADER_WITH_SIZE_SIZE_BYTES);

                            // Marhsal header.
                            var headerHandle = GCHandle.Alloc(sharedMemoryReadBuffer, GCHandleType.Pinned);
                            var header = (rF2MappedBufferHeaderWithSize)Marshal.PtrToStructure(headerHandle.AddrOfPinnedObject(), typeof(rF2MappedBufferHeaderWithSize));
                            headerHandle.Free();

                            sharedMemoryStream.BaseStream.Position = 0;
                            sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(header.mBytesUpdatedHint != 0 ? header.mBytesUpdatedHint : this.BUFFER_SIZE_BYTES);
                        }
                    }
                }
                finally
                {
                    this.mutex.ReleaseMutex();
                }

                Array.Copy(sharedMemoryReadBuffer, this.fullSizeBuffer, sharedMemoryReadBuffer.Length);

                // Marshal rF2 State buffer
                var handle = GCHandle.Alloc(this.fullSizeBuffer, GCHandleType.Pinned);

                mappedData = (MappedBufferT)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(MappedBufferT));

                handle.Free();
            }
        }
    }
    public interface IMappedDoubleBuffer<MappedBufferT>
    {
        void Connect();
        void Disconnect();
        void GetMappedData(ref MappedBufferT mappedData);
        void GetMappedDataPartial(ref MappedBufferT mappedData);

    }
}
