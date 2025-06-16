using System.Buffers;

namespace Api.Transport;

public delegate Task MessageHandler(ReadOnlySequence<byte> message);