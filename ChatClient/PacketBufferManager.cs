using System;

namespace ChatClient2
{
    class PacketBufferManager
    {
        private int _bufferSize = 0;
        private int _readPos = 0;
        private int _writePos = 0;

        private int _headerSize = 0;
        private int _maxPacketSize = 0;
        private byte[] _packetData;
        private byte[] _packetDataTemp;

        public bool Init(int size, int headerSize, int maxPacketSize)
        {
            if( size < ( maxPacketSize * 2 ) || size < 1 || headerSize < 1 || maxPacketSize < 1 )
            {
                return false;
            }

            _bufferSize = size;
            _packetData = new byte[size];
            _packetDataTemp = new byte[size];
            _headerSize = headerSize;
            _maxPacketSize = maxPacketSize;

            return true;
        }

        public bool Write(byte[] data, int pos, int size)
        {
            if( data == null || ( data.Length < ( pos + size ) ) )
            {
                return false;
            }

            var remainBufferSize = _bufferSize - _writePos;

            if( remainBufferSize < size )
            {
                return false;
            }

            Buffer.BlockCopy(data, pos, _packetData, _writePos, size);
            _writePos += size;

            if( NextFree() == false )
            {
                BufferRelocate();
            }
            return true;
        }

        public ArraySegment<byte> Read()
        {
            var enableReadSize = _writePos - _readPos;

            if( enableReadSize < _headerSize )
            {
                return new ArraySegment<byte>();
            }

            var packetDataSize = BitConverter.ToInt16(_packetData, _readPos);
            if( enableReadSize < packetDataSize )
            {
                return new ArraySegment<byte>();
            }

            var completePacketData = new ArraySegment<byte>(_packetData, _readPos, packetDataSize);
            _readPos += packetDataSize;
            return completePacketData;
        }

        bool NextFree()
        {
            var enableWriteSize = _bufferSize - _writePos;

            if( enableWriteSize < _maxPacketSize )
            {
                return false;
            }

            return true;
        }

        void BufferRelocate()
        {
            var enableReadSize = _writePos - _readPos;

            Buffer.BlockCopy(_packetData, _readPos, _packetDataTemp, 0, enableReadSize);
            Buffer.BlockCopy(_packetDataTemp, 0, _packetData, 0, enableReadSize);

            _readPos = 0;
            _writePos = enableReadSize;
        }
    }
}
