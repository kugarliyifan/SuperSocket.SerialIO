using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using SuperSocket.Channel;
using SuperSocket.ProtoBase;

namespace SuperSocket.SerialIO
{
    public class SerialIOPipeChannel<TPackageInfo> : PipeChannel<TPackageInfo>
        where TPackageInfo : class
    {
        private SerialPort _serialPort = null;

        public SerialIOPipeChannel(SerialPort serialPort, IPipelineFilter<TPackageInfo> pipelineFilter, ChannelOptions options)
            : base(pipelineFilter, options)
        {
            _serialPort = serialPort;
            StartTasks();
        }

        protected override void Close()
        {
            //throw new NotImplementedException();
        }

        protected override async ValueTask<int> FillPipeWithDataAsync(Memory<byte> memory, CancellationToken cancellationToken)
        {
            return await ReceiveAsync(memory, cancellationToken);
        }

        private async ValueTask<int> ReceiveAsync(Memory<byte> memory, CancellationToken cancellationToken)
        {
            return await _serialPort.BaseStream.ReadAsync(memory, cancellationToken);
        }

        protected override async ValueTask<int> SendOverIOAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if (buffer.IsSingleSegment)
            {
                var segment = GetArrayByMemory(buffer.First);

                _serialPort.Write(segment.Array, segment.Offset, segment.Count);

                return segment.Count;
            }

            var count = 0;

            foreach (var piece in buffer)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _serialPort.BaseStream.WriteAsync(piece, cancellationToken);

                count += piece.Length;
            }

            return count;
        }
    }
}
